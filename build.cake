// Copyright (C) Tenacom and contributors. Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

#load "./build/BuildData.cake"
#load "./build/Changelog.cake"
#load "./build/dotnet.cake"
#load "./build/environment.cake"
#load "./build/fail.cake"
#load "./build/filesystem.cake"
#load "./build/git.cake"
#load "./build/github.cake"
#load "./build/json.cake"
#load "./build/nbgv.cake"
#load "./build/options.cake"
#load "./build/process.cake"
#load "./build/public-api.cake"
#load "./build/setup-teardown.cake"
#load "./build/utilities.cake"
#load "./build/versioning.cake"
#load "./build/workspace.cake"

#nullable enable

using System;
using System.Text;

using SysDirectory = System.IO.Directory;
using SysFile = System.IO.File;
using SysPath = System.IO.Path;

// =============================================================================================
// TASKS
// =============================================================================================

Task("Default")
    .Description("Default task - Do nothing (but log build configuration data)")
    .Does(context => {
        context.Information("The default task does nothing. This is intentional.");
        context.Information("Use `dotnet cake --description` to see the list of available tasks.");
    });

Task("CleanAll")
    .Description("Delete all output directories, VS data, R# caches")
    .Does<BuildData>((context, data) => context.CleanAll(data));

Task("LocalCleanAll")
    .Description("Like CleanAll, but only runs on a local machine")
    .WithCriteria<BuildData>(data => !data.IsCI)
    .Does<BuildData>((context, data) => context.CleanAll(data));

Task("Restore")
    .Description("Restores dependencies")
    .IsDependentOn("LocalCleanAll")
    .Does<BuildData>((context, data) => context.RestoreSolution(data));

Task("Build")
    .Description("Build all projects")
    .IsDependentOn("Restore")
    .Does<BuildData>((context, data) => context.BuildSolution(data, false));

Task("Test")
    .Description("Build all projects and run tests")
    .IsDependentOn("Build")
    .Does<BuildData>((context, data) => context.TestSolution(data, false, false, true));

Task("Pack")
    .Description("Build all projects, run tests, and prepare build artifacts")
    .IsDependentOn("Test")
    .Does<BuildData>((context, data) => context.PackSolution(data, false, false));

