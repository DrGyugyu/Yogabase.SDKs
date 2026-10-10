# Yogabase Python client preview

Generated with Microsoft Kiota 1.34.1 from the current Yogabase v1-preview contract, with authored
parts beside it: a password sign-in, sign-in with an external provider, durable Jobs, schedules, a workload Delivery client and a
hierarchical storage-key adapter. It supports Python 3.12 and 3.13, the two versions it has been run
on (`requires-python = ">=3.12,<3.14"`); the pinned Kiota runtime would accept older ones, but 3.10
and 3.11 have never been run, so they are not claimed.

The generated REST surface includes bootstrap, signup and interactive auth endpoints,
Storage, stateless Function invocation, Actor Lambda invocation, Workflow start/cancel, and the
request builders of the Jobs and schedule routes. The
contract returns a separate realtime origin, but this package does not yet include or qualify a
Python SignalR wrapper. This source preview is local, unpublished, and not production-qualified.

## Getting started

Nothing is published, so install from a checkout of this repository, into a virtual environment on
Python 3.12 or 3.13, from the hash-locked dependencies:

```text
cd languages/python
python3.13 -m venv .venv
.venv/bin/python -m pip install --require-hashes -r requirements.lock -r build-requirements.lock
.venv/bin/python -m pip install --no-deps --no-build-isolation .
```

Apart from the bootstrap and the `auth` calls, every call needs a signed-in user, so sign in before
the first one. `sign_in_with_password` returns a session that is the Kiota authentication provider:

```python
import asyncio
import base64
import getpass
import hashlib
import uuid
from datetime import datetime, timedelta, timezone

import httpx
from kiota_http.httpx_request_adapter import HttpxRequestAdapter
from kiota_http.kiota_client_factory import KiotaClientFactory
from stackyoga_yogabase import YogabaseApiClient, platform_ssl_context, sign_in_with_password
from stackyoga_yogabase.models.invocation_request import InvocationRequest

GATEWAY = "https://api.example.com"            # the installation's gateway origin


async def main() -> None:
    session = await sign_in_with_password(
        GATEWAY,
        "sy_pub_example_project",               # the project's publishable key, which is not a secret
        "user@example.com",
        getpass.getpass("Password: "),
    )

    http = KiotaClientFactory.create_with_default_middleware(httpx.AsyncClient(verify=platform_ssl_context()))
    adapter = HttpxRequestAdapter(session, http_client=http)
    adapter.base_url = GATEWAY                  # this Kiota version ignores the constructor's base_url
    client = YogabaseApiClient(adapter)

    payload = b"{}"
    result = await (
        client.v1.projects.by_project_id(session.project_id)
        .functions.by_function_id("hello-world").versions.by_function_version("1").invoke
        .post(InvocationRequest(
            idempotency_key=uuid.uuid4().hex,
            correlation_id="first-call",
            mode=0,                             # 0 is synchronous, 1 asynchronous
            deadline=datetime.now(timezone.utc) + timedelta(seconds=20),
            payload_digest="sha256:" + hashlib.sha256(payload).hexdigest(),
            inline_payload_base64=base64.b64encode(payload).decode(),
        ))
    )
    print(result.outcome, base64.b64decode(result.inline_result_base64 or ""))  # outcome 4 is Completed
    await http.aclose()


asyncio.run(main())
```

