# Yogabase Java/JVM client preview

Generated with Microsoft Kiota 1.34.1 from the current Yogabase v1-preview contract. The source
targets the Java/JVM Kiota runtime and is the current Kotlin/JVM interoperability path; no native
Kotlin or Android qualification is claimed. Callers must provide a request adapter with the exact
Yogabase REST gateway and a project bearer-token authentication provider.

The generated REST surface includes bootstrap, signup and interactive auth endpoints,
Storage, stateless Function invocation, Actor Lambda invocation, and Workflow start/cancel. The
contract returns a separate realtime origin, but this package does not yet include or qualify a
JVM SignalR wrapper.

The serialization modules are direct compile dependencies because the generated client registers
their factories; the Kiota bundle currently marks those transitive modules as runtime scope.

This source preview is local, unpublished, and not production-qualified.

## The Data API and game servers are generated; builds are not

The mirrored contract declares the restored client Data API: `POST /v1/data/{table}/select|insert|upsert|update|delete`
on the application gateway, authenticated with the signed-in user's access token and run under
row-level security as that user. It is served only where the bootstrap document's requested-only
`data-rest` row says so (a client asks for the row by naming it in `X-Yogabase-Accept-Capabilities`),
and this client carries the operations as generated code, for example `client.v1().data().byTable("notes").select().post(body)` with the builders
`select()`, `insert()`, `upsert()`, `update()` and `delete()`. A response is a `DataResult` (`rows`, `affectedRows`, `hasMore`) and a refusal is an RFC 7807 problem whose `code` is one of the refusal codes
the contract lists under `x-yogabase-data-api`, which also states the limits (at most 1000 rows per
select and 500 per write, 1 MiB per value, a five-second statement timeout). Request values are plain JSON.
Nothing in this package has called the Data API against a live installation. A typed data API and
Entity Framework Core inside Lambdas stay server-side: no client SDK carries EF code or a model.

Two other surfaces were added when the Data API was withdrawn:

- **Game servers** — `POST /v1/servers/start`, `GET /v1/servers`, `GET /v1/servers/{serverId}`
  and `POST /v1/servers/{serverId}/stop` on the application gateway, authenticated with the
  signed-in player's access token. They are part of the mirrored application contract, so this
  client carries them as generated code: `client.v1().servers()` (`start().post(body)`, `get()`, `byServerId(id).get()`, `byServerId(id).stop().post()`). The list answers a bare array; a `GameServer` is
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

Generated `ObjectsRequestBuilder.byKey(...)` is single-segment-only. For logical keys containing
`/`, call the authored `YogabaseStorageLogicalKey.requestBuilder(...)` adapter with the generated
objects builder, validated gateway origin, and logical key. It applies the C# key grammar, encodes
each segment separately, and returns the generated item builder through `withUrl(...)`.

```java
var item = YogabaseStorageLogicalKey.requestBuilder(
    client.v1().storage().objectsEscaped(),
    requestAdapter.getBaseUrl(),
    "avatars/user-1/photo.png");
```

`YogabaseStorageLogicalKey` is authored Yogabase code, not Kiota output.

For `PUT`, add both required contract headers in the generated `PutRequestConfiguration` consumer:
`X-Yogabase-Content-Sha256: sha256:<64 lowercase hex>` and a bounded `Idempotency-Key`. Kiota does
not emit dedicated Java method arguments for these headers.

## Workload delivery

The package also contains a delivery client generated from the separate workload Delivery v1
contract under `io.stackyoga.yogabase.delivery`. Wrap a delivery-only `RequestAdapter` with
`YogabaseDeliveryClient`; do not reuse the application adapter or its credentials. The authored
facade validates the delivery origin, provides a rotating bearer-token provider and header helpers,
recognizes the exact terminal operation states, supports cancellable bounded polling, and parses
SSE while preserving opaque resume IDs.

```java
try (var delivery = new YogabaseDeliveryClient(
        deliveryRequestAdapter,
        URI.create("https://delivery.example.com"))) {
    var operations = delivery.api().api().v1().delivery().projects()
        .byProjectId(projectId).operations();
}
```

Artifact admission carries credential-free source and evidence metadata. No Kubernetes credential,
Agones/PlayFlow compatibility call, or unimplemented runtime health/allocation API is added.
