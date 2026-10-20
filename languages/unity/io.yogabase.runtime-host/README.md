# Yogabase Runtime Host and Publisher (Unity)

> **Synced source, not a fork.** The four `Runtime/*.cs` implementation files track the
> corresponding files in `src/dotnet/Yogabase.Yogabase.RuntimeHost` in this repository. They differ
> only in C# namespace syntax: the Unity copies use C# 9 braced namespaces because Unity 6000.5.7f1
> does not accept the .NET source's C# 10 file-scoped namespaces. The **.NET project is the source
> of truth** for behavior — change it first, then re-copy and perform the same syntax-only
> conversion if Unity still requires it. This package only gives Unity a UPM-shaped way to consume
> that module; it does not re-implement client behavior.

A UPM package with two halves:

- **`Runtime/`** carries `Yogabase.Yogabase.RuntimeHost`, the runtime-host client a hosted
  dedicated server uses to heartbeat, signal Ready, poll assignments, and drain on shutdown.
- **`Editor/`** adds `Yogabase ▸ Publish Server`: build the Linux dedicated server, zip it, upload
  it, and read the Build check — one window, no terminal step.

See this repository's `docs/UNITY_DEDICATED_SERVER.md` for the whole flow and for what the runtime
side must do; this README covers the package itself.

## What this package is

- **Package name:** `io.yogabase.runtime-host`
- **Licence:** Apache License, Version 2.0 (`LICENSE.md`); the Agones-derived files are described in
  `Third Party Notices.md`.
- **Contents:** the runtime-host client (`YogabaseRuntimeHostClient`), its options
  (`YogabaseRuntimeHostOptions`), the JSON-DOM wire models (`RuntimeHostModels.cs`), and an
  `IsExternalInit` compatibility shim (`Compatibility.cs`) needed for the `netstandard2.1` target
  Unity compiles against; plus the Editor publish flow (`YogabaseSettings`,
  `YogabasePublishServerWindow`, `YogabaseBuildUploader`, `YogabaseEditorRoutes`) in an
  Editor-only assembly.
- **Dependencies:** none declared in `package.json`. At the C# level the module depends only on
  `System.Text.Json` and `HttpClient`, both of which ship with Unity's .NET runtime — nothing to
  add to `manifest.json`.
- **Why it is a separate package from a general Yogabase client:** a headless dedicated server does
  not want a SignalR dependency, and it does not want reflection-based JSON. This module reads
  every relay response through the JSON DOM (`JsonNode`/`JsonObject`) rather than
  `JsonSerializer.Deserialize<T>` or a source-generated contract, specifically so IL2CPP managed
  code stripping (Medium or High) has no attribute-driven serialization path to break.

## Installation

This package is **not published** to any UPM or npm registry. Take it from this repository:

- Reference it from a Unity project's `Packages/manifest.json` by Git URL with the package's path:
  ```json
  "io.yogabase.runtime-host": "https://github.com/DrGyugyu/Yogabase.SDKs.git?path=languages/unity/io.yogabase.runtime-host"
  ```
- Or with a local `file:` path pointing at a checkout of this directory, e.g.:
  ```json
  "io.yogabase.runtime-host": "file:../relative/path/to/languages/unity/io.yogabase.runtime-host"
  ```
- Or copy this directory directly into the target project's `Packages/` folder.

The folder is **import-ready as-is** and carries its 24 `.meta` files, one for every file and for
the `Editor`, `Runtime`, `Tests` and `licenses` folders. They are committed because Unity ignores
an asset in an immutable package (one installed from a Git URL) that has no `.meta` file, and so
that every asset keeps the same GUID in every project that imports it. When you add a file, let
Unity generate its `.meta` in a project that references this folder by a `file:` path and commit
that file; do not write a GUID by hand.

The `unity` field of `package.json` is `6000.5` with `unityRelease` `7f1`: Unity 6000.5.7f1 is the
only version this package has been compile-checked or imported on. No other Unity version has been
tried.

## The player-side client is not in this package