Task("Release")
    .Description("Publish a new public release (CI only)")
    .Does<BuildData>(async (context, data) => {

        // Perform some preliminary checks
        context.Ensure(data.IsCI, "The Release target cannot run on a local system.");
        context.Ensure(data.IsPublicRelease, "Cannot create a release from the current branch.");

        // Perform an initial versioning consistency check.
        // This is a tad more relaxed than the final check, as it takes into account that we may still increment the current version
        // (for example by updating the changelog).
        context.CheckVersioningConsistency(
            currentVersion: data.Version,
            latestVersion: data.LatestVersion,
            latestStableVersion: data.LatestStableVersion,
            isFinalCheck: false);

        // Compute the version spec change to apply, if any.
        // This implies more checks and possibly throws, so do it as early as possible.
        var versionSpecChange = context.ComputeVersionSpecChange(
            currentVersion: data.Version,
            latestVersion: data.LatestVersion,
            latestStableVersion: data.LatestStableVersion,
            requestedChange: context.GetOption<VersionSpecChange>("versionSpecChange", VersionSpecChange.None),
            checkPublicApi: context.GetOption<bool>("checkPublicApi", true));

        // Identify Git user for later possible push
        context.GitSetUserIdentity("Buildvana", "buildvana@tenacom.it");

        // Create the release as a draft first, so if the token has no permissions we can bail out early
        var release = await context.CreateDraftReleaseAsync(data);
        var dupeTagChecked = false;
        var committed = false;
        try
        {
            // Modify version if required.
            if (versionSpecChange != VersionSpecChange.None)
            {
                var versionFile = VersionFile.Load(context);
                if (versionFile.ApplyVersionSpecChange(context, versionSpecChange))
                {
                    versionFile.Save();
                    UpdateRepo(versionFile.Path);
                }
            }

            // Update public API files only when releasing a stable version
            if (!data.IsPrerelease)
            {
                var modified = context.TransferAllPublicApiToShipped().ToArray();
                if (modified.Length > 0)
                {
                    context.Information($"{modified.Length} public API files were modified.");
                    UpdateRepo(modified);
                }
                else
                {
                    context.Information("No public API files were modified.");
                }
            }
            else
            {
                context.Information("Public API update skipped: not needed on prerelease.");
            }

            // Update changelog only on non-prerelease, unless forced
            var changelog = new Changelog(context, data);
            var changelogUpdated = false;
            if (!changelog.Exists)
            {
                context.Information($"Changelog update skipped: {Changelog.FileName} not found.");
            }
            else if (!data.IsPrerelease || context.GetOption<bool>("forceUpdateChangelog", false))
            {
                if (context.GetOption<bool>("checkChangelog", true))
                {
                    context.Ensure(
                        changelog.HasUnreleasedChanges(),
                        "Changelog check failed: the \"Unreleased changes\" section is empty or only contains sub-section headings.");

                    context.Information("Changelog check successful: the \"Unreleased changes\" section is not empty.");
                }
                else
                {
                    context.Information("Changelog check skipped: option 'checkChangelog' is false.");
                }

                // Update the changelog and commit the change before building.
                // This ensures that the Git height is up to date when computing a version for the build artifacts.
                changelog.PrepareForRelease();
                UpdateRepo(changelog.Path);
                changelogUpdated = true;
            }
            else
            {
                context.Information("Changelog update skipped: not needed on prerelease.");
            }

            // At this point we know what the actual published version will be.
            // Time for a final consistency check.
            context.CheckVersioningConsistency(
                currentVersion: data.Version,
                latestVersion: data.LatestVersion,
                latestStableVersion: data.LatestStableVersion,
                isFinalCheck: true);

            // Ensure that the release tag doesn't already exist.
            // This assumes that full repo history has been checked out;
            // however, that is already a prerequisite for using Nerdbank.GitVersioning.
            context.Ensure(!context.GitTagExists(data.VersionStr), $"Tag {data.VersionStr} already exists in repository.");
            dupeTagChecked = true;

            context.RestoreSolution(data);
            context.BuildSolution(data, false);
            context.TestSolution(data, false, false, false);
            context.PackSolution(data, false, false);

            if (changelogUpdated)
            {
                // Change the new section's title in the changelog to reflect the actual version.
                changelog.UpdateNewSectionTitle();
                UpdateRepo(changelog.Path);
            }
            else
            {
                context.Information("Changelog section title update skipped: changelog has not been updated.");
            }

            if (committed)
            {
                context.Information($"Git pushing changes to {data.Remote}...");
                _ = context.Exec("git", $"push {data.Remote} HEAD");
            }
            else
            {
                context.Information("Git push skipped: no commit to push.");
            }

            // Publish NuGet packages
            await context.NuGetPushAllAsync(data);

            // If this is not a prerelease and we are releasing from the main branch,
            // dispatch a separate workflow to publish documentation.
            // Unless, of course, there is no documentation to publish, or no workflow to do it.
            FilePath docFxJsonPath = "docs/docfx.json";
            FilePath pagesDeploymentWorkflow = ".github/workflows/deploy-pages.yml";
            if (data.IsPrerelease)
            {
                context.Information("Documentation update skipped: not needed on prerelease.");
            }
            else if (data.Branch != "main")
            {
                context.Information($"Documentation update skipped: releasing from '{data.Branch}', not 'main'.");
            }
            else if (!SysFile.Exists(pagesDeploymentWorkflow.FullPath))
            {
                context.Information($"Documentation update skipped: {docFxJsonPath} not present.");
            }
            else if (!SysFile.Exists(pagesDeploymentWorkflow.FullPath))
            {
                context.Warning($"Documentation update skipped: there is no documentation workflow.");
            }
            else
            {
                await context.DispatchWorkflow(data, SysPath.GetFileName(pagesDeploymentWorkflow.FullPath), "main");
            }

            // Read release asset lists and upload assets
            var assets = await GetReleaseAssetsAsync().ConfigureAwait(false);
            var assetCount = assets.Count;
            if (assetCount > 0)
            {
                var i = 0;
                foreach (var asset in assets)
                {
                    i++;
                    context.Information($"Uploading asset {i} of {assetCount}: {SysPath.GetFileName(asset.Path)} ({asset.Description})...");
                    await context.UploadReleaseAssetAsync(data, release, asset.Path, asset.MimeType, asset.Description).ConfigureAwait(false);
                }
            }
            else
            {
                context.Information("Asset upload skipped: no release assets defined.");
            }

            // Last but not least, publish the release.
            await context.PublishReleaseAsync(data, release);

            // Set outputs for subsequent steps in GitHub Actions
            if (data.IsGitHubAction)
            {
                context.SetActionsStepOutput("version", data.VersionStr);
            }
        }
        catch (Exception e)
        {
            context.Error(e is CakeException ? e.Message : $"{e.GetType().Name}: {e.Message}");
            await context.DeleteReleaseAsync(data, release, dupeTagChecked ? data.VersionStr : null);
            throw;
        }

        void UpdateRepo(params FilePath[] files)
        {
            foreach (var path in files)
            {
                context.Verbose($"Git adding {path}...");
                _ = context.Exec(
                    "git",
                    new ProcessArgumentBuilder()
                        .Append("add")
                        .AppendQuoted(path.FullPath));
            }

            context.Information(committed ? "Amending commit..." : "Committing changed files...");
            var arguments = new ProcessArgumentBuilder().Append("commit");
            if (committed)
            {
                arguments = arguments.Append("--amend");
            }

            arguments = arguments.Append("-m").AppendQuoted("Prepare release [skip ci]");
            _ = context.Exec("git", arguments);

            // The commit changed the Git height, so update build data
            // and amend the commit adding the right version.
            // Amending a commit does not further change the Git height.
            data.Update(context);
            _ = context.Exec(
                "git",
                new ProcessArgumentBuilder()
                    .Append("commit")
                    .Append("--amend")
                    .Append("-m")
                    .AppendQuoted($"Prepare release {data.VersionStr} [skip ci]"));

            committed = true;
        }
        
        async Task<IReadOnlyList<(string Path, string MimeType, string Description)>> GetReleaseAssetsAsync()
        {
            const string assetListMask = "*.assets.txt";
            
            var result = new List<(string Path, string MimeType, string Description)>();
            if (!SysDirectory.EnumerateFiles(data.ArtifactsPath.FullPath, assetListMask).Any())
            {
                context.Information("Skipping asset upload: no release asset lists.");
                return result;
            }

            context.Information("Reading release asset lists...");
            var assetLists = SysPath.Combine(data.ArtifactsPath.FullPath, assetListMask);
            foreach (var path in context.GetFiles(assetLists).Select(x => x.FullPath))
            {
                context.Verbose("Reading release asset list {path}...");
                var i = 0;
                await foreach (var line in SysFile.ReadLinesAsync(path))
                {
                    i++;
                    var parts = line.Split('\t');
                    if (parts.Length != 3)
                    {
                        context.Warning($"Release asset list {path}, line #{i}: invalid line '{line}'");
                        continue;
                    }
                    
                    if (!SysFile.Exists(parts[0]))
                    {
                        context.Warning($"Release asset list {path}, line #{i}: asset not found '{parts[0]}'");
                        continue;
                    }

                    result.Add((parts[0], parts[1], parts[2]));
                }
            }

            return result;
        }
    });

// =============================================================================================
// EXECUTION
// =============================================================================================

RunTarget(Argument("target", "Default"));
