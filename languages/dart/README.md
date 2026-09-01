# Yogabase Dart client preview

Generated with Microsoft Kiota 1.34.1 from the current Yogabase v1-preview contract. Dart output
is experimental in this generator version. Callers must provide a request adapter with the exact
Yogabase REST gateway and a project bearer-token authentication provider.

The generated REST surface includes bootstrap, signup and interactive auth endpoints, Data,
Storage, stateless Function invocation, Actor Lambda invocation, and Workflow start/cancel. The
contract returns a separate realtime origin, but this package does not yet include or qualify a
Dart/Flutter SignalR wrapper. This source preview is local, unpublished, and not
production-qualified.

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
and its standalone authored-source checks; Flutter integration and live-gateway behavior remain
unqualified.

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
token, delivery header helpers, and buffered SSE parsing that preserves opaque resume IDs. True
streaming remains dependent on a streaming Dart request adapter and is not claimed as qualified.
Artifact admission does not expose Kubernetes credentials or add Agones/PlayFlow or unimplemented
runtime lifecycle operations.
