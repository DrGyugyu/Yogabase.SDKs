# Yogabase TypeScript client preview

Generated with Microsoft Kiota 1.34.1 from the current Yogabase v1-preview contract, with authored
facades beside it. **It runs in Node, not in a browser:** the gateway sends no CORS headers, so a page on
another origin cannot call it at all, and sign-in needs to set a `Cookie` header and read a redirect, which a
browser does not let a script do. It was run on Node 25.9.0 and `engines` says so; older versions are untested.

The generated REST surface includes bootstrap, signup and interactive auth endpoints,
Storage, stateless Function invocation, Actor Lambda invocation, Workflow start/cancel, and the durable Jobs
and schedule routes (which authored clients below sit beside). The
contract returns a separate realtime origin, but this package does not yet include or qualify a
TypeScript SignalR wrapper. This source preview is unpublished and not production-qualified;
TypeScript generation and its Kiota runtime are themselves preview dependencies.

## Getting started

Nothing is published: the package is marked `private` and `dist` is not committed. Build it from a checkout of
this repository and install the tarball into your app (an ES module project, `"type": "module"`):

```text
cd Yogabase.SDKs/languages/typescript
npm ci        # installs the locked dependencies and compiles dist/ (the prepare script)
npm pack      # writes stackyoga-yogabase-client-preview-0.4.0-preview.1.tgz
cd path/to/your-app && npm install path/to/stackyoga-yogabase-client-preview-0.4.0-preview.1.tgz
```

Apart from the bootstrap and the `auth` calls, every call needs a signed-in user, so sign in before the first one:

```ts
import { createHash } from "node:crypto";
import { DefaultRequestAdapter } from "@microsoft/kiota-bundle";
import { createYogabaseApiClient, YogabaseSession } from "@stackyoga/yogabase-client-preview";

const session = new YogabaseSession({
  gatewayHost: "https://api.example.com",     // the installation's gateway origin
  publishableKey: "sy_pub_example_project",   // the project's publishable key
});
await session.signInWithPassword("user@example.com", password);

const adapter = new DefaultRequestAdapter(session);   // the session is the authentication provider
adapter.baseUrl = session.gatewayHost;
const yogabase = createYogabaseApiClient(adapter);

const payload = new TextEncoder().encode("{}");
const result = await yogabase.v1.projects.byProjectId(session.projectId!)   // known once signed in
  .functions.byFunctionId("hello-world").versions.byFunctionVersion("1").invoke.post({
    idempotencyKey: crypto.randomUUID(),
    correlationId: "first-call",
    deadline: new Date(Date.now() + 20_000),
    mode: 0,                                           // synchronous
    payloadDigest: `sha256:${createHash("sha256").update(payload).digest("hex")}`,
    inlinePayloadBase64: Buffer.from(payload).toString("base64"),
  });
console.log(result?.outcome);                          // 4: Completed
```

`signInWithPassword` needs no browser: it logs in for the identity cookie, then runs the OIDC authorization-code
flow with PKCE using that cookie, with the first redirect URI the bootstrap registers, and never follows the
redirect. The tokens stay in memory (they are gone when the process ends) and `session.accessToken()` refreshes
them a minute before they expire; a refresh token that no longer works is an `invalid_grant`. Besides
authenticating the generated client, `session.accessToken` is the `accessToken` of the Jobs and Schedules
clients below.

- **The user must already exist, and have confirmed their e-mail address.** A user signs up with an e-mail address
  and a password (`yogabase.auth.signup.post({ email, password })`) and must follow the link in the confirmation
  e-mail (`yogabase.auth.email.confirm`) before signing in. A project's gateway runs with open sign-up and nothing in
  the product creates an invitation, so `invitationToken` is not needed: only an invite-only mode that no
  installation setting selects reads it (`403 invitation_required`). On an installation with no e-mail transport
  configured, sign-up is a `503 authentication_email_unavailable`; a self-hosted installation can instead set
  `yogabase.authenticationEmail.confirmation: disabled`, and accounts are then created already confirmed. An
  address that already has an account is a `409 account_exists`, and account data the gateway rejects a
  `400 account_invalid`.