- **It needs no browser.** `sign_in_with_password` logs in for the identity cookie, then runs the PKCE
  code flow with that cookie answering `/connect/authorize`; the redirect is read and never followed,
  and the first redirect URI the bootstrap registers is used. The cookie is `Secure`, so the gateway
  must be an `https` origin (anything else is a `ValueError`). A `http_client=` you pass must keep
  cookies and not follow redirects (httpx's default), and stay open: the session refreshes through it.
- **The session** holds `access_token`, `refresh_token` and `project_id` (the bootstrap's, which the
  Jobs and schedules clients ask for). It refreshes its rotating tokens a minute before they expire,
  once however many calls are waiting, and adds the bearer only to requests for the gateway origin.
  The authored clients take `access_token=session.get_access_token`.
- **`HttpxRequestAdapter` ignores its `base_url` argument** in the pinned Kiota version (it reads the
  http client's), so set `adapter.base_url` after constructing it. Left unset, the generated client
  falls back to the placeholder `https://{gatewayHost}`.
- **The user must already exist, and have confirmed their e-mail address.** A user signs up with an e-mail
  address and a password (`client.auth.signup.post`) and must follow the link in the confirmation e-mail
  (`client.auth.email.confirm`) before signing in. A project's gateway runs with open sign-up and nothing in
  the product creates an invitation, so `invitation_token` is not needed: only an invite-only mode that no
  installation setting selects reads it (`403 invitation_required`). On an installation with no e-mail
  transport configured, sign-up is a `503 authentication_email_unavailable`; a self-hosted installation can
  instead set `yogabase.authenticationEmail.confirmation: disabled`, and accounts are then created already
  confirmed. An address that already has an account is a `409 account_exists`, and account data the gateway
  rejects a `400 account_invalid`.
- **A refusal is a `YogabaseAuthError`** with the gateway's `status`, `code` and `correlation_id` (quote
  it when reporting a problem). `401 invalid_credentials` is a wrong password, an unknown account, an
  unconfirmed e-mail and a locked account alike, never told apart; `403 mfa_enrollment_required` means
  the project requires MFA and the account has none; `429 authentication_rate_limited`. An account with
  MFA raises `202 mfa_required`: call again with the authenticator code as `mfa_code=`. A redirect, state
  or code that does not check out is `400 authorization_redirect_mismatch` or
  `400 authorization_response_invalid`, an `error` the authorization server sends back is the code, and
  the token endpoint keeps its own (`invalid_grant`). No token request follows any of these.
- **A live check.** `tests/test_live_smoke.py` signs in, invokes a function, then puts and gets an
  object. It is skipped unless `YOGABASE_SMOKE_GATEWAY`, `YOGABASE_SMOKE_PUBLISHABLE_KEY`,
  `YOGABASE_SMOKE_EMAIL`, `YOGABASE_SMOKE_PASSWORD` (an existing, confirmed user without MFA),
  `YOGABASE_SMOKE_FUNCTION_ID` and `YOGABASE_SMOKE_FUNCTION_VERSION` (a deployed stateless function that
  accepts `{}`) are all set. The requests it builds were checked against a fake gateway. It passed on 2026-10-10 against a local installation: the gateway of a throwaway cluster behind a local HTTPS address, a user signed up there, and a released hello function. It has not run through a public gateway name. Python trusted that address's certificate through `SSL_CERT_FILE`.

## Signing in with a provider

A project that configured Google, Microsoft or Apple signs its users in through the gateway. The browser step belongs
to your application, so there are two calls around it:

```python
# The redirect URI must be one the project registered (the bootstrap's oidcRedirectUris).
pending = await start_external_sign_in(GATEWAY, "sy_pub_example_project", "google", "com.example.app:/oauth/callback")

# Open this in the system browser, and receive the address the browser is redirected to.
redirect = await open_in_browser_and_wait_for_redirect(pending.authorization_url)

session = await complete_external_sign_in(pending, redirect)   # the same YogabaseSession sign_in_with_password returns
```

- **The provider id** is the one the project gave the provider (`google` above).
- **The one address a developer registers with each provider** is `https://<gateway>/auth/external/<provider id>/return`.
  The provider sends the browser there; the gateway sends it on to your redirect URI with `code` and `state`, or with
  `error=access_denied` and `state`.
- **The client checks the redirect itself.** A redirect with another `state` (including one from a different start), an
  `error`, no `code`, a repeated parameter or another address is a `YogabaseAuthError` (`authorization_response_invalid`,
  `access_denied`, `authorization_redirect_mismatch`) before any request. Only the S256 challenge of the verifier goes to
  `start`; the verifier goes to the callback alone. Complete a `pending` from the redirect of its own start, once.
- **A refusal is a `YogabaseAuthError` with the gateway's code**: `external_identity_invalid` (401),
  `external_identity_email_in_use` (409: an account already uses the address and nothing is linked),
  `external_email_unverified`, `external_identity_not_enrolled`, `mfa_enrollment_required` and `invitation_required` (403),
  `external_provider_unknown` (404), `external_redirect_uri_invalid` and `external_callback_invalid` (400) and, when the
  installation or the provider cannot be reached, `external_identity_unavailable` or `external_provider_unavailable` (503).
  A `start` answer that is not an https address with a state is `external_start_invalid`. An account with MFA raises
  `202 mfa_required` after the callback; call `complete_external_sign_in` again with the same `pending` and redirect and
  the authenticator code as `mfa_code=`: the spent code is not sent a second time.
- **`pending` holds no HTTP client.** It keeps the verifier and the cookies the gateway sets (the second factor's, between
  the callback and the code), so one you never complete has nothing to close. Each call opens its own client and closes it,
  or uses the `http_client=` you pass, which must keep cookies, not follow redirects, and stay open: the session
  refreshes through it.
- **There is no loopback listener and no browser launcher in the package.** Opening the address and receiving the redirect
  (a custom URI scheme, an app link, a loopback listener of your own) is the application's.
- **It has never run against a real provider.** Only fake-gateway tests cover it, with the gateway's answers written from
  its route code.

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
the gateway origin, and the logical key (`client` and `GATEWAY` are those of Getting started). The authored adapter validates the C# key grammar,
encodes each segment separately, and returns the generated builder through `with_url(...)`.

```python
item = storage_object_for_logical_key(
    client.v1.storage.objects,
    GATEWAY,
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

The product's client OpenAPI document describes the Jobs routes, so Kiota also generates their request
builders (`client.v1.projects.by_project_id(project_id).jobs`, with `Job`, `JobStatus` and `JobSubmitRequest`
among the models). This client sits **beside** those builders, not on top of them: the result route's digest
is a response header a generated builder cannot hand back, each call is checked before it is sent, and every
refusal is named by its code. Like the delivery client it takes its own gateway host and access token rather
than a Kiota adapter, and it uses the same platform trust store. `tests/test_jobs.py` pins every shape and
holds each request it sends to the request the generated builder builds for the same operation, so a
contract change that moves a route or a member fails there. `GATEWAY` and `session` are those of Getting started.

```python
import asyncio
from stackyoga_yogabase import AsyncJobsClient

async with AsyncJobsClient(
    gateway_host=GATEWAY,                     # the session from Getting started signed in to it
    project_id=session.project_id,            # the bootstrap document's projectId
    access_token=session.get_access_token,    # the signed-in user's access token, refreshed for you
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
  all; no capability row says whether one does. A status the gateway names no code for reads as the
  `STATUS_*` constants name it (`unsupported_media_type` for a submit that is not JSON, `payload_too_large`
  for a bare 413 from a gateway that predates the named one).
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

It was also run end to end on 2026-10-03 against the product's real Jobs routes, built from product `main`
`92ac1ced` (24 checks; see the root README for what that host was and was not).

## Schedules

`AsyncSchedulesClient`, authored and exported from the package root, creates, changes, reads, deletes and runs
once the signed-in user's own **recurring or one-shot schedules** of a deployed stateless function, against
`/v1/projects/{projectId}/schedules/{scheduleId}` on the application gateway and behind the same project token
(the `lambda.invoke` scope) as invoking a function. A schedule belongs to the user who created it, and only
that user reads, changes, deletes or runs it: another user's schedule is a bare 404, updating it is a 403 and
creating a schedule under its id is a 412. Like the Jobs client it sits beside the generated builders
(`client.v1.projects.by_project_id(project_id).schedules.by_schedule_id(id)`), because a write is conditional
on the schedule's **ETag**, which a generated builder neither reads from a response nor sends unless it is
added by hand; `tests/test_schedules.py` pins every shape and holds each request to the generated builder's. `GATEWAY` and `session` are those of Getting started.

```python
from datetime import datetime, timedelta, timezone
from stackyoga_yogabase import AsyncSchedulesClient, YogabaseScheduleDefinition

definition = YogabaseScheduleDefinition(
    schedule_id="nightly",
    function_id="orders-nightly",              # a deployed Stateless Lambda
    function_version="1",                      # an admitted version
    cron="0 3 * * *", time_zone="Asia/Seoul",  # five fields; or set one_shot_at_utc (aware) for a one-shot
    misfire_policy="SkipToNextOccurrence",     # required: a missing one is a 400 naming the member
    concurrency_policy="Allow",                # required too: Allow, Forbid or Replace (see below)
    max_catch_up=1, max_attempts=3, deadline_seconds=300,
    payload=b"{}",                             # optional, at most 1 MiB before base64
)
async with AsyncSchedulesClient(gateway_host=GATEWAY, project_id=session.project_id, access_token=session.get_access_token) as schedules:
    schedule = await schedules.create(definition)                          # If-None-Match: *  (201)
    schedule = await schedules.update(definition, schedule.etag)           # If-Match (200)
    run = await schedules.trigger(                                         # If-Match; runs it once now (202)
        "nightly", schedule.etag,
        idempotency_key="run-1",               # the same key against the same version is the same run
        deadline=datetime.now(timezone.utc) + timedelta(minutes=10),   # after now, at most an hour from now
    )                                          # run.outcome is "Enqueued", or "Replayed" for a repeat
    await schedules.delete("nightly", schedule.etag)                       # If-Match (204)
```

- **Every write names the ETag it read.** `etag` is built from the schedule's `version` (decimal text, a 64-bit
  number a JSON number cannot carry) and `instance_id`: `"schedule-v<version>-<instance>"`. A stale ETag,
  creating an id that is already held and updating one that does not exist are each a 412
  (`precondition_failed`) that changes nothing; the scheduler advances a recurring schedule's version as it
  runs, so an ETag held for long is stale: `get` it again. A strong `ETag` header that disagrees with the body
  is refused; a weak or repeated one (a proxy that compresses a response may weaken it) is ignored.
- **Checked before anything is sent**, with the gateway's bounds (the `YOGABASE_SCHEDULES_*` constants): for a
  write, a schedule id of 1 to 128 characters, a letter or a digit first and then letters, digits and `.` `_`
  `~` `:` `-` only (the contract's pattern; see the next note), a function id of at most 256 and a version of
  at most 64 characters, a cron expression and a time zone of at most 128, both policies, `max_catch_up` 1
  through 128, `max_attempts` 1 through 16, a deadline of 1 through 86,400 seconds, a payload of at most
  1,048,576 bytes, and an ETag in the gateway's form; for a read, a run and a delete, a schedule id of 1 to 128
  trimmed characters with no control character (and not `.` or `..`, which a URL parser would collapse). A
  one-shot instant and a run's deadline must be timezone-aware and are sent as UTC.
- **Refusals** are `YogabaseSchedulesError`s with the gateway's `status` and `code`. Only the 413 names a code,
  `schedule_payload_too_large`, with `limit_bytes` and `max_body_bytes`; every other refusal is a status with a
  fixed title or no body, and reads as the `STATUS_*` constants name it: `precondition_failed` (412),
  `precondition_required` (428), `not_found` (404; also an installation without the Scheduler), `conflict`
  (409: a function version that is not an admitted Stateless one of the project, on a write or on a run, a
  write that would turn a recurring schedule into a one-shot or the reverse, or a run key the queue holds for
  another request; a run of a withdrawn version answered 503 before product `main` `f9d57e29`), `forbidden`
  (403), `unsupported_media_type` (415) and `request_failed` (a 400 for a member the gateway refuses, such as
  a cron expression that does not parse, a policy left out or an id that breaks the rule, the last two naming
  the member in the problem's `detail`; a 503).
- **A write holds the id to one rule; an older id is still addressed.** The id is one segment of the URL path,
  so a create and an update are held to the contract's pattern, `^[A-Za-z0-9][A-Za-z0-9._~:-]{0,127}$`, before
  anything is sent, and the gateway refuses an id that breaks it with a 400 that names the member and the rule.
  A schedule written before the rule under another id is still found by that id (percent-encoded, as any path
  segment is) by `get`, `trigger` and `delete`, which therefore check only the older bounds; it
  cannot be changed, so delete it and create it again under an id that follows the rule.
- **The concurrency policy says what the scheduler does about an earlier occurrence that has not finished**
  (its invocation is queued, waiting to start or to be retried, or running, and its deadline has not passed):
  `Allow` starts the new occurrence alongside, `Forbid` skips it, and `Replace` ends an earlier occurrence that
  is still queued (failure code `schedule_occurrence_replaced`) and starts the new one, but skips it while one
  is running, because nothing can interrupt a Lambda that is executing. A skipped occurrence is consumed, not
  deferred. A manual run is not an occurrence, so the policy neither holds it back nor is held back by it, and
  a one-shot has no earlier occurrence. The contract of product `main` `f9d57e29` describes this; a build of
  the product from before it starts every occurrence whichever policy is chosen and accepts any id but one with
  a `/` (a 400).

Run end to end on 2026-10-03 against the product's real schedule routes, built from product `main` `92ac1ced`
(36 checks, on CPython 3.13.12 and 3.12.13). That run predates the id rule, the concurrency enforcement and the
409 of a manual run, and was not repeated against product `main` `f9d57e29`; its harness is in the product
repository.

## Workload delivery

`AsyncDeliveryClient` is a separate client for the release-configured Delivery API host. It uses
bearer authentication, returns ETag/Location/Retry-After metadata, and exposes the contract's
preview/submit workflow for environments, artifact admissions, releases, promotions, rollbacks,
decommissions, and operation abandonment:

```python
from stackyoga_yogabase import AsyncDeliveryClient

async with AsyncDeliveryClient(
    delivery_host="https://delivery.example.test",
    access_token=lambda: token,   # a short-lived workload token from the configured delivery authority, not the signed-in user's
) as delivery:
    preview = await delivery.preview_environment(
        project_id,                                  # your project's id
        {"name": "staging", "templateId": template_id, "templateVersion": 1},  # a template of that project
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
own default and Kiota's `get_default_client()` both trust certifi's bundle. Getting started does, and
`sign_in_with_password` does the same for the client it makes when you pass none.

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
only for Python below 3.11, so never installed on the two supported versions). `pyproject.toml` pins the build backend exactly (`setuptools==84.0.0`),
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

`test_sign_in.py` runs the sign-in against a fake gateway and pins its negatives: no token request
follows a login refusal, a missing cookie, a redirect, state, code or `error` that does not check out,
or MFA without a code; the PKCE challenge is the S256 of a verifier the authorize request never
carries; the bearer reaches only the gateway; and a rotating refresh token is spent once however many
calls wait. It also pins the provider sign-in's negatives: a redirect with another state (another start's included), an `error`,
a repeated parameter or another host, path or fragment makes no callback request and leaves the `pending` usable; the verifier never
reaches `start` and its S256 challenge matches; a discovery document naming another origin ends the call before the callback; MFA completes
without a second callback, through another client; and a retry after a later failure does not send the spent code again. `test_live_smoke.py` is skipped unless its variables are set (see Getting started).

Last run on 2026-09-22 on CPython 3.13.12 and 3.12.13 (macOS arm64, pip 26.0): both locks installed
with `--require-hashes`; 23 tests passed from source under `-X dev` with `ResourceWarning` as an
error; `build --no-isolation` produced the wheel (88 Python files) and the sdist; in a second fresh
environment the wheel installed offline over the runtime lock, `pip check` reported no broken
requirements, and the same 23 tests passed against the installed wheel. Python 3.10 and 3.11, a
live gateway, and a published index were not exercised.

Run again on 2026-10-03, on CPython 3.13.12 and 3.12.13, from source under `-X dev` with
`ResourceWarning` as an error, against the packages of the locked environment above (the interpreters'
own site-packages did not hold them, so the main checkout's `.venv` packages were put on the path): 98 tests
passed. They are the 58 of the run before (the 23, a delivery `jobsMaxRunSeconds` case, the 32 cases of
`test_jobs.py` and two generated-surface cases), the 36 of `test_schedules.py`, two cases that hold the Jobs
client's requests to the generated builders', and two generated-surface cases for the actor and workflow
routes' and the Data API's 413 problems, after the client contract was re-mirrored from product `main`
`92ac1ced` and the generated tree regenerated from it. The wheel and sdist were not rebuilt and the
installed-wheel run was not repeated, so the wheel evidence above predates `jobs.py` and `schedules.py`.

Run once more on 2026-10-03 in the same way, after the client contract was re-mirrored from product `main`
`f9d57e29` and the generated tree regenerated from it: 101 tests passed on both interpreters, the 98 above
with the schedule escaping case split into one for an id that follows the rule and one for an older id that a
read, a run and a delete still address, and two cases added (every Latin-1 character and the edge lengths held
to the pattern read from the mirrored contract, and an id's URL held to the generated builder's). The wheel
was not rebuilt.

Run on 2026-10-07 on CPython 3.13.12 and 3.12.13, each in an environment made from the locks with
`--require-hashes` (pip 26.0), from source under `-X dev` with `ResourceWarning` as an error, after the
password sign-in was added: 111 tests, 110 passed and the live smoke test skipped (no `YOGABASE_SMOKE_*`
variable was set). Nine are `test_sign_in.py`'s. On 3.13 the package was also installed from the checkout with
`pip install --no-deps --no-build-isolation .` into a clean environment (`pip check` clean) and the same
111 ran against that copy. Getting started's example was run verbatim against the same fake gateway, and the
smoke test's body once against it, to prove the requests are well formed. The wheel and sdist were not rebuilt,
and no run against a live installation is recorded: the one probe made, an anonymous
`GET /connect/authorize` on the owner's CloudFront gateway, answered OpenIddict's own `400 invalid_request`
(an unregistered `client_id`), not a Yogabase sign-in refusal.

Run again later on 2026-10-07 at version 0.4.0.dev1, in new environments made from the locks with
`--require-hashes`: 111 tests, 110 passed and the live smoke test skipped, on CPython 3.13.12 and 3.12.13
from source under `-X dev` with `ResourceWarning` as an error. The wheel and sdist were built with
`build --no-isolation`, and on 3.13.12 the wheel was installed offline over the runtime lock alone
(`pip check` clean) and the same 111 ran against that copy with `python -P`.

Run on 2026-10-08 on CPython 3.13.12 and 3.12.13, each in an environment made from the locks with `--require-hashes`, from source
under `-X dev` with `ResourceWarning` as an error, after sign-in with an external provider was added, the password sign-in's
steps were shared with it, and the provider sign-in began to read the discovery document before sending the callback: 118 tests,
117 passed and the live smoke test skipped, on both. Seven are the provider sign-in's. The wheel
and sdist were not rebuilt, and nothing here has run against a live installation or a real provider.
