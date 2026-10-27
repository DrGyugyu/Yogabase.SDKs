# Yogabase

English | [한국어](README.ko.md)

**One control plane for application developers on Kubernetes**: a managed YugabyteDB (YSQL)
database, sign-in, functions, storage, realtime and messaging from the browser, every change
previewed and audited, everything inside your own cluster.

This repository is the public home of Yogabase: the product overview below, and the open-source
client SDKs (Apache-2.0) that applications use against an installation. The product itself (its
Helm chart, images, Control Plane and operator) is closed source and is distributed as one chart
and one image family; its AWS Marketplace listing is being prepared. The only thing this
repository takes from the product is byte-identical copies of its published OpenAPI contracts.

> **Status, 2026-10-27.** The product is complete for its first release and its install, database,
> Valkey, RobustMQ and console suites have passed on real clusters; the release build itself is
> still ahead. The SDK packages are previews built from this source; none is on a registry yet
> (NuGet, npm, PyPI, Maven Central, pub.dev or a UPM registry), so each is consumed from a checkout.
> Two SDK languages are supported until a user asks for a third: .NET (including the Unity builds)
> and TypeScript. The Java, Python and Dart packages are frozen (owner decision, 2026-10-21): their
> code stays as it is, nothing is deleted, but they are no longer maintained, regenerated, tested or
> aligned to the product's contract changes until a user asks; their rows in the Packages table keep
> the facts last recorded.

## What Yogabase is

Yogabase is a Kubernetes-native control plane for application developers. You install one Control
Plane into a cluster you already run and, from a browser and without a terminal, get a project with
a YugabyteDB (YSQL) database, sign-in for your application's users, functions (Lambdas) built and
released by the installation itself, object storage, realtime delivery and messaging. Operators
manage databases and clusters through the same console and a REST API, with every mutation
previewed, concurrency-checked, idempotent, durable and recorded.

**It runs entirely in your cluster.** There is no vendor-hosted service, no telemetry endpoint and
no data path out of your cluster other than the image pulls, the sign-in federation you configure
and a backup destination you set up yourself. Secret material exists only as Kubernetes Secrets in
your own clusters, never in Helm values, resource status, logs, audit records, support bundles or
browser state.

**Pure Kubernetes.** One Helm chart and one image family install into any conformant cluster
(Kubernetes 1.33 to 1.36, arm64 nodes): a developer's k3d cluster, kubeadm on your own machines or
on EC2, or Amazon EKS. Yogabase needs no EKS, ECS, CloudFront or other cloud service to run; an AWS
object store or workload identity is an optional binding, and AWS is the only cloud it binds to.

**Operations are guarded by design.** Changes are previewed before they run, are idempotent and
concurrency-checked, and are recorded in the audit log. By default an operation is approved by
installation policy when it is submitted; independent (four-eyes) approval is a policy any
installation may declare. Deletion is never inferred from missing inventory: it requires an explicit
tombstone bound to the exact resource. Local reconciliation continues when the Control Plane is
unreachable, and an offline member starts no destructive or provisioning work.

**Sign-in is yours.** Console operators sign in with named seats or through your own OIDC provider
(Microsoft Entra ID and Google presets); your application's users sign in to the application
gateway with e-mail and password or an external provider. No console identity authenticates product
APIs, databases or application users.

**Members connect outbound.** Additional clusters enrol over mutually authenticated gRPC and expose
no inbound management endpoint. Agent certificates are short-lived and rotate automatically.

## What it does

### Databases

| | |
|---|---|
| Engine | YugabyteDB 2025.2.5.2 (the vendor's release; PostgreSQL 15 compatible), YSQL only, with TLS and client-certificate authentication; seven curated extensions |
| Topology | replication factor 1, 3 or 5; masters equal the RF; TServers from the RF up to 700; RF1 in exactly one zone; storage class, volume count and RF immutable after creation |
| Operations | scale-out one TServer at a time, compute resize, storage expansion (never shrinks), backup, restore into a new or empty database, backup deletion, health check, diagnostics, redacted support bundle, delete, client identity issue, revoke and rotate, extension enablement, volume reclaim: seventeen signed operations in all, each bound to the exact resource it targets |
| Schema | Table Editor, SQL Editor and Migrations in the console run your SQL as digest-bound migrations through a dedicated schema identity; no client SDK contract carries SQL |
| GitOps | deterministic YAML export and diff for your own repository; Yogabase never pushes |
| Backups | to the installation's own object store (a Yogabase role, the default) or to Amazon S3 |

### The application platform, per environment

Each project has environments; each environment opts into the capabilities it needs in the
console's Designer. Every module switch ships on in the chart except on-demand execution of
untrusted C# source.

| Capability | What ships |
|---|---|
| Application sign-in | sign-up, sign-in, sign-out, e-mail confirmation, password reset, MFA (TOTP and recovery codes), up to eight external OIDC providers, refresh-token rotation; sign-up needs an e-mail transport or confirmation switched off |
| Data API | select, insert, upsert, update, delete on project tables, under row-level security as the signed-in user |
| Storage | object put and get through the gateway, never a provider credential or signed URL |
| Realtime | a SignalR hub with topics, Lambda-published events and presence |
| Remote Config | per project and environment, served by the Control Plane |
| Stateless Functions | REST and gRPC invoke of a registered version; 1 MiB payload |
| Actor and Workflow Lambdas | versioned, project-local actors with reminders; immutable workflow definitions with start and cancel |
| Dynamic Code | C# written in the console, compiled, scanned and signed by the installation's own builder, then released |
| Sandbox | one load context per invocation, unloaded afterwards; egress limited to the project's database, gateway and Valkey |
| Scheduler and Jobs | cron or one-shot schedules with ETag preconditions, misfire and concurrency policies; durable jobs with result and cancel |
| Hosted self-contained services | upload an archive (a game server, say), have it inventoried, scanned and signed, then run and allocated through `/v1/servers` |
| Valkey | standalone or primary-replica with promotion; direct project-scoped access from Lambdas, a distributed cache, the realtime backplane, presence and rate-limit stores |
| RobustMQ messaging | MQTT publish; Kafka and AMQP publish over mutual TLS; durable queues with receive and acknowledge; durable streams; replay from an offset or a time; shared and independent consumer groups; consumer triggers that invoke Lambdas at least once; change-data capture of selected YSQL tables |
| External consumers | Kafka and AMQP consumers against your own TLS brokers, writing to the YSQL ledger with a dead-letter queue |

### The console

A Blazor console with four rail groups: **Core** (projects, Get started, database topology,
operations, and per environment Connect, Table Editor and SQL Editor), **Extended** (the Designer
and the modules the installation offers), **Enterprise** (audit, approvals, migrations; the parts
not built say so) and **Operate** (overview, nodes, observability, diagnostics, integrations,
trusted publishers, installation settings, access, and per-project logs, usage and settings). A
first-time developer follows one linear path: create a database, choose capabilities, connect a
client, build and release. Unfinished screens are visible and disabled rather than hidden or faked.

### Installation and operations

- **Modes:** `control-plane` or `member`; cluster kinds `generic`, `eks` or `self-managed`.
- **Material:** the chart's pre-install hook mints the installation's own material (catalog
  certificates, broker TLS, application identity keys, object-store material, admission and Lambda
  keys), create-only and immutable; you make seven Secrets of your own and receive one
  vendor-signed attestation.
- **Exposure:** ClusterIP, Ingress, LoadBalancer or NodePort for the Control Plane; host ports or a
  NodePort per instance for hosted services.
- **Members:** join outbound over mutual-TLS gRPC; the Control Plane approves a join by key
  fingerprint; member certificates last a day and renew themselves.
- **Assurance:** every mutation previewed, concurrency-checked, idempotent, audited; one approval
  policy per installation; secret values never in browser state, resource status, logs, audit
  records or support bundles; every image pinned by digest, non-root, with no runtime downloads;
  the chart has no external chart or image dependencies.

### Not in this release

- **YSQL only.** YCQL is not available.
- **No automatic scale-in, point-in-time recovery, downgrade or engine upgrade** (a new chart
  version carries the engine it pins); no cross-cluster databases.
- **One Control Plane instance by design.** While it is down, the console and API are unavailable
  and no new operations start; running databases keep serving.
- **arm64 only.** No amd64 images ship.
- **Enterprise is a library of capabilities, not a tier.** Audit, approvals and migrations are on
  every installation; authorizations, database assurance tiers, installation-wide backup, disaster
  recovery, performance, workload security, maintenance and data movement are not built and say so.
- RobustMQ's native Kafka and AMQP protocol endpoints are not offered (applications reach messaging
  through their functions); RobustMQ runs single-node; Valkey has no backup or restore; execution
  of untrusted C# source on demand ships off.
