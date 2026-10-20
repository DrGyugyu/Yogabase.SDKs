# Unity dedicated server on Yogabase

Take a Unity project to a running, reachable game server on Yogabase. There is one step:

**`Yogabase ▸ Publish Server`** in the Unity Editor, or `yogabase build upload` from the CLI.

That is the whole packaging story now. You do not write a service layout document, generate a
software bill of materials, run a vulnerability scanner, obtain a signing key, or produce a detached
ECDSA signature. **Yogabase produces the delivery evidence**: it
derives the layout manifest and a CycloneDX inventory from the bundle you hand it, scans that
inventory with its own scanner against a digest-pinned advisory snapshot, and signs manifest,
inventory, and scan together with the installation admission key. A customer signature is optional
and lives under the console's Advanced area.

> **Status.** The Control Plane build surface this window posts to and the `/v1/servers/*` surface
> a client calls are both implemented by the product, and this SDK is aligned to them:
> `contracts/yogabase-builds-v1.openapi.json` and the game-server half of
> `contracts/yogabase-client-v1.openapi.json` are byte-identical snapshots of the product's
> published OpenAPI documents, and the Editor scripts read exactly the shapes those routes write.
> Nothing below has yet been driven end to end by a Unity Editor against a live installation. See
> [Honest limits](#honest-limits) — it is short, and it is the part to read before planning around
> this.

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
  --source Builds/LinuxServer \
  --executable Server.x86_64 \
  --port game=7777/udp \
  --name battle --version 1.4.0
```

Both paths post the same multipart form to the same route:

```text
POST /api/v1/projects/{projectId}/environments/{environmentId}/builds
Authorization: Bearer <developer token>

bundle      the zip — the only required part
manifest    optional pre-signed manifest; neither the Editor nor the .NET client sends one
executable  path inside the zip, optional
ports       repeated, one per port, name=7777/udp
name        optional build name
version     optional version label

202 Location: /api/v1/projects/{p}/environments/{e}/builds/{buildId}
    { "buildId": "<uuid>", "digest": "sha256:...", "state": "checking",
      "identity": "battle:1.4.0", "attempt": 1, "kind": "SelfContainedService",
      "check": { "evidenceOrigin": "yogabase-derived", "passed": false, ... } }
```

The developer token starts with `ybt_` and is at most 128 characters; any other bearer is not a
developer token to the Control Plane and gets a bare `401`. The build id is a UUID — the route
constrains it to one — and the Editor refuses anything else before sending it.

Then poll every three seconds with backoff:

```text
GET /api/v1/projects/{p}/environments/{e}/builds/{buildId}
200 { "buildId", "digest", "state": "checking|admitted|refused",
      "identity", "attempt", "kind",
      "check": { "evidenceOrigin": "customer-supplied|yogabase-derived",
                 "inventoryDigest", "scanDigest", "databaseDigest",
                 "passed", "observedAt",
                 "findings":  [ { "id", "package", "version", "fixedIn", "severity", "entry" } ],
                 "refusals":  [ { "code", "field", "cause" } ] } }

POST /api/v1/projects/{p}/environments/{e}/builds/{buildId}/recheck
202 the same build, checking again for the same bytes
```

A refusal of the request itself (a bundle that is not a readable zip, a missing token scope, an
unknown build) is an RFC 7807 problem with a stable upper-case `code` such as
`BUILD_BUNDLE_UNRECOGNIZED`, `BUILD_TOKEN_SCOPE_REQUIRED` or `BUILD_NOT_FOUND`, the `detail`, a
`correlationId`, and `fieldErrors` naming the multipart field to correct.

The full shape is
[`contracts/yogabase-builds-v1.openapi.json`](../contracts/yogabase-builds-v1.openapi.json), a
byte-identical snapshot of the product's published build-ingest OpenAPI document.

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

**A refusal names what to fix.** A structural refusal carries a stable `code`, the `field` it is
about (or empty), and a bounded `cause` token — never prose and never a value from your bundle. A
vulnerability refusal carries the advisory id, the package, the version found, the first fixed
version, and the bundle entry it came from. Correct the build and publish again — a corrected build
is a new upload that takes the next `attempt` of the same identity and supersedes the refused one;
a build refused only because the advisory snapshot has since moved on can be re-checked in place.

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
POST /v1/servers/start   { "service": "battle", "customData": { "mode": "ranked" } }
200  { "serverId": "...", "state": "Allocated",
       "networkPorts": [ { "name": "game", "internalPort": 7777,
                           "externalPort": 31245, "protocol": "udp",
                           "host": "10.0.0.7" } ] }
```

`GET /v1/servers` (a bare array, ordered by server id), `GET /v1/servers/{serverId}`, and
`POST /v1/servers/{serverId}/stop` (the instance's state after the release; never a deletion)
complete the surface. Authentication is the **player's** access token — a different credential and
a different origin from the developer token above. A refusal is an RFC 7807 problem whose `code`
is `server_none_available`, `server_contended`, `server_not_found`,
`server_address_not_observed`, `server_stop_not_available`, `server_request_invalid`,
`server_authentication_required` or `server_unavailable`. The `service` is the hosted service's
canonical id; `customData` is at most 32 entries and 4 KiB, and it reaches your server process, so
it must never carry a credential or another player's identity.

The .NET client implements this as `YogabaseClient.Servers`, and every generated client
(TypeScript, Python, Java, Dart) carries it under `v1.servers`, because the surface is part of the
mirrored application contract
[`contracts/yogabase-client-v1.openapi.json`](../contracts/yogabase-client-v1.openapi.json).

An allocation that arrives without a usable address is refused by the .NET client rather than
returned: a caller that trusts an empty host dials its own machine. A start never answers such an
instance anyway — the Control Plane refuses with `server_none_available` instead — but a listing
does include instances whose address no member has observed yet, with empty `networkPorts`.

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
into Unity by `languages/unity/io.yogabase.runtime-host`. It is a faithful, modified C#
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

**The first calls may be told to wait.** A Pod's first heartbeat or `Ready` can reach the relay
before the member has reported that Pod, and the relay answers
`503 RUNTIME_HOST_INSTANCE_PENDING` with a `retryAfterSeconds` hint. That is the platform's own
timing, not a fault in your build, so `HeartbeatOnceAsync` and `ReadyAsync` wait it out (the hint,
or 2 seconds) until `YogabaseRuntimeHostOptions.StartupPendingBudget` is spent (default 60 seconds)
and only then throw. Every other refusal (`RUNTIME_HOST_STALE_EPOCH`,
`RUNTIME_HOST_SHUTDOWN_IN_PROGRESS`, `RUNTIME_HOST_TERMINATED`, and the rest) is thrown on the first
attempt. A build that quits on an exception from these calls therefore no longer quits during a
Pod's normal startup window, and still quits at once for a terminal refusal.
The relay also answers `413`, `429` and `504` with no body at all; each throws
`YogabaseRuntimeHostException` with that status and the code `UNKNOWN`, and `IsRetryable` is true
for the pending refusal, `429` (64 calls already waiting) and `504` (no answer within 30 seconds, so
the call may or may not have taken effect) and false for `413`. Nothing waits those two out for you.
The background loop keeps running after a failed heartbeat; pass a callback,
`runtime.Connect(exception => Log(exception.Message))`, to see those failures.

**A call is bounded by `RequestTimeout`, which by default outlasts the relay's own timeout.** The
default is the relay's 30 second answer timeout plus 10 seconds, so a call the relay cannot get
answered ends with its `504`. If the timeout does fire, the call throws `YogabaseRuntimeHostException`
with a status of 0 and the code `CLIENT_REQUEST_TIMEOUT` (`IsRetryable` true; the call may or may not
have taken effect), never a bare `TaskCanceledException`. Only the token you pass in cancels as
`OperationCanceledException`.

Three details of the watch loop are worth knowing before you write your own:

- **The resume token is advance-only.** The relay returns an empty token when it has no newer
  position to give you. Treat empty as "keep what you have", as the sample does.
- **Cancellation propagates out of the fetch.** If you use the convenience
  `WatchAssignmentsAsync(poll, token)` enumerator, cancelling during the poll *delay* ends the
  sequence quietly, but cancelling while a request is in flight throws
  `OperationCanceledException` out of the `await foreach`. That is deliberate and matches the
  heartbeat loop's contract — wrap the loop if you would rather treat shutdown as a normal exit.
- **The convenience enumerator ends on a terminal refusal and retries only what can be retried.**
  `WatchAssignmentsAsync(poll, onFailure, token)` throws the `YogabaseRuntimeHostException` out of the
  `await foreach` for a refusal the contract makes terminal on the assignments route
  (`RUNTIME_HOST_RELAY_INVALID`, `RUNTIME_HOST_REQUEST_INVALID`, `RUNTIME_HOST_RESUME_TOKEN_INVALID`,
  `RUNTIME_HOST_STALE_EPOCH`, `RUNTIME_HOST_WRONG_GENERATION`). A retryable answer
  (`IsRetryable`) or a transport fault is tried again on the next tick, and anything else is passed
  once to `onFailure` while the watch keeps polling. A hand-written loop should make the same split.

Two environment variables matter:

- `YOGABASE_RUNTIME_HOST_ENDPOINT` — the loopback relay origin, default `http://127.0.0.1:8090`.
- `YOGABASE_SERVICE_PORT_<NAME>` — one per declared port, where `<NAME>` is your port's `name`
  upper-cased with hyphens turned into underscores (e.g. a port named `game` becomes
  `YOGABASE_SERVICE_PORT_GAME`).

**Never call an allocation route from the workload.** Allocation placement and release are
Control Plane-authoritative, backend-only operations; a call to `/runtime-host/v1/self/allocate`
(under either method) is refused at the relay itself, and no `/allocations` route is mapped on it.
The refusal is:

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
builds and parses its wire payloads with `System.Text.Json.Nodes` (`JsonObject`/`JsonNode`) — a
document-object-model API — rather than an attribute-driven, reflection-based serializer. There is no `[JsonPropertyName]`-style contract for
the stripper to silently break.

## Honest limits

- **Nothing here has been run end to end.** The Editor window compiles against a real Unity
  6000.5.7f1 Editor reference set (`scripts/compile-unity-editor.sh`, C# 9, warnings as errors) and
  that is the whole of its verification. No Unity Editor has opened it, no build has been driven
  through it, and no bundle has been uploaded by it.
- **Its two upstream surfaces are implemented.** The Control Plane build surface and the
  `/v1/servers/*` allocation surface exist in the product, and `contracts/` mirrors their published
  documents byte for byte. What has not happened is a Unity Editor publishing through them, or a
  game client dialing an address they returned.
- **Per-instance addressing is new.** Each instance gets a host port and its node's address, and
  the client-facing `/v1/servers/*` surface returns `host:port`; before 2026-09-08 only the service
  as a whole was reachable, through a small pool of gateway relay listener ports. The host-port
  range and node firewall this needs are an open item, with a NodePort-per-instance fallback where
  host ports are forbidden.
- **The installation, not the bundle, can still refuse a correct build.** Artifact object storage
  must be configured and the hosted-services module switch must be on. Those refusals are
  independent of whether your bundle passes every rule above.
- **The executable-bit caveat is inference, not a confirmed refusal.** The Editor tool writes `0755`
  into the zip's external attributes for the player binary and native libraries because a bundle
  that loses that bit cannot execute at runtime. The server publishes no admission refusal code for
  it, so it is a plausible failure mode this tooling avoids rather than one the server independently
  catches.
- **Build check coverage is bounded in v1.** NuGet packages, the .NET runtime, Unity player files,
  and ELF SONAMEs against a pinned OSV snapshot. OS-package databases come later. Anything
  uncatalogued is reported as uncatalogued.

## What the server side provides

Everything in this document rests on four capabilities of a Yogabase installation. The SDK
implements the client half of each; the server half is the product's.

- **Build ingestion.** The Control Plane accepts one multipart upload per build, stages the
  archive, derives the service layout and a manifest from it, and has the member's isolated
  admission Job derive the CycloneDX inventory, scan it against the digest-pinned advisory snapshot
  and sign manifest, inventory and scan together. The routes, multipart fields, response shape and
  refusal codes are the published build-ingest contract mirrored at
  `contracts/yogabase-builds-v1.openapi.json`.
- **Developer-token authentication.** A project-scoped `ybt_` token, issued from the console and
  held by the server only as a hash, authorizes build uploads to one project and nothing else: it is
  not a player credential, it does not reach the application gateway, and it carries no cloud or
  Kubernetes authority.
- **Game-server endpoints.** The application gateway's `/v1/servers/*` routes let a signed-in
  player allocate a Ready instance, list and read the environment's instances and release an
  allocation, answering the address the installation advertises for the instance's node and the
  ports the member observed. Their bounds and codes are in the mirrored application contract,
  `contracts/yogabase-client-v1.openapi.json`.
- **Runtime-host contract.** A Pod-local relay exposes the five self-scoped loopback routes a
  hosted workload uses to heartbeat, signal Ready, be reserved, read assignments and drain; the
  Control Plane resolves which instance is calling from the member's own Pod observation, never
  from anything the workload asserts, and refuses allocation from the workload. The lifecycle states
  and codes are the published document mirrored at `contracts/yogabase-runtime-host-v1.openapi.json`.

The bundle rules in [What still refuses a build](#what-still-refuses-a-build) are the admission
rules the installation applies to every uploaded bundle, whichever tool uploaded it.
