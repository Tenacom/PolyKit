// Copyright (C) Tenacom and contributors. Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

#load "./build/BuildData.cake"
#load "./build/changelog.cake"
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
#load "./build/versioning.cake"
#load "./build/workspace.cake"

#nullable enable

using System;
using System.Text;

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
        Ensure(data.IsCI, "The Release target cannot run on a local system.");
        Ensure(data.IsPublicRelease, "Cannot create a release from the current branch.");

        // Compute the version spec change to apply, if any
        // This implies more checks and possibly throws, so do it as early as possible
        var versionSpecChange = context.ComputeVersionSpecChange(
            currentVersion: data.Version,
            requestedChange: context.GetOption<VersionSpecChange>("versionSpecChange", VersionSpecChange.None),
            checkPublicApi: context.GetOption<bool>("checkPublicApi", true));

        // Identify Git user for later push if needed
        context.GitSetUserIdentity("Buildvana", "buildvana@tenacom.it");

        // Create the release as a draft first, so if the token has no permissions we can bail out early
        var releaseId = await context.CreateDraftReleaseAsync(data);
        var dupeTagChecked = false;
        var committed = false;
        try
        {
            // Modify version if required.
            if (versionSpecChange != VersionSpecChange.None)
            {
                var versionFile = VersionFile.Load();
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
            var changelogUpdated = false;
            if (!data.IsPrerelease || context.GetOption<bool>("forceUpdateChangelog", false))
            {
                if (context.GetOption<bool>("checkChangelog", true))
                {
                    Ensure(
                        context.ChangelogHasUnreleasedChanges(data.ChangelogPath),
                        $"Changelog check failed: the \"Unreleased changes\" section is empty or only contains sub-section headings.");

                    context.Information($"Changelog check successful: the \"Unreleased changes\" section is not empty.");
                }
                else
                {
                    context.Information($"Changelog check skipped: option 'checkChangelog' is false.");
                }

                // Update the changelog and commit the change before building.
                // This ensures that the Git height is up to date when computing a version for the build artifacts.
                context.PrepareChangelogForRelease(data);
                UpdateRepo(data.ChangelogPath);
                changelogUpdated = true;
            }
            else
            {
                context.Information("Changelog update skipped: not needed on prerelease.");
            }

            // Ensure that the release tag doesn't already exist.
            // This assumes that full repo history has been checked out;
            // however, that is already a prerequisite for using Nerdbank.GitVersioning.
            Ensure(!context.GitTagExists(data.VersionStr), $"Tag {data.VersionStr} already exists in repository.");
            dupeTagChecked = true;

            context.RestoreSolution(data);
            context.BuildSolution(data, false);
            context.TestSolution(data, false, false, false);
            context.PackSolution(data, false, false);

            if (changelogUpdated)
            {
                // Change the new section's title in the changelog to reflect the actual version.
                context.UpdateChangelogNewSectionTitle(data);
                UpdateRepo(data.ChangelogPath);
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

            // Last but not least, publish the release.
            await context.PublishReleaseAsync(data, releaseId);

            // Set outputs for subsequent steps in GitHub Actions
            if (data.IsGitHubAction)
            {
                context.SetActionsStepOutput("version", data.VersionStr);
            }
        }
        catch (Exception e)
        {
            context.Error(e is CakeException ? e.Message : $"{e.GetType().Name}: {e.Message}");
            await context.DeleteReleaseAsync(data, releaseId, dupeTagChecked ? data.VersionStr : null);
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
    });

// =============================================================================================
// EXECUTION
// =============================================================================================

RunTarget(Argument("target", "Default"));