- Not yet exercised on a real tenant or cluster: the external sign-in providers, the AWS IAM
  Identity Center preset, a load balancer in front of the console, arm64 nodes on EKS.

## Where Yogabase runs

![Yogabase installs as one Kubernetes product into any conformant cluster; a cloud object store, workload identity, cluster provisioning, and AWS Marketplace are optional bindings](assets/yogabase-installation.svg)

One Yogabase release, one Helm chart and one image family, installs into any conformant Kubernetes
cluster, from a developer's k3d cluster to Amazon EKS. The installation is identified by the cluster
it runs in, attested at enrollment: the Control Plane's own cluster enrols itself as the first
member, and further clusters install the same chart in `member` mode and connect outbound. A cloud
object store, workload identity, cluster provisioning and an AWS Marketplace entitlement are each an
optional binding attached independently; none of them is the installation's identity, and installing
needs none of them. Installing does need nodes labelled `yogabase.io/workload=true` (a matching
`NoSchedule` taint is optional and tolerated), a StorageClass, network policy enforcement, and
chart values among them the catalog's StorageClass and zones, `controlPlane.targetId`, a Secret
holding the Data Protection key-encryption key, and console sign-in through either an OIDC issuer
or a Secret of seat claim tokens. The chart discovers neither a StorageClass nor node zones, so its
catalog names both in values; once a cluster is enrolled, its member Agent reports the cluster's
StorageClasses and node zones to the Control Plane. The AWS Marketplace listing is one optional
distribution of the same chart and images, whose entitlement gate and metering stay dormant unless
that binding is configured.

## Console experience

![The Yogabase Console's Get started page for a demo project](assets/yogabase-console-get-started.png)

_A real capture of the Console's Get started page, taken on 2026-09-22 at 1440 px in the light
appearance from the product's own browser-test harness with demo data, not a customer installation
or live status. The page has since been redesigned (2026-10-22) as one linear path of four steps:
add a database, choose capabilities, connect a client, build and release. The rail groups the
Console into Core, Extended, Enterprise and Operate; Enterprise and Operate start collapsed._

## How the architecture works

![Yogabase Control Plane and member architecture](assets/yogabase-architecture.svg)

Approval follows the installation's approval policy (approved at submission by default), and the
Provisioner is present only when the optional provisioning binding is enabled.

The local actuator is intentionally subordinate. It is the only component that writes Yogabase-owned
Kubernetes workloads, but it can translate only Control Plane-signed, allowlisted intent into
deterministic resources. Its own role grants no Secret read; the release namespace is the trust
boundary for the installation's keys. It does not execute commands in Pods or originate management
policy.

### Cloud and target identity

Installation identity is the Kubernetes cluster Yogabase is installed into, attested at enrollment;
it is never a console dropdown or a client API parameter. A cloud object store, workload identity,
or cluster provisioning is an optional binding attached independently, and a cloud provider names
the vendor of such a binding, not the installation. Each member's cluster identity is attested
during enrollment. Provisioning, when used, stays behind a .NET `ICloudProvisioner` boundary with
Pulumi .NET programs and is off in this release; installing or operating Yogabase never requires
it, and the product keeps one provider-neutral Control Plane and one Helm chart.

Console sign-in is the installation's choice of named seats or an external OIDC provider (Microsoft
Entra ID and Google presets; an AWS IAM Identity Center preset exists but has not been exercised).
A console identity never authenticates application or SDK calls.

### Database operations

Core manages logical YugabyteDB database services on selected Kubernetes clusters. It supports
YSQL-first database lifecycle, health and topology visibility, explicit RF1, RF3 and RF5 topology
selection, guarded scale-out, compute resize, storage expansion, safe deletion, retained volumes,
diagnostics and redacted support bundles. Logical encrypted backup, restore into a new or empty
target and backup deletion are durable operations on each database; their round trip to the
installation's own store ran as a conformance profile on a self-hosted installation on 2026-09-24.

Every change is designed as a durable operation:

1. Yogabase produces a preview of the exact requested change.
2. The request is checked against the current resource revision and an idempotency key.
3. The installation's approval policy decides whether the change waits: the shipped default
   approves it at submission by installation policy, and independent (four-eyes) approval is a
   policy any installation may declare.
4. Accepted work is auditable, recoverable, and reports bounded observed facts rather than
   fabricated status.

No client SDK contract carries SQL. The Console runs developer DDL and DML only from its Table
Editor, SQL Editor, and Migrations page, through the `schema-runner` role as the environment's
schema identity, with every write recorded as a digest-bound migration; a Lambda's own code reaches
its project database server-side. Yogabase does not expose engine-internal topology as a client
contract, remote shell access, `pods/exec` workflows, peer gossip, or an alternate control plane.
YCQL, automatic scale-in, PITR, downgrade, engine upgrade and cross-cluster database deployments
are outside the current scope.

### What happens during an operation

![Yogabase durable operation flow](assets/yogabase-operation-flow.svg)

### Security boundaries

YSQL traffic is certificate-based and fail-closed: node-to-node and client-to-server TLS are
required, and client certificate identities are explicitly allowlisted. The YCQL listener is not
started. Secret values do not belong in browser state, resource status, logs, audit records, Pulumi
output, or support bundles.

The product keeps the public Kubernetes API intentionally small: `YogaDatabase`,
`YogaDatabaseOperation`, `YogaApplicationRuntime`, `YogaValkey`, and `YogaRobustMQCluster`. These
resources are not a substitute for the Control Plane; they are signed desired-state inputs and
bounded local observations.

### Direct and GitOps use

Direct operation through Yogabase is authoritative. GitOps support exports deterministic YAML and
diffs for the customer's own local Git workflow; Yogabase does not push branches, create pull
requests, or write to customer repositories.

