# Yogabase TypeScript client preview

Generated with Microsoft Kiota 1.34.1 from the current Yogabase v1-preview contract. Install
dependencies and run `npm test` to compile and execute the authored adapter tests. Callers provide
a Kiota request adapter whose base URL is the validated Yogabase REST gateway and whose
authentication provider supplies a project bearer token for protected operations.

The generated REST surface includes bootstrap, signup and interactive auth endpoints, Data,
Storage, stateless Function invocation, Actor Lambda invocation, and Workflow start/cancel. The
contract returns a separate realtime origin, but this package does not yet include or qualify a
TypeScript SignalR wrapper. This source preview is unpublished and not production-qualified;
TypeScript generation and its Kiota runtime are themselves preview dependencies.

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
