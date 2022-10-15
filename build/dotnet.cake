// Copyright (C) Tenacom and contributors. Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

#nullable enable

// ---------------------------------------------------------------------------------------------
// .NET SDK helpers
// ---------------------------------------------------------------------------------------------

using System.IO;
using System.Linq;

using SysDirectory = System.IO.Directory;
using SysPath = System.IO.Path;

/*
 * Summary : Restore all NuGet packages for the solution.
 * Params  : context - The Cake context.
 *           data    - Build configuration data.
 */
static void RestoreSolution(this ICakeContext context, BuildData data)
{
    context.Information("Restoring NuGet packages for solution...");
    context.DotNetRestore(data.SolutionPath.FullPath, new() {
        DisableParallel = true,
        Interactive = false,
        MSBuildSettings = data.MSBuildSettings,
    });
}

/*
 * Summary : Build all projects in teh solution.
 * Params  : context - The Cake context.
 *           data    - Build configuration data.
 *           restore - true to restore NuGet packages before building, false otherwise.
 */
static void BuildSolution(this ICakeContext context, BuildData data, bool restore)
{
    context.Information($"Building solution (restore = {restore})...");
    context.DotNetBuild(data.SolutionPath.FullPath, new() {
        Configuration = data.Configuration,
        MSBuildSettings = data.MSBuildSettings,
        NoLogo = true,
        NoRestore = !restore,
    });
}

/*
 * Summary : Run all unit tests for the solution.
 * Params  : context - The Cake context.
 *           data    - Build configuration data.
 *           restore - true to restore NuGet packages before testing, false otherwise.
 *           build   - true to build the solution before testing, false otherwise.
 */
static void TestSolution(this ICakeContext context, BuildData data, bool restore, bool build)
{
    context.Information($"Running tests (restore = {restore}, build = {build})...");
    context.DotNetTest(data.SolutionPath.FullPath, new() {
        Configuration = data.Configuration,
        NoBuild = !build,
        NoLogo = true,
        NoRestore = !restore,
    });
}

/*
 * Summary : Run the Pack target on the solution. This usually produces NuGet packages,
 *           but Buildvana SDK may hijack the target to produce, for example, setup executables.
 * Params  : context - The Cake context.
 *           data    - Build configuration data.
 *           restore - true to restore NuGet packages before packing, false otherwise.
 *           build   - true to build the solution before packing, false otherwise.
 */
static void PackSolution(this ICakeContext context, BuildData data, bool restore, bool build)
{
    context.Information($"Packing solution (restore = {restore}, build = {build})...");
    context.DotNetPack(data.SolutionPath.FullPath, new() {
        Configuration = data.Configuration,
        MSBuildSettings = data.MSBuildSettings,
        NoBuild = !build,
        NoLogo = true,
        NoRestore = !restore,
    });
}

/*
 * Summary : Push all produced NuGet packages to the appropriate NuGet server.
 * Params  : context - The Cake context.
 *           data    - Build configuration data.
 * Remarks : - This method uses the following environment variables:
 *             * PRERELEASE_NUGET_SOURCE - NuGet source URL where to push prerelease packages
 *             * RELEASE_NUGET_SOURCE    - NuGet source URL where to push non-prerelease packages
 *             * PRERELEASE_NUGET_KEY    - API key for PRERELEASE_NUGET_SOURCE
 *             * RELEASE_NUGET_KEY       - API key for RELEASE_NUGET_SOURCE
 *           - If there are no .nupkg files in the designated artifacts directory, this method does nothing.
 */
static void NuGetPushAll(this ICakeContext context, BuildData data)
{
    const string nupkgMask = "*.nupkg";
    if (!SysDirectory.EnumerateFiles(data.ArtifactsPath.FullPath, nupkgMask).Any())
    {
        context.Verbose("No .nupkg files to push.");
        return;
    }

    var nugetSource = context.GetOptionOrFail<string>(data.IsPrerelease ? "prereleaseNugetSource" : "releaseNugetSource");
    var nugetApiKey = context.GetOptionOrFail<string>(data.IsPrerelease ? "prereleaseNugetKey" : "releaseNugetKey");
    var nugetPushSettings = new DotNetNuGetPushSettings {
        ForceEnglishOutput = true,
        Source = nugetSource,
        ApiKey = nugetApiKey,
        SkipDuplicate = true,
    };

    var packages = SysPath.Combine(data.ArtifactsPath.FullPath, nupkgMask);
    foreach (var path in context.GetFiles(packages))
    {
        context.Information($"Pushing {path} to {nugetSource}...");
        context.DotNetNuGetPush(path, nugetPushSettings);
    }
}
