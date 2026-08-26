# YogaBase TypeScript client preview

Generated with Microsoft Kiota 1.34.1 from the StackYoga YogaBase v1-preview contract. Install
dependencies and run `npm run build` to compile. Callers must provide a Kiota request adapter whose
base URL is the validated YogaBase gateway and whose authentication provider supplies a project
bearer token. Current runtime bootstrap documents report authentication unavailable.

This source preview is not published or production-qualified. TypeScript generation and its Kiota
runtime are themselves preview dependencies.

## Hierarchical storage keys

`objects.byKey(...)` is generated from an OpenAPI path parameter and is safe only for a validated
single path segment. For logical keys containing `/`, use the authored StackYoga adapter exported
from the package root; it applies the C# logical-key grammar and then calls the generated item's
`withUrl(...)` raw-URL override:

```ts
const item = storageObjectForLogicalKey(
  client.v1.storage.objects,
  requestAdapter.baseUrl,
  "avatars/user-1/photo.png",
);
```

This adapter is authored StackYoga code and is clearly separate from Kiota-generated files.
