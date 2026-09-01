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
| Agones client SDKs | `v1.60.0`, `67b44a783218f7811084adaa5d9b334fb28e44ba` | Apache-2.0 | Health/readiness lifecycle shape, bounded watches, cancellation, graceful shutdown, and consistent multi-platform concepts. |
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

## Deliberate boundary: no fabricated host lifecycle

The current Control Plane delivery contract lets an authenticated build workflow create an
environment, submit immutable artifact evidence, release it, and observe the resulting durable
operation. That is how a custom build reaches Yogabase's authoritative Control Plane and, through
its signed fleet and subordinate actuator, Kubernetes.

The current source does not expose a customer-build runtime API equivalent to Agones health,
ready, allocate, reserve, or shutdown calls. These SDKs therefore do not invent one. Agones,
PlayFlow, and Nakama inform the client lifecycle design only. A future runtime-host contract must
first be implemented and versioned by Yogabase before any SDK can expose those operations.
