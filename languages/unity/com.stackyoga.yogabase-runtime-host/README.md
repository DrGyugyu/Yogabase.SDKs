# Yogabase Runtime Host and Publisher (Unity)

> **Synced source, not a fork.** The four `Runtime/*.cs` implementation files track the
> corresponding files in `src/dotnet/Yogabase.Yogabase.RuntimeHost` in this repository. They differ
> only in C# namespace syntax: the Unity copies use C# 9 braced namespaces because Unity 6000.5.7f1
> does not accept the .NET source's C# 10 file-scoped namespaces. The **.NET project is the source
> of truth** for behavior — change it first, then re-copy and perform the same syntax-only
> conversion if Unity still requires it. This package only gives Unity a UPM-shaped way to consume
> that module; it does not re-implement client behavior.

A local, unpublished preview UPM package with two halves:

- **`Runtime/`** carries `Yogabase.Yogabase.RuntimeHost`, the runtime-host client a hosted
  dedicated server uses to heartbeat, signal Ready, poll assignments, and drain on shutdown.
- **`Editor/`** adds `Yogabase ▸ Publish Server`: build the Linux dedicated server, zip it, upload
  it, and read the Build check — one window, no terminal step.

See this repository's `docs/UNITY_DEDICATED_SERVER.md` for the whole flow and for what the runtime
side must do; this README covers the package itself.

## What this package is

- **Package name:** `com.stackyoga.yogabase-runtime-host`
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

This package is **not published** to any UPM registry, npm registry, or Git URL — publication is
not authorized. To use it locally:

- Reference it from a Unity project's `Packages/manifest.json` with a local `file:` path pointing
  at this directory, e.g.:
  ```json
  "com.stackyoga.yogabase-runtime-host": "file:../relative/path/to/languages/unity/com.stackyoga.yogabase-runtime-host"
  ```
- Or copy this directory directly into the target project's `Packages/` folder.

The folder is **import-ready as-is**: it deliberately contains no hand-written `.meta` files.
Unity generates `.meta` files (with fresh GUIDs) the first time it imports the package — do not
fabricate them ahead of time, since a hand-guessed GUID risks colliding with something else in the
target project.

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
constructs and disposes without throwing) — it does not port the full .NET behavioral suite
(60/60 passing there as of this writing). The .NET project's own tests remain the source of
behavioral coverage for this client; treat this package's tests as "does it compile and wire up
correctly inside a Unity assembly," nothing more.

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
