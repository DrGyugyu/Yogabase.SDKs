# Yogabase.Yogabase.Client

Preview .NET clients for the versioned Yogabase application-gateway, game-server, build, and
workload Delivery wire contracts. The package
targets .NET Standard 2.1 and .NET 10 on the 10.0 package line. Unity takes a separate build of the
same sources, `src/unity/Yogabase.Yogabase.Client.Unity` (netstandard2.1, the SignalR 8.0.15 and
System.Text.Json 8.0.5 line), because Unity 6000.5 binds its own 8.0 BCL extensions and a plugin
built against 10.x is CS1705 there; `UnityBuildTests` pins that the Unity build references nothing
at 9.x or above. The Unity build is unqualified until the demo's IL2CPP player has run it. It supports non-secret publishable-key bootstrap,
headless password sign-in, OIDC authorization-code/PKCE, mediated object
storage, stateless Function invocation, Actor and Workflow Lambdas, durable background jobs, SignalR
realtime, and game-server allocation.

Every route this package calls lives in one place, `YogabaseRoutes`, grouped by the host and
credential it belongs to. Nothing else in the package builds a path from string literals.

The publishable key identifies a client configuration only. It is never sent as application
authorization; authenticated calls use an OIDC access token. The default session store is memory
only. Supply an `IYogabaseSessionStore` backed by the host platform's secure credential facility if
sessions must survive process restarts.

## Getting started

Nothing is published, so reference the project from a checkout of this repository (it needs the .NET 10 SDK):

```text
dotnet add reference path/to/Yogabase.SDKs/src/dotnet/Yogabase.Yogabase.Client/Yogabase.Yogabase.Client.csproj
```

Apart from the bootstrap and the `Auth` calls, every call needs a signed-in user, so sign in before the first one:

```csharp
using System.Text;
using Yogabase.Yogabase;

await using var yogabase = await YogabaseClient.CreateAsync(
    new Uri("https://api.example.com"),       // the installation's gateway origin
    "sy_pub_example_project");                // the project's publishable key

await yogabase.Auth.SignInWithPasswordAsync("user@example.com", password);

var result = await yogabase.Functions.InvokeAsync("hello-world", "1", new YogabaseInvocationRequest
{
    IdempotencyKey = Guid.NewGuid().ToString("N"),
    CorrelationId = "first-call",
    Deadline = DateTimeOffset.UtcNow.AddSeconds(20),
    Mode = YogabaseInvocationMode.Synchronous,
    InlinePayload = Encoding.UTF8.GetBytes("{}"),
});
Console.WriteLine($"{result.Outcome}: {Encoding.UTF8.GetString(result.InlineResult ?? [])}");
```

`SignInWithPasswordAsync` keeps the tokens in the session store, so Functions, Storage, Realtime, Jobs and
Servers use them from then on and refresh them. It needs no browser: it logs in for the identity cookie, then
runs the PKCE code flow with that cookie, using the first redirect URI the bootstrap registers and never
following it. A custom `TransportHandler` must keep cookies (`UseCookies = true`) and not follow redirects, or
the authorize step answers `interactive_login_required`.

- **The user must already exist.** Sign-up is invite-only and needs a confirmed e-mail unless the installation
  turned either off: `Auth.SignUpAsync` without an invitation token is a `403 invitation_required`, and an
  account whose confirmation link (`Auth.ConfirmEmailAsync`) was not followed cannot sign in. Where confirmation
  is required but the installation has no e-mail transport, sign-up is a `503 authentication_email_unavailable`.
- **A refusal is a `YogabaseApiException` with the gateway's code.** `401 invalid_credentials` is a wrong
  password, an unknown account, an unconfirmed e-mail and a locked account alike, never told apart;
  `403 mfa_enrollment_required` (the project requires MFA and the account has none) and
  `429 authentication_rate_limited`. An account with MFA throws `202 mfa_required`; call again with the
  authenticator code as `mfaCode`.
