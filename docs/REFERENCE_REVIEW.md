# Client SDK reference review

Status: reference-only engineering evidence
Last reviewed: 2026-09-01

## Authority and copying boundary

The checked-in Yogabase OpenAPI contracts and the matching current server implementation are the
only wire-contract authorities for this repository. The projects below were inspected only for
client-library ergonomics, lifecycle handling, cancellation, packaging, and platform conventions.
They do not define Yogabase routes, models, terminology, or product scope. No reference source,
generated output, public API name, wire contract, example, asset, or runtime dependency is copied
into these packages.

The two authoritative contracts are:

- `contracts/yogabase-client-v1.openapi.json`: target-local application gateway.
- `contracts/yogabase-delivery-v1.openapi.json`: workload-authenticated Control Plane delivery API.

Application and delivery clients retain separate origins and credentials. Neither client receives
cloud credentials, provider selectors, Kubernetes identities, cluster coordinates, or direct
Kubernetes mutation authority.

## Locally inspected references

| Reference | Immutable/local identity | Licence | Bounded SDK lesson |
|---|---|---|---|
| Supabase JavaScript | `v2.112.4`, `ff9909e6c813662144ff6d92a010f74627c6262a` | MIT | One configured root client, capability composition, session-aware request shaping. |
| Supabase Python | `v2.31.0`, `6f522d35aa94e4a6171cf84931d13e0bbae2ffba` | MIT | Synchronous/asynchronous entry-point conventions and explicit lifecycle ownership. |
| Supabase Flutter/Dart | `supabase_flutter-v2.17.2`, `d34329274b7cba71629da1414171737025d9ab5c` | MIT | Platform adapters, initialization, session events, and resource disposal. |
| Supabase Kotlin | `3.7.0`, `835bfb0fe8e1d277d28c55548ee54af899fec175` | MIT | Coroutine-friendly modules and explicit install/configuration boundaries. |
| Supabase C# | `v1.6.0`, `ecf3354116fac924e9aacd7c2f5982bae9140a27`; consolidated comparison `87d451e0e2882be8387dd79f7521d01d6161dc21` | MIT | Options, composed service facades, session lifecycle, and platform-neutral transport seams. |
| Firebase JavaScript SDK | `50213a1cdba8e3b0f029197ec20f47c1110060a6` | Apache-2.0 | Validated bootstrap, modular capability access, caller-owned persistence, and offline/error semantics. |
| Agones client SDKs | `v1.60.0`, `67b44a783218f7811084adaa5d9b334fb28e44ba` | Apache-2.0 | Health/readiness lifecycle shape, bounded watches, cancellation, graceful shutdown, and consistent multi-platform concepts. **Adopted, not merely inspected** — the client loop shape is ported into `Yogabase.Yogabase.RuntimeHost`; see the adopted-port record below. |
| PlayFlow Unity SDK | `v1.0.10`, `e4f8e94bdbfbde688f3b847068134a6817f0773a` | Apache-2.0 | Remote build/service lifecycle requests, bounded polling, errors, and Unity-facing configuration. |
| Nakama .NET/Unity client | locally installed `3.21.2+437fb84f0d6ce48aaf7bb8aa42a0b6ac034256de`; assembly SHA-256 `8f04cb17affceb10a4958b51573429458ca631deb522cc2750d307f1a4c20218` | Apache-2.0 | Session refresh ownership, HTTP/socket separation, reconnect state, cancellation, and Unity main-thread adapter boundaries. |
| Nakama Plus runtime modules | `18e4d2cd53d5f546d5684d6f70996c08234afc45` | Apache-2.0 | Fail-closed module discovery, RPC failure boundaries, and lifecycle visibility only; it is not a client SDK. |

The repository checkouts remain under the Yogabase product repository's ignored `.references`
directory and are not package inputs. The Nakama client observation comes from the owner's existing
local Unity installation; only its assembly metadata, digest, licence headers, and externally
visible client lifecycle were reviewed. It is not copied, linked, bundled, or added as a package
dependency.

## Applied design rules

- Construct one root client from validated options and expose cohesive modules from it.
- Keep token acquisition and durable token storage caller-owned; inject short-lived tokens at
  request time and never log them.
- Keep application-gateway, identity, realtime, and Control Plane delivery origins explicit.
- Preserve cancellation through network calls, operation polling, subscriptions, and disposal.
- Use bounded operation polling with a caller-selected timeout and delay policy. Respect terminal
  server state and surface structured server errors instead of converting failures into success.
- Preserve preview, ETag/revision, idempotency, and approval semantics for every delivery mutation.
- Treat realtime and event subscriptions as owned resources with explicit close/dispose behavior.
- Keep generated protocol code behind authored, idiomatic facades and preserve those facades during
  regeneration.

## Adopted port: the runtime-host client loop

