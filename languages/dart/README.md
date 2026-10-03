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

The product's client OpenAPI document describes the Jobs routes, so Kiota generates their request builders
(`api.v1.projects.byProjectId(projectId).jobs`, with `Job`, `JobStatus` and `JobSubmitRequest` among the
models). This client **wraps those builders**: each request is the one the generated builder builds (its
route, its media types and its serialized body), sent through the Kiota request adapter of the generated
application client, and so with its authentication and origin. What it adds is what a generated builder does
not give: it checks each call before it is sent, refuses an answer that is not what the route declares, and
names every refusal by its code.

**It has not been run or analyzed.** No Dart SDK was available when it was written (nor when it was
re-written over the generated builders), so `lib/src/yogabase_jobs_client.dart`,
`lib/src/yogabase_schedules_client.dart` and the two source checks under `test/` were modelled line by line on
the generated builders and models, the Kiota Dart packages in the pub cache and the existing standalone
check, and neither the compiler nor the analyzer has seen them. Run the commands under "Local verification"
before relying on it.

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
  not serve Jobs at all; no capability row says whether one does. A status the gateway names no code for
  reads as `YogabaseStatusErrorCodes` names it (`unsupported_media_type` for a submit that is not JSON,
  `payload_too_large` for a bare 413 from a gateway that predates the named one).
- **A result is not checked against its digest here.** `result()` returns the bytes
  (`job_result_not_ready` is 409 for a job that has not settled; `job_result_not_found` is 404 for another
  user's job, an unknown one and one that finished without a result, never told apart). The other languages
  compare the bytes with the `X-Yogabase-Content-Sha256` header; a typed Kiota Dart call does not hand the
  caller response headers (only a native response handler does) and this package has no hashing dependency,
  so this client cannot. The gateway has
  already verified the stored object against the digest the job recorded before it sends a byte, and a
  result over 16 MiB is refused.
- A response member this client does not know is ignored, as the generated clients keep unknown members
  aside; a job that breaks a bound it does know (an unknown status, `maxAttempts` outside 1 to 10) is refused.

## Schedules

`YogabaseSchedulesClient`, authored and reached from `YogabaseApplicationClient.schedules(projectId)`,
creates, changes, reads, deletes and runs once the signed-in user's own **recurring or one-shot schedules** of
a deployed stateless function, against `/v1/projects/{projectId}/schedules/{scheduleId}` on the application
gateway and behind the same project token (the `lambda.invoke` scope) as invoking a function. A schedule
belongs to the user who created it, and only that user reads, changes, deletes or runs it: another user's
schedule is a bare 404, updating it is a 403 and creating a schedule under its id is a 412. Like the Jobs
client it wraps the generated builders (`api.v1.projects.byProjectId(projectId).schedules.byScheduleId(id)`),
sends each request as the generated builder builds it, and adds what a builder does not give: every write is
conditional on the schedule's **ETag**, which it sends as the header the route wants and builds from the
schedule it reads back. Like the Jobs client it **has not been run or analyzed**; its standalone check is
`test/yogabase_schedules_source_test.dart`.

```dart
final schedules = application.schedules(projectId);

var schedule = await schedules.create(const YogabaseScheduleDefinition(   // If-None-Match: *  (201)
  scheduleId: 'nightly',
  functionId: 'orders-nightly',             // a deployed Stateless Lambda
  functionVersion: '1',                     // an admitted version
  cron: '0 3 * * *', timeZone: 'Asia/Seoul', // five fields; or set oneShotAtUtc for a one-shot
  misfirePolicy: YogabaseScheduleMisfirePolicy.skipToNextOccurrence, // required: a missing one is a 400 naming the member
  concurrencyPolicy: YogabaseScheduleConcurrencyPolicy.allow,        // required too: allow, forbid or replace (see below)
  maxCatchUp: 1, maxAttempts: 3, deadlineSeconds: 300,
));
schedule = await schedules.update(definition, schedule.eTag);             // If-Match (200)
final run = await schedules.trigger('nightly', schedule.eTag, YogabaseScheduleTriggerRequest(
  idempotencyKey: 'run-1',                  // the same key against the same version is the same run
  deadline: DateTime.now().toUtc().add(const Duration(minutes: 10)),      // after now, at most an hour from now
));                                         // run.outcome is enqueued, or replayed for a repeat
await schedules.delete('nightly', schedule.eTag);                         // If-Match (204)
```

- **Every write names the ETag it read.** `eTag` is built from the schedule's `version` (decimal text, a
  64-bit number a JSON number cannot carry) and `instanceId`: `"schedule-v<version>-<instance>"`. A stale ETag,
  creating an id that is already held and updating one that does not exist are each a 412
  (`precondition_failed`) that changes nothing; the scheduler advances a recurring schedule's version as it
  runs, so an ETag held for long is stale: `get` it again. Unlike the other languages' schedule clients this
  one does not check the `ETag` header against the body, because a typed Kiota call does not hand a Dart caller
  the response headers.
