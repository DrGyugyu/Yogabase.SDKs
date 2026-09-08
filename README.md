# Yogabase SDKs

English | [한국어](README.ko.md)

Client SDK previews for applications built on **Yogabase**.

Yogabase is a .NET 10 control plane for operating data services across Kubernetes. This repository is deliberately independent of the private Yogabase product repository, its Helm chart, images, Control Plane, and Marketplace delivery graph. It contains application-facing and workload-delivery client libraries plus their contract fixtures; it is not a Kubernetes operator, a cloud-provisioning tool, or a generic management SDK.

> **Preview status:** the packages in this repository are unpublished previews. A successful local build does not authorize publication to NuGet, npm, PyPI, Maven Central, pub.dev, GitHub Packages, or any other registry.

## What Yogabase does

Yogabase gives a team one authoritative place to plan, approve, deploy, observe, and operate its data services on Kubernetes. The first product scope is **Yogabase Core**: a managed YugabyteDB lifecycle with **YSQL** as the supported database API, a Blazor Console, a central Control Plane, and portable Kubernetes provisioning for AWS, Azure, Google Cloud, and Oracle Cloud qualification profiles.

It is one Kubernetes Marketplace product delivered through one Helm chart and one coordinated image family. The chart has two installation modes:

| Mode | Role |
|---|---|
| `control-plane` | Runs the authoritative Control Plane, Console, REST API, fleet gateway, provisioning workflow, billing/metering, local actuator/agent, and its dedicated RF1 YSQL catalog. |
| `member` | Runs the lightweight services installed in each managed target: the outbound fleet agent and the deterministic local actuator. |

The Control Plane is Yogabase's sole source of accepted management intent. A member never exposes an inbound management API, never decides product policy, and never becomes a peer control plane. Members connect **outbound** to the Control Plane over mutually authenticated gRPC, persist signed target-bound intent, and keep reconciling already accepted local work while the Control Plane is unavailable.

## Marketplace architecture

![Yogabase EKS Beta marketplace architecture](assets/marketplace-architecture.svg)

The EKS Beta architecture keeps databases, secrets, and operational state inside the customer's AWS account. Members connect outbound-only to the authoritative Control Plane; the customer retains control of the optional backup destination.

## Console experience

![Illustrative Yogabase Blazor Console concept](assets/yogabase-console-concept-v2.png)

_Source-informed Console concept, not a live environment or status display. It intentionally uses empty states and does not represent customer data, metrics, or enabled capabilities._

## How the architecture works

![Yogabase Control Plane and member architecture](assets/yogabase-architecture.svg)

The local actuator is intentionally subordinate. It is the only component that writes Yogabase-owned Kubernetes workloads, but it can translate only Control Plane-signed, allowlisted intent into deterministic resources. It does not read arbitrary Secrets, execute commands in Pods, or originate management policy.

### Cloud and target identity

Cloud provider selection is an installation identity, not a console dropdown or client API parameter. The Marketplace release fixes the Control Plane provider; each member's provider and managed-cluster kind are attested during enrollment and become immutable target identity. Provisioning remains behind a .NET `ICloudProvisioner` boundary and uses Pulumi .NET programs, while the product retains one provider-neutral Control Plane and one Helm chart.

### Database operations

Core manages logical YugabyteDB database services on selected Kubernetes targets. It supports YSQL-first database lifecycle, health/topology visibility, explicit RF1/RF3/RF5 topology selection, guarded scale-out, compute resize, storage expansion, optional logical encrypted backup, restore into a new or empty target, allowlisted patch upgrades, safe deletion, retained PVCs, diagnostics, and redacted support bundles.

Every change is designed as a durable operation:

1. Yogabase produces a preview of the exact requested change.
2. The request is checked against the current resource revision and an idempotency key.
3. Risky work requires the appropriate approval.
4. Accepted work is auditable, recoverable, and reports bounded observed facts rather than fabricated status.

Yogabase does not expose arbitrary SQL, engine-internal topology as a client contract, remote shell access, `pods/exec` workflows, peer gossip, or an alternate control plane. YCQL, automatic scale-in, PITR, downgrade, and cross-cluster database deployments are outside the current Core scope.

### What happens during an operation

![Yogabase durable operation flow](assets/yogabase-operation-flow.svg)

### Security boundaries