This section replaces the earlier "no fabricated host lifecycle" boundary. That boundary rested on
a premise that has since expired, and it is recorded here as satisfied rather than waived.

The Control Plane delivery contract lets an authenticated build workflow create an environment,
submit immutable artifact evidence, release it, and observe the resulting durable operation. That
remains how a custom build reaches Yogabase's authoritative Control Plane and, through its signed
fleet and subordinate actuator, Kubernetes.

The earlier position was that these SDKs would invent no health, ready, reserve, or shutdown
operations until Yogabase itself implemented and versioned a runtime-host contract. Yogabase has
now done so. That contract is checked in at `contracts/yogabase-runtime-host-v1.openapi.json`, and
the condition the boundary set is therefore met.

| Field | Record |
|---|---|
| Adopted source | Agones client SDKs — the client loop shape in `sdks/csharp/sdk/AgonesSDK.cs` and `sdks/unity/AgonesSdk.cs` |
| Revision | `v1.60.0`, commit `67b44a783218f7811084adaa5d9b334fb28e44ba` |
| Licence | Apache-2.0; the unmodified text ships at `licenses/Apache-2.0-Agones.txt` |
| Attribution | Copyright Contributors to Agones a Series of LF Projects, LLC |
| Adopted | The client loop *shape* only: a background health loop started by a connect call, one-shot ready/reserve/shutdown calls, and an assignment watch. |
| Not adopted | Agones type or method names, its `proto/sdk` wire contracts, `GameServer`/`Fleet`/allocation resource concepts, host-port policies, and any PlayFlow-compatible hosting API. |
| Modifications | Retargeted to Yogabase's self-scoped loopback routes `/runtime-host/v1/self/{heartbeat,ready,reserve,shutdown,assignments}`; the client never learns, holds, or asserts an instance id; it sends no credential of any kind; assignments are polled rather than streamed, because the relay is request/response; no allocation call is exposed to a workload, which the relay refuses by construction. |
| Marking | Every derived file carries a prominent header naming the revision, licence, attribution, its status as a modified derivative, and the modifications above. |
| Replacement strategy | The module is a thin authored client over an HTTP contract Yogabase owns. Were the Apache grant ever unusable, it could be rewritten directly from `contracts/yogabase-runtime-host-v1.openapi.json` and the relay route allowlist without reference to Agones. |

PlayFlow remains reference-only and is not ported. The Nakama .NET/Unity **client** likewise stays
reference-only; the Nakama Plus **runtime module** surface is now a shape adoption, recorded
immediately below.

## Adopted shape: the Lambda runtime context

`Yogabase.Lambda` presents one context object to customer code. The grouping it presents — a
database handle, an object store, a user directory with ban and token-issuance operations, a topic
publish call, the caller's identity, a logger, and per-invocation facts — is the grouping Nakama's
`nk.*` runtime module presents, translated into C#. That is an adoption of shape, not of source.

| Field | Record |
|---|---|
| Adopted source | Nakama Plus runtime modules (`https://github.com/doublemo/nakama-plus.git`) |
| Revision | commit `18e4d2cd53d5f546d5684d6f70996c08234afc45`, observed on `main` 2026-08-22 |
| Licence | Apache-2.0; the unmodified text ships at `licenses/Apache-2.0-Nakama.txt`, SHA-256 `cfc7749b96f63bd31c3c42b5c471bf756814053e847c10f3eb003417bc523d30` |
| Attribution | Copyright 2018 The Nakama Authors. No `NOTICE` file exists in the pinned tree. |
| Adopted | The *shape* of the runtime-module context surface only: which capabilities a server-side function is handed, and that they arrive grouped on one context object rather than as free functions. |
| Not adopted | No Nakama source, Go/Lua/JavaScript runtime, type or method name, wire contract, storage/collection model, or match, session, party, tournament, leaderboard, wallet, notification, or social concept. No Nakama dependency, binary, or generated code. |
| Modifications | The surface is C#, BCL-typed, and bound to Yogabase's own internal Lambda runtime API routes (`LambdaRoutes`) and identity boundaries. The database is `System.Data.Common.DbDataSource` under the project's `yogabase_lambda` identity, not a Nakama storage engine; there is no key/value collection API, no storage index, and no runtime-module registration surface in this package. |
| Marking | `src/dotnet/Yogabase.Lambda/THIRD-PARTY-NOTICES.md`, packed into the package. |
| Replacement strategy | The surface is an authored wrapper over an HTTP contract and a BCL type Yogabase owns. Were the Apache grant ever unusable, it could be rewritten from `LambdaRoutes` and the entry contract alone. |

The same pinned revision is already recorded in the product repository's `references.lock.json` for
its separate, source-level C# translation of runtime-module registration. That translation lives in
the product repository and does not enter this one.