- **A live check.** `LiveSmokeTests` signs in, invokes a function, writes and reads an object and, given a topic,
  receives a realtime message. It is skipped unless `YOGABASE_SMOKE_GATEWAY`, `_PUBLISHABLE_KEY`, `_EMAIL`,
  `_PASSWORD` (an existing, confirmed user without MFA), `_FUNCTION_ID` and `_FUNCTION_VERSION` are set;
  `_TOPIC` adds the realtime step. No run against a live installation is recorded here yet.

## Reading and writing data

**There is no Data API client in this package yet.** The client-facing Data API, withdrawn on
2026-09-08, was restored by the owner decision of 2026-09-17 and implemented at the gateway on
2026-09-23: `POST /v1/data/{table}/select|insert|upsert|update|delete`, run under row-level security
as the signed-in user and served only where the bootstrap's requested-only `data-rest` row says so.
The mirrored contract (`contracts/yogabase-client-v1.openapi.json`) declares it and the generated
TypeScript, Python, Java and Dart clients carry it, but this package has no Data client and does not
name `data-rest` in `X-Yogabase-Accept-Capabilities`, so
`Configuration.IsAvailable(YogabaseCapability.WithdrawnDataRest)` answers `false` whatever the
installation publishes. Until it has one, a .NET application reaches its project's data through a
Lambda, which runs server-side with a full-access database context, and through Auth, Storage, and
Realtime. Call a Lambda with `yogabase.Functions`, and write the Lambda itself against the
`Yogabase.Lambda` package in this repository.

## Game servers

```csharp
var server = await yogabase.Servers.StartAsync("battle");   // after signing in, as above
var game = server.FindPort("game");                          // game.Endpoint is "node-ip:hostPort"
```

`yogabase.Servers` allocates hosted-service instances for the signed-in player against
`/v1/servers/*` on the application gateway. `StartAsync` returns a `YogabaseServer` whose
`NetworkPorts` carry the node `Host` and the `ExternalPort` a client dials; an allocation missing a
usable address is refused rather than returned, because a caller that trusts an empty host dials its
own machine. `ListAsync` reads the bare array the contract pins — an instance whose address no
member has observed yet is listed with empty `NetworkPorts`, and must not be dialed — and
`StopAsync` answers the instance's state after the release, never a deletion. The `service` is
the hosted service's canonical id (`[a-z0-9]([-a-z0-9]{0,61}[a-z0-9])?`); `CustomData` is at most
32 entries, 64-byte keys, 1024-byte values, and 4 KiB as canonical JSON, checked here before the
gateway checks it again. A refusal surfaces as `YogabaseApiException` with the gateway's own code:
`server_none_available`, `server_contended`, `server_not_found`, `server_address_not_observed`,
`server_stop_not_available`, `server_request_invalid`, `server_authentication_required` or
`server_unavailable`. The bootstrap document carries no capability identifier for this surface, so
the client does not pretend to know whether an installation enables it — an installation without it
answers the gateway's own refusal.

`YogabaseServer` and each `YogabaseServerPort` also accept the Agones-shaped typed addresses the
product has decided to add: `Address`, the address a client is handed, and `Addresses`, every typed
address observed (`Type` such as `Advertised`, `ExternalIP`, `InternalIP` or `PodIP`, and `Address`).
Today's product sends neither — `Host` already carries the advertised address — and will add them only
once a client that accepts them is the minimum, because this client refuses members it does not know;
both read as `null` until then. When present they are checked: at most 16 addresses, each typed and
non-empty, and a stated `Address` among them, or the answer is refused as `server_response_invalid`.

## Background jobs

`yogabase.Jobs` starts, reads, cancels and collects the result of a durable background job for the
signed-in user, against `/v1/projects/{projectId}/jobs` on the application gateway and behind the same
project token (the `lambda.invoke` scope) as invoking a function. A **job type is the id of a deployed
stateless function**, and its highest released version runs the job; nothing assigns job types, so the
next deploy changes them. A job belongs to the user who submitted it, and another user's job is
answered exactly as one that does not exist.

