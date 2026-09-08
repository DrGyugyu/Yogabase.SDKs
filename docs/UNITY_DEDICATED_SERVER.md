# Unity dedicated server on Yogabase

Take a Unity project to a running, reachable game server on Yogabase. There is one step:

**`Yogabase ▸ Publish Server`** in the Unity Editor, or `yogabase build upload` from the CLI.

That is the whole packaging story now. You do not write a service layout document, generate a
software bill of materials, run a vulnerability scanner, obtain a signing key, or produce a detached
ECDSA signature. **Yogabase produces the delivery evidence** (owner decision, 2026-09-08): it
derives the layout manifest and a CycloneDX inventory from the bundle you hand it, scans that
inventory with its own scanner against a digest-pinned advisory snapshot, and signs manifest,
inventory, and scan together with the installation admission key. A customer signature is optional
and lives under the console's Advanced area.

> **Status.** The publish flow described here is being implemented across the product and this SDK
> repository at the same time. The Unity Editor window exists and compiles; the Control Plane build
> surface it posts to is landing in parallel. Nothing below has been driven end to end by a Unity
> Editor against a live installation. See [Honest limits](#honest-limits) — it is short, and it is
> the part to read before planning around this.

## The publish flow

1. **Create the settings asset.** `Assets ▸ Create ▸ Yogabase ▸ Settings`. Fill in the Control Plane
   URL, the project and environment ids from the console, the service name, the executable name
   (`Server.x86_64` by default), the target architecture, and the ports your server listens on.
   Commit it: everything on it is project configuration.

2. **Paste a developer token.** In the console, open **Settings** for the project and issue a
   *developer token*. Paste it into the settings asset's inspector. It is stored in this machine's
   `EditorPrefs`, keyed by project id, and **never written into the asset** — a serialized field
   would commit a secret to the repository and hand it to everyone who clones the project. The
   server holds only a hash of it.

   The token authorizes uploading builds to one project. It is not a player credential, it does not
   reach the application gateway, and it carries no cloud or Kubernetes authority.

3. **Publish.** `Yogabase ▸ Publish Server` → **Build and publish**. The window builds the Linux
   Dedicated Server, verifies the binary, zips it, uploads it, and shows the Build check result.
   Tick *Upload the existing build output without rebuilding* to re-upload after fixing something
   without paying for another IL2CPP build.

From a terminal instead:

```text
yogabase build upload \
  --project <project-id> --environment <environment-id> \
  --bundle Builds/LinuxServer \
  --executable Server.x86_64 \
  --port game=7777/udp \
  --name battle --version 1.4.0
```

Both paths post the same multipart form to the same route:

```text
POST /api/v1/projects/{projectId}/environments/{environmentId}/builds
Authorization: Bearer <developer token>

bundle      the zip
executable  path inside the zip, optional
ports       repeated, one per port, name=7777/udp
name        optional build name
version     optional version label

202 { "buildId": "...", "digest": "sha256:...", "state": "checking" }
```

and then poll every three seconds with backoff:

```text
GET /api/v1/projects/{p}/environments/{e}/builds/{buildId}
200 { "buildId", "digest", "state": "checking|admitted|refused",
      "check": { "passed", "findings": [...], "refusals": [...] } }
```

The full shape is written down in
[`contracts/planned/yogabase-builds-v1.openapi.json`](../contracts/planned/yogabase-builds-v1.openapi.json)
— marked `planned` because the product is implementing it in parallel, so it is a description of the
agreed contract rather than a mirrored product snapshot.

## What "Build check" means

The Build check is Yogabase's own pass over the bundle you uploaded. It:

- reads the archive's layout and derives the service manifest, so you do not write one;
- catalogues the components it can identify — NuGet packages, the .NET runtime, Unity player files,
  ELF SONAMEs — into a CycloneDX inventory;
- matches that inventory against a **digest-pinned OSV snapshot** carried inside the Tasks image,
  updatable only through the console with the new digest recorded;
- signs manifest, inventory, and scan together with the installation admission key.

**A pass means one specific thing**: no Critical or High finding among the components Yogabase could
catalogue, in that pinned snapshot, on its recorded date. It is never a claim that the build is
secure. Nothing in the bundle is executed. Statically linked and `dlopen`ed libraries are opaque.
Native OS packages are matched only once an OS-package database is carried. Anything uncatalogued is
reported as uncatalogued, never as clean.

**A refusal names what to fix.** A structural refusal carries a code, the failing field or bundle
path, and a message. A vulnerability refusal carries the advisory id, the package, the version
found, the first fixed version, and the bundle entry it came from. Correct the build and publish
again — a corrected build is a new upload, not an override.

The scanner is a C# translation of OSV-Scanner's version comparison and affected-range matching and
of Syft's .NET/ELF cataloguing approach. **No foreign scanner image ships or runs**: Yogabase runs
no third-party binary on your bundle.

## What the Editor window does for you

- **Builds the Dedicated Server subtarget, not a Player build with `-nographics`.** A Player build
  still links the desktop graphics and audio stack, and those libraries are not in Yogabase's
  base-image allowlist, so its dependency closure will not resolve. The window sets
  `StandaloneBuildSubtarget.Server` and IL2CPP explicitly, and restores your project settings
  afterwards.
- **Sets the architecture where Unity actually reads it.**
  `UnityEditor.LinuxStandalone.UserBuildSettings.architecture` is the only input the Linux player
  build consults; `PlayerSettings.SetArchitecture` and
  `EditorUserBuildSettings.SetPlatformSettings("Standalone", "Architecture", …)` are not in that
  chain at all. A build that sets only those reports success and emits an x86-64 binary under an
  ARM64 name — which is exactly what happened on 2026-09-07. That property lives in the Linux
  module's extension assembly, so it is reached by reflection, and its absence is a refusal rather
  than a silent skip.
- **Checks the ELF before uploading.** It reads the built binary's `e_machine` and refuses when it
  disagrees with the declared architecture, so a mismatch costs a local check instead of a round
  trip.
- **Keeps the executable bit.** The zip carries `0755` on the declared executable, on `*.x86_64` /
  `*.aarch64`, and on `*.so*`. Without it a Unity Linux dedicated server will not start, and a zip
  written by a tool that discards Unix mode bits is the usual way that bit is lost.
- **Points the entrypoint at the ELF, never a script.** A `.sh` entrypoint is refused as a foreign
  runtime; the settings asset refuses one before you ever build.

## What still refuses a build

These are properties of the bundle, not of the tooling, so they apply to the CLI path too.

| Rule | Why |
|---|---|
| Entrypoint must be the ELF binary, not a `.sh`/`.py`/`.js` wrapper or a `MZ`/`CAFEBABE` file | Yogabase ships no foreign runtime; a script entrypoint is refused naming the runtime it would need. |
| `PT_INTERP` absent, or exactly the base image's glibc loader (`/lib64/ld-linux-x86-64.so.2`, `/lib/ld-linux-aarch64.so.1`) | A musl or relocated loader will not exist in the runtime image. A stock Unity IL2CPP Linux build passes unmodified. |
| Every `DT_NEEDED` resolves in-bundle or against the image-pinned SONAME allowlist | This is what a Player build fails: `libX11`, `libGL`, `libasound` and the rest of the desktop stack are not on that list. `UnityPlayer.so` and `GameAssembly.so` resolve because they sit next to the entrypoint. |
| ELF64 little-endian, `e_machine` matching the declared architecture | A binary that disagrees with its declaration would run under emulation or not at all. |
| No symlinks, no setuid/setgid entries | Neither has a legitimate use in a game-server bundle, and both are attack surface. |
| No secret-shaped content in entries, arguments, or environment values | Do not bake API keys or connection strings into a shipped config asset. Fetch them at runtime instead. |
| Ports: 1–8, unique names and numbers | Each declared port becomes an address a client can be handed. |

## Getting an address for a client

A hosted-service instance is one Pod with a host port (NodePort per instance where host ports are
forbidden) and its node's address. A game client asks the application gateway for one:

```text
POST /v1/servers/start   { "service": "battle" }
200  { "serverId": "...", "state": "Allocated",
       "networkPorts": [ { "name": "game", "internalPort": 7777,
                           "externalPort": 31245, "protocol": "udp",
                           "host": "10.0.0.7" } ] }
```

`GET /v1/servers`, `GET /v1/servers/{id}`, and `POST /v1/servers/{id}/stop` complete the surface.
Authentication is the **player's** access token — a different credential and a different origin from
the developer token above. The .NET client implements this as `YogabaseClient.Servers`; the shape is
written down in
[`contracts/planned/yogabase-servers-v1.openapi.json`](../contracts/planned/yogabase-servers-v1.openapi.json).

An allocation that arrives without a usable address is refused by the client rather than returned: a
caller that trusts an empty host dials its own machine.

## Readiness: the SDK is optional

A build that never calls the runtime-host SDK still works. When the service declares
`readiness: port`, the instance becomes Ready as soon as its declared port opens — that is the whole
contract, and it is enough for a server that just binds a socket and listens.

Use the runtime-host client when you want more than that: to signal readiness only once your own
warm-up is finished, to be reserved rather than allocated, to see assignments, or to drain
gracefully on `SIGTERM` instead of being killed mid-match. The section below is that path.

## Runtime host: heartbeat, Ready, assignments, drain

**This section is optional.** A build that declares `readiness: port` becomes Ready when its
declared port opens and needs none of what follows. Use the client when you want to control
readiness yourself, be reserved, see assignments, or drain on `SIGTERM`.

The C# API is `YogabaseRuntimeHostClient` in the `Yogabase.Yogabase.RuntimeHost` package, carried
into Unity by `languages/unity/com.stackyoga.yogabase-runtime-host`. It is a faithful, modified C#
derivative of Agones' `sdks/csharp/sdk/AgonesSDK.cs` client-loop shape, retargeted to Yogabase's
self-scoped, instance-id-free wire contract; the copying boundary is recorded in
[`REFERENCE_REVIEW.md`](REFERENCE_REVIEW.md).

Startup sequence:

1. One synchronous heartbeat before anything else, so the supervisor sees the process is alive.
2. Start the background heartbeat loop.
3. Bind your declared listener(s) on the port(s) your settings declare.
4. Signal `Ready`.
5. Poll for assignments, always carrying forward the last-seen resume token.
6. On `SIGTERM`, drain in-flight work, then call `Shutdown`.

```csharp
// The default constructor resolves YOGABASE_RUNTIME_HOST_ENDPOINT itself and falls back to
// http://127.0.0.1:8090, so there is nothing to wire up. Pass a YogabaseRuntimeHostOptions
// instead only if you want a non-default heartbeat interval or request timeout.
await using var runtime = new YogabaseRuntimeHostClient();

await runtime.HeartbeatOnceAsync();
runtime.Connect();

var port = int.Parse(Environment.GetEnvironmentVariable("YOGABASE_SERVICE_PORT_GAME")!);
StartListening(port);

await runtime.ReadyAsync();

var resumeToken = string.Empty;
while (!stopping.IsCancellationRequested)
{
    var page = await runtime.WatchAssignmentsOnceAsync(resumeToken, stopping.Token);

    // An empty resume token means "no new position" -- keep the one you already hold.
    // Assigning unconditionally would silently rewind the stream to the beginning and
    // replay assignments you have already handled.
    if (!string.IsNullOrEmpty(page.ResumeToken))
    {
        resumeToken = page.ResumeToken;
    }

    HandleAssignments(page.Events);
    await Task.Delay(TimeSpan.FromSeconds(2), stopping.Token);
}

await DrainAsync();
await runtime.ShutdownAsync();
```

Two details of the watch loop are worth knowing before you write your own:

- **The resume token is advance-only.** The relay returns an empty token when it has no newer
  position to give you. Treat empty as "keep what you have", as the sample does.
- **Cancellation propagates out of the fetch.** If you use the convenience
  `WatchAssignmentsAsync(poll, token)` enumerator, cancelling during the poll *delay* ends the
  sequence quietly, but cancelling while a request is in flight throws
  `OperationCanceledException` out of the `await foreach`. That is deliberate and matches the
  heartbeat loop's contract — wrap the loop if you would rather treat shutdown as a normal exit.

Two environment variables matter:

- `YOGABASE_RUNTIME_HOST_ENDPOINT` — the loopback relay origin, default `http://127.0.0.1:8090`.
- `YOGABASE_SERVICE_PORT_<NAME>` — one per declared port, where `<NAME>` is your port's `name`
  upper-cased with hyphens turned into underscores (e.g. a port named `game` becomes
  `YOGABASE_SERVICE_PORT_GAME`).

**Never call an allocation route from the workload.** Allocation placement and release are
Control Plane-authoritative, backend-only operations; a self-allocate call is refused at the relay
itself with:

```json
{ "code": "RUNTIME_HOST_RELAY_ALLOCATION_BACKEND_ONLY", "detail": "Allocation placement is a backend-only Control Plane operation; a workload cannot self-allocate." }
```

returned as HTTP `403`. Relatedly, the workload never learns its own instance id — every relay
route is self-scoped (`/runtime-host/v1/self/{heartbeat|ready|reserve|shutdown|assignments}`); the
Control Plane resolves which instance is calling from the Agent-stamped Pod binding, not from
anything the workload asserts.

## IL2CPP stripping

Unity's IL2CPP pipeline AOT-compiles your project's managed code, including whatever
`Yogabase.Yogabase.RuntimeHost` module you reference, and Unity's managed code stripping (Medium or
High) can remove types and members that are only reached through reflection if they are not
referenced elsewhere and not preserved by a `link.xml`. The package intends to ship a `link.xml`
defensively, but the main hazard it would otherwise guard against does not apply here: the module
mirrors `HostedServiceFixtureClient.cs`, which builds and parses its wire payloads with
`System.Text.Json.Nodes` (`JsonObject`/`JsonNode`) — a document-object-model API — rather than an
attribute-driven, reflection-based serializer. There is no `[JsonPropertyName]`-style contract for
the stripper to silently break.