## Custom build delivery and Kubernetes

![Custom build delivery through the Yogabase Control Plane](assets/custom-build-delivery-flow.svg)

A custom build reaches Kubernetes through the authoritative Control Plane, never by receiving
Kubernetes or cloud credentials. A developer uploads one archive — from the Console's upload page, or
with a project-scoped `ybt_` developer token through `yogabase build upload`, the .NET
`YogabaseBuildsClient`, or the Unity Editor's Publish Server window — without preparing a manifest,
SBOM, or scan by hand. The Control Plane's Builds ingest stages the archive and derives the service
layout and a detached manifest from the archive and any declared name, version, and ports. The
member's isolated `artifact-admission` Job derives the CycloneDX inventory, scans it with a C# port
of OSV-Scanner's matcher against a digest-pinned OSV snapshot, and signs manifest, inventory, and
scan with the installation's admission key; a refused build names the failing field, or the CVE,
package, version, and fixed-in version. An admitted build is released as a runtime command under the
installation's approval policy, and signed intent travels over the outbound mutually authenticated
fleet channel to the member's subordinate local actuator, which alone reconciles the Kubernetes
workload.

The separately configured Delivery client is the CI path. It uses a short-lived workload OIDC token
to call the TLS-only `/api/v1/delivery` host, which the chart leaves off by default
(`controlPlane.delivery.enabled: false`). It can discover approved templates, preview and submit environments and
immutable artifact admissions, create releases, promote or roll back them, decommission an
environment, abandon a stuck operation, and follow the resulting durable operation through bounded
polling or server-sent events. Its artifact submission carries a credential-free provider-native
locator and exact bundle, manifest, SBOM, and scan-evidence digests.

The server now exposes a versioned runtime-host contract that lets a running build report health
and readiness, be reserved or allocated, and drain on shutdown
(`contracts/yogabase-runtime-host-v1.openapi.json`). The routes a workload actually calls, those of
its Pod's loopback relay, are described by a second document,
`contracts/yogabase-runtime-host-self-v1.openapi.json` (mirrored on 2026-10-20), which the
runtime-host tests hold the client to. `Yogabase.Yogabase.RuntimeHost` implements the workload side
of it as a faithful, modified C# derivative of Agones' client loop shape under
Apache-2.0; see [`docs/REFERENCE_REVIEW.md`](docs/REFERENCE_REVIEW.md) and
[`docs/UNITY_DEDICATED_SERVER.md`](docs/UNITY_DEDICATED_SERVER.md). PlayFlow, Firebase, Supabase,
and the Nakama .NET/Unity client remain reference-only, informing client lifecycle, session,
cancellation, polling, event-stream, and platform ergonomics. `Yogabase.Lambda` adopts the shape of
the Nakama Plus runtime-module API in C# under Apache-2.0, with its licence packed; see
[`docs/REFERENCE_REVIEW.md`](docs/REFERENCE_REVIEW.md).

## Application runtime and SDK boundary

Yogabase can expose a cluster-local application gateway for approved application operations. That is the boundary these SDKs implement:

- Bootstrap returns non-secret, project-bound REST, identity, and separately deployed realtime endpoint configuration.
- Authentication covers signup, login/logout, email confirmation, password reset, MFA and recovery codes, external-identity callbacks, plus authorization code with PKCE and rotating refresh tokens. SDKs never contain confidential client credentials.
- **The Data API, in every application client.** A client selects, inserts, upserts, updates and
  deletes rows of a project table through `POST /v1/data/{table}/select|insert|upsert|update|delete`,
  under row-level security as the signed-in user, and only where the bootstrap's requested-only
  `data-rest` row says it is served. The gateway has served it since 2026-09-23 and the mirrored
  contract declares it (`x-yogabase-data-api` states its limits and refusal codes, among them the
  named `413 data_request_too_large`), so the TypeScript, Python, Java and Dart clients, which are
  generated from that contract, carry it, and the authored .NET client has carried it since
  2026-10-20 (`yogabase.Data`, which names `data-rest` in `X-Yogabase-Accept-Capabilities` and checks
  the contract's bounds before it sends). Inside a Lambda, `Yogabase.Lambda` wraps the runtime's typed
  data API (`context.Data`, the same operations plus a database-function call, as the Lambda identity
  or, with `AsCaller()`, as the signed-in caller under row-level security) and its caller-scoped
  connection. Entity Framework Core stays server-side: no package here carries EF code or a model.
  No package here has called the Data API against a live installation; the .NET Data client and the
  Lambda wrapper have run in-process against the product's Data API route code and engine.
- Storage is mediated, digest- and idempotency-bound, and never returns provider credentials or signed URLs.
- Stateless Functions invoke an exact registered version through the bounded public contract. A payload is at most 1 MiB before base64, sent inline or as the object an object reference names, and exactly that is accepted; a request over the limit is refused with `413 payload_too_large`, whose problem names `limitBytes` and `maxBodyBytes`, and the .NET client carries both on `YogabaseApiException`. The actor invoke, workflow start and workflow cancel routes refuse over their own limit of 480 KiB with the same problem, and the Data API over its limit with `413 data_request_too_large`; the generated TypeScript, Python and Java clients decode the problem's `code`, `limitBytes` and `maxBodyBytes` for each, and each of those suites has a test that does.
- **Durable Jobs** (in the contract since 2026-10-03): a signed-in user submits a run of a deployed stateless function, reads it, cancels it and fetches its result through `/v1/projects/{projectId}/jobs`, behind the project token's `lambda.invoke` scope. A job type is the id of a deployed function and a job belongs to the user who submitted it. A submit is idempotent per user and key (the key is a member of the request body, not a header; the same request again answers the job it already made, a different one under that key is `409 job_idempotency_conflict`), and a result carries `X-Yogabase-Content-Sha256`. The product's client OpenAPI document describes these routes (`x-yogabase-jobs`), so Kiota generates their request builders for TypeScript, Python, Java and Dart, and each language's Jobs client stays authored on top because it carries what a generated builder cannot: the strict response checks, the result route's digest header and the bounds checked before anything is sent. The Java and Dart clients build every request from the generated builders and send it through the Kiota request adapter; the TypeScript and Python clients stay standalone and are held to the generated builders by tests that build each request both ways and compare verb, URL, headers and body; the .NET client has no generated code, so `ContractOperationParityTests` sends its operations and compares the first request with the contract's verb and path. `ContractLimitsAndCodesTests` compares the contract's limits, statuses and refusal codes with every client's constants. On 2026-10-03 the .NET, TypeScript, Python and Java Jobs clients were each also run against the product's real Jobs route code (`MapYogabaseJobs` with its authorization policy, gateway request body policy and result streaming, built from the product's route code as of that day and hosted in-process on loopback over the product's in-memory ledger test double and a stub result reader, so not its object store, its engine SQL or an installation): 25, 26, 24 and 24 end-to-end checks passed, among them the replay and the 409, 422, 404, 403 and 502 refusals, a payload of exactly 1 MiB, a 300 KB result with its digest and the real 413 problems of the Jobs and invoke routes. The harness references the product's code and is not part of this repository. They are the .NET `yogabase.Jobs`, the TypeScript `YogabaseJobsClient`, the Python `AsyncJobsClient`, the Java `YogabaseJobsClient` (also `YogabaseApplicationClient.jobs`) and the Dart `YogabaseJobsClient`, which cannot check the result digest because its typed send cannot read a response header. The Unity build of the .NET client carries the .NET one.
- **Schedules** (in the contract since 2026-10-03; the routes were served and acceptance-tested before the document described them, and the document was tightened the same day): a signed-in user creates, changes, reads, deletes and runs once a recurring (`cron` in `timeZone`) or one-shot (`oneShotAtUtc`) schedule of a deployed stateless function version through `/v1/projects/{projectId}/schedules/{scheduleId}`, behind the same `lambda.invoke` scope, and a schedule belongs to its creator (another user's is a bare 404, an update of it a 403). A create sends `If-None-Match: *` (201) and an update `If-Match` with the ETag last read (200); a delete (204) and the manual run, `POST …/trigger` with `{idempotencyKey, deadline}` (202; the same key against the same version answers the same invocation), need `If-Match` too. The ETag carries a version the scheduler also advances as it runs, so a client that holds one for long reads the schedule again; a stale precondition is 412 and a missing one 428. A payload is at most 1 MiB (`413 schedule_payload_too_large`); every other refusal is a problem with a fixed title and no code, or no body, and each client names those by status (`precondition_failed`, `precondition_required`, `not_found`, `conflict`, `forbidden`, `unsupported_media_type`, `request_failed`). A write holds the schedule's id to one rule, `^[A-Za-z0-9][A-Za-z0-9._~:-]{0,127}$`, because the id is one segment of the URL path: every client holds a create and an update to it before sending, while a schedule written before the rule under another id is still found by that id (percent-encoded) by a read, a run and a delete, so those three keep the older check and such a schedule can only be deleted and created again. Both policies are required (a request that leaves one out is a 400 naming the member), and `concurrencyPolicy` says what the scheduler does when an occurrence is due while an earlier one has not finished: `Forbid` skips it, `Replace` ends an earlier occurrence that is still queued and skips the new one while one is running (a Lambda that is executing cannot be stopped), a skipped occurrence is consumed, not deferred, and a manual run is not an occurrence, so the policy does not touch it. A manual run of a schedule whose function version is no longer admitted is a 409, as a write of it is. A build of the product from before these changes starts every occurrence whichever policy is chosen, answers that manual run 503 and refuses only an id with a `/` (400). The clients are the .NET `yogabase.Schedules`, the TypeScript `YogabaseSchedulesClient`, the Python `AsyncSchedulesClient`, the Java `YogabaseSchedulesClient` (also `YogabaseApplicationClient.schedules`) and the Dart `YogabaseSchedulesClient`, related to the generated builders as the Jobs clients are. On 2026-10-03 the first four were run against the product's real schedule route code (`MapYogabaseSchedules`, built from the product's route code as of that day over the product's in-memory schedule store test double, so not its store or an installation): 36, 41, 36 and 36 end-to-end checks passed, among them the create, update, delete and run preconditions, the 404 and 403 of another user's schedule, every refusal status, a payload of exactly 1 MiB, the real 413 problem, a run key the queue already holds and a function version that is no longer admitted. That run predates the id rule, the concurrency enforcement and the 409 of a manual run, and was not repeated against the product as of the pinned contract. The Dart client derives the ETag from the response body because its typed send cannot read headers.
- Actor Lambdas address one versioned, project-local instance; Workflow Lambdas start one immutable definition version and expose only the v1 cancel operation.
- Realtime uses the separately returned SignalR origin for topic subscribe/unsubscribe, signed Lambda-binding invocation, events a Lambda publishes to a topic or addresses to one signed-in user, and at-least-once completion events deduplicated by `eventId` in the authored .NET client. JSON remains the default Hub protocol. The authored .NET client also exposes each event's payload as raw JSON text (`PayloadJson`) beside the typed `JsonElement`, and presence where the bootstrap's requested-only `realtime-presence` row is available: track and untrack the signed-in subject on a topic, read the topic's present subjects, and receive join and leave events.
- Game servers: through `/v1/servers/*` a signed-in player allocates a Ready instance of a hosted service, lists and reads its environment's instances, and releases its own allocation (`stop` never deletes an instance); an allocation returns the address the installation advertises for the instance's node and the ports the member observed. The .NET client also accepts the Agones-shaped typed `address` and `addresses` the product will add once it is the minimum.

Clients are provider-neutral and shard-oblivious. They never receive a cloud selector, cloud credential, Kubernetes identity, TServer address, tablet/shard identifier, shard key, physical placement map, or topology generation. YugabyteDB owns its internal routing and placement; Yogabase observes bounded health and capacity facts without introducing another data-path router.

The SDKs deliberately exclude generic human Control Plane management APIs, infrastructure provisioning, fleet commands, Kubernetes CRDs, approval mutation, remote shell, `pods/exec`, general notifications, analytics, GameSessions, and provider credentials. The only Control Plane surfaces are the workload-authenticated Delivery v1 contract described above, the Builds v1 build-ingest contract that a developer tool (the .NET `YogabaseBuildsClient` or the Unity Editor publisher) calls with a project-scoped `ybt_` developer token, and the self-scoped Runtime Host v1 contract that a hosted workload reaches over loopback through its Pod-local relay, which accepts no application or Delivery credential and exposes no allocation call to the workload.

These surfaces changed on 2026-09-17. The product implements each of the following, and where it belongs to a mirrored contract that contract carries it; each item says what these packages do with it:

- A Lambda uploaded through Builds must declare its execution kind (`Stateless` or `Stateful`) and its realtime ingress and egress binding ids; an upload without them is refused at ingest with `400 BUILD_LAMBDA_DECLARATION_REQUIRED` (builds contract). `YogabaseBuildsClient` sends it when a request sets `Lambda`, and the Unity publisher when its settings publish a Lambda archive; a server upload sends none of it. A bundle whose entry is `RegisterLambdas` also states the Lambda ids it registers in `registeredLambdaIds` (refused when missing with `BUILD_LAMBDA_REGISTRATION_REQUIRED`, and refused beside a single-function bundle); both tools send it only for a registering bundle.
- A Lambda's realtime publish to a topic its function version did not declare is refused with `403 realtime_binding_not_declared` (the Lambda runtime API, which no mirrored contract describes); `Yogabase.Lambda` surfaces it as a `LambdaApiException` with status 403 and that code.
- The runtime-host assignments page carries an additive `headState` and `headSequence` (runtime-host contract); the .NET runtime-host client and the Unity runtime-host package read them when the Control Plane states them (a page without them, or with a value outside the contract, is read as having no head), and `ReadCurrentStateAsync` answers the current state in one read, paging to the end of the stream against a Control Plane that predates the head.
- The client bootstrap adds a requested-only `realtime-presence` row for a client that names it in `X-Yogabase-Accept-Capabilities`, and the realtime Hub gains `TrackPresence`, `UntrackPresence` and `GetPresence` (application contract). The .NET client requests the row and implements all three calls and the `presence` event; the other languages have no realtime client.
- A game-server allocation's `host` is the address the installation advertises for the instance's node, picked the way Agones picks one, rather than a node-internal address. Typed `address` and `addresses` fields are decided, and the product emits them only once an SDK that accepts them is the minimum; the .NET client reads them when present (`Address`, `Addresses` on the instance and on each port) and the other languages' generated clients keep unknown fields as additional data.

## Capability truth

Yogabase reports module and protocol availability from signed observations and release-locked evidence. Configuration alone is never treated as a successful deployment. A capability gate opens on passing local conformance evidence (k3d runs and in-process rehearsals); real-cloud runs are follow-up validation, not prerequisites. Twenty-three of the product's 27 compiled gates are open; the four closed ones are the AWS Marketplace gates and vendor-created clusters.

| Area | Current boundary |
|---|---|
| Core | The primary product scope: managed YugabyteDB/YSQL operations as durable operations under the installation's approval policy (approved at submission by default; independent four-eyes approval is a policy any installation may declare). |
| Application modules | Authentication, Storage, Realtime (SignalR), and Remote Config ship enabled in the chart. Realtime requires Authentication, and Storage reports that it is waiting for an object store until one is bound. |
| YB Lambda (formerly Extended) | Stateless Functions, Actor Lambdas, Workflow Lambdas, Dynamic Code, the sandbox, Scheduler/Jobs and consumer triggers, plus hosted self-contained services, with optional Orleans, SignalR, Redis (Valkey), Kafka and RabbitMQ/AMQP (RobustMQ) APIs. These ship enabled in the chart, and the capabilities behind compiled evidence gates (hosted services, the runtime host, consumers and consumer triggers, and the Lambda Valkey and RobustMQ APIs) are qualified on local evidence (in-process rehearsals and k3d conformance runs). A function written in the Console is compiled, scanned, and signed by the installation's own builder and then released; the separate dynamic Stateless and Actor Lambda modules, which execute customer-submitted C# on demand, stay off. Consumer triggers from RobustMQ queues, and external Kafka and AMQP consumers against a customer's own brokers, are qualified on local evidence. A Lambda-side typed data API and Entity Framework Core exist in the product since 2026-09-23; the `Yogabase.Lambda` wrapper in this repository wraps the typed data API and the caller-scoped connection, and leaves Entity Framework Core to the bundle. The Console groups YB Lambda with Authentication, Storage, Realtime, Remote Config, Valkey and RobustMQ under its Extended tier. |
| Valkey | Uses a Yogabase-owned C# reconciler. Lifecycle, Job-actuated promotion, and Redis protocol semantics are qualified on local conformance evidence, as is direct Lambda access over TLS under a per-project ACL identity; real-cloud runs remain follow-up validation. |
| RobustMQ | Uses a bounded Yogabase-owned C# lifecycle port. Lifecycle and authenticated MQTT are qualified on local evidence, as are gateway-mediated Lambda publication over MQTT, Kafka and AMQP, durable queues and streams, replay, consumer groups and consumer triggers. The broker's native Kafka and AMQP protocol endpoints are not offered: applications reach messaging through their functions. The broker runs single-node. |
| Enterprise | A library of capabilities, not a tier. The Console's Enterprise group offers Audit, Approvals (showing the installation's policy) and Migrations on every installation; Authorizations, database assurance tiers and installation-wide Backup are shown as not built; disaster recovery, performance, workload security, maintenance and data movement are reported as one unavailable entry. |

This README describes product architecture and boundaries, not a promise that a disabled or evidence-gated capability is currently enabled.

## Packages

The identifiers below are the names each package is built under. None is on a registry yet, and a name is not a support commitment. Version `0.3` added the workload-authenticated Control Plane Delivery v1 surface and cross-platform lifecycle improvements. The application contract has changed since without a version change: the client-facing Data API was removed (2026-09-08) and restored in the contract (2026-09-23, so only the generated clients carry it), the `/v1/servers/*` game-server surface was added, the function invoke route gained its named `413 payload_too_large` refusal (and the actor, workflow, Jobs, schedule and Data routes their own), the durable Jobs routes and the schedule routes were described in it on 2026-10-03, and the .NET package gained its Servers, Builds, durable Jobs and Schedules clients, all still versioned `0.3.0-preview.1`. Version `0.4` is the first to follow them: the .NET, TypeScript, Python and Dart clients gained a headless password sign-in (2026-10-07), and the Java package takes the same number. A package that says `0.3.0-preview.1` was built before the sign-in and has none. The .NET client then gained sign-in with an external provider (2026-10-08, two calls around the application's own browser step), and the TypeScript, Python and Dart clients followed in their `0.4` versions. Version `0.5` (2026-10-20) is the .NET client's Data API client and the Java client's sign-in, by password and with an external provider; the TypeScript, Python and Dart packages did not change and stay at `0.4`. `Yogabase.Lambda` `0.2` wraps the runtime's typed data API and caller-scoped connection.

| Platform | Location | Current package identity | Qualification |
|---|---|---|---|
| .NET | `src/dotnet/Yogabase.Yogabase.Client` | `Yogabase.Yogabase.Client` `0.5.0-preview.1` | Authored application (password and external-provider sign-in, the latter in two calls around the application's own browser step and never run against a real provider), PKCE, SignalR, Data API, Delivery, game-server, durable Jobs, Schedules, and build clients; `netstandard2.1` and `net10.0`; fake-transport tests (on 2026-10-20, on Linux with SDK 10.0.402, the client suite's 513 cases and 831 across the solution ran, and all passed but the live smoke test, which was skipped), the in-process runs against the product's route code described above (the Data client's on 2026-10-20), and the live smoke test's passes on 2026-10-10 against a local installation and on 2026-10-12 through a public gateway name (sign-in, a function, Storage; the realtime step did not run) |
| TypeScript | `languages/typescript` | `@yogabase/client` `0.4.0-preview.1` | Generated application client (including the Jobs, schedule and Data request builders) plus an authored headless sign-in (`YogabaseSession`, Node only) with a password, or with an external provider in two calls around the application's own browser step (run only against fake-fetch tests, never a real provider), and Delivery, Jobs, Schedules and storage-key facades; on 2026-10-20, after a fresh `npm ci` on Node 25.9.0 (Linux), its 104 tests ran, 103 passed and the live smoke test was skipped; the live smoke test passed on 2026-10-10 against a local installation and on 2026-10-12 through a public gateway name |
| Python | `languages/python` | `yogabase-client` `0.4.0.dev1` (module `yogabase_client`) | **frozen 2026-10-21 (unmaintained until a user asks).** The facts below are as last recorded and are not updated while frozen. Generated application client (including the Jobs, schedule and Data request builders) plus a headless sign-in with a password, or with an external provider in two calls around the application's own browser step (run only against fake-gateway tests, never a real provider), and authored async Delivery, Jobs, Schedules and storage-key facades; hash-locked dependencies (`requirements.lock`, `build-requirements.lock`) and an exactly pinned `setuptools==84.0.0` backend; its own HTTP clients trust the platform certificate store, although `certifi` stays installed because httpx and httpcore require it; on 2026-10-20, in environments made from the locks with `--require-hashes`, its 118 tests (117 passed, the live smoke test skipped) ran from source on CPython 3.13.16 and 3.12.3 (Linux) under `-X dev` with `ResourceWarning` as an error, as they had on 3.13.12 and 3.12.13 on 2026-10-08; a wheel and sdist were built from the locks on 2026-10-07, and on 3.13.12 111 tests ran against the wheel installed in a clean virtual environment (`pip check` clean), a run not repeated since the external sign-in was added; the live smoke test passed on 2026-10-10 against a local installation and on 2026-10-12 through a public gateway name; only Python 3.12 and 3.13 are claimed |
| Java/JVM | `languages/java` | `io.yogabase:yogabase-client` `0.5.0-preview.1` (package `io.yogabase.client`) | **frozen 2026-10-21 (unmaintained until a user asks).** The facts below are as last recorded and are not updated while frozen. Generated application/Delivery clients (including the Data request builders) plus authored JVM facades: a headless sign-in with a password, or with an external provider in two calls around the application's own browser step (`YogabaseSignIn`, `YogabaseSession` as the Kiota token provider; run only against a fake loopback gateway, never a real provider or installation), and the Jobs and Schedules clients (both built over the generated request builders); Kotlin/JVM interop path; on 2026-10-20, with JDK 21.0.12.1 at `--release 17` and Maven 3.9.11 (Linux), `mvn test-compile` and `mvn package` were clean and its six main-class checks passed (storage key, generated surface 26, Delivery, Jobs 336, Schedules 1988, sign-in 120) against Kiota 1.9.3; no live run is recorded |
| Dart | `languages/dart` | `yogabase_client` `0.4.0-dev.1` | **frozen 2026-10-21 (unmaintained until a user asks).** The facts below are as last recorded and are not updated while frozen. Experimental generated application/Delivery source (including the Data request builders) plus a headless sign-in with a password (`signInWithPassword`), or with an external provider in two calls around the application's own browser step (`startExternalSignIn`, `completeExternalSignIn`; run only against fake-gateway checks, never a real provider), and authored Delivery, Jobs, Schedules and storage-key facades; Dart VM only, not Flutter web; on 2026-10-20, with Dart 3.13.5 (the Linux x64 archive, SHA-256 checked), `dart pub get --enforce-lockfile` left the lockfile unchanged, `dart analyze --fatal-infos --fatal-warnings` reported no issues and its five standalone source programs (Delivery, Jobs, Schedules, sign-in, Storage) passed, as on macOS arm64 on 2026-10-08 (there is no `package:test` suite: `test` is not in the lockfile); Storage object reads and writes work through a raw-bytes `application/octet-stream` factory pair the application client registers; the live smoke program (password sign-in, a function, Storage) passed on 2026-10-10 against a local installation and on 2026-10-12 through a public gateway name; the external sign-in has not run against a real provider |
| .NET Lambda context | `src/dotnet/Yogabase.Lambda` | `Yogabase.Lambda` `0.2.0-preview.1` | Authored runtime-context wrapper for a Lambda bundle: project database (and, on `net10.0`, the same pool as the signed-in caller), the runtime's typed data API (`Data`: select, insert, upsert, update, delete and a database-function call, as the Lambda identity or as the caller), Storage, Users, Realtime (including publishing to one user), the caller, Actor, Workflow, and reminder calls, direct Valkey access and the runtime `IDistributedCache`, gateway-mediated MQTT, Kafka, and AMQP publication, stream reads and partition declarations, durable-queue receive with acknowledgement, and a typed `yogabase.cdc.v1` change-event and record-key parser; `netstandard2.1` and `net10.0` (the database handles only on `net10.0`); Entity Framework Core is not wrapped; 235 fake-provider tests passed on 2026-10-20, and the data wrapper also ran in-process against the product's Data API engine that day; no live installation evidence |
| .NET runtime host | `src/dotnet/Yogabase.Yogabase.RuntimeHost` | `Yogabase.Yogabase.RuntimeHost` `0.2.1-preview.1` | Authored self-scoped runtime-host client; `netstandard2.1` and `net10.0`; waits out `503 RUNTIME_HOST_INSTANCE_PENDING` on a heartbeat or Ready call for up to `StartupPendingBudget` per call (default 60 seconds) using the refusal's `RetryAfterSeconds`, and reports background heartbeat failures through `Connect(onHeartbeatFailure)` (added without a version change); since 0.2.0 the relay's bodyless `413`, `429` and `504` answers throw `YogabaseRuntimeHostException` with their status (they were a JSON parse error), and `IsRetryable` marks the pending refusal, `429` and `504`; since 0.2.1 the default `RequestTimeout` is the relay's 30 second answer timeout plus 10 seconds (it was 10, so a default client cancelled before the relay's `504`) and a local timeout throws `YogabaseRuntimeHostException` (status 0, code `CLIENT_REQUEST_TIMEOUT`, retryable) instead of a bare cancellation, `WatchAssignmentsAsync` ends by throwing on a refusal the document makes terminal for the assignments route and retries only retryable answers and transport faults, a `headSequence` below 1 reads as no sequence, and a refusal's code is read from `code` alone (no `title` fallback); fake-transport tests only in this repository — an earlier copy ran live only as the Unity package's 0.1.0 runtime module |
| Unity builds (8.0 line) | `src/unity/Yogabase.Yogabase.Client.Unity`, `src/unity/Yogabase.Yogabase.RuntimeHost.Unity` | not packaged; assemblies `Yogabase.Yogabase.Client` `0.5.0-preview.1` and `Yogabase.Yogabase.RuntimeHost` `0.3.1-preview.1` | The same sources as the two .NET packages, compiled for `netstandard2.1` only against SignalR 8.0.32 and System.Text.Json 8.0.5 (`src/unity/Directory.Packages.props`), because Unity 6000.5 binds its own 8.0 BCL extensions and a plugin built against 10.x is CS1705 there. `UnityBuildTests` pins that neither build references or locks anything at 9.x or above; both builds are deterministic. A Unity project takes these as digest-locked assemblies |
| Unity | `languages/unity/io.yogabase.runtime-host` | `io.yogabase.runtime-host` `0.3.1-preview.1` | UPM package carrying the runtime-host module (with the startup wait-out above, the one-read page head, the exceptions for the relay's bodyless answers, and the request timeout and assignment-watch handling of the .NET 0.2.1 line), its `link.xml`, and the Editor publish flow (`Yogabase ▸ Publish Server`), which also publishes a prebuilt Lambda archive with its execution declaration and, for a `RegisterLambdas` bundle, its `registeredLambdaIds`; its Runtime folder and Editor scripts compile at C# 9 against Unity 6000.5.7f1 reference sets, but no Unity Editor import of this version is recorded, no IL2CPP build carries it, no live relay session has run it, and no Editor has run the publish window. The 0.1.0 runtime module did run live in an IL2CPP dedicated server (see Platform qualification) |
| .NET sample | `samples/dotnet/Yogabase.ServerPathProbe` | not a package | Editor-free diagnostic walk of the server-side game path (server start, one UDP datagram, a Lambda invoke, an optional realtime subscription) with a player access token read only from environment variables; argument, sign-in-context, and preview tests only, never run against a live installation |

The immutable application snapshot is `contracts/yogabase-client-v1.openapi.json` (SHA-256 `5fcbed400575ae191a577202a3548e41f7ef1b18f90971d7b58fc2e71fa867de`); it carries the player-facing `/v1/servers/*` game-server surface, the restored `/v1/data/{table}/*` Data API, the `413` refusals of the function, actor, workflow, Jobs, schedule and Data routes, the durable Jobs routes (`/v1/projects/{projectId}/jobs`) and the schedule routes (`/v1/projects/{projectId}/schedules/{scheduleId}` and its `/trigger`), which are therefore generated for TypeScript, Python, Java and Dart (35 paths, 39 operations and 49 schemas). The build-ingest snapshot is `contracts/yogabase-builds-v1.openapi.json` (SHA-256 `368a15e2baa2567983894752444cf7efe5687da2e40b461cef7523d124923750`); it is a developer-tool surface reached with a project-scoped `ybt_` developer token, implemented by the .NET `YogabaseBuildsClient` and the Unity Editor package and deliberately not generated for the other languages. The workload snapshot is `contracts/yogabase-delivery-v1.openapi.json` (SHA-256 `d44f95c0364d49071aa29e0d555687c942d91acbc9215cb71bd1eefb294d9c39`). The runtime-host snapshot is `contracts/yogabase-runtime-host-v1.openapi.json` (SHA-256 `c09a0842c3e295685a4112638ea74a6ab23e3b390fe2f6ab0e1a635f1e6797e2`); it is checked in as the server-side reference for the lifecycle states and error codes and is deliberately never code-generated, because a workload calls the self-scoped relay routes rather than the instance-addressed paths that document declares. The self-scoped runtime-host snapshot is `contracts/yogabase-runtime-host-self-v1.openapi.json` (SHA-256 `a2a9e201eaee833251f66eb89acc73edd7ce06383170824d39f6b386509f95aa`, mirrored on 2026-10-20); it describes the five loopback relay routes a workload calls (`POST /runtime-host/v1/self/{heartbeat,ready,reserve,shutdown}` and `GET /runtime-host/v1/self/assignments`), the lease and page schemas with `headState` and `headSequence`, each route's refusal codes, the relay's own limits and its bodyless `413`, `429` and `504` answers (5 paths, 5 operations and 10 schemas). It is checked in and checksum-verified but not generated: the authored runtime-host clients speak those routes by hand, and `YogabaseRuntimeHostContractTests` holds their routes, limits and refusal codes to it. Their adjacent checksums, Kiota locks, and `MANIFEST.json` bind this repository to the exact contracts and generator. After the snapshot gained the `/auth/external/{provider}/start` and `/return` routes and the callback's `codeVerifier` (2026-10-08) and the four application trees were regenerated, the TypeScript (98 tests, 97 passed, one skipped), Python (111 tests on CPython 3.12 and 3.13, one skipped), Dart (analyzer clean, five source programs) and Java (five main-class checks) suites ran again on 2026-10-08 with their totals unchanged. On 2026-10-20 regenerating all six trees with the pinned Kiota changed nothing that is committed.

Every document under `contracts/` is a byte-identical copy of the product's published OpenAPI document of the same name, as last mirrored on 2026-10-08 (the self-scoped runtime-host document on 2026-10-20) and compared with the product's again on 2026-10-20, when the four earlier ones were still identical; the digests above identify exactly which revision of each document this repository is built against. `ContractDigestTests` pins each file to its `.sha256` sidecar and to the digests published in `MANIFEST.json` and both READMEs; it cannot see the product, so it does not itself prove byte identity. `MANIFEST.json` also pairs each generated contract's digest with the description hash Kiota wrote into the generated trees' locks, and `ContractOperationParityTests` fails when the mirrored application contract holds an operation that a shipped language has no surface for. The former `contracts/planned/` documents were written here while the product implemented the build and game-server surfaces; the product has published both, so they were deleted rather than reconciled.

## Local verification

```text
dotnet restore Yogabase.SDKs.slnx --locked-mode
dotnet build Yogabase.SDKs.slnx -c Release --no-restore
dotnet test Yogabase.SDKs.slnx -c Release --no-restore --no-build
dotnet pack src/dotnet/Yogabase.Yogabase.Client/Yogabase.Yogabase.Client.csproj -c Release --no-restore
dotnet pack src/dotnet/Yogabase.Yogabase.RuntimeHost/Yogabase.Yogabase.RuntimeHost.csproj -c Release --no-restore
dotnet pack src/dotnet/Yogabase.Lambda/Yogabase.Lambda.csproj -c Release --no-restore

cd languages/typescript && npm ci && npm test
cd languages/python && python3 -m venv .venv && .venv/bin/python -m pip install --require-hashes -r requirements.lock -r build-requirements.lock
cd languages/python && PYTHONPATH=src .venv/bin/python -X dev -W error::ResourceWarning -m unittest discover -s tests && .venv/bin/python -m build --no-isolation
cd languages/java && mvn -B -q test-compile && mvn -B -q org.apache.maven.plugins:maven-dependency-plugin:3.8.1:build-classpath -Dmdep.outputFile=target/cp.txt
cd languages/java && for c in YogabaseStorageLogicalKeyTest YogabaseGeneratedSurfaceTest YogabaseDeliveryClientTest YogabaseJobsClientTest YogabaseSchedulesClientTest YogabaseSignInTest; do java -cp "target/classes:target/test-classes:$(cat target/cp.txt)" io.yogabase.client.$c || break; done   # the checks are main-method programs; mvn test would run none of them
cd languages/dart && dart pub get --enforce-lockfile && dart analyze --fatal-infos --fatal-warnings && dart run test/yogabase_delivery_source_test.dart && dart run test/yogabase_jobs_source_test.dart && dart run test/yogabase_schedules_source_test.dart && dart run test/yogabase_sign_in_source_test.dart && dart run test/yogabase_storage_source_test.dart

scripts/compile-unity-editor.sh
```

[`docs/CLOUD_ENVIRONMENT.md`](docs/CLOUD_ENVIRONMENT.md) records the exact toolchains these commands ran on in a Linux cloud environment on 2026-10-20, with the publishers' digests and the commands that install them, and [`docs/PARITY_2026-10-20.md`](docs/PARITY_2026-10-20.md) records that day's comparison of every client with the product and each suite's result.

`scripts/compile-unity-editor.sh` compile-checks the Unity package's Editor scripts (not its Runtime
folder) in a real Unity Editor compile environment: it reuses a response file Unity's own Bee build
already wrote for an Editor assembly, keeps its Editor-installation references, defines and language
version, and swaps in this package's sources. Set `UNITY_RSP` to point at one; without it the script
looks in one default local Unity projects folder. If it finds no response file, or no Roslyn
compiler under the .NET SDK, it exits 2 and says the scripts were not checked, rather than reporting
a pass it did not perform.