```csharp
var job = await yogabase.Jobs.SubmitAsync(new YogabaseJobSubmitRequest
{
    JobType = "orders-create",
    IdempotencyKey = "order-42",                      // keep it across retries of this submit
    Payload = Encoding.UTF8.GetBytes("hello"),        // optional, at most 1 MiB before base64
    MaxAttempts = 3,                                  // optional, 1 to 10; the gateway's default is 5
});

while (!job.IsTerminal)                               // Succeeded, Cancelled or DeadLettered
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    job = await yogabase.Jobs.GetAsync(job.JobId);
}

if (job.Status == YogabaseJobStatus.Succeeded && job.ResultReference is not null)
{
    YogabaseJobResult result = await yogabase.Jobs.GetResultAsync(job.JobId);
    // result.Content is exactly what the function returned; result.Digest is its sha256: digest.
}

await yogabase.Jobs.CancelAsync(job.JobId);           // idempotent; a running job is flagged and stopped by its worker
```

- **Retrying a submit is safe.** The idempotency key is a member of the request body, not a header
  (the gateway reads no `Idempotency-Key` header on this route). The same request under the same key
  answers the job it already made (202 again, whatever the job has become); a different request under
  that key is refused with `job_idempotency_conflict` (409). The key belongs to the signed-in user, so
  another user's identical key makes that user's own job. Keep one key across the retries of one submit
  and a new one for a new job.
- **Checked before anything is sent**, with the gateway's own bounds: a job type of 1 to 128 ASCII
  letters, digits, `.`, `-`, `_` or `:`; a trimmed idempotency key of 1 to 256 characters with no
  control character; a payload of at most 1,048,576 bytes (a payload of exactly that is a job); and
  `MaxAttempts` from 1 to 10. A function that fails and asks for a retry is retried with backoff until
  `MaxAttempts`, and the job is then `DeadLettered` with the function's own failure code. A function
  still running at the longest a job may run (30 seconds unless the environment's designer set
  another, up to 15 minutes) fails that attempt as `job_duration_exceeded`.
- **Refusals** surface as `YogabaseApiException` with the gateway's code, which `YogabaseJobErrors`
  names: `job_type_not_registered` (422, no deployed function has that name),
  `job_idempotency_conflict` (409), `job_payload_too_large` (413, with `LimitBytes` and `MaxBodyBytes`
  naming the payload and body limits), `job_request_invalid` (400), `job_access_denied` (403),
  `job_unavailable` (503) and `job_not_found` (404, for no such job and for another user's job, never
  told apart). A plain `not_found` with no body is an installation that does not serve Jobs at all.
- **A result is read separately.** `ResultReference` on the job is a name, not a capability; the bytes
  come from `GetResultAsync`, which hashes them and compares the digest with the
  `X-Yogabase-Content-Sha256` header the gateway sent. A result that does not match
  (`job_result_digest_mismatch`), arrives without exactly one well-formed `sha256:` digest, is larger
  than 16 MiB or is not a 200 is refused (`job_response_invalid`, 502) and never returned.
  `job_result_not_ready` (409) is your own job that has not settled; `job_result_not_found` (404) is
  another user's job, an unknown one and a job that finished without a result, which are never told
  apart; `job_result_integrity_failed` (502) and `job_result_reference_invalid` (500) are the gateway
  refusing a stored result that is not what the job recorded, with no byte of it sent.
- **No capability gates it.** The bootstrap document carries no capability identifier for Jobs, so
  the client does not pretend to know whether an installation serves them and, unlike
  `yogabase.Functions`, does not refuse on `functions-rest`.
- **It is in the contract, and this client is not generated.** The product's client OpenAPI document describes
  the Jobs routes (`x-yogabase-jobs`), and the TypeScript, Python, Java and Dart clients are generated from
  it; this package has no generated code, so the surface is authored here and held to the document by tests:
  `ContractOperationParityTests` sends each of the four operations and compares the first request with the
  contract's verb and path, `ContractLimitsAndCodesTests` compares the limits, the statuses and every refusal
  code with the constants below, and `JobsClientTests` pins every shape. As everywhere in this client, a
  response member it does not know is refused (`response_invalid`) rather than ignored.
