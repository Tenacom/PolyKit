// Copyright (C) Tenacom and contributors. Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

#nullable enable

// =============================================================================================
// Setup and Teardown, common to all scripts
// =============================================================================================

Setup<BuildData>(context =>
{
    var data = new BuildData(context);
    if (data.IsCI && !data.IsGitHubAction)
    {
        throw new CakeException(255, "This script can only run locally or in a GitHub Actions workflow.");
    }

    return data;
});

Teardown<BuildData>((context, data) =>
{
    // For some reason, DotNetBuildServerShutdown hangs in a GitHub Actions runner;
    // it is still useful on a local machine though
    if (!data.IsCI)
    {
        context.DotNetBuildServerShutdown(new DotNetBuildServerShutdownSettings
        {
            Razor = true,
            VBCSCompiler = true,
        });
    }
});
