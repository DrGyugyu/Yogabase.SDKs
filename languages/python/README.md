# YogaBase Python client preview

Generated with Microsoft Kiota 1.34.1 from the StackYoga YogaBase v1-preview contract. Python 3.10
or newer is required by the pinned Kiota runtime. Callers must provide a Kiota request adapter whose
base URL is the validated YogaBase gateway and whose authentication provider supplies a project
bearer token. Current runtime bootstrap documents report authentication unavailable.

This source preview is local, unpublished, and not production-qualified.

## Hierarchical storage keys

Generated `objects.by_key(...)` is single-segment-only. For a logical key containing `/`, import
`storage_object_for_logical_key` from `stackyoga_yogabase` and pass the generated objects builder,
the validated gateway origin, and the logical key. The authored adapter validates the C# key grammar,
encodes each segment separately, and returns the generated builder through `with_url(...)`.

```python
item = storage_object_for_logical_key(
    client.v1.storage.objects,
    request_adapter.base_url,
    "avatars/user-1/photo.png",
)
```

The adapter module is authored StackYoga code, not Kiota output.