- A refusal the gateway names no code for reads as `YogabaseStatusErrors` names it: `unsupported_media_type`
  for a submit that is not JSON (415), `payload_too_large` for a bare 413 from a gateway that predates the
  named one.

It was also run end to end on 2026-10-03 against the product's real Jobs routes, built from product `main`
`92ac1ced` (25 checks; see the root README for what that host was and was not).

A function invoked directly by `yogabase.Functions` that is refused as too large (413
`payload_too_large`) carries the same two limits: the payload limit is 1 MiB before base64, the same
whether the payload is sent inline or as the object an `objectReference` names, and a request at
exactly the limit is accepted.

## Schedules

`yogabase.Schedules` creates, changes, reads, deletes and runs once the signed-in user's own **recurring or
one-shot schedules** of a deployed stateless function, against
`/v1/projects/{projectId}/schedules/{scheduleId}` on the application gateway and behind the same project token
(the `lambda.invoke` scope) as invoking a function. A schedule belongs to the user who created it, and only that
user reads, changes, deletes or runs it: another user's schedule is a bare 404, updating it is a 403 and
creating a schedule under its id is a 412.

```csharp
var definition = new YogabaseScheduleDefinition
{
    ScheduleId = "nightly",
    FunctionId = "orders-nightly",                    // a deployed Stateless Lambda
    FunctionVersion = "1",                            // an admitted version
    Cron = "0 3 * * *",                               // five fields; set OneShotAtUtc for a one-shot instead
    TimeZone = "Asia/Seoul",
    MisfirePolicy = YogabaseScheduleMisfirePolicy.SkipToNextOccurrence,   // required: a missing one is a 400 naming the member
    ConcurrencyPolicy = YogabaseScheduleConcurrencyPolicy.Allow,          // required too: Allow, Forbid or Replace (see below)
    MaxCatchUp = 1,
    MaxAttempts = 3,
    DeadlineSeconds = 300,
    Payload = Encoding.UTF8.GetBytes("{}"),           // optional, at most 1 MiB before base64
};

var schedule = await yogabase.Schedules.CreateAsync(definition);                       // If-None-Match: *  (201)
schedule = await yogabase.Schedules.UpdateAsync(definition, schedule.ETag);            // If-Match (200)
var run = await yogabase.Schedules.TriggerAsync("nightly", schedule.ETag, new YogabaseScheduleTriggerRequest
{
    IdempotencyKey = "run-1",                         // the same key against the same version is the same run
    Deadline = DateTimeOffset.UtcNow.AddMinutes(10),  // after now and at most an hour from now
});                                                   // run.Outcome is Enqueued, or Replayed for a repeat
await yogabase.Schedules.DeleteAsync("nightly", schedule.ETag);                        // If-Match (204)
```

- **Every write names the ETag it read.** `ETag` is built from the schedule's `Version` (decimal text, a
  64-bit number a JSON number cannot carry) and `InstanceId`: `"schedule-v<version>-<instance>"`. A stale ETag,
  creating an id that is already held and updating one that does not exist are each a 412
  (`precondition_failed`) that changes nothing; the scheduler advances a recurring schedule's version as it
  runs, so an ETag held for long is stale: `GetAsync` it again. A strong `ETag` header that disagrees with the
  body is refused; a weak or repeated one (a proxy that compresses a response may weaken it) is ignored.