A game that signs a player in, invokes Functions, uses Storage or Realtime, or asks for a game server
uses the .NET client, `Yogabase.Yogabase.Client`. **There is no UPM package for it**, and this one
does not carry it: it holds only the dedicated server's runtime-host module and the Editor publisher.
What exists is that client's own source, built for Unity's profile (paths below are from this
repository's root):

- `src/unity/Yogabase.Yogabase.Client.Unity` compiles the same `*.cs` files as
  `src/dotnet/Yogabase.Yogabase.Client` for `netstandard2.1` into an assembly named
  `Yogabase.Yogabase.Client`, against SignalR client 8.0.32 and System.Text.Json 8.0.5
  (`src/unity/Directory.Packages.props`). The NuGet package is built against the 10.x line, and Unity
  6000.5 binds its own 8.0 extension assemblies, so a plugin built against 10.x is CS1705 there.
- **To use it, a developer builds it and brings its dependencies by hand.** With the .NET 10 SDK,
  `dotnet build src/unity/Yogabase.Yogabase.Client.Unity -c Release` writes
  `bin/Release/netstandard2.1/Yogabase.Yogabase.Client.dll` (with its `.pdb`, XML documentation and
  `.deps.json`). The output does not contain the SignalR client or the rest of its closure, and no
  script in this repository gathers them. The closure is the 27 packages locked in that project's
  `packages.lock.json`; each package's `netstandard2.0` or `netstandard2.1` assembly is in its `lib`
  folder in the NuGet cache after a restore. `src/unity/Directory.Packages.props` records that
  Unity 6000.5.7f1 supplies System.Text.Json, Microsoft.Extensions.Logging.Abstractions,
  Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Bcl.AsyncInterfaces,
  System.Diagnostics.DiagnosticSource and System.Text.Encodings.Web itself at 8.0.0.0, and
  `docs/DEPENDENCY_LICENSES.md` that `System.Memory`, `System.Buffers`, `System.Numerics.Vectors` and
  `System.Threading.Tasks.Extensions` need no DLL; which of the rest a given project must add has not
  been worked out here. Copy the assemblies into the project's `Assets` (for example `Assets/Plugins`)
  and keep each package's MIT notice with them.
- The SignalR versions are the ones the AI Game Developer (MCP) Unity package restores into
  `Assets/Plugins/NuGet`, so a project that has it holds byte-identical copies.

**Never tried in an Editor.** `UnityBuildTests` checks that the Unity build compiles and that neither
its references nor its lock reach any assembly at 9.x or above. This repository records no run of
the build in a Unity Editor, none in a player and none in an IL2CPP build. Three things stay untried:
whether the assemblies a developer gathers by hand resolve in Unity; whether the client's password
sign-in works on Unity's `HttpClientHandler`, since it needs the handler to keep the identity cookie
and not follow the authorize redirect; and whether Medium or High managed stripping breaks the client,
which reads and writes JSON with `System.Text.Json.JsonSerializer` in a few places (the runtime-host
module deliberately avoids that, see above) while the `link.xml` here preserves only the runtime-host
assembly.

## Publishing a build

`Yogabase ▸ Publish Server` is the whole flow. There is no `yogabase-service.json` to write, no
SBOM to generate, no scanner to run, and no detached signature to produce: Yogabase derives the
layout manifest and a software inventory from the bundle you upload, scans it against a
digest-pinned advisory snapshot, and signs manifest, inventory and scan together with the
installation admission key.

1. Create the settings asset with `Assets ▸ Create ▸ Yogabase ▸ Settings` and commit it. It holds
   the Control Plane URL, the project and environment ids, the service name, the executable name
   (`Server.x86_64` by default), the target architecture, the declared ports, and the build output
   directory.
2. Paste a **project-scoped developer token** from the console's Settings page into the asset's
   inspector. It is stored in this machine's `EditorPrefs`, keyed by project id, and **never in the
   asset** — a serialized field would commit a secret into the repository and hand it to everyone
   who clones the project. The inspector shows only whether a token is present.
3. Open `Yogabase ▸ Publish Server`, assign the settings asset, and press **Build and publish**.
   Tick *Upload the existing build output without rebuilding* to re-upload after fixing packaging
   without paying for another IL2CPP build.

The window then:

- builds `BuildTarget.StandaloneLinux64` with `StandaloneBuildSubtarget.Server` and IL2CPP —
  the Dedicated Server subtarget, **not** a Player build run with `-nographics`, because a Player
  build still links the desktop graphics and audio stack and those libraries are not in Yogabase's
  base-image allowlist;
- reads the built binary's ELF `e_machine` and refuses to upload when it disagrees with the
  declared architecture, so a mismatch costs a local check rather than a round trip;
- zips the output with forward-slash entry names and `0755` on the player binary and native
  libraries — without that bit the server will not start;
- uploads one multipart request, writing every metadata field before the bundle so a server reading
  the form as a stream can refuse a malformed port before buffering the archive;
- polls the Build check every three seconds with backoff and renders its findings (severity, id,
  package, version, fixed-in version, bundle entry) and its refusals (code, path, message).

### Publishing a Lambda archive

Set **What to publish** to *Lambda* to upload a Lambda bundle zip built outside Unity (for example
with `dotnet`) instead of a server. The window then builds nothing, zips nothing and checks no ELF:
it uploads the archive at *Lambda archive path* (relative to the project folder) with the
execution declaration the Control Plane requires of a Lambda, because Yogabase never works it out
from the bytes:

- `executionKind` — exactly `Stateless` (no state between calls) or `Stateful` (durable state per
  actor);
- `realtimeIngressBindings` and `realtimeEgressBindings` — the realtime binding ids the Lambda
  receives events from and publishes events to, comma-separated and sent even when empty, because
  an empty field declares none while an absent one is refused (`BUILD_LAMBDA_DECLARATION_REQUIRED`).
  A Lambda can publish only to the egress bindings it declares;
- `registeredLambdaIds` — only for a bundle whose entry is `RegisterLambdas`: every Lambda id it
  registers, comma-separated. Admission never runs the bundle's code, so the ids are stated, and the
  sandbox later refuses a bundle that registers anything else. Leave *Registered Lambda Ids* empty
  for a single-function (`InvokeAsync`) bundle; the field is then not sent, because the Control Plane
  refuses it there (`BUILD_LAMBDA_REGISTRATION_UNEXPECTED`) as it refuses a registering bundle
  without it (`BUILD_LAMBDA_REGISTRATION_REQUIRED`).

The settings asset checks the declaration with the admission rules before anything is sent: each
binding id starts with a lowercase letter and uses lowercase letters, digits, `.`, `_` and `-`, at
most 96 characters; at most 16 per direction; none repeated. Registered ids follow the same
grammar at up to 128 characters, at most 64, none repeated, none equal to the *Service name*, and
every ingress binding must be one of them. The *Service name* is the Lambda's
function id and may be left empty to use the bundle's own. A server upload sends none of these
fields, exactly as before. This path is compile-checked like the rest of the Editor code (C# 9,
Unity 6000.5.7f1 reference set, warnings as errors) and has not been run in an Editor.

### Why the Editor code does not reference the .NET client package

`Yogabase.Yogabase.Client` targets `netstandard2.1` and would import as a managed plugin, but its
dependency closure — SignalR, `System.Text.Json`, and their transitive assemblies — would have to
be vendored into the project alongside it, and none of that closure is needed to send one multipart
form. The Editor tool therefore speaks the request itself with `HttpClient` and reads responses with
Unity's own `JsonUtility`. The cost is a small amount of duplicated request shape; the routes and
field names live in one file per side (`Editor/YogabaseEditorRoutes.cs` here,
`src/dotnet/Yogabase.Yogabase.Client/YogabaseRoutes.cs` there) so the two stay alignable.

### Architecture, and why it is set by reflection

`UnityEditor.LinuxStandalone.UserBuildSettings.architecture` is the only input the Linux player
build reads when it chooses an architecture. `PlayerSettings.SetArchitecture` and
`EditorUserBuildSettings.SetPlatformSettings("Standalone", "Architecture", …)` are not in that
chain: a build that sets only those reports success while emitting an x86-64 binary under an ARM64
name — which is exactly what happened on 2026-09-07. That property lives in the Linux module's
extension assembly, which Editor assemblies do not reference and which exists only when the module
is installed, so it is reached by reflection and its absence is a refusal rather than a silent skip.
All three settings are written anyway, so nothing keying off the legacy values disagrees with what
was actually built, and every one is restored afterwards.

## Startup sequence

Mirrors the sequence documented in `docs/UNITY_DEDICATED_SERVER.md`:

```csharp
using Yogabase.Yogabase.RuntimeHost;

await using var host = new YogabaseRuntimeHostClient();

await host.HeartbeatOnceAsync();      // one synchronous heartbeat before anything else
host.Connect();                       // starts the background heartbeat loop

// bind your declared listener(s) on the port(s) named in yogabase-service.json's ports[]

await host.ReadyAsync();              // signal readiness once the server can accept players

var resumeToken = string.Empty;
await foreach (var assignment in host.WatchAssignmentsAsync(TimeSpan.FromSeconds(2), stopping))
{
    // handle assignment events; the client carries the resume token forward internally
}

// on SIGTERM: drain in-flight work, then:
await host.ShutdownAsync();
```

`HeartbeatOnceAsync` and `ReadyAsync` wait out `503 RUNTIME_HOST_INSTANCE_PENDING`, the relay's
answer while the member has not reported a freshly started Pod yet, for up to
`YogabaseRuntimeHostOptions.StartupPendingBudget` (default 60 seconds), and throw every other
refusal at once. `Connect(onHeartbeatFailure)` reports the failures the background loop otherwise
swallows. See `docs/UNITY_DEDICATED_SERVER.md` for the details.

Two environment variables matter at runtime, both injected by the Yogabase platform into the
workload's process environment:

- `YOGABASE_RUNTIME_HOST_ENDPOINT` — the loopback relay origin, default `http://127.0.0.1:8090`.
- `YOGABASE_SERVICE_PORT_<NAME>` — one per port declared in `yogabase-service.json`'s `ports[]`,
  where `<NAME>` is the port's `name` upper-cased with hyphens turned into underscores.

**Never call an allocation route from the workload.** Allocation placement and release are
backend-only Control Plane operations; a self-allocate call is refused by the relay itself. See
`docs/UNITY_DEDICATED_SERVER.md` for the exact refusal shape.

## IL2CPP stripping

`Runtime/link.xml` preserves this module's assembly plus `System.Text.Json` and `System.Net.Http`
against Medium/High managed code stripping. As noted above, the main historical hazard a
`link.xml` guards against — an attribute-mapped model type reachable only through reflection —
does not really apply to this module, since it uses the JSON DOM throughout. The file is still
included defensively.

## Tests

`Tests/Yogabase.Yogabase.RuntimeHost.Tests.asmdef` uses the modern explicit-reference test-assembly
form — `"references": ["UnityEngine.TestRunner", "UnityEditor.TestRunner"]` plus
`"precompiledReferences": ["nunit.framework.dll"]` and `"defineConstraints": ["UNITY_INCLUDE_TESTS"]`
— rather than the older `"optionalUnityReferences": ["TestAssemblies"]` field, because that is what
Unity's own "Tests Assembly Folder" template still generates in the Unity 6000 LTS line; the older
field is a legacy alias Unity keeps for backward compatibility, not the current default.

The test file is a **minimal smoke test** (options defaults resolve correctly; the client
constructs and disposes without throwing) — it does not port the full .NET behavioral suite (33 of 33
test cases in `YogabaseRuntimeHostClientTests.cs` passed on 2026-09-17). The .NET project's own tests
remain the source of behavioral coverage for this client; treat this package's tests as "does it
compile and wire up correctly inside a Unity assembly," nothing more.

## Verification status and limits

- **Unity import and compilation succeeded.** Unity 6000.5.7f1 imported this local file package
  into the AdaptiveSynce project. Its `Yogabase.Yogabase.RuntimeHost` assembly was visible from the
  dedicated-server assembly, and the project's EditMode suite passed 29/29 tests, including the
  runtime-host assembly-visibility check.
- **The package's own test assembly was not discovered by that runner.** The successful 29-test
  project suite verifies compilation and assembly visibility; it is not a substitute for executing
  `Yogabase.Yogabase.RuntimeHost.Tests` itself.
- **The requested ARM64 IL2CPP Dedicated Server build did not produce an artifact.** Unity's Bee
  input recorded `"Architecture":"x64"`, then hard-stopped with `No Toolchain found for host
  platform` and `Unable to find an Linux Sysroot`. No x64 fallback, Player build, bundle, signature,
  or admission was accepted. Accordingly, `link.xml` has not yet been confirmed in a stripped ARM64
  IL2CPP output.
- **No live relay session has exercised this package.** The startup sequence above mirrors
  `docs/UNITY_DEDICATED_SERVER.md`; live heartbeat, readiness, shutdown, and assignment polling
  still require a valid ARM64 server artifact and a UDP-capable deployed gateway.
- **The Editor publish flow has been compile-checked, not run.**
  `scripts/compile-unity-editor.sh` compiles `Editor/*.cs` against a real Unity 6000.5.7f1 Editor
  reference set at C# 9, warnings as errors, and it passes. No Unity Editor has opened this window,
  no build has been driven through it, and no bundle has been uploaded by it. Its two upstream
  dependencies are also still landing in the product: the Control Plane build surface it posts to
  and the Build check it renders. Treat every claim above as "what the code will do", not as a
  tested result.

Treat this package as a first attempt to get the packaging right, not a proven integration.
