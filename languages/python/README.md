# Yogabase Python client preview

Generated with Microsoft Kiota 1.34.1 from the current Yogabase v1-preview contract. Python 3.10
or newer is required by the pinned Kiota runtime. Callers provide a Kiota request adapter whose
base URL is the validated Yogabase REST gateway and whose authentication provider supplies a
project bearer token for protected operations.

The generated REST surface includes bootstrap, signup and interactive auth endpoints,
Storage, stateless Function invocation, Actor Lambda invocation, and Workflow start/cancel. The
contract returns a separate realtime origin, but this package does not yet include or qualify a
Python SignalR wrapper. This source preview is local, unpublished, and not production-qualified.

## The Data API and game servers are generated; builds are not

The mirrored contract declares the restored client Data API: `POST /v1/data/{table}/select|insert|upsert|update|delete`
on the application gateway, authenticated with the signed-in user's access token and run under
row-level security as that user. It is served only where the bootstrap document's requested-only
`data-rest` row says so (a client asks for the row by naming it in `X-Yogabase-Accept-Capabilities`),
and this client carries the operations as generated code, for example `client.v1.data.by_table("notes").select.post(body)` with the builders
`select`, `insert`, `upsert`, `update` and `delete`. A response is a `DataResult` (`rows`, `affectedRows`, `hasMore`) and a refusal is an RFC 7807 problem whose `code` is one of the refusal codes
the contract lists under `x-yogabase-data-api`, which also states the limits (at most 1000 rows per
select and 500 per write, 1 MiB per value, a five-second statement timeout). Request values are plain JSON.
Nothing in this package has called the Data API against a live installation. A typed data API and
Entity Framework Core inside Lambdas stay server-side: no client SDK carries EF code or a model.

Two other surfaces were added when the Data API was withdrawn:

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

## Durable Jobs

`AsyncJobsClient`, authored and exported from the package root, starts, reads, cancels and collects the
result of a durable background job as the signed-in user, against `/v1/projects/{projectId}/jobs` on the
application gateway and behind the same project token (the `lambda.invoke` scope) as invoking a function.
A **job type is the id of a deployed stateless function**; its highest released version runs the job. A
job belongs to the user who submitted it, and another user's job is answered exactly as one that does not
exist.

Nothing is generated for it: the product serves these routes (owner decision, 2026-10-03) but its client
OpenAPI document does not describe them yet, so the client is written from the gateway's source and
developer documentation and pinned by `tests/test_jobs.py`. Like the delivery client it takes its own
gateway host and access token rather than a Kiota adapter, because the result route's digest is a response
header the adapter would hide, and it uses the same platform trust store.

```python
from stackyoga_yogabase import AsyncJobsClient

async with AsyncJobsClient(
    gateway_host="https://apps.example.test",
    project_id=project_id,                    # the bootstrap document's projectId
    access_token=lambda: session.access_token,  # the signed-in user's access token
) as jobs:
    job = await jobs.submit(
        job_type="orders-create",
        idempotency_key="order-42",           # keep it across retries of this submit
        payload=b"hello",                     # optional, at most 1 MiB before base64
        max_attempts=3,                       # optional, 1 through 10; the gateway's default is 5
    )
    while not job.is_terminal:                # Succeeded, Cancelled or DeadLettered
        await asyncio.sleep(1)
        job = await jobs.get(job.job_id)
    if job.status == "Succeeded" and job.result_reference is not None:
        result = await jobs.result(job.job_id)  # result.content, result.digest
    await jobs.cancel(job.job_id)             # idempotent; a running job is flagged and stopped by its worker
```

- **Retrying a submit is safe.** The idempotency key is a member of the request body, not a header (the
  gateway reads no `Idempotency-Key` header on this route). The same request under the same key answers
  the job it already made (202 again, whatever the job has become); a different request under that key is
  refused with `job_idempotency_conflict` (409). The key belongs to the signed-in user.
- **Checked before anything is sent**, with the gateway's own bounds: a job type of 1 to 128 ASCII letters,
  digits, `.`, `-`, `_` or `:`; a trimmed idempotency key of 1 to 256 characters with no control character;
  a payload of at most 1,048,576 bytes (exactly that is a job); and `max_attempts` from 1 through 10.