- **Checked before anything is sent**, with the gateway's bounds (`YogabaseSchedulesLimits`): for a write, a
  schedule id of 1 to 128 characters, a letter or a digit first and then letters, digits and `.` `_` `~` `:`
  `-` only (the contract's pattern; see the next note), a function id of at most 256 and a version of at most
  64 characters, a cron expression and a time zone of at most 128, both policies, `MaxCatchUp` 1 to 128,
  `MaxAttempts` 1 to 16, a deadline of 1 to 86,400 seconds, a payload of at most 1,048,576 bytes, and an ETag
  in the gateway's form; for a read, a run and a delete, a schedule id of 1 to 128 trimmed characters with no
  control character (and not `.` or `..`, which a URL parser would collapse). A one-shot instant and a run's
  deadline are sent as UTC instants.
- **Refusals** surface as `YogabaseApiException`. Only the 413 names a code, `schedule_payload_too_large`
  (`YogabaseScheduleErrors`, with `LimitBytes` and `MaxBodyBytes`); every other refusal is a status with a fixed
  title or no body, and reads as `YogabaseStatusErrors` names it: `precondition_failed` (412),
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
  segment is) by `GetAsync`, `TriggerAsync` and `DeleteAsync`, which therefore check only the older bounds; it
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
- **No capability gates it**, and the contract is held as for Jobs: `ContractOperationParityTests` sends the
  four operations, `ContractLimitsAndCodesTests` compares the limits, policy names, outcome numbers and the
  ETag's pattern with the contract's, and `SchedulesClientTests` pins every shape.

Run end to end on 2026-10-03 against the product's real schedule routes, built from product `main`
`92ac1ced` (36 checks). That run predates the id rule, the concurrency enforcement and the 409 of a manual
run, and was not repeated against product `main` `f9d57e29`; its harness is in the product repository.

## Uploading a build

`YogabaseClient.Builds(...)` returns a `YogabaseBuildsClient` for the **Control Plane** origin, not
the application gateway, authenticated with a **project-scoped developer token** issued from the
console's Settings page — a `ybt_`-prefixed value of at most 128 characters, which the client
checks before sending anything, because the Control Plane answers any other bearer with a bare
`401`. A developer tool calls it; a shipped game client never does. The token is a secret: keep it
in the host's credential store, never in source control and never in a shipped build.

```csharp
using var builds = YogabaseClient.Builds(
    new Uri("https://control-plane.example.com/"),
    developerToken);

var build = await builds.UploadDirectoryAsync(
    projectId,
    environmentId,
    "Builds/LinuxServer",
    new YogabaseBuildUploadRequest
    {
        Executable = "Server.x86_64",
        Name = "battle",
        Version = "1.4.0",
        Ports = new[] { new YogabaseBuildPort { Name = "game", Port = 7777, Protocol = "udp" } },
    });

var checked_ = await builds.WaitForCheckAsync(projectId, environmentId, build.BuildId);
if (!checked_.IsAdmitted)
{
    foreach (var finding in checked_.Check.Findings)
    {
        Console.WriteLine($"{finding.Severity} {finding.Id} {finding.Package} {finding.Version} -> {finding.FixedIn}");
    }
    foreach (var refusal in checked_.Check.Refusals)
    {
        Console.WriteLine($"{refusal.Code} field={refusal.Field} cause={refusal.Cause}");
    }
}

// A build refused by an advisory snapshot that has since moved on can be checked again in place.
var again = await builds.RecheckAsync(projectId, environmentId, build.BuildId);
```

A `YogabaseBuild` is exactly what the Control Plane writes: `BuildId` (a UUID), `Digest`,
`State` (`checking`, `admitted`, `refused`), `Identity` (`name:version`), `Attempt` (a corrected
re-upload takes the next attempt of the same identity), `Kind`, and a `Check` that is always
present — `EvidenceOrigin` (`customer-supplied` or `yogabase-derived`), the inventory, scan and
advisory-database digests, `Passed` (false until the member reports; an absent result is never a
clean one), `ObservedAt`, `Findings` and `Refusals`. A refusal carries a stable `Code`, the
`Field` it is about, and a bounded `Cause` token — never prose. A request the Control Plane refuses
outright surfaces as `YogabaseApiException` with an upper-case code such as
`BUILD_BUNDLE_UNRECOGNIZED`, `BUILD_TOKEN_SCOPE_REQUIRED` or `BUILD_NOT_FOUND` and the
`X-Correlation-ID` the response carried.

