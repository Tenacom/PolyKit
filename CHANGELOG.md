# Changelog

All notable changes to PolyKit will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased changes

### New features

### Changes to existing features

### Bugs fixed in this release

### Known problems introduced by this release

## [3.0.4](https://github.com/Tenacom/PolyKit/releases/tag/3.0.4) (2023-11-20)

### New features

- .NET 8 was added as a target platform.
- All polyfills were updated with modifications (if any) made to BCL types up to the release of .NET 8.0.0.
- The polyfill for [`ExperimentalAttribute`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.experimentalattribute) has been added; however, this attribute is only polyfilled by PolyKit.Embedded when compiling with .NET SDK 8.0. This avoids giving the user the false impression that the attribute is supported, when the compiler doesn't actually support it.
- [`DateOnly`](https://learn.microsoft.com/en-us/dotnet/api/system.dateonly) and [`TimeOnly`](https://learn.microsoft.com/en-us/dotnet/api/system.timeonly) polyfills now support [`deconstruction`](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/deconstruct).

### Changes to existing features

- **BREAKING CHANGE:** Extension classes that were previously in `PolyKit.*` namespaces have been moved to `System.*` namespaces, in order for their members to be more easily discoverable (e.g. by Intellisense) and usable without additional `using` statements.  
This change may seem to go against best practices, as it places PolyKit types in namespaces usually reserved for the BCL; however, it makes sense if you consider that one of the purposes of polyfills is to minimize the amount of code changes necessary to backport code.  
Affected extension classes include:
  - `PolyKitEnumerable`, moved from `PolyKit.Linq` to `System.Linq`;
  - `PolyKitExceptionExtensions`, moved from `PolyKit.Diagnostics` to `System`;
  - `PolyKitStackTraceExtensions`, moved from `PolyKit.Diagnostics` to `System.Diagnostics`.

## [2.0.30](https://github.com/Tenacom/PolyKit/releases/tag/2.0.30) (2022-11-24)

### Bugs fixed in this release

- The polyfill for [`DateOnly`](https://learn.microsoft.com/en-us/dotnet/api/system.dateonly) could generate a `CS8602` warning (possible dereference of a null reference).
- The polyfill for [`TimeOnly`](https://learn.microsoft.com/en-us/dotnet/api/system.timeonly), when compiled without span support, generated two `CS1503` errors because of a missing preprocessor conditional.

## [2.0.26](https://github.com/Tenacom/PolyKit/releases/tag/2.0.26) (2022-11-24)

### Bugs fixed in this release

- The polyfills for [`DateOnly`](https://learn.microsoft.com/en-us/dotnet/api/system.dateonly) and [`TimeOnly`](https://learn.microsoft.com/en-us/dotnet/api/system.timeonly) only worked when either the target platform supported spans, or the [`System.Memory`](https://www.nuget.org/packages/System.Memory) package was referenced.

## [2.0.24](https://github.com/Tenacom/PolyKit/releases/tag/2.0.24) (2022-11-23)

### New features

- .NET 7 was added as a target platform.
- Features that were introduced with .NET 7 are not polyfilled by PolyKit.Embedded when compiling with .NET SDK 6.0. This avoids giving the user the false impression that, for example, `UnscopedRefAttribute` is supported, when the compiler doesn't actually support it.
- PolyKit now provides a quasi-polyfill for [`Enumerable.TryGetNonEnumeratedCount<TSource>`](https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.trygetnonenumeratedcount). Since extending the `Enumerable` class is not possible, PolyKit adds (in the `PolyKit.Linq` namespace) a `TryGetCountWithoutEnumerating<TSource>` extension method that calls `TryGetNonEnumeratedCount<TSource>` on .NET6.0+ and polyfills as much functionality as possible on older frameworks.
- Polyfills for the following features were added:
  - [required members](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-11#required-members);
  - [`scoped` modifier](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-11.0/low-level-struct-improvements) (including the `UnscopedRef` attribute);
  - [`AsyncMethodBuilder` attribute](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/general#asyncmethodbuilder-attribute);
  - [`ModuleInitializer` attribute](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/general#moduleinitializer-attribute);
  - [trimming incompatibility attributes](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming#resolve-trim-warnings);
  - [`UnconditionalSuppressMessage` attribute](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming#unconditionalsuppressmessage);
  - [`CompilerFeatureRequired` attribute](https://github.com/dotnet/runtime/issues/66167);
  - [`RequiresPreviewFeatures` attribute](https://github.com/dotnet/designs/blob/main/accepted/2021/preview-features/preview-features.md);
  - [`UnmanagedCallersOnly` attribute](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/function-pointers#systemruntimeinteropservicesunmanagedcallersonlyattribute);
  - [`ConstantExpected` attribute](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.constantexpectedattribute);
  - [`StringSyntax` attribute](https://github.com/dotnet/runtime/issues/62505);
  - [`ISpanFormattable` interface](https://learn.microsoft.com/en-us/dotnet/api/system.ispanformattable) (note that the polyfill does NOT add `ISpanFormattable` support to .NET Runtime types);
  - [`DateOnly`](https://learn.microsoft.com/en-us/dotnet/api/system.dateonly) and [`TimeOnly`](https://learn.microsoft.com/en-us/dotnet/api/system.timeonly) structs (not that the polyfills do NOT implement [`IParsable`](https://learn.microsoft.com/en-us/dotnet/api/system.iparsable) and [`ISpanParsable<TSelf>`](https://learn.microsoft.com/en-us/dotnet/api/system.ispanparsable-1), although the methods are there and can be used as public methods of the individual types).

### Changes to existing features

- **BREAKING CHANGE:** Following .NET's [Library support for older frameworks](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/7.0/old-framework-support) policy, support for .NET Core 3.1 has been removed.
- All types provideed by PolyKit are now flagged with the necessary attributes to be ignored by code analyzers, debuggers, code coverage tools, code metrics, etc. See [this blog post](https://riccar.do/posts/2022/2022-05-30-well-behaved-guest-code.html) for more information about the attributes added to types and the rationale behind each of them.

## [1.0.16](https://github.com/Tenacom/PolyKit/releases/tag/1.0.16) (2022-11-01)

Initial release.
