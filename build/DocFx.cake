// Copyright (C) Tenacom and contributors. Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

#tool nuget:?package=docfx.console&version=2.59.4

#nullable enable

// ---------------------------------------------------------------------------------------------
// DocFx class
// ---------------------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

/*
 * Summary : Implements DocFx operations
 */
sealed class DocFx
{
    private const string ToolExeName = "docfx.exe";

    private readonly DirectoryPath _docsPath;
    private FilePath? _docFxPath;

    /*
     * Summary : Initializes a new instance of the DocFx class.
     * Params  : context - The Cake context.
     *           docsPath - The path to the folder where DocFX operates.
     */
    public DocFx(ICakeContext context, BuildData buildData, DirectoryPath docsPath)
    {
        Context = context;
        BuildData = buildData;
        _docsPath = docsPath;
    }

    private ICakeContext Context { get; }

    private BuildData BuildData { get; }

    /*
     * Summary : Extracts language metadata according to docfx.json settings.
     */
    public void Metadata()
    {
        var docFxJsonPath = _docsPath.CombineWithFilePath("docfx.json");
        var json = LoadJsonObject(docFxJsonPath);
        if (!json.TryGetPropertyValue("metadata", out _))
        {
            Context.Information("No metadata to generate.");
            return;
        }

        Context.Information("Running DocFx...");
        Run("metadata");
    }

    /*
     * Summary : Generates documentation according to docfx.json settings.
     */
    public void Build()
    {
        Context.Information("Running DocFx...");
        Run("build");
    }

    /*
     * Summary : Hosts the built documentation web site.
     */
    public void Serve()
    {
        if (BuildData.IsCI)
        {
            Context.Information("DocFX web server not suitable for cloud builds, skipping.");
            return;
        }

        Context.Information("Starting DocFX web server...");
        var (_, process) = Start("serve _site");
        Console.WriteLine("Press any key to stop serving...");
        _ = WaitForKey();
        Context.Information("Stopping DocFX web server...");
        process.Kill();
        process.WaitForExit();
    }

    private static ConsoleKeyInfo WaitForKey()
    {
        while (Console.KeyAvailable)
        {
            _ = Console.ReadKey(true);
        }

        return Console.ReadKey(true);
    }

    private void Run(ProcessArgumentBuilder arguments)
    {
        var (commandName, process) = Start(arguments);
        process.WaitForExit();
        var exitCode = process.GetExitCode();
        Context.Ensure(exitCode == 0, $"{commandName} exited with code {exitCode}.");
    }

    private (string commandName, IProcess Process) Start(ProcessArgumentBuilder arguments)
    {
        _docFxPath ??= Context.Tools.Resolve(ToolExeName);
        Context.Ensure(_docFxPath != null, $"Cannot find {ToolExeName}");
        FilePath command = _docFxPath;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            command = "mono";
            arguments = arguments.PrependQuoted(_docFxPath.FullPath);
        }

        var process = Context.StartAndReturnProcess(command, new ProcessSettings()
        {
            Arguments = arguments,
            WorkingDirectory = _docsPath,
        });

        return (command.GetFilenameWithoutExtension().ToString(), process);
    }
}