`UploadDirectoryAsync` zips the directory in memory with forward-slash entry names and keeps the
Linux player binary and native libraries executable (`0755`), because a Unity dedicated server will
not start if the executable bit is lost in transit. `UploadAsync` takes an already-built zip stream
and never disposes it. The multipart form carries the `bundle` part plus the optional `executable`,
`name`, `version` and repeated `ports` hints; the Control Plane also accepts an optional pre-signed
`manifest` part, which this client never sends because Yogabase derives the manifest, the inventory
and the scan from the bundle itself.

A **Lambda** archive also states how it runs, because Yogabase never works that out from the
bytes: set `Lambda` on the request and the client sends `executionKind` (exactly `Stateless` or
`Stateful`) and the comma-separated `realtimeIngressBindings` and `realtimeEgressBindings` — sent
even when empty, because an empty field declares none while an absent one is refused with
`BUILD_LAMBDA_DECLARATION_REQUIRED`. A Lambda can publish only to the egress bindings it declares.
The binding ids are checked with the admission rules before anything is sent (a lowercase letter,
then lowercase letters, digits, `.`, `_` or `-`, at most 96 characters; at most 16 per direction;
none repeated), and a Lambda request that also names an `Executable` or `Ports` is refused, because
those describe a self-contained service. Leave `Lambda` unset for a server build: the Control Plane
refuses the declaration there (`BUILD_LAMBDA_DECLARATION_UNEXPECTED`).

A bundle whose entry is `RegisterLambdas` registers several Lambda ids, which admission never runs
the code to discover, so it states them: set `RegisteredLambdaIds` and the client sends the
comma-separated `registeredLambdaIds` after the declaration. The client checks the product's rules
first — 1 to 64 ids, each a lowercase letter then lowercase letters, digits, `.`, `_` or `-` up to 128
characters, none repeated, none equal to the build's own `Name`, and every realtime ingress binding one
of them. Leave it empty for a single-function (`InvokeAsync`) bundle: the field is then not sent,
because the Control Plane refuses it there (`BUILD_LAMBDA_REGISTRATION_UNEXPECTED`), as it refuses a
registering bundle without it (`BUILD_LAMBDA_REGISTRATION_REQUIRED`).

```csharp
var lambda = await builds.UploadAsync(projectId, environmentId, File.OpenRead("hello-world.zip"),
    new YogabaseBuildUploadRequest
    {
        Name = "hello-world",   // the function id; omit to use the bundle's own
        Version = "7",
        Lambda = new YogabaseLambdaDeclaration
        {
            ExecutionKind = YogabaseLambdaExecutionKind.Stateless,
            RealtimeEgressBindings = new[] { "match.events" },
        },
    });

// A RegisterLambdas bundle that registers three functions under one build.
var registering = await builds.UploadAsync(projectId, environmentId, File.OpenRead("game.lambdas-3.zip"),
    new YogabaseBuildUploadRequest
    {
        Name = "game.lambdas",
        Version = "3",
        Lambda = new YogabaseLambdaDeclaration
        {
            RealtimeEgressBindings = new[] { "demo.notify" },
            RegisteredLambdaIds = new[] { "claim_reward", "gacha_pull", "season_rollover" },
        },
    });
```

"Build check" is Yogabase's own pass over the bundle: it derives the layout manifest and a CycloneDX
inventory, and matches the catalogued components against a digest-pinned OSV snapshot. A pass means
no Critical/High finding among the catalogued components in that snapshot on its recorded date. It
is never a claim that the build is secure, and anything uncatalogued is reported as uncatalogued.

`YogabaseClientConfiguration` carries separate `GatewayEndpoint` and `RealtimeEndpoint` origins.
The client rejects malformed or insecure non-loopback origins and never derives realtime traffic
from the REST gateway. Storage uploads send `X-Yogabase-Content-Sha256`; Actor and Workflow inline
payloads are digest-checked before transport. Realtime Lambda completions are at-least-once and the
client suppresses duplicate event IDs within a bounded in-memory window.

