// Copyright (C) Tenacom and contributors. Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

#nullable enable

// ---------------------------------------------------------------------------------------------
// File system helpers
// ---------------------------------------------------------------------------------------------

/*
 * Summary : Delete a directory, including its contents, if it exists.
 * Params  : context   - The Cake context.
 *           directory - The directory to delete.
 */
static void DeleteDirectoryIfExists(this ICakeContext context, DirectoryPath directory)
{
    if (!context.DirectoryExists(directory))
    {
        context.Verbose($"Skipping non-existent directory: {directory}");
        return;
    }

    context.Information($"Deleting directory: {directory}");
    context.DeleteDirectory(directory, new() { Force = false, Recursive = true });
}
