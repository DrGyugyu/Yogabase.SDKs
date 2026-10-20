# Yogabase Java client

`io.yogabase:yogabase-client` is the Java client for the Yogabase application gateway and the
workload Delivery API. Its request builders and models are generated with Microsoft Kiota 1.34.1
from the Yogabase v1 OpenAPI contracts under `contracts/` (package `io.yogabase.client`, and
`io.yogabase.client.delivery` for the Delivery contract); the sign-in, storage-key, Jobs, Schedules
and delivery facades described below are authored. It targets Java 17 on the Kiota Java runtime and
is also the Kotlin/JVM interoperability path; no native Kotlin or Android qualification is claimed.
Sign a user in with `YogabaseSignIn` and build the application client with
`YogabaseApplicationClient.forSession(session)`, or provide a Kiota request adapter bound to the exact
Yogabase REST gateway and a bearer-token authentication provider of your own.

The generated REST surface includes bootstrap, signup and interactive auth endpoints, Storage,
stateless Function invocation, Actor Lambda invocation, Workflow start/cancel, Jobs, Schedules, the
Data API and game servers. The contract returns a separate realtime origin, but this package does
not include a JVM SignalR wrapper.

The serialization modules are direct compile dependencies because the generated client registers
their factories; the Kiota bundle marks those transitive modules as runtime scope.

The package is not yet published to Maven Central. Build it from source as described next.

## Building and running the checks

Build the jar with Maven on JDK 17 or newer: `mvn -q -DskipTests package` in this directory. The
checks under `src/test/java` are plain programs with a `public static void main` and no
test-framework dependency, so `mvn test` runs none of them. `mvn test-compile` compiles them into
`target/test-classes`, and the dependency plugin writes the classpath; then run each class from this
directory, which `YogabaseSchedulesClientTest` needs in order to find the contract under
`../../contracts`:

```sh
mvn -B -q test-compile
mvn -B -q org.apache.maven.plugins:maven-dependency-plugin:3.8.1:build-classpath -Dmdep.outputFile=target/cp.txt
for c in YogabaseStorageLogicalKeyTest YogabaseGeneratedSurfaceTest YogabaseDeliveryClientTest \
         YogabaseJobsClientTest YogabaseSchedulesClientTest YogabaseSignInTest; do
  java -cp "target/classes:target/test-classes:$(cat target/cp.txt)" io.yogabase.client.$c || break
done
```

Each prints a `PASS` line (with its check count where it counts) or exits with an `AssertionError`
naming the failed check. On 2026-10-20, on Linux with JDK 21.0.12.1 and Maven 3.9.11 (see
[`docs/CLOUD_ENVIRONMENT.md`](../../docs/CLOUD_ENVIRONMENT.md)), `mvn test-compile` and `mvn package`
were clean and all six passed: storage key, generated surface (26 checks), Delivery, Jobs (336),
Schedules (1988) and sign-in (120).

## Signing in

`YogabaseSignIn` signs a project user in with no browser, as the .NET, TypeScript, Python and Dart
clients do. `signInWithPassword` posts the e-mail and password to `/auth/login` for the identity
cookie (and the authenticator code to `/auth/mfa/verify` for an account with MFA), sends that cookie
with an authorization-code PKCE request to the authorization endpoint the gateway's own discovery
document names, reads the redirect without following it, holds it to the registered redirect URI
and to the state it sent, and exchanges the code for tokens. The result is a `YogabaseSession`, the
Kiota `AccessTokenProvider` of the application client:

```java
YogabaseSession session = YogabaseSignIn.signInWithPassword(
    URI.create("https://<installation-host>"), "<publishable-key>", "<user-email>", "<user-password>");
YogabaseApplicationClient application = YogabaseApplicationClient.forSession(session);
YogabaseJobsClient jobs = application.jobs(session.projectId());
```

