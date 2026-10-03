# Yogabase Dart client preview

Generated with Microsoft Kiota 1.34.1 from the current Yogabase v1-preview contract. Dart output
is experimental in this generator version. Callers must provide a request adapter with the exact
Yogabase REST gateway and a project bearer-token authentication provider.

The generated REST surface includes bootstrap, signup and interactive auth endpoints,
Storage, stateless Function invocation, Actor Lambda invocation, and Workflow start/cancel. The
contract returns a separate realtime origin, but this package does not yet include or qualify a
Dart/Flutter SignalR wrapper. This source preview is local, unpublished, and not
production-qualified.

## The Data API and game servers are generated; builds are not

The mirrored contract declares the restored client Data API: `POST /v1/data/{table}/select|insert|upsert|update|delete`
on the application gateway, authenticated with the signed-in user's access token and run under
row-level security as that user. It is served only where the bootstrap document's requested-only
`data-rest` row says so (a client asks for the row by naming it in `X-Yogabase-Accept-Capabilities`),
and this client carries the operations as generated code, for example `client.v1.data.byTable('notes').select.postAsync(body)` with the builders
`select`, `insert`, `upsert`, `update` and `delete`. A response is a `DataResult` (`rows`, `affectedRows`, `hasMore`) and a refusal is an RFC 7807 problem whose `code` is one of the refusal codes
the contract lists under `x-yogabase-data-api`, which also states the limits (at most 1000 rows per
select and 500 per write, 1 MiB per value, a five-second statement timeout). Request values are plain JSON.
Nothing in this package has called the Data API against a live installation. A typed data API and
Entity Framework Core inside Lambdas stay server-side: no client SDK carries EF code or a model.

Two other surfaces were added when the Data API was withdrawn:

- **Game servers** — `POST /v1/servers/start`, `GET /v1/servers`, `GET /v1/servers/{serverId}`
  and `POST /v1/servers/{serverId}/stop` on the application gateway, authenticated with the
  signed-in player's access token. They are part of the mirrored application contract, so this
  client carries them as generated code: `client.v1.servers` (`start.post(body)`, `get()`, `byServerId(id).get()`, `byServerId(id).stop.post()`). The list answers a bare array; a `GameServer` is
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
and its standalone authored-source checks (see Local verification below); Flutter integration and
live-gateway behavior remain unqualified.

For `PUT`, add both required contract headers through the Kiota request configuration:
`X-Yogabase-Content-Sha256: sha256:<64 lowercase hex>` and a bounded `Idempotency-Key`. Kiota does
not emit dedicated Dart method arguments for these headers.

## Durable Jobs

`YogabaseJobsClient`, authored and reached from `YogabaseApplicationClient.jobs(projectId)`, starts,
reads, cancels and fetches the result of a durable background job as the signed-in user, against
`/v1/projects/{projectId}/jobs` on the application gateway and behind the same project token (the
`lambda.invoke` scope) as invoking a function. A **job type is the id of a deployed stateless function**;
its highest released version runs the job. A job belongs to the user who submitted it, and another user's
job is answered exactly as one that does not exist.

Nothing is generated for it: the product serves these routes (owner decision, 2026-10-03) but its client
OpenAPI document does not describe them yet, so the client is written from the gateway's source and
developer documentation. It shares the Kiota request adapter, and so the authentication and the origin, of
the generated application client.

**It has not been run or analyzed.** No Dart SDK was available when it was written, so
`lib/src/yogabase_jobs_client.dart` and `test/yogabase_jobs_source_test.dart` were modelled line by line on
the generated builders and models and the existing standalone check, and neither the compiler nor the
analyzer has seen them. Run the commands under "Local verification" before relying on it.

```dart
final application = YogabaseApplicationClient(requestAdapter, Uri.parse('https://apps.example.test'));
final jobs = application.jobs(projectId); // projectId is the bootstrap document's projectId

var job = await jobs.submit(
  jobType: 'orders-create',
  idempotencyKey: 'order-42',               // keep it across retries of this submit
  payload: Uint8List.fromList(utf8.encode('hello')), // optional, at most 1 MiB before base64
  maxAttempts: 3,                           // optional, 1 to 10; the gateway's default is 5
);
while (!job.isTerminal) {                   // succeeded, cancelled or deadLettered
  await Future<void>.delayed(const Duration(seconds: 1));
  job = await jobs.get(job.jobId);
}
if (job.status == YogabaseJobStatus.succeeded && job.resultReference != null) {
  final result = await jobs.result(job.jobId); // result.content
}
await jobs.cancel(job.jobId);               // idempotent; a running job is flagged and stopped by its worker
```

