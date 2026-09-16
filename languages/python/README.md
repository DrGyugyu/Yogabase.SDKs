# Yogabase Python client preview

Generated with Microsoft Kiota 1.34.1 from the current Yogabase v1-preview contract. Python 3.10
or newer is required by the pinned Kiota runtime. Callers provide a Kiota request adapter whose
base URL is the validated Yogabase REST gateway and whose authentication provider supplies a
project bearer token for protected operations.

The generated REST surface includes bootstrap, signup and interactive auth endpoints,
Storage, stateless Function invocation, Actor Lambda invocation, and Workflow start/cancel. The
contract returns a separate realtime origin, but this package does not yet include or qualify a
Python SignalR wrapper. This source preview is local, unpublished, and not production-qualified.

## The Data API is withdrawn; game servers are generated, builds are not

The client-facing Data API module is withdrawn (owner decision, 2026-09-08). An application reaches
its project's data through a Lambda — which runs server-side with a full-access database context —
and through Auth, Storage and Realtime. `/v1/data/*`, its `Data*` models and the `data-rest`
capability id are gone from the mirrored contract and from this generated client; nothing replaces
them on the client side.

Two surfaces replaced it:

- **Game servers** — `POST /v1/servers/start`, `GET /v1/servers`, `GET /v1/servers/{serverId}`
  and `POST /v1/servers/{serverId}/stop` on the application gateway, authenticated with the
  signed-in player's access token. They are part of the mirrored application contract, so this
  client carries them as generated code: `client.v1.servers` (`start.post(body)`, `get()`, `by_server_id(id).get()`, `by_server_id(id).stop.post()`). The list answers a bare array; a `GameServer` is
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

## Local verification

```text
cd languages/python
python3 -m venv .venv
.venv/bin/python -m pip install --require-hashes -r requirements.lock -r build-requirements.lock
PYTHONPATH=src .venv/bin/python -m unittest discover -s tests
.venv/bin/python -m build --no-isolation
```

`requirements.lock` hash-pins the runtime closure of the two direct pins (22 wheels, one of them
only for Python below 3.11). `build-requirements.lock` hash-pins `build` 1.6.1 and the setuptools
84.0.0 backend, so the wheel and sdist are built with exactly those tools rather than whatever an
isolated build environment resolves. The tests import whichever `stackyoga_yogabase` is on the path:
with `PYTHONPATH=src` they check this source tree; in a fresh environment holding only
`requirements.lock` and the built wheel (`pip install --no-index dist/*.whl`), running
`python -P -m unittest discover -s tests` from this folder checks the installed wheel instead.
`test_generated_surface.py` imports every module file in the package, because the generated
subpackages have no `__init__.py` and a package walk would miss them, and round-trips the generated
game-server list and refusal through a real Kiota `HttpxRequestAdapter` over an in-memory transport.

Last run on 2026-09-17 on CPython 3.13.12 and 3.12.13 (macOS arm64, pip 26.0): both locks installed
with `--require-hashes`; 15 tests passed from source under `-X dev` with `ResourceWarning` as an
error; `build --no-isolation` produced the wheel (88 Python files) and the sdist; in a second fresh
environment the wheel installed offline over the runtime lock, `pip check` reported no broken
requirements, and the same 15 tests passed against the installed wheel. Python 3.10 and 3.11, a
live gateway, and a published index were not exercised.