The user must already exist and, unless the installation turned confirmation off, have confirmed
the address. A refusal is a `YogabaseAuthException` (a `YogabaseGatewayException`) with the
gateway's `status()` and `code()`: `invalid_credentials` (401: a wrong password and an unknown,
unconfirmed or locked account alike), `mfa_enrollment_required` (403), `authentication_rate_limited`
(429), and `mfa_required` (202) for an account with MFA: call again with
`new YogabaseSignIn.Options().mfaCode("123456")`. The client names a few itself for an answer that is
not this call's: `oidc_discovery_invalid` (a discovery document whose endpoints are not on the
gateway's origin, which ends the call before the password is sent), `authorization_response_invalid`
and `authorization_redirect_mismatch` (a redirect with another state or address), and
`token_response_invalid`.

The session sends its bearer to the gateway origin the user signed in at and to no other, and
refreshes the access token with the rotating refresh token within a minute of its expiry, once for
concurrent callers. It keeps the tokens in memory only.

### Signing in with a provider

Signing in with Google, Microsoft or Apple is two calls around the application's own browser step.
`startExternalSignIn(gateway, key, provider, redirectUri, options)` sends only the S256 challenge of a
random verifier with a redirect URI the project registered, and answers a `YogabaseExternalSignIn`
whose `authorizationUrl()` the application opens in the system browser. When the browser comes back
to the redirect URI, `completeExternalSignIn(pending, redirect, mfaCode)` holds the address to the
registered one and to the state, sends the code with the verifier to the provider's callback, and
then signs in as above. An account with MFA refuses with `mfa_required`; call again with the same
`pending` and the code, and the provider's code, already spent, is not sent again. The address to
register with each provider is `https://<gateway>/auth/external/<provider id>/return`. Refusals add
the gateway's `external_*` codes and `external_start_invalid` for a start that is not an https
address and a state.

Both flows use the JDK's own HTTP client, which keeps cookies and never follows a redirect; an
`HttpClient` passed in `Options.httpClient` must do both. Plain HTTP is accepted only for a loopback
gateway with `Options.allowInsecureLoopback(true)`. `YogabaseSignInTest` runs both flows against a
fake gateway on a loopback port; neither has run against a real provider or a live installation.

## The Data API and game servers are generated; builds are not

The mirrored contract declares the restored client Data API: `POST /v1/data/{table}/select|insert|upsert|update|delete`
on the application gateway, authenticated with the signed-in user's access token and run under
row-level security as that user. It is served only where the bootstrap document's requested-only
`data-rest` row says so (a client asks for the row by naming it in `X-Yogabase-Accept-Capabilities`),
and this client carries the operations as generated code, for example `client.v1().data().byTable("notes").select().post(body)` with the builders
`select()`, `insert()`, `upsert()`, `update()` and `delete()`. A response is a `DataResult` (`rows`, `affectedRows`, `hasMore`) and a refusal is an RFC 7807 problem whose `code` is one of the refusal codes
the contract lists under `x-yogabase-data-api`, which also states the limits (at most 1000 rows per
select and 500 per write, 1 MiB per value, a five-second statement timeout). Request values are plain JSON.
Nothing in this package has called the Data API against a live installation. A Lambda's typed data API (which `Yogabase.Lambda` wraps for a .NET Lambda) and Entity Framework Core stay server-side: no client SDK carries EF code or a model.

Two further surfaces are part of the contracts:

- **Game servers** — `POST /v1/servers/start`, `GET /v1/servers`, `GET /v1/servers/{serverId}`
  and `POST /v1/servers/{serverId}/stop` on the application gateway, authenticated with the
  signed-in player's access token. They are part of the mirrored application contract, so this
  client carries them as generated code: `client.v1().servers()` (`start().post(body)`, `get()`, `byServerId(id).get()`, `byServerId(id).stop().post()`). The list answers a bare array; a `GameServer` is
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

Generated `ObjectsRequestBuilder.byKey(...)` is single-segment-only. For logical keys containing
`/`, call the authored `YogabaseStorageLogicalKey.requestBuilder(...)` adapter with the generated
objects builder, validated gateway origin, and logical key. It applies the C# key grammar, encodes
each segment separately, and returns the generated item builder through `withUrl(...)`.

```java
var item = YogabaseStorageLogicalKey.requestBuilder(
    client.v1().storage().objectsEscaped(),
    requestAdapter.getBaseUrl(),
    "avatars/user-1/photo.png");
```

`YogabaseStorageLogicalKey` is authored Yogabase code, not Kiota output.