- **A refusal is a `YogabaseAuthError`** with the gateway's `status` and `code` (and `correlationId`):
  `401 invalid_credentials` is a wrong password, an unknown account, an unconfirmed e-mail and a locked account
  alike, never told apart; `403 mfa_enrollment_required` (the project requires MFA and the account has none),
  `429 authentication_rate_limited` and `401 interactive_login_required` (the identity cookie did not come back).
  An account with MFA throws `202 mfa_required`; call again with the authenticator code,
  `signInWithPassword(email, password, code)`. An authorize answer that is not this call's (another state or
  redirect URI, no code, or an `error`) is `authorization_response_invalid`, `authorization_redirect_mismatch` or
  the `error` itself, and no token is requested after it.
- **A live check.** `tests/liveSmoke.test.mjs` signs in, invokes a function and puts and gets an object. It is
  skipped unless `YOGABASE_SMOKE_GATEWAY`, `YOGABASE_SMOKE_PUBLISHABLE_KEY`, `YOGABASE_SMOKE_EMAIL`,
  `YOGABASE_SMOKE_PASSWORD` (an existing, confirmed user without MFA), `YOGABASE_SMOKE_FUNCTION_ID` and
  `YOGABASE_SMOKE_FUNCTION_VERSION` are set. No run against a live installation is recorded here yet.

## Signing in with a provider

A project that configured Google, Microsoft or Apple signs its users in through the gateway. The browser step belongs
to your application, so there are two calls around it (Node only, like `signInWithPassword`):

```ts
// The redirect URI must be one the project registered (the bootstrap's `oidcRedirectUris`).
const pending = await session.startExternalSignIn("google", "com.example.app:/oauth/callback");

// Open this in the system browser, and receive the address the browser is redirected to.
const redirect: string = await openInBrowserAndWaitForRedirect(pending.authorizationUrl);

await session.completeExternalSignIn(pending, redirect);   // the tokens are in the session now
```

- **The provider id** is the one the project gave the provider (`google` above).
- **The one address a developer registers with each provider** is `https://<gateway>/auth/external/<provider id>/return`.
  The provider sends the browser there; the gateway sends it on to your redirect URI with `code` and `state`, or with
  `error=access_denied` and `state`.
- **The client checks the redirect itself.** A redirect with another `state`, an `error`, no `code`, a repeated
  parameter or another address is a `YogabaseAuthError` (`authorization_response_invalid`, `access_denied`,
  `authorization_redirect_mismatch`) before any request. Only the S256 challenge of the verifier is sent to `start`; the
  verifier goes to the callback alone. `pending` holds the state and the verifier out of reach and completes the
  redirect of its own start.
- **A refusal is a `YogabaseAuthError` with the gateway's code**: `external_identity_invalid` (401),
  `external_identity_email_in_use` (409: an account already uses the address and nothing is linked),
  `external_email_unverified`, `external_identity_not_enrolled`, `mfa_enrollment_required` and `invitation_required`
  (403), `external_provider_unknown` (404), `external_redirect_uri_invalid` and `external_callback_invalid` (400) and,
  when the installation or the provider cannot be reached, `external_identity_unavailable` or
  `external_provider_unavailable` (503). A `start` answer that is not an https address with a state is
  `external_start_invalid`. An account with MFA throws `202 mfa_required` after the callback; call
  `completeExternalSignIn` again with the same `pending` and the authenticator code as the third argument, and the
  spent code is not sent a second time.
- **There is no loopback listener and no browser launcher in the package.** Opening the address and receiving the
  redirect (a custom URI scheme, an app link, a loopback listener of your own) is the application's.
- **It has never run against a real provider.** Only fake-fetch tests cover it, with the gateway's answers written from
  its route code.

## The Data API and game servers are generated; builds are not

The mirrored contract declares the restored client Data API: `POST /v1/data/{table}/select|insert|upsert|update|delete`
on the application gateway, authenticated with the signed-in user's access token and run under
row-level security as that user. It is served only where the bootstrap document's requested-only
`data-rest` row says so (a client asks for the row by naming it in `X-Yogabase-Accept-Capabilities`),
and this client carries the operations as generated code, for example `yogabase.v1.data.byTable("notes").select.post(body)` with the builders
`select`, `insert`, `upsert`, `update` and `delete`. A response is a `DataResult` (`rows`, `affectedRows`, `hasMore`) and a refusal is an RFC 7807 problem whose `code` is one of the refusal codes
the contract lists under `x-yogabase-data-api`, which also states the limits (at most 1000 rows per
select and 500 per write, 1 MiB per value, a five-second statement timeout). Request values are plain JSON.
Nothing in this package has called the Data API against a live installation. A typed data API and
Entity Framework Core inside Lambdas stay server-side: no client SDK carries EF code or a model.

