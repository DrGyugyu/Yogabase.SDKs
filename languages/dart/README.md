# Yogabase Dart client preview

Generated with Microsoft Kiota 1.34.1 from the current Yogabase v1-preview contract (Dart output is
experimental in this generator version), with authored parts beside it: a password sign-in, durable
Jobs, schedules, a workload Delivery client and a hierarchical storage-key adapter. It runs on the Dart
VM (a command line, a server, Flutter on mobile or desktop), not in a browser.

The generated REST surface includes bootstrap, signup and interactive auth endpoints,
Storage, stateless Function invocation, Actor Lambda invocation, Workflow start/cancel, and the
request builders of the Jobs and schedule routes. The
contract returns a separate realtime origin, but this package does not yet include or qualify a
Dart/Flutter SignalR wrapper. This source preview is local, unpublished, and not
production-qualified.

## Getting started

Nothing is published, so depend on a checkout of this repository by path (a `git:` dependency with
`path: languages/dart` works the same way):

```yaml
dependencies:
  stackyoga_yogabase_client_preview:
    path: ../Yogabase.SDKs/languages/dart
  crypto: 3.0.7   # only for the snippet below
  uuid: 4.6.0     # likewise
```

Apart from the bootstrap and the `auth` calls, every call needs a signed-in user, so sign in before
the first one. `signInWithPassword` returns a session that is the Kiota authentication provider:

```dart
import 'dart:convert';
import 'dart:io';

import 'package:crypto/crypto.dart';
import 'package:microsoft_kiota_bundle/microsoft_kiota_bundle.dart';
import 'package:stackyoga_yogabase_client_preview/src/models/invocation_request.dart';
import 'package:stackyoga_yogabase_client_preview/stackyoga_yogabase_client_preview.dart';
import 'package:uuid/uuid.dart';

Future<void> main() async {
  final gateway = Uri.parse('https://api.example.com'); // the installation's gateway origin
  final session = await signInWithPassword(
    gateway,
    'sy_pub_example_project', // the project's publishable key, which is not a secret
    'user@example.com',
    stdin.readLineSync()!, // the password: prompt for it however you like
  );

  final application = YogabaseApplicationClient(DefaultRequestAdapter(authProvider: session), gateway);
  final api = application.api; // the generated client

  final payload = utf8.encode('{}');
  final result = await api.v1.projects
      .byProjectId(UuidValue.fromString(session.projectId))
      .functions
      .byFunctionId('hello-world')
      .versions
      .byFunctionVersion('1')
      .invoke
      .postAsync(InvocationRequest()
        ..idempotencyKey = const Uuid().v4()
        ..correlationId = 'first-call'
        ..mode = 0 // 0 is synchronous, 1 asynchronous
        ..deadline = DateTime.now().toUtc().add(const Duration(seconds: 20))
        ..payloadDigest = 'sha256:${sha256.convert(payload)}'
        ..inlinePayloadBase64 = base64.encode(payload));
  print('${result?.outcome} ${utf8.decode(base64.decode(result?.inlineResultBase64 ?? ''))}'); // outcome 4 is Completed
}
```

- **It needs no browser.** `signInWithPassword` logs in for the identity cookie, then runs the PKCE code
  flow with that cookie answering the authorize endpoint; the redirect is read and never followed, and
  the first redirect URI the bootstrap registers is used. The cookie is `Secure`, so the gateway must be
  an `https` origin (anything else is an `ArgumentError`). A `httpClient:` you pass is used for the
  sign-in and kept for refreshing, so keep it open; any `dart:io` client works, a browser one cannot
  (it hides the redirect and sets no `Cookie`).
- **The session** holds the rotating tokens and the `projectId` (the bootstrap's, which the generated
  builders and the Jobs and schedules clients ask for). It refreshes them a minute before they expire,
  once however many calls are waiting, and adds the bearer only to requests for the gateway origin.
  `getAccessToken()` returns the current token.
- **Generated models are not exported** by the library: import the ones you build, such as
  `InvocationRequest`, from `package:stackyoga_yogabase_client_preview/src/models/`.
- **The user must already exist.** A user signs up with an e-mail address and a password
  (`api.auth.signup.postAsync`) and must confirm the address (`api.auth.email.confirm`) before signing in.
  Sign-up is invite-only unless the installation turned that off: without an invitation token it is a
  `403 invitation_required`. Where confirmation is required but the installation has no e-mail transport,
  sign-up is a `503 authentication_email_unavailable`.
