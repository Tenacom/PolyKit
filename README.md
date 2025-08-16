# ![PolyKit - C# polyfills, your way](graphics/Readme.png)

[![License](https://badgen.net/badge/license/MIT/blue)](https://github.com/Tenacom/PolyKit/blob/main/LICENSE)
[![Latest release](https://badgen.net/github/release/Tenacom/PolyKit?label=latest)](https://github.com/Tenacom/PolyKit/releases)
[![Latest stable release](https://badgen.net/github/release/Tenacom/PolyKit/stable?label=stable)](https://github.com/Tenacom/PolyKit/releases)
[![Changelog](https://badgen.net/badge/changelog/Keep%20a%20Changelog%20v1.0.0/orange)](https://github.com/Tenacom/PolyKit/blob/main/CHANGELOG.md)

[![Build, test, and pack](https://github.com/Tenacom/PolyKit/actions/workflows/build-test-pack.yml/badge.svg)](https://github.com/Tenacom/PolyKit/actions/workflows/build-test-pack.yml)
[![CodeQL](https://github.com/Tenacom/PolyKit/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/Tenacom/PolyKit/actions/workflows/codeql-analysis.yml)

![Repobeats analytics image](https://repobeats.axiom.co/api/embed/cd9c861045a0253b9f3eaaa32dc5b247ad56e562.svg "Repobeats analytics image")

| Latest packages | NuGet | MyGet |
|-----------------|-------|-------|
| PolyKit | [![PolyKit @ NuGet](https://badgen.net/nuget/v/PolyKit?icon=nuget&label=)](https://nuget.org/packages/PolyKit) | [![PolyKit @ MyGet](https://img.shields.io/myget/tenacom-preview/vpre/PolyKit?label=&color=orange)](https://www.myget.org/feed/tenacom-preview/package/nuget/PolyKit) |
| PolyKit.Embedded | [![PolyKit.Embedded @ NuGet](https://badgen.net/nuget/v/PolyKit.Embedded?icon=nuget&label=)](https://nuget.org/packages/PolyKit.Embedded) | [![PolyKit.Embedded @ MyGet](https://img.shields.io/myget/tenacom-preview/vpre/PolyKit.Embedded?label=&color=orange)](https://www.myget.org/feed/tenacom-preview/package/nuget/PolyKit.Embedded) |

---

- [Read this first](#read-this-first)
- [What is PolyKit](#what-is-polykit)
  - [How it works](#how-it-works)
  - [Compatibility](#compatibility)
    - [.NET SDK](#net-sdk)
    - [Target frameworks](#target-frameworks)
    - [Build toolchain](#build-toolchain)
    - [Language version](#language-version)
    - [Analyzers, code coverage, and other tools](#analyzers-code-coverage-and-other-tools)
- [Features](#features)
- [Quick start](#quick-start)
  - [How to use shared polyfills across multiple projects](#how-to-use-shared-polyfills-across-multiple-projects)
  - [How to add polyfills to a stand-alone project (simple application, source generator)](#how-to-add-polyfills-to-a-stand-alone-project-simple-application-source-generator)
  - [How to create your own shared polyfill library](#how-to-create-your-own-shared-polyfill-library)
- [Contributing](#contributing)
- [Contributors](#contributors)

---

## Read this first

Hi there! I'm Riccardo a.k.a. [@rdeago](https://github.com/rdeago), founder of [Tenacom](https://github.com/Tenacom) and author of PolyKit.

I won't insult your intelligence by explaining what a polyfill, a package reference, or a MSBuild property is. If these are new concepts to you, well... we both know you have a browser and aren't afraid to use it. :wink:

Throughout this document, I'll assume that you know what follows:

- what is a polyfill and when you need polyfills;
- how to create a C# project;
- the meaning of `TargetFramework` (or `TargetFrameworks`) in the context of a `.csproj` file;
- the meaning of `PackageReference` in the context of a `.csproj` file;
- the meaning of `PrivateAssets="all"` in a `PackageReference`;
- how to make minor modifications to a project file, for example adding a property or an item.

If you're yawning, great, read on. If you're panicking, do your ~~googling~~ due diligence and come back. I'll wait, no problem.

## What is PolyKit

PolyKit is both a run-time library (provided via the [`PolyKit`](https://nuget.org/packages/PolyKit) NuGet package) and a set of ready-to-compile source files (provided via the [`PolyKit.Embedded`](https://nuget.org/packages/PolyKit.Embedded) NuGet package) that add support for latest C# features as well as recent additions to the .NET runtime library, even in projects targeting .NET Framework or .NET Standard 2.0.

How you use PolyKit depends on your project:

- for a single-project application or library, such as a simple console program or a source generator DLL, `internal` polyfills provided by the `PolyKit.Embedded` package will suit just fine;
- for a library whose public-facing APIs may require polyfills, for example if some `public` method accepts or returns `Span`s, `public` polyfills provided by the `PolyKit` package will polyfill dependent applications where necessary, and get out of the way on platforms that do not require them;
- for a multi-project solution, with some application and several shared libraries, you may want to avoid code duplication by using the `public` polyfills provided by the `PolyKit` package;
- if your solution contains a "core" library, referenced by all other projects, you may even spare an additional dependency by incorporating `public` polyfills _in your own library_.

### How it works

PolyKit employs the same technique used by the .NET Standard library to expose `public` types on older frameworks without creating conflicts on newer ones: types that need no polyfilling (for example the `StringSyntax` attribute on .NET 5+) are [forwarded](https://learn.microsoft.com/en-us/dotnet/standard/assembly/type-forwarding) to their .NET runtime implementation.

This way you can safely reference `PolyKit` and use, even expose, polyfilled types in a .NET Standard 2.0 library, because the actual type used will depend upon the target framework of each application.

The same goes for `public` polyfills created by `PolyKit.Embedded`. In this case, however, to ensure that no type conflicts happen at runtime, you should multi-target according to the application target frameworks you want to support. For example, if your library targets .NET Standard 2.0 _only_, a .NET 6.0 application will "see" two identical `StringSyntax` types: one in the .NET runtime and the other in `PolyKit.dll`.

The solution is simple: when using `PolyKit.Embedded` to add `public` polyfills to a library, set your `TargetFrameworks` property so that you generate all possible sets of polyfills.

The optimal set of target frameworks for `public` polyfills is equal to the target frameworks of the `PolyKit` package. At the time of writing it is `net462;net47;netstandard2.0;netstandard2.1;net6.0;net7.0;net8.0`, but you may refer to [this NuGet page](https://www.nuget.org/packages/PolyKit#supportedframeworks-body-tab) at any time to find out which frameworks are targeted by the latest version of `PolyKit`.

### Compatibility

#### .NET SDK

`PolyKit` and `PolyKit.Embedded` require that you build dependent projects with .NET SDK version 6.0 or later.

#### Target frameworks

Polyfills provided by `PolyKit` and `PolyKit.Embedded` are compatible with all flavors of .NET supported by Microsoft at the time of publishing, as well as all corresponding versions of .NET Standard:

- .NET Framework 4.6.2 or greater;
- .NET 6 or greater;
- .NET Standard 2.0 or greater.

#### Build toolchain

A minimum of Visual Studio / Build Tools 2022 and/or .NET SDK 6.0 is required to compile the polyfills provided by `PolyKit.Embedded`.

#### Language version

C# language version 10.0 or greater is required to compile the polyfills provided by `PolyKit.Embedded`.

It is recommended to set the `LangVersion` property to `latest` in projects that reference `PolyKit` or `PolyKit.Embedded`, in order to take advantage of all the polyfilled features.

#### Analyzers, code coverage, and other tools

All code provided by `PolyKit.Embedded` is [well-behaved guest code](https://riccar.do/posts/2022/2022-05-30-well-behaved-guest-code.html):

- all source files bear the "auto-generated" mark, so that code style analyzers will happily skip them;
- every source file name ends in `.g` (e.g. `Index.g.cs`) so that it can be automatically excluded from code coverage;
- all added types are marked with a [`GeneratedCode`](https://learn.microsoft.com/en-us/dotnet/api/system.codedom.compiler.generatedcodeattribute) attribute;
- all added classes and structs are marked with [`ExcludeFromCodeCoverage`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.excludefromcodecoverageattribute) and [`DebuggerNonUserCode`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.debuggernonusercodeattribute) attributes.

This ensures that using `PolyKit.Embedded` will have zero impact on your coverage measurements, code metrics, analyzer diagnostic output, and debugging experience.

## Features

PolyKit provides support for the following features across all [compatible target frameworks](#target-frameworks).  
Please note that some types will only be polyfilled when compiling with a .NET SDK version that actually supports them. 

| Feature | Minimum .NET SDK version | Notes |
|---------|:------------------------:|-------|
| [nullable reference types](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references), including null state analysis | 6.0 | |
| [indices and ranges](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-8#indices-and-ranges) | 6.0 | Note #1 |
| [`init` accessor on properties and indexers](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/init) | 6.0 | |
| [`DateOnly` struct](https://learn.microsoft.com/en-us/dotnet/api/system.dateonly) | 6.0 | Note #2 |
| [`TimeOnly` struct](https://learn.microsoft.com/en-us/dotnet/api/system.timeonly) | 6.0 | Note #2 |
| [caller argument expressions](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/caller-information#argument-expressions) | 6.0 | |
| [`AsyncMethodBuilder` attribute](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/general#asyncmethodbuilder-attribute) | 6.0 | |
| [`ModuleInitializer` attribute](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/general#moduleinitializer-attribute) | 6.0 | |
| [`SkipLocalsInit` attribute](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/skip-localsinit) | 6.0 | |
| [`UnconditionalSuppressMessage` attribute](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming#unconditionalsuppressmessage) | 6.0 | |
| [`RequiresPreviewFeatures` attribute](https://github.com/dotnet/designs/blob/main/accepted/2021/preview-features/preview-features.md) | 6.0 | |
| [`UnmanagedCallersOnly` attribute](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/function-pointers#systemruntimeinteropservicesunmanagedcallersonlyattribute) | 6.0 | |
| [`ValidatedNotNull` attribute](https://learn.microsoft.com/en-us/dotnet/api/microsoft.validatednotnullattribute) | 6.0 | Note #3 |
| [`StackTraceHidden` attribute](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stacktracehiddenattribute) | 6.0 | Note #4 |
| support for writing [custom string interpolation handlers](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/interpolated-string-handler) | 6.0 | |
| [`Enumerable.TryGetNonEnumeratedCount<TSource>` method](https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.trygetnonenumeratedcount) | 6.0 | Note #5 |
| [`ISpanFormattable` interface](https://learn.microsoft.com/en-us/dotnet/api/system.ispanformattable) | 6.0 | Note #6 |
| [trimming incompatibility attributes](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming#resolve-trim-warnings) | 6.0 / 7.0| Note #7 |
| [required members](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-11#required-members) | 7.0 | |
| [`scoped` modifier](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-11.0/low-level-struct-improvements) (including the `UnscopedRef` attribute) | 7.0 | |
| [`CompilerFeatureRequired` attribute](https://github.com/dotnet/runtime/issues/66167) | 7.0 | |
| [`ConstantExpected` attribute](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.constantexpectedattribute) | 7.0 | |
| [`StringSyntax` attribute](https://github.com/dotnet/runtime/issues/62505) | 7.0 | |
| [`Experimental` attribute](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.experimentalattribute) | 8.0 | |

**Note #1:** This feature depends on `System.ValueTuple`, which is not present in .NET Framework versions prior to 4.7, and `System.HashCode`, absent from .NET Framework (all versions) and .NET Standard 2.0.
If you reference the `PolyKit.Embedded` package in a project targeting .NET Framework or .NET Standard 2.0, you must also add a package reference to [`Microsoft.Bcl.HashCode`](https://www.nuget.org/packages/Microsoft.Bcl.HashCode);
for .NET Framewrok 4.6.2, a package reference to [`System.ValueTuple`](https://www.nuget.org/packages/System.ValueTuple) is also needed. Otherwise, compilation will not fail, but features dependent on HashCode and/or ValueTuple will not be present in the compiled assembly.

**Note #2:** Polyfills for `DateOnly` and `TimeOnly`, unlike their .NET Runtime counterparts, do not support the [`IParsable`](https://learn.microsoft.com/en-us/dotnet/api/system.iparsable) and [`ISpanParsable<TSelf>`](https://learn.microsoft.com/en-us/dotnet/api/system.ispanparsable-1) interfaces because they contain [static virtual members](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/static-virtual-interface-members), a feature of C# 11.0 that cannot be polyfilled.  
The methods are there, you can use them (for instance you can call `DateOnly.Parse(str)`), but they are just public methods of the individual types, not associated with any interface.

**Note #3:** This is not, strictly speaking, a polyfill, but it spares you the trouble of defining your own internal `ValidatedNotNullAttribute` or referencing Visual Studio SDK.  
The attribute provided by PolyKit is in the `PolyKit.Diagnostics.CodeAnalysis` namespace.

**Note #4:** Polyfilling `StackTraceHiddenAttribute` would be worthless without providing actual support for it. PolyKit cannot replace relevant code (`Exception.StackTrace` getter and `StackTrace.ToString()` method) in frameworks prior to .NET 6.0.  
PolyKit provides two extension methods, `Exception.GetStackTraceHidingFrames()` and `StackTrace.ToStringHidingFrames()`; these methods retrofit the behavior of `Exception.StackTrace` and `StackTrace.ToString()` respectively to frameworks prior to .NET 6, where `StackTraceHiddenAttribute` was first introduced.  
When used on .NET 6.0+, the above extension methods are just façades for their BCL counterparts.
Be aware of the following limitations:

- the output of the "retrofitted" extension methods is always in US English, regardless of any locale setting;
- external exception stack trace boundaries ("End of stack trace from previous location" lines) are missing from returned strings.

**Note #5:** Obviously PolyKit cannot extend `System.Linq.Enumerable`, so we'll have to meet halfway on this.  
PolyKit adds a `System.Linq.PolyKitEnumerable` class, containing a `TryGetCountWithoutEnumerating<TSource>` method that will just call `TryGetNonEnumeratedCount<TSource>`on .NET 6.0+ and replicate most of its functionality (except where it requires access to runtime internal types) on older frameworks.

**Note #6:** PolyKit does not (and can not) add `ISpanFormattable` support to .NET Runtime types: `intVar is ISpanFormattable` will still be `false` except on .NET 6.0 and later versions.  
You can, however, implement `ISpanFormattable` in a type exposed by a multi-target library, no `#if` needed: it will behave just as you expect on .NET 6.0+, and still have a `TryFormat` method on older platforms (unless you used an explicit implementation).  
Also note that, in projects referencing `PolyKit.Embedded` and targeting .NET Framework or .NET Standard 2.0, `ISpanFormattable` will only be compiled if package [`System.Memory`](https://www.nuget.org/packages/System.Memory) is also referenced.

**Note #7:** [`RequiresDynamicCodeAttribute`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.requiresdynamiccodeattribute) was introduced with .NET 7.0 and requires .NET SDK 7.0 to be properly supported duting build.

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

_Of course_ we accept contributions! :smiley: Just take a look at our [Code of Conduct](https://github.com/Tenacom/.github/blob/main/CODE_OF_CONDUCT.md) and [Contributors guide](https://github.com/Tenacom/.github/blob/main/CONTRIBUTING.md), create your fork, and let's party! :tada:

## Contributors

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tbody>
    <tr>
      <td align="center"><a href="https://github.com/rdeago"><img src="https://avatars.githubusercontent.com/u/139223?v=4" width="100px;" alt=""/><br /><sub><b>Riccardo De Agostini</b></sub></a></td>
    </tr>
  </tbody>
  <tfoot>
    <tr>
      <td align="center" size="13px" colspan="7">
        <img src="https://raw.githubusercontent.com/all-contributors/all-contributors-cli/1b8533af435da9854653492b1327a23a4dbd0a10/assets/logo-small.svg">
          <a href="https://all-contributors.js.org/docs/en/bot/usage">Add your contributions</a>
        </img>
      </td>
    </tr>
  </tfoot>
</table>

<!-- markdownlint-restore -->
<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->
