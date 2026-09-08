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

## The Data API is withdrawn, and Builds/Servers are not here yet

The client-facing Data API module is withdrawn (owner decision, 2026-09-08). An application reaches
its project's data through a Lambda — which runs server-side with a full-access database context —
and through Auth, Storage and Realtime. `/v1/data/*` and its `Data*` models are gone from the
contract and from this generated client; nothing replaces them on the client side.

Two surfaces did replace it, and neither has a client in this language yet:

- **Game servers** (`/v1/servers/start`, `/v1/servers`, `/v1/servers/{id}`, `/v1/servers/{id}/stop` on the application gateway, player token)
- **Builds** (`/api/v1/projects/{p}/environments/{e}/builds` on the Control Plane, developer token)

Both are implemented today only in `src/dotnet/Yogabase.Yogabase.Client`
(`YogabaseClient.Servers` and `YogabaseClient.Builds(...)`). Clients for this language are later
scope. The planned shapes are written down in `contracts/planned/yogabase-servers-v1.openapi.json`
and `contracts/planned/yogabase-builds-v1.openapi.json`, both marked
`"x-yogabase-status": "planned"` because the product is implementing them in parallel; they are not
mirrored product snapshots and must not be generated from as if they were.
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
by the client.
