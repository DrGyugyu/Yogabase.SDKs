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

## Workload delivery

`AsyncDeliveryClient` is a separate client for the release-configured Delivery API host. It uses
bearer authentication, returns ETag/Location/Retry-After metadata, and exposes the contract's
preview/submit workflow for environments, artifact admissions, releases, promotions, rollbacks,
decommissions, and operation abandonment:

```python
from stackyoga_yogabase import AsyncDeliveryClient

async with AsyncDeliveryClient(
    delivery_host="https://delivery.example.test",
    access_token=lambda: token,
) as delivery:
    preview = await delivery.preview_environment(
        project_id,
        {"name": "staging", "templateId": template_id, "templateVersion": 1},
        if_match='W/"7"',
    )
    accepted = await delivery.submit_environment(
        project_id,
        {"previewId": preview.data["previewId"], "previewDigest": preview.data["previewDigest"]},
        idempotency_key="request-1",
    )
    operation = await delivery.wait_for_operation(
        project_id,
        accepted.data["operationId"],
        terminal_states=("Succeeded", "Failed", "Abandoned", "Cancelled", "Blocked"),
    )
```

Polling has explicit attempt/time bounds and supports an `asyncio.Event` cancellation gate.
`stream_operation_events` parses the contract's `text/event-stream` response using `httpx`'s
async streaming runtime and yields `{id, event, data}`. The `id` is an opaque resume token that
callers may explicitly pass back as `last_event_id`; heartbeat comment frames are ignored and
resume tokens are never persisted or parsed by the client. Artifact admission submits immutable
source and evidence metadata plus digests; it does not upload bundle bytes and this client adds no
custom-build, Kubernetes, Agones, PlayFlow, or other compatibility operations.