For `PUT`, add both required contract headers in the generated `PutRequestConfiguration` consumer:
`X-Yogabase-Content-Sha256: sha256:<64 lowercase hex>` and a bounded `Idempotency-Key`. Kiota does
not emit dedicated Java method arguments for these headers.

## Durable Jobs

`YogabaseJobsClient`, authored and reached from `YogabaseApplicationClient.jobs(projectId)`, starts,
reads, cancels and collects the result of a durable background job as the signed-in user, against
`/v1/projects/{projectId}/jobs` on the application gateway and behind the same project token (the
`lambda.invoke` scope) as invoking a function. A **job type is the id of a deployed stateless function**;
its highest released version runs the job. A job belongs to the user who submitted it, and another user's
job is answered exactly as one that does not exist.

The product's client OpenAPI document describes the Jobs routes, so Kiota generates their request builders
(`api().v1().projects().byProjectId(projectId).jobs()`, with `Job`, `JobStatus` and `JobSubmitRequest` among the
models). This client **wraps those builders**: each request is the one the generated builder builds (its
route, its media types and its serialized body), sent through the Kiota request adapter of the generated
application client, and so with its authentication and origin. What it adds is what a generated builder
cannot give: it reads the response through the adapter's native response, because the result route's digest
is a response header (it needs the `OkHttpRequestAdapter` of the Kiota bundle, and names the adapter if it is
given another), checks each call before it is sent, refuses an answer that is not what the route declares,
and names every refusal by its code. It reads JSON with Gson, which arrives with Kiota's JSON serialization
module. `YogabaseJobsClientTest` pins every shape and holds each request to the generated builder's.

```java
final var application = new YogabaseApplicationClient(requestAdapter, URI.create("https://apps.example.test"));
final var jobs = application.jobs(projectId);   // projectId is the bootstrap document's projectId

YogabaseJob job = jobs.submit(new YogabaseJobSubmitRequest(
    "orders-create",
    "order-42",                                  // keep it across retries of this submit
    "hello".getBytes(StandardCharsets.UTF_8),    // optional, at most 1 MiB before base64
    3));                                         // optional, 1 to 10; the gateway's default is 5
while (!job.isTerminal()) {                      // succeeded, cancelled or dead-lettered
    Thread.sleep(1000);
    job = jobs.get(job.jobId());
}
if (job.status() == YogabaseJobStatus.SUCCEEDED && job.resultReference() != null) {
    final YogabaseJobResult result = jobs.result(job.jobId());   // result.content(), result.digest()
}
jobs.cancel(job.jobId());                        // idempotent; a running job is flagged and stopped by its worker
```

- **Retrying a submit is safe.** The idempotency key is a member of the request body, not a header (the
  gateway reads no `Idempotency-Key` header on this route). The same request under the same key answers
  the job it already made (202 again, whatever the job has become); a different request under that key is
  refused with `job_idempotency_conflict` (409). The key belongs to the signed-in user.
- **Checked before anything is sent**, with the gateway's own bounds: a job type of 1 to 128 ASCII letters,
  digits, `.`, `-`, `_` or `:`; a trimmed idempotency key of 1 to 256 characters with no control character;
  a payload of at most 1,048,576 bytes (exactly that is a job); and `maxAttempts` from 1 to 10.