Run `scripts/regenerate-clients.sh` to verify all five contracts, restore pinned Kiota 1.34.1, regenerate all four application clients, and regenerate the Java and Dart delivery protocol clients. The two runtime-host contracts and the builds contract are checksum-verified but never generated, and the authored runtime-host, builds, and Lambda modules are not regenerated source. Authored delivery facades are preserved. Each generated-language folder retains its own package manifest, dependency lock where available, Kiota lock, source, documentation, and conformance helper. Downloaded tools, caches, dependency trees, and build intermediates are excluded.

### Connecting a client to a self-hosted installation

The .NET client's public entry point is `Yogabase.Yogabase.YogabaseClient`. It has no public constructor; a consumer creates it through the static `YogabaseClient.CreateAsync(Uri gatewayEndpoint, string publishableKey, YogabaseClientOptions? options = null, CancellationToken cancellationToken = default)` factory. `gatewayEndpoint` is the installation's application-gateway origin — exactly the origin the installation's client configuration names, because `CreateAsync` refuses any other origin with `client_config_origin_mismatch` — and `publishableKey` is the project's publishable key. Plain `http://` is accepted only for a loopback origin with `AllowInsecureLoopback`.

```csharp
using Yogabase.Yogabase;

await using var yogabase = await YogabaseClient.CreateAsync(
    new Uri("https://<installation-host>"),
    "<publishable-key>");

await yogabase.Auth.SignInWithPasswordAsync("<user-email>", "<user-password>");
```