## Realtime payloads and presence

A `YogabaseRealtimeEvent` carries its payload twice over: `Payload` is the `JsonElement`, and
`PayloadJson` is the same payload as the JSON text it arrived as (or `null` when there was none),
for callers that parse with their own serializer — Unity's `JsonUtility` or Newtonsoft — instead of
walking a `JsonElement`. `PayloadJson` is computed from `Payload` and is never part of the wire shape.

Presence follows the product hub. `CreateAsync` asks for the requested-only `realtime-presence`
bootstrap row by sending `X-Yogabase-Accept-Capabilities: realtime-presence`; an installation whose
gateway predates presence ignores the header, publishes no row, and the presence calls then throw
`NotSupportedException` without connecting, as they do when the row says unavailable.

```csharp
yogabase.Realtime.PresenceChanged += presence =>
{
    Console.WriteLine($"{presence.Subject} {presence.Kind} {presence.Topic}"); // kind: join | leave
    return Task.CompletedTask;
};
await yogabase.Realtime.TrackPresenceAsync("lobby");          // present, and subscribed to the topic
var snapshot = await yogabase.Realtime.GetPresenceAsync("lobby"); // {Topic, Members[{Subject, Connections}], Truncated}
await yogabase.Realtime.UntrackPresenceAsync("lobby");
```

`TrackPresenceAsync` also subscribes the connection to the topic; tracking a tracked topic is a
no-op; a connection tracks at most 16 topics, and the seventeenth is refused here with
`presence_topic_limit` before it reaches the hub. Tracked topics are tracked again whenever the
connection is re-established, because presence belongs to a connection. `UntrackPresenceAsync`
leaves the hub's subscription in place, but that implicit subscription is not restored after a
reconnect — use `SubscribeAsync` to keep receiving the topic. Presence events are delivered at least
once across gateway replicas and carry no id to de-duplicate on; the snapshot is authoritative. It
holds at most 1,000 members and says `Truncated` when more existed; a snapshot outside those bounds,
or for another topic, is refused as `realtime_presence_invalid`. The hub's refusals surface as
`YogabaseApiException`: `presence_unavailable` (503), `presence_topic_invalid` (400),
`presence_topic_limit` (409) and `presence_store_unavailable` (503).

These are pinned by `RealtimePresenceTests` against a real SignalR hub on loopback that speaks the
product hub's presence surface; no live installation has run them.

## Workload delivery

`YogabaseDeliveryClient` is deliberately independent from `YogabaseClient`. Give it the TLS-only
Delivery host and a callback that obtains a short-lived workload `DeliveryBearer` token. It exposes
the implemented template, environment, artifact-admission, release, promotion, rollback,
decommission, durable-operation, abandon, polling, and SSE surfaces while preserving `If-Match`,
`Idempotency-Key`, `Location`, `Retry-After`, and opaque SSE resume IDs.

```csharp
using var delivery = new YogabaseDeliveryClient(
    new Uri("https://delivery.example.com/"),
    cancellationToken => workloadTokens.GetAsync(cancellationToken));

var preview = await delivery.PreviewEnvironmentAsync(
    projectId,
    new YogabaseDeliveryEnvironmentPreviewRequest
    {
        Name = "staging",
        TemplateId = templateId,
        TemplateVersion = 1,
    },
    currentETag);
```

The delivery client submits credential-free artifact locators and immutable evidence; it does not
receive Kubernetes credentials or add Agones/PlayFlow compatibility calls. The current Yogabase
contract does not expose runtime health, ready, allocation, reservation, or shutdown operations.

Interactive login/MFA methods use the protected cookie session maintained by the client's HTTP
handler. PKCE methods manage bearer-token sessions through `IYogabaseSessionStore`. Hosts that
replace the default handler are responsible for equivalent cookie behavior and redirect safety.

Yogabase and this package remain preview capabilities until the repository's release gates pass.
The signed bootstrap capability list is authoritative for each deployment; unknown capability
identifiers are treated as unavailable.