YSQL traffic is certificate-based and fail-closed: node-to-node and client-to-server TLS are required, and client certificate identities are explicitly allowlisted. Secret values do not belong in browser state, resource status, logs, audit records, Pulumi output, or support bundles.

The product keeps the public Kubernetes API intentionally small: `YogaDatabase`, `YogaDatabaseOperation`, `YogaApplicationRuntime`, `YogaValkey`, and `YogaRobustMQCluster`. These resources are not a substitute for the Control Plane; they are signed desired-state inputs and bounded local observations.

### Direct and GitOps use

Direct operation through Yogabase is authoritative. GitOps support exports deterministic YAML and diffs for the customer's own local Git workflow; Yogabase does not push branches, create pull requests, or write to customer repositories.

## Custom build delivery and Kubernetes

![Custom build delivery through the Yogabase Control Plane](assets/custom-build-delivery-flow.svg)

A custom-build workflow reaches Kubernetes through the authoritative Control Plane, never by
receiving Kubernetes or cloud credentials. The separately configured Delivery client uses a
short-lived workload OIDC token to call the TLS-only `/api/v1/delivery` host. It can discover
approved templates, preview and submit environments and immutable artifact admissions, create
releases, promote or roll back them, decommission an environment, and follow the resulting durable
operation through bounded polling or server-sent events.

Artifact submission carries a credential-free provider-native locator and exact bundle, manifest,
SBOM, and scan-evidence digests. The Control Plane records intent; an isolated admission job verifies
the artifact; signed intent then travels over the outbound mutually authenticated fleet channel to
the member's subordinate local actuator, which alone reconciles the Kubernetes workload.

The server now exposes a versioned runtime-host contract that lets a running build report health
and readiness, be reserved or allocated, and drain on shutdown
(`contracts/yogabase-runtime-host-v1.openapi.json`). `Yogabase.Yogabase.RuntimeHost` implements the
workload side of it as a faithful, modified C# derivative of Agones' client loop shape under
Apache-2.0; see [`docs/REFERENCE_REVIEW.md`](docs/REFERENCE_REVIEW.md) and
[`docs/UNITY_DEDICATED_SERVER.md`](docs/UNITY_DEDICATED_SERVER.md). PlayFlow, Nakama, Firebase, and
Supabase remain reference-only, informing client lifecycle, session, cancellation, polling,
event-stream, and platform ergonomics.

## Application runtime and SDK boundary

Yogabase can expose a target-local application gateway for approved application operations. That is the boundary these SDKs implement:

- Bootstrap returns non-secret, project-bound REST, identity, and separately deployed realtime endpoint configuration.
- Authentication covers signup, login/logout, email confirmation, password reset, MFA and recovery codes, external-identity callbacks, plus authorization code with PKCE and rotating refresh tokens. SDKs never contain confidential client credentials.
- **There is no client-facing data surface.** The Data API module is withdrawn (owner decision,
  2026-09-08). An application reaches its project's data through a Lambda, which runs server-side
  with a full-access database context, and through Auth, Storage and Realtime.
- Storage is mediated, digest- and idempotency-bound, and never returns provider credentials or signed URLs.
- Stateless Functions invoke an exact registered version through the bounded public contract.
- Actor Lambdas address one versioned, project-local instance; Workflow Lambdas start one immutable definition version and expose only the v1 cancel operation.
- Realtime uses the separately returned SignalR origin for topic subscribe/unsubscribe, signed Lambda-binding invocation, and at-least-once completion events deduplicated by `eventId` in the authored .NET client. JSON remains the default Hub protocol.

Clients are provider-neutral and shard-oblivious. They never receive a cloud selector, cloud credential, Kubernetes identity, TServer address, tablet/shard identifier, shard key, physical placement map, or topology generation. YugabyteDB owns its internal routing and placement; Yogabase observes bounded health and capacity facts without introducing another data-path router.

The SDKs deliberately exclude generic human Control Plane management APIs, infrastructure provisioning, fleet commands, Kubernetes CRDs, approval mutation, remote shell, `pods/exec`, general notifications, analytics, GameSessions, and provider credentials. The only Control Plane surfaces are the exact workload-authenticated Delivery v1 contract described above and the self-scoped Runtime Host v1 contract that a hosted workload reaches over loopback, which accepts no application or Delivery credential and exposes no allocation call to the workload.