Two other surfaces were added when the Data API was withdrawn:

- **Game servers** — `POST /v1/servers/start`, `GET /v1/servers`, `GET /v1/servers/{serverId}`
  and `POST /v1/servers/{serverId}/stop` on the application gateway, authenticated with the
  signed-in player's access token. They are part of the mirrored application contract, so this
  client carries them as generated code: `yogabase.v1.servers` (`start.post(body)`, `get()`, `byServerId(id).get()`, `byServerId(id).stop.post()`). The list answers a bare array; a `GameServer` is
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

`objects.byKey(...)` is generated from an OpenAPI path parameter and is safe only for a validated
single path segment. For logical keys containing `/`, use the authored Yogabase adapter exported
from the package root; it applies the C# logical-key grammar and then calls the generated item's
`withUrl(...)` raw-URL override:

```ts
const item = storageObjectForLogicalKey(
  yogabase.v1.storage.objects,
  adapter.baseUrl,
  "avatars/user-1/photo.png",
);
```

This adapter is authored Yogabase code and is clearly separate from Kiota-generated files.

For `PUT`, pass both required contract headers through the Kiota request configuration:
`X-Yogabase-Content-Sha256: sha256:<64 lowercase hex>` and a bounded `Idempotency-Key`. Kiota
models operation header parameters through the generic `headers` record rather than dedicated
method arguments in this generated TypeScript surface.

## Durable Jobs

`YogabaseJobsClient`, authored and exported from the package root, starts, reads, cancels and collects
the result of a durable background job as the signed-in user, against `/v1/projects/{projectId}/jobs`
on the application gateway and behind the same project token (the `lambda.invoke` scope) as invoking a
function. A **job type is the id of a deployed stateless function**; its highest released version runs
the job. A job belongs to the user who submitted it, and another user's job is answered exactly as one
that does not exist.

The product's client OpenAPI document describes the Jobs routes, so Kiota also generates their request
builders (`yogabase.v1.projects.byProjectId(projectId).jobs`, with `Job`, `JobStatus` and `JobSubmitRequest`
among the models). This client sits **beside** those builders, not on top of them: the result route's
digest is a response header a generated builder cannot hand back, each call is checked before it is sent,
and every refusal is named by its code. Like the delivery client it takes its own gateway host and access
token rather than a Kiota adapter. `tests/jobs.test.mjs` pins every shape and holds each request it sends
to the request the generated builder builds for the same operation, so a contract change that moves a
route or a member fails there.

```ts
const jobs = new YogabaseJobsClient({       // `session` is the signed-in YogabaseSession from above
  gatewayHost: session.gatewayHost,
  projectId: session.projectId!,
  accessToken: session.accessToken,           // the signed-in user's access token, refreshed as needed
});
let job = await jobs.submit({
  jobType: "orders-create",
  idempotencyKey: "order-42",                 // keep it across retries of this submit
  payload: new TextEncoder().encode("hello"), // optional, at most 1 MiB before base64
  maxAttempts: 3,                             // optional, 1 through 10; the gateway's default is 5
});
while (!(YOGABASE_JOB_TERMINAL_STATUSES as readonly string[]).includes(job.status)) {   // Succeeded, Cancelled or DeadLettered
  await new Promise(resolve => setTimeout(resolve, 1000));
  job = await jobs.get(job.jobId);
}
if (job.status === "Succeeded" && job.resultReference !== null) {
  const { content, digest } = await jobs.result(job.jobId);
}
await jobs.cancel(job.jobId);                 // idempotent; a running job is flagged and stopped by its worker
```

- **Retrying a submit is safe.** The idempotency key is a member of the request body, not a header (the
  gateway reads no `Idempotency-Key` header on this route). The same request under the same key answers
  the job it already made (202 again, whatever the job has become); a different request under that key is
  refused with `job_idempotency_conflict` (409). The key belongs to the signed-in user.
- **Checked before anything is sent**, with the gateway's own bounds: a job type of 1 to 128 ASCII letters,
  digits, `.`, `-`, `_` or `:`; a trimmed idempotency key of 1 to 256 characters with no control character;
  a payload of at most 1,048,576 bytes (exactly that is a job); and `maxAttempts` from 1 through 10.
