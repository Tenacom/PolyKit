# Changelog

All notable changes to PolyKit will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased changes

### New features

- PolyKit now provides a quasi-polyfill for [`Enumerable.TryGetNonEnumeratedCount<TSource>`](https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.trygetnonenumeratedcount). Since extending the `Enumerable` class is not possible, PolyKit adds (in the `PolyKit.Linq` namespace) a `TryGetCountWithoutEnumerating<TSource>` extension method that calls `TryGetNonEnumeratedCount<TSource>` on .NET6.0+ and polyfills as much functionality as possible on older frameworks.

### Changes to existing features

- All types provideed by PolyKit are now flagged with the necessary attributes to be ignored by code analyzers, debuggers, code coverage tools, code metrics, etc. See [this blog post](https://riccar.do/posts/2022/2022-05-30-well-behaved-guest-code.html) for more information about the attributes added to types and the rationale behind each of them.

### Bugs fixed in this release

### Known problems introduced by this release

## [1.0.16](https://github.com/Tenacom/PolyKit/releases/tag/1.0.16) (2022-11-01)

Initial release.
