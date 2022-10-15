// Copyright (C) Tenacom and contributors. Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

#load "./build/BuildData.cake"
#load "./build/DocFx.cake"
#load "./build/environment.cake"
#load "./build/fail.cake"
#load "./build/filesystem.cake"
#load "./build/git.cake"
#load "./build/github.cake"
#load "./build/json.cake"
#load "./build/nbgv.cake"
#load "./build/options.cake"
#load "./build/process.cake"
#load "./build/setup-teardown.cake"
#load "./build/version.cake"
#load "./build/workspace.cake"

#nullable enable

using System.Collections.Generic;
using System.Text.Json;

using SysFile = System.IO.File;

// =============================================================================================
// TASKS
// =============================================================================================

DocFx _docfx = null!;

Task("Default")
    .Description("Default task - Do nothing (but log build configuration data)")
    .Does(context => {
        context.Information("The default task does nothing. This is intentional.");
        context.Information("Use `dotnet cake --description` to see the list of available tasks.");
    });

Task("_init")
    .Description("Initialize DocFx support in script")
    .Does<BuildData>((context, data) => {
        _docfx = new DocFx(context, data, "docs");
        var globalMetadata= new
        {
            RepoOwner = data.RepositoryOwner,
            RepoName = data.RepositoryName,
            RepoUrl = $"{data.RepositoryHostUrl}/{data.RepositoryOwner}/{data.RepositoryName}",
            RepoVersion = data.Version,
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var jsonPath = new DirectoryPath("docs").CombineWithFilePath("globalMetadata.json");
        using var stream = SysFile.Create(jsonPath.FullPath);
        JsonSerializer.Serialize(stream, globalMetadata, options);
    });

Task("Metadata")
    .Description("Generate documentation metadata from sources")
    .IsDependentOn("_init")
    .Does<BuildData>(_ => _docfx.Metadata());

Task("Build")
    .Description("Build documentation from metadata")
    .IsDependentOn("_init")
    .Does<BuildData>(_ => _docfx.Build());

Task("Serve")
    .Description("Host documentation web site (only on local machine)")
    .WithCriteria<BuildData>(data => !data.IsCI)
    .IsDependentOn("_init")
    .Does<BuildData>(_ => _docfx.Serve());

Task("All")
    .Description("Generate (on local machine, also host) documentation from sources")
    .IsDependentOn("_init")
    .IsDependentOn("Metadata")
    .IsDependentOn("Build")
    .IsDependentOn("Serve");

// =============================================================================================
// EXECUTION
// =============================================================================================

RunTarget(Argument("target", "Default"));
