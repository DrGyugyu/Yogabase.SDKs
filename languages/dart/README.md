# YogaBase Dart client preview

Generated with Microsoft Kiota 1.34.1 from the StackYoga YogaBase v1-preview contract. Dart output
is experimental in this generator version. Callers must provide a request adapter with the exact
YogaBase gateway and a project bearer-token authentication provider.

This source preview is local, unpublished, and not production-qualified.

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

The adapter is authored StackYoga code, not Kiota output. Dart remains source-inspected only until
the Dart SDK is available for compilation and tests.