- **A refusal is a `YogabaseAuthException`** with the gateway's `status`, `code` and `correlationId` (quote
  it when reporting a problem). `401 invalid_credentials` is a wrong password, an unknown account, an
  unconfirmed e-mail and a locked account alike, never told apart; `403 mfa_enrollment_required` means
  the project requires MFA and the account has none; `429 authentication_rate_limited`. An account with
  MFA is `202 mfa_required`: call again with the authenticator code as `mfaCode:`. A redirect, state or
  code that does not check out is `400 authorization_redirect_mismatch` or
  `400 authorization_response_invalid`, a discovery document that names another origin is
  `502 oidc_discovery_invalid` before the password is sent, an `error` the authorization server sends
  back is the code, and the token endpoint keeps its own (`invalid_grant`). No token request follows any
  of these.
- **A live check.** `test/yogabase_live_smoke.dart` signs in, invokes a function, then puts and gets an object. It prints SKIPPED
  unless `YOGABASE_SMOKE_GATEWAY`, `YOGABASE_SMOKE_PUBLISHABLE_KEY`, `YOGABASE_SMOKE_EMAIL`,
  `YOGABASE_SMOKE_PASSWORD` (an existing, confirmed user without MFA), `YOGABASE_SMOKE_FUNCTION_ID` and
  `YOGABASE_SMOKE_FUNCTION_VERSION` (a deployed stateless function that accepts `{}`) are all set. Its
  invoke, put and get requests were checked against in-memory gateways; no run against a live installation is
  recorded here yet.

## The Data API and game servers are generated; builds are not

The mirrored contract declares the restored client Data API: `POST /v1/data/{table}/select|insert|upsert|update|delete`
on the application gateway, authenticated with the signed-in user's access token and run under
row-level security as that user. It is served only where the bootstrap document's requested-only
`data-rest` row says so (a client asks for the row by naming it in `X-Yogabase-Accept-Capabilities`),
and this client carries the operations as generated code, for example `api.v1.data.byTable('notes').select.postAsync(body)` with the builders
`select`, `insert`, `upsert`, `update` and `delete`. A response is a `DataResult` (`rows`, `affectedRows`, `hasMore`) and a refusal is an RFC 7807 problem whose `code` is one of the refusal codes
the contract lists under `x-yogabase-data-api`, which also states the limits (at most 1000 rows per
select and 500 per write, 1 MiB per value, a five-second statement timeout). Request values are plain JSON.
Nothing in this package has called the Data API against a live installation. A typed data API and
Entity Framework Core inside Lambdas stay server-side: no client SDK carries EF code or a model.

Two other surfaces were added when the Data API was withdrawn:

- **Game servers** — `POST /v1/servers/start`, `GET /v1/servers`, `GET /v1/servers/{serverId}`
  and `POST /v1/servers/{serverId}/stop` on the application gateway, authenticated with the
  signed-in player's access token. They are part of the mirrored application contract, so this
  client carries them as generated code: `api.v1.servers` (`start.postAsync(body)`, `getAsync()`, `byServerId(id).getAsync()`, `byServerId(id).stop.postAsync()`). The list answers a bare array; a `GameServer` is
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
  api.v1.storage.objects,
  gateway.origin,
  'avatars/user-1/photo.png',
);
final bytes = await File('photo.png').readAsBytes();
await item.putAsync(bytes, (request) {
  request.headers.put('X-Yogabase-Content-Sha256', 'sha256:${sha256.convert(bytes)}'); // 64 lowercase hex
  request.headers.put('Idempotency-Key', const Uuid().v4()); // bounded
});
final stored = await item.getAsync(); // the same bytes
```

The adapter is authored Yogabase code, not Kiota output; its standalone check passes (see Local
verification below). A `PUT` needs the contract's two headers, which Kiota does not emit as method
arguments, and its body must be a `Uint8List` (any other list is `Unsupported scalar value type`).

Objects travel as `application/octet-stream`, for which the pinned Kiota Dart runtime registers no
serializer or parser. The first `YogabaseApplicationClient` registers a pair that passes the bytes through
(`lib/src/yogabase_octet_stream.dart`, process-wide like the JSON one). Before that, both calls threw
`Content type application/octet-stream does not have a factory registered`.

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

Its standalone check passes (see Local verification below); no live gateway has served it. In the snippets
here and below, `session`, `application` and `api` are the ones from Getting started.

```dart
final jobs = application.jobs(session.projectId);

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
  caller response headers (only a native response handler does), so this client cannot. The gateway has
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
schedule it reads back. Its standalone check
is `test/yogabase_schedules_source_test.dart`; no live gateway has served it.