- **Refusals** are `YogabaseJobsError`s carrying the gateway's `status` and `code`
  (`YOGABASE_JOB_ERROR_CODES` names them): `job_type_not_registered` (422), `job_idempotency_conflict`
  (409), `job_payload_too_large` (413, with `limitBytes` and `maxBodyBytes` naming the payload and body
  limits), `job_request_invalid` (400), `job_access_denied` (403), `job_unavailable` (503) and
  `job_not_found` (404, for no such job and for another user's, never told apart). A plain `not_found` is
  an installation that does not serve Jobs at all; no capability row says whether one does. A status the
  gateway names no code for reads as `YOGABASE_STATUS_ERROR_CODES` names it (`unsupported_media_type` for a
  submit that is not JSON, `payload_too_large` for a bare 413 from a gateway that predates the named one).
- **A result is read separately.** `resultReference` is a name, not a capability; `result()` returns
  `{ content, digest }` after hashing the bytes (Web Crypto) and comparing them with the
  `X-Yogabase-Content-Sha256` header. A result that does not match (`job_result_digest_mismatch`), arrives
  without exactly one well-formed `sha256:` digest, is larger than 16 MiB or is not a 200 is refused
  (`job_response_invalid`) and never returned. `job_result_not_ready` (409) is your own job that has not
  settled; `job_result_not_found` (404) is another user's job, an unknown one and a job that finished
  without a result, never told apart.
- A response member this client does not know is ignored, as the generated clients keep unknown members
  aside; a job that breaks a bound it does know (an unknown status, `maxAttempts` outside 1 through 10) is refused.

It was also run end to end on 2026-10-03 against the product's real Jobs routes, built from product
`main` `92ac1ced` (26 checks; see the root README for what that host was and was not).

## Schedules

`YogabaseSchedulesClient`, authored and exported from the package root, creates, changes, reads, deletes and
runs once the signed-in user's own **recurring or one-shot schedules** of a deployed stateless function,
against `/v1/projects/{projectId}/schedules/{scheduleId}` on the application gateway and behind the same
project token (the `lambda.invoke` scope) as invoking a function. A schedule belongs to the user who created
it, and only that user reads, changes, deletes or runs it: another user's schedule is a bare 404, updating it
is a 403 and creating a schedule under its id is a 412. Like the Jobs client it sits beside the generated
builders (`yogabase.v1.projects.byProjectId(projectId).schedules.byScheduleId(id)`), because a write is
conditional on the schedule's **ETag**, which a generated builder neither reads from a response nor sends
unless it is added by hand; `tests/schedules.test.mjs` pins every shape and holds each request to the
generated builder's.

```ts
const schedules = new YogabaseSchedulesClient({
  gatewayHost: session.gatewayHost, projectId: session.projectId!, accessToken: session.accessToken,
});
const definition: YogabaseScheduleDefinition = {
  scheduleId: "nightly",
  functionId: "orders-nightly",                     // a deployed Stateless Lambda
  functionVersion: "1",                             // an admitted version
  cron: "0 3 * * *", timeZone: "Asia/Seoul",        // five fields; or set oneShotAtUtc for a one-shot
  misfirePolicy: "SkipToNextOccurrence",            // required: a missing one is a 400 naming the member
  concurrencyPolicy: "Allow",                       // required too: Allow, Forbid or Replace (see below)
  maxCatchUp: 1, maxAttempts: 3, deadlineSeconds: 300,
  payload: new TextEncoder().encode("{}"),          // optional, at most 1 MiB before base64
};
let schedule = await schedules.create(definition);  // If-None-Match: *  (201)
schedule = await schedules.update({ ...definition, cron: "0 4 * * *" }, schedule.eTag);   // If-Match (200)
const run = await schedules.trigger("nightly", schedule.eTag, {   // If-Match; runs it once now (202)
  idempotencyKey: "run-1",                          // the same key against the same version is the same run
  deadline: new Date(Date.now() + 10 * 60_000),     // after now and at most an hour from now
});                                                 // run.outcome is "Enqueued", or "Replayed" for a repeat
await schedules.delete("nightly", schedule.eTag);   // If-Match (204)
```

- **Every write names the ETag it read.** `eTag` is built from the schedule's `version` (decimal text, a 64-bit
  number a JSON number cannot carry) and `instanceId`: `"schedule-v<version>-<instance>"`. A stale ETag,
  creating an id that is already held and updating one that does not exist are each a 412
  (`precondition_failed`) that changes nothing; the scheduler advances a recurring schedule's version as it
  runs, so an ETag held for long is stale: `get` it again. A strong `ETag` header that disagrees with the
  body is refused; a weak or repeated one (a proxy that compresses a response may weaken it) is ignored.
