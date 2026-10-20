# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.4] - 2026-10-20

### Changed

- Pin the .NET SDK to 10.0.402 (#73).
- Bump `System.CommandLine`, `Microsoft.Extensions.Logging.Abstractions`,
  `Microsoft.NET.Test.Sdk`, `Meziantou.Analyzer`, and
  `Microsoft.SourceLink.GitHub` (#75).
- Bump `xunit.v3` from 3.2.2 to 4.0.2, and `FsCheck` and `FsCheck.Xunit.v3`
  to 3.5.0 (#72).
- Move the local tool manifest to `.config/dotnet-tools.json` so dependabot
  keeps the local tools up to date (#74).
- Bump the local tools `dotnet-coverage` (#75) and `dotnet-stryker` (#76).
- Bump GitHub Actions: `actions/upload-artifact` and
  `actions/download-artifact` (#68, #69).

## [0.2.3] - 2026-09-28

### Changed

- Pin the .NET SDK to 10.0.401 (#63).
- Bump `System.CommandLine`, `Microsoft.Extensions.Logging.Abstractions`,
  `Microsoft.NET.Test.Sdk`, `Meziantou.Analyzer`, and
  `Microsoft.SourceLink.GitHub` (#64, #66).

## [0.2.2] - 2026-08-25

### Fixed

- Block the MSBuild bootstrap until locator registration completes. Callers that
  returned once registration had merely started could trigger JIT compilation of
  code referencing `Microsoft.Build` types before the locator's assembly
  resolver was registered, which failed with `FileNotFoundException` (#56).

### Changed

- Pin the .NET SDK to 10.0.400 (#50).
- Bump `System.CommandLine`, `Microsoft.Extensions.Logging.Abstractions`,
  `Microsoft.NET.Test.Sdk`, `NSubstitute`, `Meziantou.Analyzer`, and
  `Microsoft.SourceLink.GitHub` (#54).
- Align the `Microsoft.Extensions` packages at 10.0.11 and bump `FsCheck` and
  `FsCheck.Xunit.v3` to 3.3.4.
- Bump the local tools `dotnet-coverage`, `dotnet-reportgenerator-globaltool`,
  and `dotnet-stryker`.
- Bump GitHub Actions: `actions/checkout` and `actions/attest`.

## [0.2.1] - 2026-07-17

### Changed

- Bump `NSubstitute` from 5.3.0 to 6.0.0 (#41).
- Bump `Meziantou.Analyzer`, `Microsoft.Extensions.Logging.Abstractions`,
  `Microsoft.NET.Test.Sdk`, `Microsoft.SourceLink.GitHub`, and
  `System.CommandLine` (#40).
- Bump GitHub Actions: `actions/setup-dotnet`, `actions/cache`,
  `actions/attest-build-provenance`, and the .NET SDK patch version.

## [0.2.0] - 2026-06-25

### Added

- Accept `.slnf` solution filter files as input, scoping affected-project
  analysis to a project subset (#11).

### Fixed

- Trust all nuget.org repository signing certificates (#31).
- Use `global.json` to pin SDK patch versions so builds resolve a consistent SDK.

### Changed

- Bump `Meziantou.Analyzer` and 5 other dependencies (#30).
- Bump GitHub Actions: `actions/checkout`, `actions/cache`,
  `actions/setup-dotnet`, and the .NET SDK patch version.

## [0.1.1] - 2026-04-03

### Fixed

- Only pack the CLI tool, not the internal library projects.

## [0.1.0] - 2026-04-03

### Added

- Initial release of `cycle`, an affected-project resolver for .NET CI.

[Unreleased]: https://github.com/jensk-dev/dotnet-cycle/compare/v0.2.4...HEAD
[0.2.4]: https://github.com/jensk-dev/dotnet-cycle/compare/v0.2.3...v0.2.4
[0.2.3]: https://github.com/jensk-dev/dotnet-cycle/compare/v0.2.2...v0.2.3
[0.2.2]: https://github.com/jensk-dev/dotnet-cycle/compare/v0.2.1...v0.2.2
[0.2.1]: https://github.com/jensk-dev/dotnet-cycle/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/jensk-dev/dotnet-cycle/compare/v0.1.1...v0.2.0
[0.1.1]: https://github.com/jensk-dev/dotnet-cycle/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/jensk-dev/dotnet-cycle/releases/tag/v0.1.0
