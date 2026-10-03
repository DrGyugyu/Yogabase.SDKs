# Yogabase TypeScript client preview

Generated with Microsoft Kiota 1.34.1 from the current Yogabase v1-preview contract. Install
dependencies and run `npm test` to compile and execute the authored adapter tests. Callers provide
a Kiota request adapter whose base URL is the validated Yogabase REST gateway and whose
authentication provider supplies a project bearer token for protected operations.

The generated REST surface includes bootstrap, signup and interactive auth endpoints,
Storage, stateless Function invocation, Actor Lambda invocation, and Workflow start/cancel. The
contract returns a separate realtime origin, but this package does not yet include or qualify a
TypeScript SignalR wrapper. This source preview is unpublished and not production-qualified;
TypeScript generation and its Kiota runtime are themselves preview dependencies.

## The Data API and game servers are generated; builds are not

The mirrored contract declares the restored client Data API: `POST /v1/data/{table}/select|insert|upsert|update|delete`
on the application gateway, authenticated with the signed-in user's access token and run under
row-level security as that user. It is served only where the bootstrap document's requested-only
`data-rest` row says so (a client asks for the row by naming it in `X-Yogabase-Accept-Capabilities`),
and this client carries the operations as generated code, for example `client.v1.data.byTable("notes").select.post(body)` with the builders
`select`, `insert`, `upsert`, `update` and `delete`. A response is a `DataResult` (`rows`, `affectedRows`, `hasMore`) and a refusal is an RFC 7807 problem whose `code` is one of the refusal codes
the contract lists under `x-yogabase-data-api`, which also states the limits (at most 1000 rows per
select and 500 per write, 1 MiB per value, a five-second statement timeout). Request values are plain JSON.
Nothing in this package has called the Data API against a live installation. A typed data API and
Entity Framework Core inside Lambdas stay server-side: no client SDK carries EF code or a model.

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

`objects.byKey(...)` is generated from an OpenAPI path parameter and is safe only for a validated
single path segment. For logical keys containing `/`, use the authored Yogabase adapter exported
from the package root; it applies the C# logical-key grammar and then calls the generated item's
`withUrl(...)` raw-URL override:

```ts
const item = storageObjectForLogicalKey(
  client.v1.storage.objects,
  requestAdapter.baseUrl,
  "avatars/user-1/photo.png",
);
```

This adapter is authored Yogabase code and is clearly separate from Kiota-generated files.

For `PUT`, pass both required contract headers through the Kiota request configuration:
`X-Yogabase-Content-Sha256: sha256:<64 lowercase hex>` and a bounded `Idempotency-Key`. Kiota
models operation header parameters through the generic `headers` record rather than dedicated
method arguments in this generated TypeScript surface.

## Workload delivery

The authored `YogabaseDeliveryClient` is a separate client for the release-configured Delivery
API host. It uses a bearer token provider, returns response metadata (`etag`, `location`, and
`retryAfter`), and exposes the contract's preview/submit workflow for environments, artifact
admissions, releases, promotions, rollbacks, decommissions, and operation abandonment:

```ts
const delivery = new YogabaseDeliveryClient({
  deliveryHost: "https://delivery.example.test",
  accessToken: () => token,
});
const preview = await delivery.previewEnvironment(
  projectId,
  { name: "staging", templateId, templateVersion: 1 },
  { ifMatch: 'W/"7"' },
);
const accepted = await delivery.submitEnvironment(
  projectId,
  { previewId: preview.data.previewId, previewDigest: preview.data.previewDigest },
  { idempotencyKey: crypto.randomUUID() },
);
const operation = await delivery.waitForOperation(projectId, accepted.data.operationId, {
  terminalStates: ["Succeeded", "Failed", "Abandoned", "Cancelled", "Blocked"],
  signal: abortController.signal,
});
```

Artifact admission submits immutable source and evidence metadata plus digests; it does not upload
bundle bytes and this client does not add custom-build, Kubernetes, Agones, PlayFlow, or other
compatibility operations. The event stream is parsed with the platform Web Streams API and yields
`{ id, event, data }`; `id` is an opaque resume token that callers may explicitly pass back as
`lastEventId`. Heartbeat comment frames are ignored and resume tokens are never persisted or parsed
by the client. As the SSE grammar says, exactly one space after a field's colon is removed and any
further leading whitespace stays in the value, so an id round-trips unchanged; until 2026-09-22 this
client trimmed every leading space, as the Python client did before 2026-09-17.

`npm ci && npm test` passed 13 tests on 2026-09-22 (Node 25.9.0, npm 11.12.1).
