# ![PolyKit - C# polyfills, your way](graphics/Readme.png)

[![License](https://img.shields.io/github/license/Buildvana/PolyKit.svg)](https://github.com/Buildvana/PolyKit/blob/main/LICENSE)
[![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/v/release/Buildvana/PolyKit?include_prereleases)](https://github.com/Buildvana/PolyKit/releases)
[![Changelog](https://img.shields.io/badge/changelog-Keep%20a%20Changelog%20v1.0.0-%23E05735)](https://github.com/Buildvana/PolyKit/blob/main/CHANGELOG.md)

[![CodeFactor](https://www.codefactor.io/repository/github/Buildvana/PolyKit/badge)](https://www.codefactor.io/repository/github/Buildvana/PolyKit)
[![Last commit](https://img.shields.io/github/last-commit/Buildvana/PolyKit.svg)](https://github.com/Buildvana/PolyKit/commits/main)
[![Open issues](https://img.shields.io/github/issues-raw/Buildvana/PolyKit.svg?label=open+issues)](https://github.com/Buildvana/PolyKit/issues?q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc)
[![Closed issues](https://img.shields.io/github/issues-closed-raw/Buildvana/PolyKit.svg?label=closed+issues)](https://github.com/Buildvana/PolyKit/issues?q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc)

---

<!--TOC-->
  - [Read this first](#read-this-first)
  - [What is PolyKit](#what-is-polykit)
    - [Compatibility](#compatibility)
      - [Target frameworks](#target-frameworks)
      - [Build toolchain](#build-toolchain)
      - [Compiler](#compiler)
      - [Analyzers](#analyzers)
      - [Code coverage tools](#code-coverage-tools)
    - [Features](#features)
  - [Quick start](#quick-start)
    - [How to use shared polyfills across multiple projects](#how-to-use-shared-polyfills-across-multiple-projects)
    - [How to add polyfills to a stand-alone project (simple application, source generator)](#how-to-add-polyfills-to-a-stand-alone-project-simple-application-source-generator)
    - [How to create your own shared polyfill library](#how-to-create-your-own-shared-polyfill-library)
  - [Contributing](#contributing)
  - [Acknowledgements](#acknowledgements)
    - [Third-party projects](#third-party-projects)
<!--/TOC-->

---

## Read this first

I won't insult your intelligence by explaining what a polyfill, a package reference, or a MSBuild property is. We both know you have a browser and aren't afraid to use it. :wink:

Throughout this document, I'll assume that you know what follows:

- what is a polyfill and when you need polyfills;
- how to create a C# project;
- the meaning of `TargetFramework` (or `TargetFrameworks`) in the context of a `.csproj` file;
- the meaning of `PackageReference` in the context of a `.csproj` file;
- the meaning of `PrivateAssets="all"` in a `PackageReference`;
- how to make minor modifications to a project file, for example adding a property or an item.

If you're yawning, great, read on. If you're panicking, either do your ~~googling~~ due diligence and come back. I'll wait, no problem.

## What is PolyKit

PolyKit is both a run-time library (provided via the [`PolyKit`](https://nuget.org/packages/PolyKit) NuGet package) and a set of ready-to-compile source files (provided via the [`PolyKit.Embedded`](https://nuget.org/packages/PolyKit.Embedded) NuGet package) that add support for latest C# features as well as recent additions to the .NET runtime library, even in projects targeting .NET Framework or .NET Standard 2.0.

How you use PolyKit depends on your project:

- for a single-project application or library, such as a simple console program or a source generator DLL, `internal` polyfills provided by the `PolyKit.Embedded` package will suit just fine;
- for a multi-project solution, with some application and several shared libraries, you may want to avoid code duplication by using the `public` polyfills provided by the `PolyKit` package;
- if your solution contains a "core" library, referenced by all other projects, you may even spare an additional dependency by incorporating `public` polyfills _in your own library_.

### Compatibility

#### Target frameworks

Polyfills provided by `PolyKit` and `PolyKit.Embedded` are compatible with all flavors of .NET supported by Microsoft at the time of publishing, as well as all corresponding versions of .NET Standard:

- .NET Framework 4.6.2 or greater;
- .NET Core 3.1;
- .NET 6 or greater;
- .NET Standard 2.0 or greater.

#### Build toolchain

A minimum of Visual Studio / Build Tools 2022 or .NET SDK 6.0 is required to compile the polyfills provided by `PolyKit.Embedded`.

#### Compiler

C# language version 10.0 or greater is required to compile the polyfills provided by `PolyKit.Embedded`.

It is recommended to set the `LangVersion` proprty to `latest` in projects that reference `PolyKit.Embedded`.

#### Analyzers

Hell is other people's code, right? By referencing `PolyKit.Embedded` you are inviting our code into your project, that may well have very different code style settings. Are you thus condemned to see dozens of warnings pollute your Error List window and/or your build log forever?

Of course not! All code provided by `PolyKit.Embedded` is marked as being "auto-generated", so that analyzers won't even glance in its direction.

#### Code coverage tools

Besides bearing the "auto-generated" mark, every source file added to your project by `PolyKit.Embedded` has a name ending in `.g` (e.g. `Index.g.cs`) so that it can be automatically excluded from code coverage.

Furthermore, all classes and structs provided by `PolyKit.Embedded` have an `ExcludeFromCodeCoverage` attribute.

All this ensures that using `PolyKit.Embedded` will have zero impact on your coverage measurements.

### Features

PolyKit provides support for the following features across all [compatible target frameworks](#target-frameworks):

- [nullable reference types](https://docs.microsoft.com/en-us/dotnet/csharp/nullable-references), including null state analysis;
- [indices and ranges](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-8#indices-and-ranges) (see note #1);
- [`init` accessor on properties and indexers](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/init);
- [caller argument expressions](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/caller-information#argument-expressions);
- [System.HashCode](https://docs.microsoft.com/en-us/dotnet/api/system.hashcode) (see note #2);
- [SkipLocalsInit](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/general#skiplocalsinit-attribute);
- [ValidatedNotNull](https://docs.microsoft.com/en-us/dotnet/api/microsoft.validatednotnullattribute) (see note #3);
- [StackTraceHidden](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.stacktracehiddenattribute) (see note #4).

**Note #1:** This feature depends on `System.ValueTuple`, which is not present in .NET Framework versions prior to 4.7. If you reference the `PolyKit.Embedded` package in a project targeting .NET Framework 4.6.2, you must also add a package reference to [`System.ValueTuple`](https://www.nuget.org/packages/System.ValueTuple); otherwise, compilation will not fail, but features dependent on ValueTuple will not be present in the compiled assembly.

**Note #2:** In projects referencing `PolyKit.Embedded` and targeting .NET Framework or .NET Standard 2.0, method `HashCode.AddBytes(ReadOnlySpan<byte>)` will only be compiled if package [`System.Memory`](https://www.nuget.org/packages/System.Memory) is also referenced.

**Note #3:** This is not, strictly speaking, a polyfill, but it spares you the trouble of defining your own internal `ValidatedNotNullAttribute` or referencing Visual Studio SDK. The attribute provided by PolyKit is in the `PolyKit.Diagnostics.CodeAnalysis` namespace.

**Note #4:** Polyfilling `StackTraceHiddenAttribute` would be worthless without providing actual support for it.
PolyKit cannot replace relevant code (`Exception.StackTrace` getter and `StackTrace.ToString()` method) in frameworks prior to .NET 6.0, but it provides the two extension methods `Exception.GetStackTraceHidingFrames()` and `StackTrace.ToStringHidingFrames()` that retrofit the behavior of `Exception.StackTrace` and `StackTrace.ToString()` respectively to frameworks prior to .NET 6, where `StackTraceHiddenAttribute` was first introduced, while being simply facades for the "real" code in .NET 6.0+.
Be aware, though, of the following limitations: the output of the "retrofitted" extension methods is always in US English, regardless of any locale setting; external exception stack trace boundaries ("End of stack trace from previous location" lines) are missing from returned strings.

## Quick start

### How to use shared polyfills across multiple projects

- Ensure that all your target frameworks are [supported](#target-frameworks) by PolyKit.
- Add a package reference to [`PolyKit`](https://nuget.org/packages/PolyKit) to your projects.

### How to add polyfills to a stand-alone project (simple application, source generator)

- Ensure that all your target frameworks are [supported](#target-frameworks) by PolyKit.
- Set the `LangVersion` property in your project to `Latest`, `Preview`, or at least 10.
- Add a package reference to [`PolyKit.Embedded`](https://nuget.org/packages/PolyKit.Embedded) to your project.  
Remember to set `PrivateAssets="all"` if you add the package reference manually.
- Add optional package references as needed (see notes #1 and #2 [above](#features)).

### How to create your own shared polyfill library

- Ensure that all your target frameworks are [supported](#target-frameworks) by PolyKit.
- Set the `LangVersion` property in your project to `Latest`, `Preview`, or at least 10.
- Add a package reference to [`PolyKit.Embedded`](https://nuget.org/packages/PolyKit.Embedded) to your library project.  
Remember to set `PrivateAssets="all"` if you add the package reference manually.
- Add optional package references as needed (see notes #1 and #2 [above](#features)).
- Set the `PolyKit_GeneratePublicTypes` property to `true` in your project file.
- Add your own code to the library.
- You can now use your own library instead of `PolyKit` in your projects.

## Contributing

_**TODO**_

## Acknowledgements

### Third-party projects

You can find proper attributions to projects I "stole" code from in the [third-party notice file](./THIRD-PARTY-NOTICES). Here I want to mention projects that have provided, and keep providing, experience and inspiration.

- [EmbedIO](https://github.com/unosquare/embedio) has been my first experience as a collaborator to an open-source project; working on it has taught me a lot. Special thanks to @geoperez for dealing with me while I moved my first steps.
- [.NET Community Toolkit](https://github.com/CommunityToolkit/dotnet) is not just an invaluable tool, but a great source of inspiration too. The idea for the `Fail` class came from their own [ThrowHelper](https://docs.microsoft.com/en-us/windows/communitytoolkit/developer-tools/throwhelper), although the implementation approach is different.