- **Checked before anything is sent**, with the gateway's bounds (`YOGABASE_SCHEDULES_LIMITS`): for a write, a
  schedule id of 1 to 128 characters, a letter or a digit first and then letters, digits and `.` `_` `~` `:`
  `-` only (the contract's pattern; see the next note), a function id of at most 256 and a version of at most
  64 characters, a cron expression and a time zone of at most 128, both policies, `maxCatchUp` 1 through 128,
  `maxAttempts` 1 through 16, a deadline of 1 through 86,400 seconds, a payload of at most 1,048,576 bytes,
  and an ETag in the gateway's form; for a read, a run and a delete, a schedule id of 1 to 128 trimmed
  characters with no control character (and not `.` or `..`, which a URL parser would collapse). A one-shot
  instant and a run's deadline are sent as UTC instants.
- **Refusals** are `YogabaseSchedulesError`s with the gateway's `status` and `code`. Only the 413 names a code,
  `schedule_payload_too_large`, with `limitBytes` and `maxBodyBytes`; every other refusal is a status with a
  fixed title or no body, and reads as `YOGABASE_STATUS_ERROR_CODES` names it: `precondition_failed` (412),
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
  segment is) by `get`, `trigger` and `delete`, which therefore check only the older bounds; it cannot be
  changed, so delete it and create it again under an id that follows the rule.
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
(41 checks, five of them raw requests the client never makes). That run predates the id rule, the concurrency
enforcement and the 409 of a manual run, and was not repeated against product `main` `f9d57e29`; its harness is
in the product repository.

## Workload delivery

The authored `YogabaseDeliveryClient` is a separate client for the release-configured Delivery
API host. It uses a bearer token provider, returns response metadata (`etag`, `location`, and
`retryAfter`), and exposes the contract's preview/submit workflow for environments, artifact
admissions, releases, promotions, rollbacks, decommissions, and operation abandonment:

```ts
// Placeholders: workloadToken is a short-lived workload OIDC token (the CI path, not a signed-in user's token);
// projectId and templateId name your project and an approved template.
const abortController = new AbortController();
const delivery = new YogabaseDeliveryClient({
  deliveryHost: "https://delivery.example.test",
  accessToken: () => workloadToken,
});
const preview = await delivery.previewEnvironment(
  projectId,
  { name: "staging", templateId, templateVersion: 1 },
  { ifMatch: 'W/"7"' },
);
const accepted = await delivery.submitEnvironment(
  projectId,
  { previewId: preview.data.previewId, previewDigest: preview.data.previewDigest },
  { idempotencyKey: crypto.randomUUID() },
);
const operation = await delivery.waitForOperation(projectId, accepted.data.operationId, {
  terminalStates: ["Succeeded", "Failed", "Abandoned", "Cancelled", "Blocked"],
  signal: abortController.signal,
});
```

Artifact admission submits immutable source and evidence metadata plus digests; it does not upload
bundle bytes and this client does not add custom-build, Kubernetes, Agones, PlayFlow, or other
compatibility operations. The event stream is parsed with the platform Web Streams API and yields
`{ id, event, data }`; `id` is an opaque resume token that callers may explicitly pass back as
`lastEventId`. Heartbeat comment frames are ignored and resume tokens are never persisted or parsed
by the client. As the SSE grammar says, exactly one space after a field's colon is removed and any
further leading whitespace stays in the value, so an id round-trips unchanged; until 2026-09-22 this
client trimmed every leading space, as the Python client did before 2026-09-17.

`npm ci && npm test` passed 13 tests on 2026-09-22 (Node 25.9.0, npm 11.12.1). On 2026-10-03 `npm test` passed 88 tests (the 13 of the first run and the cases added since for delivery, Jobs, schedules, the generated surface and the schedule id rule held to the contract's pattern) on the same Node and npm, over a `node_modules` tree copied from that locked install rather than a fresh `npm ci`. On 2026-10-07, after a fresh `npm ci`, `npm test` ran 98 tests: 97 passed and the live smoke test was skipped (the 88, nine for the password sign-in, and the smoke test). On 2026-10-08, after a fresh `npm ci`, `npm test` ran 104 tests: 103 passed and the live smoke test was skipped (the 98 and six for external sign-in).