## Capability truth

Yogabase reports module and protocol availability from signed observations and release-locked evidence. Configuration alone is never treated as a successful deployment.

| Area | Current boundary |
|---|---|
| Core | The primary product scope: managed YugabyteDB/YSQL operations with durable, approval-aware workflows. |
| Extended | Scoped to Functions, Actor Lambdas, Workflow Lambdas, Dynamic Code, sandboxing, and Scheduler/Jobs. Capabilities remain unavailable until their own runtime evidence qualifies them. |
| Valkey | Uses a Yogabase-owned C# reconciliation path. Promotion and Redis protocol claims remain gated on live qualification evidence. |
| RobustMQ | Uses a bounded Yogabase-owned C# lifecycle port. Lifecycle and Kafka/AMQP/MQTT protocol claims remain unavailable until independently qualified. |

This README describes product architecture and boundaries, not a promise that a disabled or evidence-gated capability is currently enabled.

## Packages

The non-.NET package identifiers retain their existing preview names for continuity. They do not imply registry publication or a support commitment. Version `0.3` adds the workload-authenticated Control Plane Delivery v1 surface and cross-platform lifecycle improvements while retaining the existing application contract.

| Platform | Location | Current package identity | Qualification |
|---|---|---|---|
| .NET | `src/dotnet/Yogabase.Yogabase.Client` | `Yogabase.Yogabase.Client` `0.3.0-preview.1` | Authored application, PKCE, SignalR, Delivery, game-server, and build clients; `netstandard2.1` and `net10.0` |
| TypeScript | `languages/typescript` | `@stackyoga/yogabase-client-preview` `0.3.0-preview.1` | Generated application client plus authored Delivery and storage-key facades |
| Python | `languages/python` | `stackyoga-yogabase-client-preview` `0.3.0.dev1` | Generated application client plus authored async Delivery and storage-key facades |
| Java/JVM | `languages/java` | `io.stackyoga:yogabase-client-preview` `0.3.0-preview.1` | Generated application/Delivery clients plus authored JVM facades; Kotlin/JVM interop path |
| Dart | `languages/dart` | `stackyoga_yogabase_client_preview` `0.3.0-dev.1` | Experimental generated application/Delivery source plus authored facades |
| .NET Lambda context | `src/dotnet/Yogabase.Lambda` | `Yogabase.Lambda` `0.1.0-preview.1` | Authored runtime-context wrapper for a Lambda bundle; `netstandard2.1` and `net10.0` (the database handle only on `net10.0`); fake-provider tests only, no live installation evidence |
| .NET runtime host | `src/dotnet/Yogabase.Yogabase.RuntimeHost` | `Yogabase.Yogabase.RuntimeHost` `0.1.0-preview.1` | Authored self-scoped runtime-host client; `netstandard2.1` and `net10.0`; fake-transport tests only, no live-gateway evidence |
| Unity | `languages/unity/com.stackyoga.yogabase-runtime-host` | `com.stackyoga.yogabase-runtime-host` `0.2.0-preview.1` | UPM package carrying the runtime-host module, its `link.xml`, and the Editor publish flow (`Yogabase ▸ Publish Server`); the Editor scripts compile against a real Unity 6000.5.7f1 reference set at C# 9, but no Editor has run the window and no IL2CPP build has verified the runtime module |

The immutable application snapshot is `contracts/yogabase-client-v1.openapi.json` (SHA-256 `4aa4820d15e76f68aa2840fa02be3b110e7a1fd850d8100c050f8cd421f7d1cf`). The workload snapshot is `contracts/yogabase-delivery-v1.openapi.json` (SHA-256 `1eca152807ed2c6ab834b6ad7fea5fedb514ce359bd8f02f028635b03b8b6cc4`). The runtime-host snapshot is `contracts/yogabase-runtime-host-v1.openapi.json` (SHA-256 `e0393d8dd7962a0bf2447fd5a79ee22dbaa3f271f6bb98b92d8e4749365967e7`); it is checked in as the server-side reference for the lifecycle states and error codes and is deliberately never code-generated, because a workload calls the self-scoped relay routes rather than the instance-addressed paths that document declares. Their adjacent checksums, Kiota locks, and `MANIFEST.json` bind this repository to the exact contracts and generator.

