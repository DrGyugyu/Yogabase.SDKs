# Changelog

All notable changes to this package are documented in this file.

## [0.1.0-preview.1]

- Initial local preview of the Unity package wrapping `Yogabase.Yogabase.RuntimeHost`: the
  runtime-host client, its options, and the JSON-DOM wire models, copied byte-for-byte from
  `src/dotnet/Yogabase.Yogabase.RuntimeHost` in this repository.
- Adds a `link.xml` preserving the module, `System.Text.Json`, and `System.Net.Http` against
  IL2CPP managed code stripping.
- Adds a minimal Unity Test Runner smoke test; the full behavioral suite lives in the .NET project
  and is not ported here.
- Not published to any package registry or Git URL. Local, unpublished preview only.