- **Refusals** are `YogabaseJobsException`s (unchecked) carrying the gateway's `status()` and `code()`
  (`YogabaseJobErrorCodes` names them): `job_type_not_registered` (422), `job_idempotency_conflict` (409),
  `job_payload_too_large` (413, with `limitBytes()` and `maxBodyBytes()` naming the payload and body limits),
  `job_request_invalid` (400), `job_access_denied` (403), `job_unavailable` (503) and `job_not_found` (404, for
  no such job and for another user's, never told apart). A plain `not_found` is an installation that does
  not serve Jobs at all; no capability row says whether one does. A status the gateway names no code for
  reads as `YogabaseStatusErrorCodes` names it (`unsupported_media_type` for a submit that is not JSON,
  `payload_too_large` for a bare 413 from a gateway that predates the named one). `YogabaseJobsException`
  and `YogabaseSchedulesException` share the base `YogabaseGatewayException`.
- **A result is read separately.** `resultReference()` is a name, not a capability; `result()` returns
  `YogabaseJobResult(content, digest)` after hashing the bytes and comparing them with the
  `X-Yogabase-Content-Sha256` header. A result that does not match (`job_result_digest_mismatch`), arrives
  without exactly one well-formed `sha256:` digest, is larger than 16 MiB or is not a 200 is refused
  (`job_response_invalid`) and never returned. `job_result_not_ready` (409) is your own job that has not
  settled; `job_result_not_found` (404) is another user's job, an unknown one and a job that finished
  without a result, never told apart.
- A response member this client does not know is ignored, as the generated clients keep unknown members
  aside; a job that breaks a bound it does know (an unknown status, `maxAttempts` outside 1 to 10) is refused.
  Calls block: run them off the UI thread.

The Jobs client was also run end to end on 2026-10-03, over the default Kiota OkHttp pipeline, against the
product's real Jobs routes, built from the product's route code as of that day (24 checks).

## Schedules

`YogabaseSchedulesClient`, authored and reached from `YogabaseApplicationClient.schedules(projectId)`, creates,
changes, reads, deletes and runs once the signed-in user's own **recurring or one-shot schedules** of a deployed
stateless function, against `/v1/projects/{projectId}/schedules/{scheduleId}` on the application gateway and
behind the same project token (the `lambda.invoke` scope) as invoking a function. A schedule belongs to the user
who created it, and only that user reads, changes, deletes or runs it: another user's schedule is a bare 404,
updating it is a 403 and creating a schedule under its id is a 412. Like the Jobs client it wraps the generated
builders (`api().v1().projects().byProjectId(projectId).schedules().byScheduleId(id)`), sends each request as the
generated builder builds it, and adds what a builder does not give: every write is conditional on the
schedule's **ETag**, which it sends as the header the route wants and builds from the schedule it reads back.
`YogabaseSchedulesClientTest` pins every shape and holds each request to the generated builder's.

```java
final var schedules = application.schedules(projectId);

YogabaseSchedule schedule = schedules.create(new YogabaseScheduleDefinition(   // If-None-Match: *  (201)
    "nightly",
    "orders-nightly", "1",                                // a deployed Stateless Lambda, an admitted version
    "0 3 * * *", "Asia/Seoul",                            // five fields; or set oneShotAtUtc for a one-shot
    YogabaseScheduleMisfirePolicy.SKIP_TO_NEXT_OCCURRENCE, // required: a missing one is a 400 naming the member
    YogabaseScheduleConcurrencyPolicy.ALLOW,              // required too: ALLOW, FORBID or REPLACE (see below)
    1, 3, 300,                                            // maxCatchUp, maxAttempts, deadlineSeconds
    "{}".getBytes(StandardCharsets.UTF_8),                // optional payload, at most 1 MiB before base64
    null));
schedule = schedules.update(definition, schedule.eTag());                      // If-Match (200)
final var run = schedules.trigger("nightly", schedule.eTag(), new YogabaseScheduleTriggerRequest(
    "run-1",                                              // the same key against the same version is the same run
    OffsetDateTime.now().plusMinutes(10)));               // after now, at most an hour from now
// run.outcome() is ENQUEUED, or REPLAYED for a repeat
schedules.delete("nightly", schedule.eTag());                                  // If-Match (204)
```

- **Every write names the ETag it read.** `eTag()` is built from the schedule's `version` (decimal text, a
  64-bit number a JSON number cannot carry) and `instanceId`: `"schedule-v<version>-<instance>"`. A stale ETag,
  creating an id that is already held and updating one that does not exist are each a 412
  (`precondition_failed`) that changes nothing; the scheduler advances a recurring schedule's version as it
  runs, so an ETag held for long is stale: `get` it again. A strong `ETag` header that disagrees with the body
  is refused; a weak or repeated one (a proxy that compresses a response may weaken it) is ignored.
