# Yogabase Runtime Host (Unity)

> **Copies, not a fork.** Everything under `Runtime/*.cs` in this package is a byte-for-byte copy
> of the corresponding file in `src/dotnet/Yogabase.Yogabase.RuntimeHost` in this repository. The
> **.NET project is the source of truth** — if you need to change client behavior, change it there
> first, then re-copy the four files here and re-verify with `diff`. This package exists only to
> give Unity a UPM-shaped way to consume that already-tested module; it does not re-implement or
> fork it.

A local, unpublished preview UPM package that carries `Yogabase.Yogabase.RuntimeHost` — the
runtime-host client for a Yogabase-hosted self-contained dedicated server workload — into a Unity
Linux dedicated-server project. See this repository's `docs/UNITY_DEDICATED_SERVER.md` for the full
packaging and admission story (build target, `yogabase-service.json`, signing, ELF/dependency
constraints); this README covers only the client module itself.

## What this package is

- **Package name:** `com.stackyoga.yogabase-runtime-host`
- **Contents:** the runtime-host client (`YogabaseRuntimeHostClient`), its options
  (`YogabaseRuntimeHostOptions`), the JSON-DOM wire models (`RuntimeHostModels.cs`), and an
  `IsExternalInit` compatibility shim (`Compatibility.cs`) needed for the `netstandard2.1` target
  Unity compiles against.
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

## Honest limits

- **No Unity Editor has imported this package.** Nothing here has been opened in the Unity Editor.
  There is no Unity Editor available in the environment that authored this package.
- **No IL2CPP build has exercised it.** The `link.xml` preservation directives above are inferred
  from reading the module's source, not confirmed by an actual stripped IL2CPP Linux Dedicated
  Server build.
- **No live relay session has exercised this package.** The startup sequence above mirrors
  `docs/UNITY_DEDICATED_SERVER.md`, which is itself derived from reading product source, not from a
  working Unity export talking to a running Yogabase installation.
- **The `.asmdef` and `package.json` shapes are believed correct for Unity 6000** based on current
  UPM/Unity Test Framework documentation and convention, not verified by a real Unity import.

Treat this package as a first attempt to get the packaging right, not a proven integration.
