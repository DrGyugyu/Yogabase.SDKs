# Changelog

All notable changes to this package are documented in this file.

## [0.3.0-preview.1]

- The relay answers `413`, `429` and `504` with no body at all, and the client parsed every answer as
  a JSON object, so those surfaced as a JSON parse error. A bodyless answer now throws
  `YogabaseRuntimeHostException` with its status (code `UNKNOWN`, empty `BodyJson`), and so does any
  other refusal whose body is not a JSON object naming a code.
- `YogabaseRuntimeHostException.IsRetryable` is true for `RUNTIME_HOST_INSTANCE_PENDING`, `429` and
  `504` and false for everything else, `413` included. The client still waits out only the pending
  refusal, and only on `HeartbeatOnceAsync` and `ReadyAsync`; a heartbeat repeated after a `504`
  carries the next sequence.
- The comments and documentation no longer say that `POST /allocations` is refused with
  `RUNTIME_HOST_RELAY_ALLOCATION_BACKEND_ONLY`: the relay answers that for
  `/runtime-host/v1/self/allocate`, and `/allocations` is not mapped. They no longer name
  `RUNTIME_HOST_INSTANCE_NOT_FOUND`, which can never reach a relay call (nor can
  `RUNTIME_HOST_WRONG_POD` or `RUNTIME_HOST_BINDING_REJECTED`), and assignment event kinds are
  documented in lower case (`allocated`, `allocation-released`), as the relay sends them.
- The client's routes, limits and refusal codes are held to
  `contracts/yogabase-runtime-host-self-v1.openapi.json`, the document the product published on
  2026-10-20 for the loopback relay, by tests in the SDK repository.
- The `Runtime` folder compiles with no warnings or errors at C# 9, warnings as errors, as
  `netstandard2.1` against System.Text.Json 8.0.5. It was not compiled again against the Unity
  6000.5.7f1 reference set; no Unity Editor has imported this version and no IL2CPP build has run it.
- The package id is `io.yogabase.runtime-host` and its display name is *Yogabase Runtime Host*;
  the earlier id, which carried a predecessor's name, is withdrawn. No `.meta` GUID changed, so a
  project that referenced the old folder keeps its assets by changing the key and path in
  `manifest.json`.
- The package is licensed under the Apache License, Version 2.0 (`LICENSE.md`, the licence of this
  repository), where `LICENSE.md` carried a proprietary notice. `package.json` states `license`,
  `repository`, `documentationUrl`, `changelogUrl` and `licensesUrl`.
- `package.json` names Unity 6000.5 (release 7f1), the version the compile check and the one import
  ran on, where it named 6000.0.
- The README says the folder carries its 24 committed `.meta` files, where it said it carried none,
  and says how a Unity project gets the player-side client, which is not in this package.

## [0.2.3-preview.1]

- A Lambda publish states `registeredLambdaIds` for a bundle whose entry is `RegisterLambdas`
  (*Registered Lambda Ids* in the settings asset), and sends nothing for a single-function bundle.
  The settings asset checks the Control Plane's registration rules first: 1 to 64 ids of up to 128
  characters, none repeated, none equal to the service name, every ingress binding one of them.
- The runtime-host client reads a page's `headState` and `headSequence` when the Control Plane states
  them, and `ReadCurrentStateAsync` answers the current state in one read, paging to the end of the
  stream against a Control Plane that predates the head.
- The runtime folder is also built as an assembly, `src/unity/Yogabase.Yogabase.RuntimeHost.Unity`,
  against System.Text.Json 8.0.5, for projects that take the SDK as built, digest-locked
  assemblies.
- `Yogabase ▸ Publish Server` can publish a Lambda archive: with *What to publish* set to Lambda it
  uploads a prebuilt Lambda bundle zip with the execution declaration the Control Plane requires
  since 2026-09-17 — `executionKind` (`Stateless` or `Stateful`) and the comma-separated
  `realtimeIngressBindings` and `realtimeEgressBindings`, sent even when empty. The settings asset
  checks binding ids with the admission rules first. Server uploads send exactly what they did.
  Compile-checked at C# 9 against the Unity 6000.5.7f1 Editor reference set; not run in an Editor.

## [0.2.2-preview.1]

- The runtime-host client waits out `503 RUNTIME_HOST_INSTANCE_PENDING` on `HeartbeatOnceAsync` and
  `ReadyAsync`, the relay's answer while the member has not yet reported a freshly started Pod.
  Each wait is the refusal's `retryAfterSeconds` hint (2 seconds without one), within the new
  `YogabaseRuntimeHostOptions.StartupPendingBudget` (default 60 seconds; zero disables it). Every
  other refusal code is still thrown on the first attempt. Before this, a server that treated any
  exception from its first heartbeat as fatal quit once on every Pod start.
- `YogabaseRuntimeHostException.RetryAfterSeconds` exposes that hint. A malformed or repeated hint
  reads as null and never makes the exception's constructor throw.
- `Connect(onHeartbeatFailure)` reports the failures the background heartbeat loop keeps running
  through; they were previously swallowed with no trace.
- The `Runtime` folder compiles with no errors at C# 9 against the Unity 6000.5.7f1
  `netstandard2.1` reference set, including its `System.Text.Json`. No Unity Editor has imported
  this version, and no IL2CPP build has run it.

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
- Not published to any package registry.