- **Checked before anything is sent**, with the gateway's bounds (the `MAXIMUM_*` constants): for a write, a
  schedule id of 1 to 128 characters, a letter or a digit first and then letters, digits and `.` `_` `~` `:`
  `-` only (the contract's pattern; see the next note), a function id of at most 256 and a version of at most
  64 characters, a cron expression and a time zone of at most 128, both policies, `maxCatchUp` 1 to 128,
  `maxAttempts` 1 to 16, a deadline of 1 to 86,400 seconds, a payload of at most 1,048,576 bytes, and an ETag
  in the gateway's form; for a read, a run and a delete, a schedule id of 1 to 128 trimmed characters with no
  control character (and not `.` or `..`, which a URL parser would collapse). A one-shot instant and a run's
  deadline are sent as UTC instants.
- **Refusals** are `YogabaseSchedulesException`s (unchecked) with the gateway's `status()` and `code()`. Only
  the 413 names a code, `schedule_payload_too_large` (`YogabaseScheduleErrorCodes`), with `limitBytes()` and
  `maxBodyBytes()`; every other refusal is a status with a fixed title or no body, and reads as
  `YogabaseStatusErrorCodes` names it: `precondition_failed` (412), `precondition_required` (428), `not_found`
  (404; also an installation without the Scheduler), `conflict` (409: a function version that is not an
  admitted Stateless one of the project, on a write or on a run, a write that would turn a recurring schedule
  into a one-shot or the reverse, or a run key the queue holds for another request; a run of a withdrawn
  version answered 503 before the contract was tightened later on 2026-10-03), `forbidden` (403), `unsupported_media_type` (415)
  and `request_failed` (a 400 for a member the gateway refuses, such as a cron expression that does not
  parse, a policy left out or an id that breaks the rule, the last two naming the member in the problem's
  `detail`; a 503). Calls block: run them off the UI thread.
- **A write holds the id to one rule; an older id is still addressed.** The id is one segment of the URL path,
  so a create and an update are held to the contract's pattern, `^[A-Za-z0-9][A-Za-z0-9._~:-]{0,127}$`, before
  anything is sent, and the gateway refuses an id that breaks it with a 400 that names the member and the rule.
  A schedule written before the rule under another id is still found by that id (percent-encoded, as any path
  segment is) by `get`, `trigger` and `delete`, which therefore check only the older bounds; it
  cannot be changed, so delete it and create it again under an id that follows the rule.
- **The concurrency policy says what the scheduler does about an earlier occurrence that has not finished**
  (its invocation is queued, waiting to start or to be retried, or running, and its deadline has not passed):
  `ALLOW` starts the new occurrence alongside, `FORBID` skips it, and `REPLACE` ends an earlier occurrence that
  is still queued (failure code `schedule_occurrence_replaced`) and starts the new one, but skips it while one
  is running, because nothing can interrupt a Lambda that is executing. A skipped occurrence is consumed, not
  deferred. A manual run is not an occurrence, so the policy neither holds it back nor is held back by it, and
  a one-shot has no earlier occurrence. The mirrored client contract (SHA-256 `5fcbed40…`) describes this; a build of
  the product from before it starts every occurrence whichever policy is chosen and accepts any id but one with
  a `/` (a 400).

Run end to end on 2026-10-03, over the default Kiota OkHttp pipeline, against the product's real schedule
routes, built from the product's route code as of that day (36 checks). That run predates the id rule, the concurrency
enforcement and the 409 of a manual run, and was not repeated against the contract as tightened that day; its harness referenced the product's code and is in neither repository.

## Workload delivery

The package also contains a delivery client generated from the separate workload Delivery v1
contract under `io.yogabase.client.delivery`. Wrap a delivery-only `RequestAdapter` with
`YogabaseDeliveryClient`; do not reuse the application adapter or its credentials. The authored
facade validates the delivery origin, provides a rotating bearer-token provider and header helpers,
recognizes the exact terminal operation states, supports cancellable bounded polling, and parses
SSE while preserving opaque resume IDs.

```java
try (var delivery = new YogabaseDeliveryClient(
        deliveryRequestAdapter,
        URI.create("https://delivery.example.com"))) {
    var operations = delivery.api().api().v1().delivery().projects()
        .byProjectId(projectId).operations();
}
```

Artifact admission carries credential-free source and evidence metadata. No Kubernetes credential,
Agones/PlayFlow compatibility call, or unimplemented runtime health/allocation API is added.

## Licence

Apache-2.0; see [`LICENSE.md`](LICENSE.md). Copyright Yogabase.