- **Checked before anything is sent**, with the gateway's bounds, as `ArgumentError`s (the `maximum*` constants
  of `YogabaseSchedulesClient`): for a write, a schedule id of 1 to 128 characters, a letter or a digit first
  and then letters, digits and `.` `_` `~` `:` `-` only (the contract's pattern; see the next note), a function
  id of at most 256 and a version of at most 64 characters, a cron expression and a time zone of at most 128,
  `maxCatchUp` 1 to 128, `maxAttempts` 1 to 16, a deadline of 1 to 86,400 seconds, a payload of at most
  1,048,576 bytes, and an ETag in the gateway's form; for a read, a run and a delete, a schedule id of 1 to 128
  trimmed characters with no control character (and not `.` or `..`, which a URL parser would collapse). Both
  policies are required constructor arguments, so a definition cannot leave one out. A one-shot instant and a
  run's deadline are sent as UTC instants.
- **Refusals** are `YogabaseSchedulesException`s carrying the gateway's `status` and `code`. Only the 413 names
  a code, `schedule_payload_too_large` (`YogabaseScheduleErrorCodes`), with `limitBytes` and `maxBodyBytes`;
  every other refusal is a status with a fixed title or no body, and reads as `YogabaseStatusErrorCodes`
  names it: `precondition_failed` (412), `precondition_required` (428), `not_found` (404; also an installation
  without the Scheduler), `conflict` (409: a function version that is not an admitted Stateless one of the
  project, on a write or on a run, a write that would turn a recurring schedule into a one-shot or the
  reverse, or a run key the queue holds for another request; a run of a withdrawn version answered 503 before
  product `main` `f9d57e29`), `forbidden` (403), `unsupported_media_type` (415) and `request_failed` (a 400
  for a member the gateway refuses, such as a cron expression that does not parse or an id that breaks the
  rule, which names the member in the problem's `detail`; a 503).
- **A write holds the id to one rule; an older id is still addressed.** The id is one segment of the URL path,
  so a create and an update are held to the contract's pattern, `^[A-Za-z0-9][A-Za-z0-9._~:-]{0,127}$`, before
  anything is sent, and the gateway refuses an id that breaks it with a 400 that names the member and the rule.
  A schedule written before the rule under another id is still found by that id (percent-encoded, as any path
  segment is) by `get`, `trigger` and `delete`, which therefore check only the older bounds; it
  cannot be changed, so delete it and create it again under an id that follows the rule.
- **The concurrency policy says what the scheduler does about an earlier occurrence that has not finished**
  (its invocation is queued, waiting to start or to be retried, or running, and its deadline has not passed):
  `allow` starts the new occurrence alongside, `forbid` skips it, and `replace` ends an earlier occurrence that
  is still queued (failure code `schedule_occurrence_replaced`) and starts the new one, but skips it while one
  is running, because nothing can interrupt a Lambda that is executing. A skipped occurrence is consumed, not
  deferred. A manual run is not an occurrence, so the policy neither holds it back nor is held back by it, and
  a one-shot has no earlier occurrence. The contract of product `main` `f9d57e29` describes this; a build of
  the product from before it starts every occurrence whichever policy is chosen and accepts any id but one with
  a `/` (a 400).

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
dart run test/yogabase_schedules_source_test.dart
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
invoke builder's 413 mapping and `jobsMaxRunSeconds` on the delivery release configuration), the source
regenerated again that day from the client contract of product `main` `92ac1ced` (the Jobs and schedule
builders and models, the actor and workflow 413 mappings), and the authored Jobs and schedule clients with
their standalone programs. After that, on 2026-10-03, the client contract was re-mirrored from product `main`
`f9d57e29` and the source regenerated again (doc comments only), and the schedule client was changed to hold a
write's id to the contract's pattern and to describe the policies as the contract now does, with a standalone
check for each. No Dart SDK was installed on the machine that made those changes, so none of them
has been through `dart analyze` or `dart run`: the commands above are the check they are waiting for.
