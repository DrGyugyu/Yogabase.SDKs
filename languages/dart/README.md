# Yogabase Dart client preview

Generated with Microsoft Kiota 1.34.1 from the current Yogabase v1-preview contract. Dart output
is experimental in this generator version. Callers must provide a request adapter with the exact
Yogabase REST gateway and a project bearer-token authentication provider.

The generated REST surface includes bootstrap, signup and interactive auth endpoints,
Storage, stateless Function invocation, Actor Lambda invocation, and Workflow start/cancel. The
contract returns a separate realtime origin, but this package does not yet include or qualify a
Dart/Flutter SignalR wrapper. This source preview is local, unpublished, and not
production-qualified.

## No Data API client yet; game servers are generated, builds are not

There is no client-facing data surface in this package today. The Data API module was withdrawn on
2026-09-08. On 2026-09-17 the owner decided to restore it — clients querying and writing project
tables through the gateway under row-level security as the signed-in user — and to give Lambdas a
typed data API and Entity Framework Core, which stays server-side (no client SDK will carry EF code
or a model). None of this is implemented yet: the mirrored contract still declares no `/v1/data`
route, its `Data*` models or the `data-rest` capability id, and this generated client has no Data
surface; regenerating from `scripts/regenerate-clients.sh` will only add one once the product
publishes the route and the contract here is re-mirrored. Today an application reaches its project's
data through a Lambda — which runs server-side with a full-access database context — and through
Auth, Storage and Realtime.

Two other surfaces were added when the Data API was withdrawn:

- **Game servers** — `POST /v1/servers/start`, `GET /v1/servers`, `GET /v1/servers/{serverId}`
  and `POST /v1/servers/{serverId}/stop` on the application gateway, authenticated with the
  signed-in player's access token. They are part of the mirrored application contract, so this
  client carries them as generated code: `client.v1.servers` (`start.post(body)`, `get()`, `byServerId(id).get()`, `byServerId(id).stop.post()`). The list answers a bare array; a `GameServer` is
  `serverId`, `state` and `networkPorts[{name, internalPort, externalPort, protocol, host}]`; a
  refusal is an RFC 7807 problem whose `code` is one of `server_request_invalid`,
  `server_authentication_required`, `server_none_available`, `server_contended`,
  `server_not_found`, `server_address_not_observed`, `server_stop_not_available` or
  `server_unavailable`. An instance listed with empty `networkPorts` has no observed address yet;
  do not connect to it. The start request names the hosted service by its canonical id
  (`[a-z0-9]([-a-z0-9]{0,61}[a-z0-9])?`) and may carry at most 32 `customData` entries, 64-byte
  keys, 1024-byte values, 4 KiB as canonical JSON.
- **Builds** — `POST`/`GET` `/api/v1/projects/{p}/environments/{e}/builds[/{buildId}[/recheck]]`
  on the Control Plane, authenticated with a project-scoped `ybt_` developer token. This is a
  developer-tool surface (the Unity Editor, a CLI, CI), not a shipped-client surface. It is
  mirrored at `contracts/yogabase-builds-v1.openapi.json` and implemented by the .NET
  `YogabaseBuildsClient` and the Unity package; no client is generated for it in this language.

## Hierarchical storage keys

Generated `objects.byKey(...)` is single-segment-only. For a logical key containing `/`, use the
authored `storageObjectForLogicalKey(...)` adapter exported from the package library. It validates
the C# key grammar, encodes each segment separately, and returns
`WithKeyItemRequestBuilder.withUrl(...)` with the resulting raw URL.

```dart
final item = storageObjectForLogicalKey(
  client.v1.storage.objects,
  requestAdapter.baseUrl!,
  'avatars/user-1/photo.png',
);
```

The adapter is authored Yogabase code, not Kiota output. The package passes Dart 3.13.2 analysis
and its standalone authored-source checks (see Local verification below); Flutter integration and
live-gateway behavior remain unqualified.

For `PUT`, add both required contract headers through the Kiota request configuration:
`X-Yogabase-Content-Sha256: sha256:<64 lowercase hex>` and a bounded `Idempotency-Key`. Kiota does
not emit dedicated Dart method arguments for these headers.

## Workload delivery

The package exports a second generated protocol client under `src/delivery` plus the authored
`YogabaseDeliveryClient` facade. Construct it with a delivery-only request adapter and the TLS-only
Delivery origin; never share the application adapter or bearer credential.

```dart
final delivery = YogabaseDeliveryClient(
  deliveryRequestAdapter,
  Uri.parse('https://delivery.example.com'),
);
final operations = delivery.api.api.v1.delivery.projects
    .byProjectId(projectId)
    .operations;
```

The facade provides terminal-state recognition, bounded polling with an explicit cancellation
token, delivery header helpers, and buffered SSE parsing that preserves opaque resume IDs. Cancelling
the token ends a poll between requests; during the delay between attempts it also stops the delay's
timer, so an abandoned poll does not keep the isolate alive for the rest of the interval. True
streaming remains dependent on a streaming Dart request adapter and is not claimed as qualified.
Artifact admission does not expose Kubernetes credentials or add Agones/PlayFlow or unimplemented
runtime lifecycle operations.

## Local verification

```text
cd languages/dart
dart pub get --enforce-lockfile
dart analyze --fatal-infos --fatal-warnings
dart run test/yogabase_delivery_source_test.dart
```

The package declares no `dev_dependencies`, so `dart test` is not wired; the checks are one
standalone program that prints `dart delivery source checks: PASS` or exits non-zero with the failed
expectation. Every request goes to an in-memory `RequestAdapter`. It covers SSE parsing (including
the one-space field rule), the cancellation token, the delivery bearer origin check, the storage
logical-key cases shared with the Python suite, the generated game-server list and stop requests,
polling to a terminal state, cancellation during a polling delay (inside a zone that records every
timer, asserting none is left active), an already-cancelled poll sending nothing, and the
event-stream request's `Last-Event-ID`, `Accept` and `operationId` query.

Last run on 2026-09-17 with the Dart 3.13.2 stable macOS arm64 SDK (revision
`60a57cd42d64dc03e9f07aa60a2e250755c1ef28`), unpacked from the archive whose SHA-256 matched
`MANIFEST.json`: `pub get --enforce-lockfile` left `pubspec.lock` unchanged, analysis found no
issues with infos and warnings fatal, and the standalone program passed and exited within a second.
Flutter, a streaming adapter, and a live gateway were not exercised.