Two further surfaces — game-server allocation and build ingest — are described in this repository under `contracts/planned/`, marked `"x-yogabase-status": "planned"`. Those documents are written here from the agreed contract while the product implements it in parallel. They are **not** mirrored product snapshots and are not authoritative: when the product publishes them, its documents supersede these and the planned files are deleted rather than reconciled. The .NET client implements both today; the other languages do not.

## Local verification

```text
dotnet restore Yogabase.SDKs.slnx --locked-mode
dotnet build Yogabase.SDKs.slnx -c Release --no-restore
dotnet test Yogabase.SDKs.slnx -c Release --no-restore --no-build
dotnet pack src/dotnet/Yogabase.Yogabase.Client/Yogabase.Yogabase.Client.csproj -c Release --no-restore

cd languages/typescript && npm ci && npm test
cd languages/python && python -m unittest discover -s tests
cd languages/java && mvn test
cd languages/dart && dart analyze

scripts/compile-unity-editor.sh
```

`scripts/compile-unity-editor.sh` compile-checks the Unity package's Editor scripts in a real Unity
Editor compile environment: it reuses a response file Unity's own Bee build already wrote for an
Editor assembly, keeps its Editor-installation references, defines and language version, and swaps
in this package's sources. Set `UNITY_RSP` to point at one. With no response file it exits 2 and
says the scripts were not checked, rather than reporting a pass it did not perform.

Run `scripts/regenerate-clients.sh` to verify all three contracts, restore pinned Kiota 1.34.1, regenerate all four application clients, and regenerate the Java and Dart delivery protocol clients. The runtime-host contract is checksum-verified but never generated, and the authored runtime-host module is not regenerated source. Authored delivery facades are preserved. Each generated-language folder retains its own package manifest, dependency lock where available, Kiota lock, source, documentation, and conformance helper. Downloaded tools, caches, dependency trees, and build intermediates are excluded. Historical imported artifacts remain unchanged under `artifacts/imported`; current sibling-repository builds belong under `artifacts/local`.

### Connecting a client to a self-hosted installation

The .NET client's public entry point is `Yogabase.Yogabase.YogabaseClient`. It has no public constructor; a consumer creates it through the static `YogabaseClient.CreateAsync(Uri gatewayEndpoint, string publishableKey, YogabaseClientOptions? options = null, CancellationToken cancellationToken = default)` factory. `gatewayEndpoint` is the installation's application-gateway origin and `publishableKey` is the project's publishable key.

```csharp
using Yogabase.Yogabase;

await using var yogabase = await YogabaseClient.CreateAsync(
    new Uri("https://<installation-host>"),
    "<publishable-key>");
```

`<installation-host>` and `<publishable-key>` are placeholders; this repository does not choose either. The hostname of a self-hosted installation and the publishable key for a given project are both supplied by that installation's owner. A real publishable key begins with `sy_pub_`, and `CreateAsync` rejects one that does not.

A publishable key is not a secret. It identifies the project to the gateway and is safe to ship in client code, and it authenticates nothing: the bootstrap document it fetches carries endpoints, identifiers, and capability facts only. Yogabase issues no privileged counterpart key that a client could hold instead. Every authenticated call carries a per-user access token obtained from the project's OIDC authority, so a client never ships a credential that speaks for the project as a whole.

A console seat is claimed at `https://<installation-host>/console-seat/claim`, using the hostname the installation's owner supplies.

## Platform qualification

Generated REST source is not the same as a production-qualified platform integration. `netstandard2.1` compatibility alone does not qualify Unity or AOT. The Dart package passes Dart 3.13.2 analysis and authored-source tests, but Android, Flutter, Kotlin/JVM runtime integration, Unity IL2CPP, native secure storage, interactive-cookie handling, non-.NET SignalR integration, and live-gateway conformance each still need explicit platform evidence before they can be claimed as supported.

The runtime-host module and its Unity package are authored against the checked-in contract and covered only by fake-transport unit tests. No Unity Editor import, no IL2CPP build, and no live relay session has exercised them, and the per-family runtime-host conformance battery (restart, replay, duplicate, stale-epoch, double-allocation, reservation-expiry, disconnect, Pod-replacement, drain/shutdown-race, credential-confusion, and secret-negative cases) has not been run. Treat both as unqualified previews.
