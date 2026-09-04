# Unity dedicated server on Yogabase

This document describes how to package a Unity Linux dedicated server (IL2CPP) so it is admitted
by Yogabase's hosted-service artifact admission and can run under the Control Plane. It is derived
from reading the product source listed at the end of this document, not from running a real Unity
build against Yogabase. Treat every claim here as "what the source says will happen," not as a
tested result — see [Honest limits](#honest-limits).

**Status of the capability itself.** Both compiled qualification gates are **open**:
`HostedServiceQualificationEvidence.V1Accepted` and `RuntimeHostLifecycle.V1Accepted` are each
`true`, opened by two consecutive passing local-conformance runs covering 27 cases. So the
hosted-service pipeline and the runtime-host lifecycle are live rather than gated off.

What remains conditional is the *installation*, not the capability. A hosted `SelfContainedService`
upload is refused unless the target installation's provider identity is container-scoped, and the
installation must have artifact object storage configured and the `extended.hostedServices` module
switch enabled (it defaults off and requires `extended.sandbox`). A self-hosted installation is
refused at upload-intent time for that reason.

## Quick checklist

1. Build with Unity's **Dedicated Server** build target for Linux (not a Player build with
   `-nographics`) — see [why](#rule-4-dependency-closure-dedicated-server-not-player--nographics).
2. Zip the build output with `yogabase-service.json` (schema version 1) at the **root** of the
   archive — not inside a wrapping folder.
3. Point `launch.entrypoint` at the **ELF binary** Unity produces (e.g. `Server.x86_64`), never at
   a `.sh` wrapper script.
4. Do not touch `PT_INTERP` — a normal IL2CPP Linux build's glibc dynamic loader is on the
   allowlist as-is; a musl or relocated loader is not.
5. Set the execute bit on the entrypoint's zip external attributes; include no symlinks; use no
   setuid/setgid bits.
6. Produce a detached manifest containing a byte-identical copy of `yogabase-service.json` under
   `service` and an ECDSA P-256/SHA-256 signature over it, per [Packaging](#packaging).
7. In-game code drives lifecycle through the SDK's `YogabaseRuntimeHostClient` (a separate worker
   in this repository is authoring it): heartbeat, `Ready`, poll assignments, `Shutdown` on
   `SIGTERM`. Never call an allocation route yourself.

## 1. What you are uploading

A Unity Linux dedicated server is uploaded as `AppArtifactKind.SelfContainedService` — **a zip
archive**, not a container image. Yogabase never builds or pulls a customer-supplied container
image for this kind; there is no image-build path at all. The doc comment on the artifact-kind
enum is explicit about this:

```csharp
/// <summary>
/// Inert desktop player output. A Unity Linux dedicated server that should run is uploaded as
/// <see cref="SelfContainedService"/>, never as this kind.
/// </summary>
UnityDesktop,
```

(`src/Yogabase.Core/AppPlatform/Apps/AppArtifactContracts.cs`)

`SelfContainedService` is the second (and, as of this source read, last) artifact kind that is
ever allowed to execute on Yogabase compute — the first is the sandboxed `.NET` Lambda kind, which
does not apply here. `SelfContainedService` covers "a build that carries its own runtime": a
self-contained .NET publish, a NativeAOT build, a Unity IL2CPP Linux dedicated server, or a Go/Rust
binary. The admission Job inspects ELF headers and dynamic sections; it never links, loads,
`dlopen`s, or executes anything in the bundle.

## 2. The `yogabase-service.json` layout

This is schema version 1 (`HostedServiceBundleContract.LayoutSchemaVersion = 1`). It must sit at
the archive root under the exact entry name `yogabase-service.json`
(`HostedServiceBundleContract.LayoutEntryName`).

```json
{
  "schemaVersion": 1,
  "serviceId": "match-server",
  "version": "1.4.2",
  "targetArchitecture": "linux-x64",
  "launch": {
    "kind": "Executable",
    "entrypoint": "Server.x86_64",
    "arguments": ["-batchmode", "-nolog"],
    "environment": [
      { "name": "MATCH_TICK_RATE", "value": "30" }
    ],
    "workingDirectory": ""
  },
  "ports": [
    { "name": "game", "port": 7777 }
  ],
  "health": {
    "periodSeconds": 5,
    "failureThreshold": 3,
    "initialDelaySeconds": 5
  },
  "terminationGraceSeconds": 30
}
```

Field-by-field:

| Field | Meaning |
|---|---|
| `schemaVersion` | Must be exactly `1`. |
| `serviceId` | Stable service identifier; see the pattern in the constraints table. |
| `version` | Your own build/version string. |
| `targetArchitecture` | `linux-x64` or `linux-arm64` — must match the entrypoint ELF's actual `e_machine`. |
| `launch.kind` | `Executable` for a Unity IL2CPP build (the entrypoint carries its own runtime). The other value, `DotNetFrameworkDependent`, is for a managed `.dll` launched by the product's own `dotnet` host — not applicable to Unity. |
| `launch.entrypoint` | Path (zip entry name) to the ELF binary Unity produces. **Not** a `.sh` script. |
| `launch.arguments` | Up to 32 command-line arguments passed to the entrypoint. |
| `launch.environment` | Up to 64 non-secret `name`/`value` pairs injected into the child's environment. Names must not start with `YOGABASE_` or `LD_` — those are reserved/forbidden. |
| `launch.workingDirectory` | Optional working directory, relative to the bundle root. |
| `ports` | 1–8 named TCP ports the process listens on. |
| `health` | Liveness/readiness cadence. Non-positive values fall back to Agones-derived defaults (`periodSeconds` 5, `failureThreshold` 3, `initialDelaySeconds` 5) before validation runs — there is deliberately no "disabled" switch; readiness is always evidence-gated. |
| `terminationGraceSeconds` | How long the supervisor waits after `SIGTERM` before `SIGKILL`. |

The canonical-JSON form of this document must **byte-equal** the `service` object inside the
detached signed manifest (see [Packaging](#packaging)). Any difference — including a field the
signer never saw — is refused as `artifact-service-layout-manifest-mismatch`.

## 3. Constraints table

All values are from `src/Yogabase.Core/Delivery/HostedServiceBundleContract.cs` unless noted.

| Constraint | Rule | Refusal code |
|---|---|---|
| Bundle size (compressed) | ≤ 1 GiB (`1024×1024×1024` bytes) | `artifact-service-bundle-too-large` |
| Bundle size (expanded) | ≤ 4 GiB (`4096×1024×1024` bytes) | `artifact-service-expanded-too-large` |
| Entry count | 1–20,000 | `artifact-service-too-many-entries` |
| Compression ratio | expanded ≤ 100 × compressed, per entry | `artifact-service-compression-ratio-exceeded` |
| `yogabase-service.json` size | 1 byte – 64 KiB | `artifact-service-layout-invalid: size` |
| Entry name length | ≤ 1024 chars; printable ASCII, no `\ : * ? " < > \|`, no leading/trailing space, no `.`/`..` segment, no leading `/` | `artifact-service-entry-name-invalid: <name>` |
| Symlink entries | refused (zip external attributes marking a symbolic-link file type) | `artifact-service-entry-name-invalid: <name>` |
| setuid/setgid entries | any entry with the setuid or setgid mode bit | `artifact-service-setuid-entry: <path>` |
| `serviceId` | `^[a-z0-9](?:[-a-z0-9]{0,61}[a-z0-9])?$`, ≤ 63 chars | `artifact-service-service-id-invalid` |
| `version` | `^[0-9A-Za-z][0-9A-Za-z._+-]{0,63}$`, ≤ 64 chars | `artifact-service-version-invalid` |
| `targetArchitecture` | exactly `linux-x64` or `linux-arm64` | `artifact-service-target-architecture-invalid` |
| `launch.entrypoint` | safe entry name, present in the archive, not `.dll` for `Executable` kind, not the layout file itself | `artifact-service-entrypoint-invalid` |
| Entrypoint needs a foreign runtime | `.jar`/`.class`, `.js`, `.py`, `.rb`, `.exe`, `.sh`/`.bash`, a shebang line, `MZ`, or `CAFEBABE` leading bytes; or a `.runtimeconfig.json` naming any framework other than the two permitted | `artifact-service-entrypoint-requires-foreign-runtime: <runtime>` |
| `launch.arguments` | ≤ 32 entries, each ≤ 1024 chars, no control characters | `artifact-service-argument-invalid` |
| `launch.environment` count | ≤ 64 variables | `artifact-service-environment-invalid: count` |
| `launch.environment` name | `^[A-Za-z_][A-Za-z0-9_]{0,127}$`, ≤ 128 chars, unique | `artifact-service-environment-invalid` |
| `launch.environment` name prefix | must not start with `YOGABASE_` or `LD_` | `artifact-service-environment-reserved: <name>` |
| `launch.environment` value | ≤ 4096 chars, no control characters | `artifact-service-environment-invalid: <name>` |
| Secret-shaped argument/environment/file content | any hit against the shared secret-pattern scanner (bounded to the first 1 MiB of each entry's bytes) | `artifact-service-secret-material: <detail>` |
| `launch.workingDirectory` | must be a safe entry name if non-empty | `artifact-service-working-directory-invalid` |
| Ports (count) | 1–8 | `artifact-service-port-invalid: count` |
| Port `name` | `^[a-z](?:[a-z0-9-]{0,13}[a-z0-9])?$`, ≤ 15 chars, no `--`, unique per bundle | `artifact-service-port-invalid: name` |
| Port `port` | 1024–65535, unique per bundle | `artifact-service-port-invalid: <name>` |
| `health.periodSeconds` / `initialDelaySeconds` | 1–3600 (0 or negative takes the default before this check runs) | `artifact-service-health-invalid` |
| `health.failureThreshold` | 1–100 | `artifact-service-health-invalid` |
| `terminationGraceSeconds` | 10–1800 (default 30) | `artifact-service-termination-grace-invalid` |
| ELF shape | ELF64, little-endian, `e_machine` `62` (x86-64) or `183` (AArch64) matching `targetArchitecture` | `artifact-service-elf-invalid`, `artifact-service-elf-architecture-mismatch` |
| `PT_INTERP` | absent, or exactly the base image's glibc loader for that architecture | `artifact-service-elf-interpreter-forbidden: <path>` |
| `DT_NEEDED` closure | every dependency resolves in-bundle or against the image-pinned allowlist | `artifact-service-unresolved-shared-library: <soname>` |
| Detached-manifest layout mismatch | canonical `yogabase-service.json` must byte-equal the manifest's `service` object | `artifact-service-layout-manifest-mismatch` |
| Provider identity scoping | this artifact kind is refused outright on GoogleCloud bindings (Workload Identity cannot be scoped below Pod level there) | `hosted-service-provider-identity-not-container-scoped` |

Reserved/allowlisted ports and identifiers, from `src/Yogabase.Core/AppPlatform/Runtime/YogaApplicationRuntime.cs` and the ADR:

| Purpose | Value |
|---|---|
| Runtime-host relay, loopback to the workload | `8090` |
| Runtime-host relay, member-internal (Agent) | `8091` |
| `service-host` liveness/readiness probe port | `8092` |
| Application-gateway TCP-relay listener pool | `8100`–`8103` (≤ 4 declared routes per runtime) |

`launch.environment` names starting with `YOGABASE_` or `LD_` are reserved/forbidden regardless of
these specific values — the supervisor injects `YOGABASE_RUNTIME_HOST_ENDPOINT` and
`YOGABASE_SERVICE_PORT_<NAME>` itself into a **cleared** child environment; nothing you declare can
collide with or steer the loader via `LD_*`.

## 4. Five admission rules that will reject a naive Unity build

### Rule 1: point `entrypoint` at the ELF, never at Unity's wrapper script

Unity's standalone Linux build output is sometimes launched through a generated `.sh` script in
other hosting environments. Do not use one here. `ClassifyForeignRuntimeByEntrypointName` maps the
`entrypoint`'s file extension straight to a foreign-runtime refusal:

```csharp
".sh" or ".bash" or ".zsh" or ".ksh" => HostedServiceForeignRuntimes.Shell,
```

Point `launch.entrypoint` at the ELF binary itself — e.g. `Server.x86_64` — never at a
`run-server.sh`-style wrapper. The refusal is
`artifact-service-entrypoint-requires-foreign-runtime: shell`.

### Rule 2: leading-byte refusals

Even without a telltale extension, `ClassifyForeignRuntimeByLeadingBytes` inspects the entrypoint's
first bytes:

- `#!` (a shebang line) — the interpreter named on that line selects `python`, `node`, `ruby`, or
  falls back to `shell` for anything else (including no match).
- `MZ` (a Windows PE header) — classified as `mono`.
- `CA FE BA BE` (a Java class-file magic number) — classified as `java`.

A genuine Unity Linux IL2CPP ELF binary hits none of these, so this rule is normally invisible —
it only bites if something upstream (a build script, a CI step) accidentally substitutes a
non-ELF file for the real binary.

### Rule 3: `PT_INTERP` allowlist

The entrypoint (and every `*.so*` in the bundle) may have **no** `PT_INTERP` segment (a fully
static binary) or must name **exactly** the base image's glibc dynamic loader for the declared
architecture:

| `targetArchitecture` | Allowed `PT_INTERP` |
|---|---|
| `linux-x64` | `/lib64/ld-linux-x86-64.so.2` |
| `linux-arm64` | `/lib/ld-linux-aarch64.so.1` |

A standard Unity IL2CPP Linux Dedicated Server build dynamically links against glibc and uses this
exact interpreter path, so it passes unmodified. A musl-linked build, a build with a relocated or
bundled loader (e.g. a custom `rpath`-patched interpreter), or anything produced by a distro other
than the base image's Ubuntu 24.04.4/glibc 2.39 lineage is refused as
`artifact-service-elf-interpreter-forbidden: <path>`.

### Rule 4: dependency closure — Dedicated Server, not Player + `-nographics`

Every `DT_NEEDED` entry of every ELF object in the bundle must resolve either in-bundle (via
`$ORIGIN`-relative `DT_RUNPATH`/`DT_RPATH`, or simply by being co-located as a sibling file in the
same directory — sibling resolution needs no explicit run path at all) or against the image-pinned
SONAME allowlist generated from the pinned `mcr.microsoft.com/dotnet/aspnet:10.0.11` base image
(Ubuntu 24.04.4 LTS, glibc 2.39):

```
libBrokenLocale.so.1  libacl.so.1        libanl.so.1          libapt-pkg.so.6.0
libapt-private.so.0.0 libassuan.so.0     libattr.so.1         libaudit.so.1
libblkid.so.1         libbz2.so.1.0      libc.so.6            libc_malloc_debug.so.0
libcap-ng.so.0        libcap.so.2        libcom_err.so.2      libcrypt.so.1
libcrypto.so.3        libdb-5.3.so       libdebconfclient.so.0 libdl.so.2
libdrop_ambient.so.0  libe2p.so.2        libext2fs.so.2       libffi.so.8
libformw.so.6         libgcc_s.so.1      libgcrypt.so.20      libgmp.so.10
libgnutls.so.30       libgpg-error.so.0  libhogweed.so.6      libicudata.so.74
libicui18n.so.74      libicuio.so.74     libicutest.so.74     libicutu.so.74
libicuuc.so.74        libidn2.so.0       liblz4.so.1          liblzma.so.5
libm.so.6             libmd.so.0         libmemusage.so       libmenuw.so.6
libmount.so.1         libmvec.so.1       libncursesw.so.6     libnettle.so.8
libnpth.so.0          libnsl.so.1        libnss_compat.so.2   libnss_dns.so.2
libnss_files.so.2     libnss_hesiod.so.2 libp11-kit.so.0      libpam.so.0
libpam_misc.so.0      libpamc.so.0       libpanelw.so.6       libpcprofile.so
libpcre2-8.so.0       libproc2.so.0      libpsx.so.2          libpthread.so.0
libresolv.so.2        librt.so.1         libseccomp.so.2      libselinux.so.1
libsemanage.so.2      libsepol.so.2      libsmartcols.so.1    libss.so.2
libssl.so.3           libstdc++.so.6     libsystemd.so.0      libtasn1.so.6
libthread_db.so.1     libtic.so.6        libtinfo.so.6        libudev.so.1
libunistring.so.5     libutil.so.1       libuuid.so.1         libxxhash.so.0
libz.so.1             libzstd.so.1
```

plus the platform's own loader SONAME (`ld-linux-x86-64.so.2` or `ld-linux-aarch64.so.1`).

**`libX11`, `libGL`, `libasound`, and every other X11/OpenGL/ALSA library are NOT on this list.**
This is exactly why you must build with Unity's **Dedicated Server** build target (Linux headless
server), not a regular Player build run with `-nographics` — a Player build still links against
the desktop graphics/audio stack even when told not to render, and those libraries will not
resolve. The Dedicated Server target strips that dependency at build time.

`UnityPlayer.so` and `GameAssembly.so` (and, in older Unity versions, `libmonobdwgc-2.0.so` for
Mono-scripting builds — not applicable to an IL2CPP build) ship inside the bundle next to the
entrypoint. The admission Job treats any file whose name contains `.so` as a shared-object
candidate, reads its ELF `.dynamic` section, and resolves its own `DT_NEEDED` entries the same way:
against the allowlist above, or against files co-located in the same bundle directory (which is
where Unity places `UnityPlayer.so`/`GameAssembly.so` relative to the entrypoint by default — no
explicit `RUNPATH`/`RPATH` entry is required for this same-directory case). An unresolved SONAME
anywhere in this closure is refused as `artifact-service-unresolved-shared-library: <soname>`.

### Rule 5: privilege bits and secret scanning

Any bundle entry carrying a setuid or setgid mode bit is refused as
`artifact-service-setuid-entry: <path>` (the materializer would strip the bits again on extraction
even if admission somehow missed one — but admission refuses the upload outright). Every entry's
leading bytes (bounded to 1 MiB per entry) and every `launch.environment` name/value pair are
scanned against the shared secret-pattern detector; a hit refuses the upload as
`artifact-service-secret-material: <entry-or-name>`. Do not bake API keys, connection strings, or
credentials into a config asset shipped in the bundle, and do not pass them as `launch.arguments`
or `launch.environment` values — pass a bootstrap reference instead and fetch the real secret at
runtime through your own out-of-band mechanism (Yogabase's own secret values never enter this
bundle, browser state, CRD status, logs, or audit records either).

## 5. Packaging

- Zip the build **contents**, with `yogabase-service.json` at the archive root — the admission Job
  looks for an entry whose full name is exactly `yogabase-service.json`; if it is nested inside a
  wrapping top-level folder (a common side effect of some zip tools), the layout entry will not be
  found and the upload is refused as `artifact-service-layout-missing`.
- Set the executable bit on the entrypoint (and any other binary that needs it) in the zip's
  external file attributes before uploading — a normal `zip` or `System.IO.Compression`-based
  packaging step from a Unix build host preserves Unix mode bits into the zip's external
  attributes; a packaging step that discards them (some Windows-hosted zip tools do) will produce
  a bundle that passes admission but cannot actually be executed at runtime. This specific failure
  mode is not independently confirmed by a dedicated admission refusal code in the source read for
  this document — flagging it as something to verify empirically once a real build is tested.
- No symlinks anywhere in the archive (refused as `artifact-service-entry-name-invalid`).
- Produce a **detached manifest** alongside the bundle. Its `service` property must be a
  byte-identical copy of the embedded `yogabase-service.json` document (compared as canonical
  JSON — property order and whitespace do not matter, but every field does), and its `signature`
  property must be exactly:

  ```json
  {
    "signerId": "<your canonical signer identity>",
    "keyId": "<your canonical key identity>",
    "algorithm": "ECDSA-P256-SHA256",
    "value": "<base64 of one 64-byte IEEE P1363 P-256/SHA-256 signature>"
  }
  ```

  Any structural deviation (extra/missing field, wrong algorithm string, a signature that is not
  exactly 64 raw bytes in canonical base64) fails signature-shape validation before the signature
  itself is even checked. A `service` object that does not byte-equal the embedded layout is
  refused as `artifact-service-layout-manifest-mismatch`.

## 6. What the server must do at runtime

The C# API is `YogabaseRuntimeHostClient` in the `Yogabase.Yogabase.RuntimeHost` package (being
authored in this repository as of this writing, mirroring the loop shape in this product
repository's `tools/Yogabase.Operator.Conformance/HostedServiceFixtureClient.cs`, which is itself
a faithful, minimal C# port of Agones' `sdks/csharp/sdk/AgonesSDK.cs` client-loop shape adapted to
Yogabase's self-scoped, instance-id-free wire contract).

Startup sequence:

1. One synchronous heartbeat before anything else, so the supervisor sees the process is alive.
2. Start the background heartbeat loop.
3. Bind your declared listener(s) on the port(s) named in `ports[]`.
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

## 7. IL2CPP stripping

Unity's IL2CPP pipeline AOT-compiles your project's managed code, including whatever
`Yogabase.Yogabase.RuntimeHost` module you reference, and Unity's managed code stripping (Medium or
High) can remove types and members that are only reached through reflection if they are not
referenced elsewhere and not preserved by a `link.xml`. The package intends to ship a `link.xml`
defensively, but the main hazard it would otherwise guard against does not apply here: the module
mirrors `HostedServiceFixtureClient.cs`, which builds and parses its wire payloads with
`System.Text.Json.Nodes` (`JsonObject`/`JsonNode`) — a document-object-model API — rather than an
attribute-driven, reflection-based serializer. There is no `[JsonPropertyName]`-style contract for
the stripper to silently break.

## 8. Honest limits

- **Cluster-internal exposure is the current baseline, not per-instance public reachability.**
  Per ADR 0059, the actuator emits one ClusterIP Service and one NetworkPolicy admitting only
  project-labeled workload Pods and the application-gateway Pods. External reachability, once the
  gateway TCP relay exists, reaches the **service as a whole** through a small pool of gateway
  listener ports (`8100`–`8103`, at most 4 declared routes per runtime) with no L4 authentication
  of its own — the hosted service must authenticate its own clients. A dedicated public
  endpoint for one specific game-server **instance** (the shape most game clients actually need to
  connect to a specific match) is explicitly out of scope for this kind and is deferred to a later,
  not-yet-designed `SelfContainedGameServer` kind with its own host-port design.
- **The capability is open; the installation is the constraint.** Both
  `HostedServiceQualificationEvidence.V1Accepted` and `RuntimeHostLifecycle.V1Accepted` are compiled
  `true`. What can still refuse a correct bundle is the installation it is uploaded to: the target's
  provider identity must be container-scoped, artifact object storage must be configured, and the
  `extended.hostedServices` module switch must be on (it defaults off and requires
  `extended.sandbox`). A self-hosted installation is refused at upload-intent time on the first of
  those, independent of whether your bundle would otherwise pass every rule above.
- **This document is derived from product source, not from a working example.** Every constraint,
  code, and path above was read directly out of the C# files listed below. None of it has been
  exercised against an actual Unity export.
- **No Unity Editor run has verified any of this.** Nothing here was tested in the Unity Editor or
  against a real IL2CPP Linux Dedicated Server build. Treat the packaging and startup-sequence
  guidance as a first attempt to get right, not a proven recipe — particularly the executable-bit
  preservation caveat in [Packaging](#packaging), which is a plausible failure mode inferred from
  how zip tools generally behave, not something confirmed in the admission source itself.

## Sources read for this document

All paths are relative to the product repository worktree read while authoring this document
(`/Users/drgyugyu/Desktop/Yogabase/.claude/worktrees/local-selfhost`), not this SDK repository:

- `src/Yogabase.Core/Delivery/HostedServiceBundleContract.cs`
- `src/Yogabase.Core/Delivery/HostedServiceBaseLibraryAllowlist.cs`
- `src/Yogabase.Core/AppPlatform/Apps/AppArtifactContracts.cs`
- `src/Yogabase.Core/AppPlatform/Runtime/YogaApplicationRuntime.cs`
- `src/Yogabase.Core/RuntimeHost/RuntimeHostRelayContracts.cs`
- `src/Yogabase.Provisioner/Job/SelfContainedServiceBundleInspector.cs`
- `src/Yogabase.Tasks/AppPlatform/Hosting/RuntimeHostRelayProcess.cs`
- `tools/Yogabase.Operator.Conformance/HostedServiceFixtureClient.cs`
- `docs/decisions/0059-hosted-self-contained-service-workloads.md`
