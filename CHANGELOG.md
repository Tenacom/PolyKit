# Changelog

All notable changes to PolyKit will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased changes

### New features

### Changes to existing features

### Bugs fixed in this release

### Known problems introduced by this release

## [1.0.0-preview.4](https://github.com/Buildvana/PolyKit/releases/tag/1.0.0-preview.4) (2022-08-15)

### Bugs fixed in this release

- When using [Public API analyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) in a project that uses `PolyKit.Embedded` to generate public polyfills, you had to add all polyfilled APIs to public API files. This made no sense, as polyfills are not part of your APIs; instead, they should be "felt" as part of the runtime library. [Warning RS0016](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/Microsoft.CodeAnalysis.PublicApiAnalyzers.md#rs0016-add-public-types-and-members-to-the-declared-api) is now disabled in all polyfill source files.
- When using [Public API analyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) in a project that uses `PolyKit.Embedded` to generate public polyfills, [warning RS0041](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/Microsoft.CodeAnalysis.PublicApiAnalyzers.md#rs0041-public-members-should-not-use-oblivious-types) could sometimes be raised in polyfill source files, even with RS0016 disabled per the fix above. RS0041 is now disabled in all polyfill source files.

## [1.0.0-preview.3](https://github.com/Buildvana/PolyKit/releases/tag/1.0.0-preview.3) (2022-08-15)

### Bugs fixed in this release

- `CallerArgumentExpressionAttribute` was mistakingly polyfilled for .NET Core 3.1. The attribute was actually introduced in .NET Core 3.0.

## [1.0.0-preview.2](https://github.com/Buildvana/PolyKit/releases/tag/1.0.0-preview.2) (2022-08-15)

### Bugs fixed in this release

- `PolyKit.Embedded` did not correctly add polyfill sources to compilation.

## [1.0.0-preview.1](https://github.com/Buildvana/PolyKit/releases/tag/1.0.0-preview.1) (2022-08-15)

Initial release.
