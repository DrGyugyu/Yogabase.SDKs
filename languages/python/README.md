# Yogabase Python client preview

Generated with Microsoft Kiota 1.34.1 from the current Yogabase v1-preview contract. Python 3.10
or newer is required by the pinned Kiota runtime. Callers provide a Kiota request adapter whose
base URL is the validated Yogabase REST gateway and whose authentication provider supplies a
project bearer token for protected operations.

The generated REST surface includes bootstrap, signup and interactive auth endpoints, Data,
Storage, stateless Function invocation, Actor Lambda invocation, and Workflow start/cancel. The
contract returns a separate realtime origin, but this package does not yet include or qualify a
Python SignalR wrapper. This source preview is local, unpublished, and not production-qualified.

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

The adapter module is authored Yogabase code, not Kiota output.

For `PUT`, add both required contract headers to the Kiota request configuration's
`HeadersCollection`: `X-Yogabase-Content-Sha256: sha256:<64 lowercase hex>` and a bounded
`Idempotency-Key`. Kiota does not emit dedicated Python method arguments for these headers.
