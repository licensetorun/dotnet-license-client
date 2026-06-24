# Changelog

All notable changes to the .NET license client are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-06-24

Initial release.

### Added
- `LicenseClient` with `ActivateAsync()`, `DeactivateAsync()`, `ValidateAsync()`,
  `IsValidAsync()`, `SwapAsync()`, `ActivationsAsync()` and `CheckForUpdateAsync()`.
- Targets `netstandard2.0` (runs on .NET Framework 4.6.1+, .NET Core and .NET 5+).
- `instance` defaults to `Environment.MachineName` when not provided.
- `LicenseResult` with `JsonElement Body`; every method returns one and never
  throws on network/HTTP errors — failures come back as `{ "error": "network_error" }`.

[Unreleased]: https://github.com/licensetorun/dotnet-license-client/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/licensetorun/dotnet-license-client/releases/tag/v1.0.0
