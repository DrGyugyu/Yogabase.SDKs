# Changelog

All notable changes to this package are documented in this file.

## [0.2.1-preview.1]

- Aligns the Editor publish flow to the build surface the Control Plane actually serves
  (`contracts/yogabase-builds-v1.openapi.json`, a byte-identical product snapshot): a refusal is
  read as `code`, `field` and `cause` (it was `path` and `message`, which the server never sent, so
  the window showed empty lines); the build's `identity`, `attempt` and `kind` and the check's
  `evidenceOrigin` are read and shown; the build id is treated as the UUID the route constrains it
  to; and the settings asset refuses a stored token that does not start with `ybt_` or exceeds 128
  characters, because the Control Plane answers such a bearer with a bare `401`.
- Still compile-checked only, against a real Unity 6000.5.7f1 Editor reference set at C# 9. No
  Unity Editor has run the window.

## [0.2.0-preview.1]

- Adds the Editor half of the package: `Yogabase ▸ Publish Server` builds the Linux Dedicated
  Server (IL2CPP) at the declared architecture, verifies the built binary's ELF machine type against
  that declaration, zips the output with `0755` on the player binary and native libraries, uploads
  it to the Control Plane, and shows the Build check's findings and refusals in the window.
- Adds `YogabaseSettings`, a `ScriptableObject` created through `Assets ▸ Create ▸ Yogabase ▸
  Settings`, holding the Control Plane URL, project and environment ids, service name, executable
  name, target architecture, declared ports, and build output directory. **The developer token is
  deliberately not a field on it**: a serialized field would put a secret in a committed `.asset`,
  so it lives in `EditorPrefs` keyed by project and is pasted through the asset's inspector.
- Adds `YogabaseEditorRoutes`, the one file holding every Control Plane route and multipart field
  name the Editor tooling uses, mirroring `YogabaseRoutes` in the .NET client.
- The Editor tool speaks the multipart upload itself with `HttpClient` and reads responses with
  `JsonUtility` rather than referencing the `Yogabase.Yogabase.Client` NuGet package: that package
  imports as a `netstandard2.1` plugin, but its SignalR and `System.Text.Json` dependency closure
  would have to be vendored alongside it, and none of it is needed to send one form.
- Replaces the former hand-packaging story. A developer no longer writes `yogabase-service.json`,
  generates an SBOM, runs a scanner, or produces a detached ECDSA signature: Yogabase derives the
  manifest and inventory from the uploaded bundle, scans it, and signs all three with the
  installation admission key.
- The Editor scripts are compile-checked by `scripts/compile-unity-editor.sh` against a real Unity
  6000.5.7f1 Editor reference set at C# 9. **No Unity Editor has run this window**: the build,
  upload, and Build check flow is unexercised.

## [0.1.0-preview.1]

- Initial local preview of the Unity package wrapping `Yogabase.Yogabase.RuntimeHost`: the
  runtime-host client, its options, and the JSON-DOM wire models, copied byte-for-byte from
  `src/dotnet/Yogabase.Yogabase.RuntimeHost` in this repository.
- Adds a `link.xml` preserving the module, `System.Text.Json`, and `System.Net.Http` against
  IL2CPP managed code stripping.
- Adds a minimal Unity Test Runner smoke test; the full behavioral suite lives in the .NET project
  and is not ported here.
- Not published to any package registry or Git URL. Local, unpublished preview only.
