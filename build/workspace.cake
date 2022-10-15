// Copyright (C) Tenacom and contributors. Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

#nullable enable

// ---------------------------------------------------------------------------------------------
// Workspace helpers
// ---------------------------------------------------------------------------------------------

/*
 * Summary : Delete all intermediate and output directories.
 *           On a local machine, also delete Visual Studio and ReSharper caches.
 * Params  : context - The Cake context.
 */
static void CleanAll(this ICakeContext context, BuildData data)
{
    context.DeleteDirectoryIfExists(".vs");
    context.DeleteDirectoryIfExists("_ReSharper.Caches");
    context.DeleteDirectoryIfExists("artifacts");
    context.DeleteDirectoryIfExists("logs");
    foreach (var project in data.Solution.Projects)
    {
        var projectDirectory = project.Path.GetDirectory();
        context.DeleteDirectoryIfExists(projectDirectory.Combine("bin"));
        context.DeleteDirectoryIfExists(projectDirectory.Combine("obj"));
    }
}