`<installation-host>` and `<publishable-key>` are placeholders; this repository does not choose either. The hostname of a self-hosted installation and the publishable key for a given project are both supplied by that installation's owner. A real publishable key begins with `sy_pub_`, and `CreateAsync` rejects one that does not.

`SignInWithPasswordAsync` signs the user in with no browser and keeps the tokens every other call uses. The user must already exist: a user signs up with an e-mail address and a password (a project's gateway runs with open sign-up) and, unless a self-hosted installation turned confirmation off, must confirm the address before signing in; with no e-mail transport a sign-up then answers `503 authentication_email_unavailable`. The [client's README](src/dotnet/Yogabase.Yogabase.Client/README.md#getting-started) lists the refusals. No package is on NuGet yet, so reference the client's project from a checkout of this repository. The TypeScript client signs in the same way, for Node only, with `YogabaseSession.signInWithPassword`, or with a provider through `startExternalSignIn` and `completeExternalSignIn`; see its [README](languages/typescript/README.md#getting-started).

The Python client signs in the same way: `session = await sign_in_with_password("https://<installation-host>", "<publishable-key>", "<user-email>", "<user-password>")` returns a session that is the Kiota authentication provider and the token source of the Jobs and schedules clients, with the same refusals as a `YogabaseAuthError`; with a provider it is `start_external_sign_in` and `complete_external_sign_in`. See its [Getting started](languages/python/README.md#getting-started). It is not on PyPI yet and supports Python 3.12 and 3.13, the two versions it has run on.

The Dart client signs in the same way, for the Dart VM only: `final session = await signInWithPassword(Uri.parse('https://<installation-host>'), '<publishable-key>', '<user-email>', '<user-password>')` returns a session that is the Kiota authentication provider (`DefaultRequestAdapter(authProvider: session)`), with the same refusals as a `YogabaseAuthException`; with a provider it is `startExternalSignIn` and `completeExternalSignIn`. See its [Getting started](languages/dart/README.md#getting-started). It is not on pub.dev yet and has run on Dart 3.13.5 only.

The Java client signs in the same way: `YogabaseSession session = YogabaseSignIn.signInWithPassword(URI.create("https://<installation-host>"), "<publishable-key>", "<user-email>", "<user-password>")` returns a session that is the Kiota `AccessTokenProvider` (`YogabaseApplicationClient.forSession(session)` builds the application client over it, and `session.projectId()` is what the Jobs and schedules clients take), with the same refusals as a `YogabaseAuthException`; with a provider it is `startExternalSignIn` and `completeExternalSignIn`. See its [Signing in](languages/java/README.md#signing-in). It is not on Maven Central yet and has run only against a fake gateway on a loopback port.

Signing in with Google, Microsoft or Apple is two calls around the application's own browser step in the .NET, TypeScript, Python, Java and Dart clients. The address to register with each provider is `https://<gateway>/auth/external/<provider id>/return`. It has run only against fake-transport tests written from the gateway's route code, never against a real provider; each client's README, under "Signing in with a provider", lists the refusals.

A publishable key is not a secret. It identifies the project to the gateway and is safe to ship in client code, and it authenticates nothing: the bootstrap document it fetches carries endpoints, identifiers, and capability facts only. Yogabase issues no privileged counterpart key that a client could hold instead. Every authenticated call carries a per-user access token obtained from the project's OIDC authority, so a client never ships a credential that speaks for the project as a whole.

On an installation that uses named console seats, a seat is claimed on the installation's Console at `https://<console-host>/console-seat/claim`. The Console host is supplied by the installation's owner and is generally not the application-gateway host above, and a console seat never authenticates SDK calls.

## Platform qualification

Generated REST source is not the same as a production-qualified platform integration. `netstandard2.1` compatibility alone does not qualify Unity or AOT. The Python package passed its tests from source on 2026-10-08 (macOS arm64) and 2026-10-20 (Linux x86-64) and from its built wheel on 2026-10-07 (before the external sign-in was added), and the Dart package passed analysis and its standalone source checks on 2026-10-08 (macOS arm64) and 2026-10-20 (Linux x86-64). The .NET, TypeScript, Python and Dart live smoke tests (password sign-in, a function, Storage) passed on 2026-10-10 against a local installation and on 2026-10-12 through a public gateway name; nothing else has run against an installation. Android, Flutter, Kotlin/JVM runtime integration, Unity IL2CPP of the application client, native secure storage, interactive-cookie handling, non-.NET SignalR integration, sign-in with a real external provider, and live-gateway conformance each still need explicit platform evidence before they can be claimed as supported.

The runtime-host module and its Unity package are authored against the checked-in contracts and covered in this repository by fake-transport unit tests, which also hold the client's routes, limits and refusal codes to the self-scoped relay document. The Unity package's 0.1.0 runtime module has run live: compiled by Unity 6000.5.7f1 into an IL2CPP Linux arm64 dedicated server and hosted on a self-hosted Yogabase installation, its heartbeat and Ready calls were accepted through the Pod-local relay, first on 2026-09-07, when the instance reached Ready, and again on 2026-09-16 and 2026-09-17. That run also showed that assignment polls carrying the query-string `resumeToken` never advanced past the first page, because the product's relay dropped the query string; the product fix made on 2026-09-17 has not yet been observed live, and no allocation has yet been delivered to a build through the relay. The current versions — the .NET package 0.2.0 and Unity package 0.3.0, including the startup wait-out, the page head and the exceptions for the relay's bodyless answers — have not run in an IL2CPP build or against a live relay, and the per-family runtime-host conformance battery (restart, replay, duplicate, stale-epoch, double-allocation, reservation-expiry, disconnect, Pod-replacement, drain/shutdown-race, credential-confusion, and secret-negative cases) has not been run. Treat both as unqualified previews.

## Licence

This repository is licensed under the Apache License, Version 2.0; the full text is in [`LICENSE.md`](LICENSE.md). Copyright 2026 Yogabase.

Third-party notices are in [`NOTICE`](NOTICE): the runtime-host client is a modified derivative of the Agones C# and Unity client SDKs, and `Yogabase.Lambda` follows the runtime-module shape of Nakama Plus, both Apache-2.0, with the unmodified licence texts under [`licenses/`](licenses/) and the copying boundaries recorded in [`docs/REFERENCE_REVIEW.md`](docs/REFERENCE_REVIEW.md). Every package dependency, its licence and its obligations are inventoried in [`docs/DEPENDENCY_LICENSES.md`](docs/DEPENDENCY_LICENSES.md).