```dart
final schedules = application.schedules(session.projectId);

const definition = YogabaseScheduleDefinition(
  scheduleId: 'nightly',
  functionId: 'orders-nightly',             // a deployed Stateless Lambda
  functionVersion: '1',                     // an admitted version
  cron: '0 3 * * *', timeZone: 'Asia/Seoul', // five fields; or set oneShotAtUtc for a one-shot
  misfirePolicy: YogabaseScheduleMisfirePolicy.skipToNextOccurrence, // required: a missing one is a 400 naming the member
  concurrencyPolicy: YogabaseScheduleConcurrencyPolicy.allow,        // required too: allow, forbid or replace (see below)
  maxCatchUp: 1, maxAttempts: 3, deadlineSeconds: 300,
);
var schedule = await schedules.create(definition);                        // If-None-Match: *  (201)
schedule = await schedules.update(definition, schedule.eTag);             // If-Match (200); pass a changed definition
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
final deliveryOrigin = Uri.parse('https://delivery.example.com');
final workloadToken = Platform.environment['YOGABASE_DELIVERY_TOKEN']!; // a workload token, not the signed-in user's
final delivery = YogabaseDeliveryClient(
  DefaultRequestAdapter(
    authProvider: BaseBearerTokenAuthenticationProvider(
      YogabaseDeliveryBearerTokenProvider(
        deliveryOrigin: deliveryOrigin,
        tokenProvider: (uri, context) async => workloadToken,
      ),
    ),
  ),
  deliveryOrigin,
);
final operations = delivery.api.api.v1.delivery.projects
    .byProjectId(UuidValue.fromString('11111111-1111-4111-8111-111111111111')) // the project's id
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
dart run test/yogabase_sign_in_source_test.dart
dart run test/yogabase_storage_source_test.dart
```

The package declares no `dev_dependencies` (`package:test` is not in the lockfile), so `dart test` is not
wired; each check is a standalone program that prints `PASS` or exits non-zero with the failed
expectation. Every request goes to an in-memory `RequestAdapter` or `MockClient`:

- **delivery**: SSE parsing (including the one-space field rule), the cancellation token, the delivery
  bearer origin check, the storage logical-key cases shared with the Python suite, the generated
  game-server list and stop requests, polling to a terminal state, cancellation during a polling delay
  (inside a zone that records every timer, asserting none is left active), an already-cancelled poll
  sending nothing, and the event-stream request's `Last-Event-ID`, `Accept` and `operationId` query.
- **jobs, schedules**: each request the authored client builds against the one the generated builder builds,
  the bounds checked before anything is sent, and the refusals kept with their status and code.
- **sign-in**: against a fake gateway, the S256 challenge, the verifier and password sent only where they
  belong, each named login refusal, a missing cookie, a state mismatch, a missing code, an `error`
  parameter and a foreign redirect (none followed by a token request), MFA by name and with the code, a
  discovery document naming another origin, the token reaching the gateway's origin and no other, and one
  shared refresh that spends the rotated token.
- **storage**: a put and a get of bytes that are not valid UTF-8 come back unchanged, the contract's two
  headers reach the request, and a JSON body still goes through the JSON writer.

Last run on 2026-10-07 with Dart 3.13.5 (stable, macOS arm64, Homebrew `dart-sdk`): `pub get
--enforce-lockfile` left `pubspec.lock` unchanged, analysis found no issues with infos and warnings fatal,
and the five programs passed. The first such run of the source regenerated on 2026-10-03 (the Data API, Jobs
and schedule builders, the authored Jobs and schedules clients) found four unneeded `dart:typed_data`
imports and nothing else. The earlier run, on 2026-09-17 with Dart 3.13.2, predates that source. The
pubspec allows Dart `^3.9.0`; only 3.13.2 and 3.13.5 have run. Not exercised: Flutter, a streaming adapter
and any live gateway (the sign-in, the Storage calls and the smoke program included).