- **Retrying a submit is safe.** The idempotency key is a member of the request body, not a header (the
  gateway reads no `Idempotency-Key` header on this route). The same request under the same key answers
  the job it already made (202 again, whatever the job has become); a different request under that key is
  refused with `job_idempotency_conflict` (409). The key belongs to the signed-in user.
- **Checked before anything is sent**, with the gateway's own bounds, as `ArgumentError`s: a job type of 1 to
  128 ASCII letters, digits, `.`, `-`, `_` or `:`; a trimmed idempotency key of 1 to 256 characters with no
  control character; a payload of at most 1,048,576 bytes (exactly that is a job); and `maxAttempts` from 1 to 10.
- **Refusals** are `YogabaseJobsException`s carrying the gateway's `status` and `code`
  (`YogabaseJobErrorCodes` names them): `job_type_not_registered` (422), `job_idempotency_conflict` (409),
  `job_payload_too_large` (413, with `limitBytes` and `maxBodyBytes` naming the payload and body limits),
  `job_request_invalid` (400), `job_access_denied` (403), `job_unavailable` (503) and `job_not_found` (404, for
  no such job and for another user's, never told apart). A plain `not_found` is an installation that does
  not serve Jobs at all; no capability row says whether one does.
- **A result is not checked against its digest here.** `result()` returns the bytes
  (`job_result_not_ready` is 409 for a job that has not settled; `job_result_not_found` is 404 for another
  user's job, an unknown one and one that finished without a result, never told apart). The other languages
  compare the bytes with the `X-Yogabase-Content-Sha256` header; a Kiota Dart adapter does not hand the caller
  response headers and this package has no hashing dependency, so this client cannot. The gateway has
  already verified the stored object against the digest the job recorded before it sends a byte, and a
  result over 16 MiB is refused.
- A response member this client does not know is ignored, as the generated clients keep unknown members
  aside; a job that breaks a bound it does know (an unknown status, `maxAttempts` outside 1 to 10) is refused.

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
token, delivery header helpers, and buffered SSE parsing that preserves opaque resume IDs. Cancelling
the token ends a poll between requests; during the delay between attempts it also stops the delay's
timer, so an abandoned poll does not keep the isolate alive for the rest of the interval. True
streaming remains dependent on a streaming Dart request adapter and is not claimed as qualified.
Artifact admission does not expose Kubernetes credentials or add Agones/PlayFlow or unimplemented
runtime lifecycle operations.

## Local verification

```text
cd languages/dart
dart pub get --enforce-lockfile
dart analyze --fatal-infos --fatal-warnings
dart run test/yogabase_delivery_source_test.dart
dart run test/yogabase_jobs_source_test.dart
```

The package declares no `dev_dependencies`, so `dart test` is not wired; the checks are one
standalone program that prints `dart delivery source checks: PASS` or exits non-zero with the failed
expectation. Every request goes to an in-memory `RequestAdapter`. It covers SSE parsing (including
the one-space field rule), the cancellation token, the delivery bearer origin check, the storage
logical-key cases shared with the Python suite, the generated game-server list and stop requests,
polling to a terminal state, cancellation during a polling delay (inside a zone that records every
timer, asserting none is left active), an already-cancelled poll sending nothing, and the
event-stream request's `Last-Event-ID`, `Accept` and `operationId` query.

Last run on 2026-09-17 with the Dart 3.13.2 stable macOS arm64 SDK (revision
`60a57cd42d64dc03e9f07aa60a2e250755c1ef28`), unpacked from the archive whose SHA-256 matched
`MANIFEST.json`: `pub get --enforce-lockfile` left `pubspec.lock` unchanged, analysis found no
issues with infos and warnings fatal, and the standalone program passed and exited within a second.
Flutter, a streaming adapter, and a live gateway were not exercised.

That run predates the source regenerated on 2026-10-03 (the restored Data API builders and models, the
invoke builder's 413 mapping and `jobsMaxRunSeconds` on the delivery release configuration) and the
authored Jobs client with its standalone program. No Dart SDK was installed on the machine that made
those changes, so none of them has been through `dart analyze` or `dart run`: the commands above are the
check they are waiting for.