## Honest limits

- **Nothing here has been run end to end.** The Editor window compiles against a real Unity
  6000.5.7f1 Editor reference set (`scripts/compile-unity-editor.sh`, C# 9, warnings as errors) and
  that is the whole of its verification. No Unity Editor has opened it, no build has been driven
  through it, and no bundle has been uploaded by it.
- **Its two upstream dependencies are landing in parallel.** The Control Plane build surface this
  posts to and the `/v1/servers/*` allocation surface a client calls are being implemented in the
  product repository at the same time as this document. Their shapes live under
  `contracts/planned/` and are marked `planned` for exactly that reason: they describe the agreed
  contract, not a published product snapshot. If a field name here disagrees with the product when
  it lands, the product wins.
- **Per-instance addressing is new.** Each instance getting a host port and its node's address, and
  a client-facing `/v1/servers/*` surface returning `host:port`, is the 2026-09-08 owner decision —
  it supersedes the earlier position that only the service as a whole was reachable through a small
  pool of gateway relay listener ports. The host-port range and node firewall this needs are an open
  item, with a NodePort-per-instance fallback where host ports are forbidden.
- **The installation, not the bundle, can still refuse a correct build.** Artifact object storage
  must be configured and the hosted-services module switch must be on. Those refusals are
  independent of whether your bundle passes every rule above.
- **The executable-bit caveat is inference, not a confirmed refusal.** The Editor tool writes `0755`
  into the zip's external attributes for the player binary and native libraries because a bundle
  that loses that bit cannot execute at runtime. No admission refusal code was found for it in the
  product source, so it is a plausible failure mode this tooling avoids rather than one the server
  independently catches.
- **Build check coverage is bounded in v1.** NuGet packages, the .NET runtime, Unity player files,
  and ELF SONAMEs against a pinned OSV snapshot. OS-package databases come later. Anything
  uncatalogued is reported as uncatalogued.

## Sources read for this document

The bundle rules and runtime-host contract below were read out of the product repository. The
publish flow above comes from the 2026-09-08 owner decisions recorded in that repository's
`docs/PRODUCT_CONTEXT.md`, whose implementation is in progress:

- `src/Yogabase.Core/Delivery/HostedServiceBundleContract.cs`
- `src/Yogabase.Core/Delivery/HostedServiceBaseLibraryAllowlist.cs`
- `src/Yogabase.Core/AppPlatform/Apps/AppArtifactContracts.cs`
- `src/Yogabase.Core/AppPlatform/Runtime/YogaApplicationRuntime.cs`
- `src/Yogabase.Core/RuntimeHost/RuntimeHostRelayContracts.cs`
- `src/Yogabase.Provisioner/Job/SelfContainedServiceBundleInspector.cs`
- `src/Yogabase.Tasks/AppPlatform/Hosting/RuntimeHostRelayProcess.cs`
- `tools/Yogabase.Operator.Conformance/HostedServiceFixtureClient.cs`
- `docs/PRODUCT_CONTEXT.md` — the 2026-09-08 platform-simplification decisions, items 4 (Yogabase
  produces the delivery evidence) and 6 (game servers get an address). Every ADR in that repository
  was retired on 2026-09-06; `docs/history/decisions/0059-hosted-self-contained-service-workloads.md`
  survives as background only and carries no gate.