- **Refusals** are `YogabaseJobsError`s carrying the gateway's `status` and `code` (the `JOB_*` constants
  name them): `job_type_not_registered` (422), `job_idempotency_conflict` (409), `job_payload_too_large`
  (413, with `limit_bytes` and `max_body_bytes` naming the payload and body limits), `job_request_invalid`
  (400), `job_access_denied` (403), `job_unavailable` (503) and `job_not_found` (404, for no such job and for
  another user's, never told apart). A plain `not_found` is an installation that does not serve Jobs at
  all; no capability row says whether one does.
- **A result is read separately.** `result_reference` is a name, not a capability; `result()` returns
  `YogabaseJobResult(content, digest)` after hashing the bytes and comparing them with the
  `X-Yogabase-Content-Sha256` header. A result that does not match (`job_result_digest_mismatch`), arrives
  without exactly one well-formed `sha256:` digest, is larger than 16 MiB or is not a 200 is refused
  (`job_response_invalid`) and never returned. `job_result_not_ready` (409) is your own job that has not
  settled; `job_result_not_found` (404) is another user's job, an unknown one and a job that finished
  without a result, never told apart.
- Timestamps stay the ISO 8601 text the gateway wrote (it writes seven fractional digits, which Python
  before 3.11 cannot parse). A response member this client does not know is ignored, as the generated
  clients keep unknown members aside; a job that breaks a bound it does know (an unknown status,
  `maxAttempts` outside 1 through 10) is refused.

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

## TLS trust

`platform_ssl_context()` returns a verifying `ssl.SSLContext` built by
`ssl.create_default_context()` with no CA file: the interpreter's default trust, which is the
OpenSSL default verify paths (plus the system stores on Windows). It requires a valid certificate
and a matching host name. `AsyncDeliveryClient` uses it for the HTTP client it creates when you
pass none. The generated application client uses whatever `httpx.AsyncClient` you give its Kiota
`HttpxRequestAdapter`; build that client with `verify=platform_ssl_context()` too, because httpx's
own default and Kiota's `get_default_client()` both trust certifi's bundle:

```python
import httpx
from kiota_http.httpx_request_adapter import HttpxRequestAdapter
from kiota_http.kiota_client_factory import KiotaClientFactory
from stackyoga_yogabase import YogabaseApiClient, platform_ssl_context

http = KiotaClientFactory.create_with_default_middleware(
    httpx.AsyncClient(verify=platform_ssl_context(), http2=True))
adapter = HttpxRequestAdapter(auth_provider, http_client=http, base_url="https://<installation-host>")
client = YogabaseApiClient(adapter)
```

An interpreter whose OpenSSL has no default store configured (some macOS installers ship one
empty until a certificate step is run) then fails verification instead of silently trusting
certifi's copy; install the platform's certificates rather than switching verification off.

`certifi` (MPL-2.0) is still installed. The owner decided on 2026-09-17 to drop it, but httpx
0.28.1 and httpcore 1.0.9 — the HTTP stack under the Kiota Python runtime — both declare
`Requires-Dist: certifi` unconditionally and import it, so `requirements.lock` keeps it and a
hash-checked install cannot leave it out. Removing it would mean replacing httpx beneath Kiota,
which no locked dependency here allows.

## Local verification

```text
cd languages/python
python3 -m venv .venv
.venv/bin/python -m pip install --require-hashes -r requirements.lock -r build-requirements.lock
PYTHONPATH=src .venv/bin/python -m unittest discover -s tests
.venv/bin/python -m build --no-isolation
```

`requirements.lock` hash-pins the runtime closure of the two direct pins (22 wheels, one of them
only for Python below 3.11). `pyproject.toml` pins the build backend exactly (`setuptools==84.0.0`),
and `build-requirements.lock` hash-pins `build` 1.6.1 and that backend, so the wheel and sdist are
built with exactly those tools rather than whatever an isolated build environment resolves. The tests import whichever `stackyoga_yogabase` is on the path:
with `PYTHONPATH=src` they check this source tree; in a fresh environment holding only
`requirements.lock` and the built wheel (`pip install --no-index dist/*.whl`), running
`python -P -m unittest discover -s tests` from this folder checks the installed wheel instead.
`test_generated_surface.py` imports every module file in the package, because the generated
subpackages have no `__init__.py` and a package walk would miss them, and round-trips the generated
game-server list and refusal through a real Kiota `HttpxRequestAdapter` over an in-memory transport.

`test_packaging_and_trust.py` pins the exact build-backend pin against the build lock and checks
that the Delivery client's default transport uses `platform_ssl_context()` without consulting
certifi's bundle.

Last run on 2026-09-22 on CPython 3.13.12 and 3.12.13 (macOS arm64, pip 26.0): both locks installed
with `--require-hashes`; 23 tests passed from source under `-X dev` with `ResourceWarning` as an
error; `build --no-isolation` produced the wheel (88 Python files) and the sdist; in a second fresh
environment the wheel installed offline over the runtime lock, `pip check` reported no broken
requirements, and the same 23 tests passed against the installed wheel. Python 3.10 and 3.11, a
live gateway, and a published index were not exercised.

Run again on 2026-10-03, on CPython 3.13.12 and 3.12.13, from source under `-X dev` with
`ResourceWarning` as an error, against the packages of the locked environment above: 58 tests passed (the
23, a delivery `jobsMaxRunSeconds` case, the 32 cases of `test_jobs.py`, and two generated-surface cases
for the invoke route's 413 problem and the restored Data select). The wheel and sdist were not rebuilt and
the installed-wheel run was not repeated, so the wheel evidence above predates `jobs.py`.
