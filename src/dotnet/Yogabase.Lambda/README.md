# Yogabase.Lambda

The context a Yogabase Lambda runs with: the project database, mediated object storage, the
application-user directory, realtime publication, actors, workflows, reminders, direct Valkey,
authenticated messaging, who called, and which invocation this is.

**This is where a Yogabase application reads and writes data.** The client-facing Data API module is
withdrawn. A client reaches data only through a Lambda, Auth, Storage and Realtime, so the database
handle below is the data path — not a bounded typed query surface layered over one.

The package targets .NET Standard 2.1 and .NET 10. It references
`Microsoft.Extensions.Logging.Abstractions` and `StackExchange.Redis`, which the sandbox host
already carries and unifies at compatible versions; the .NET Standard target also references
`System.Text.Json`. Every dependency a bundle adds must be admissible in the sandbox.
`IHttpClientFactory` is resolved from the service provider by type name rather than referenced, so
`Microsoft.Extensions.Http` never enters a bundle's closure.

## The entry contract

A Lambda's v3 entry point is:

```csharp
public static ValueTask<ReadOnlyMemory<byte>> InvokeAsync(
    IServiceProvider context,
    ReadOnlyMemory<byte> input,
    CancellationToken cancellationToken)
```

`LambdaContext.From(context)` reads that provider. It is a convenience, not a privileged path: it
resolves exactly the BCL-typed services and admissible client the runtime registered and can obtain nothing the
raw `IServiceProvider` could not.

## A whole Lambda

Read a row, write a row, and look up the caller:

```csharp
using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Yogabase.Lambda;

public static class BattlePass
{
    public static async ValueTask<ReadOnlyMemory<byte>> InvokeAsync(
        IServiceProvider services, ReadOnlyMemory<byte> input, CancellationToken cancellationToken)
    {
        var context = LambdaContext.From(services);
        var player = context.Caller.RequireSubjectId();
        var awarded = JsonSerializer.Deserialize<int>(input.Span);

        await using var connection = await context.Database.OpenConnectionAsync(cancellationToken);
        await using var read = connection.CreateCommand();
        read.CommandText = "select xp from bp_progress where player_id = $1";
        read.Parameters.Add(new Npgsql.NpgsqlParameter { Value = player });
        var current = await read.ExecuteScalarAsync(cancellationToken) as int? ?? 0;

        await using var write = connection.CreateCommand();
        write.CommandText =
            "insert into bp_progress (player_id, xp) values ($1, $2) "
            + "on conflict (player_id) do update set xp = excluded.xp";
        write.Parameters.Add(new Npgsql.NpgsqlParameter { Value = player });
        write.Parameters.Add(new Npgsql.NpgsqlParameter { Value = current + awarded });
        await write.ExecuteNonQueryAsync(cancellationToken);

        var user = await context.Users.GetAsync(player.ToString(), cancellationToken);
        context.Logger.LogInformation(
            "Awarded {Xp} to {User} in invocation {Invocation}.",
            awarded, user?.Username, context.Invocation.InvocationId);
        return JsonSerializer.SerializeToUtf8Bytes(new { xp = current + awarded });
    }
}
```

`Npgsql` above is the provider whose types the host already carries; a bundle pinning an
incompatible major of it is refused at admission naming the version. Only `DbDataSource` and
`DbCommand` — both BCL types — are part of the contract.

## What the context exposes

| Member | What it is |
|---|---|
| `Database` | `System.Data.Common.DbDataSource` for the project, under the full-access `yogabase_lambda` identity. Open a connection per unit of work and dispose it; the runtime owns the pool. |
| `Storage` | `PutAsync`, `ReplaceAsync`, `GetAsync`, `TryGetAsync`, `DeleteAsync`, `ListAsync`, `ListAllAsync` over the project's mediated object store. Keys are logical and project-local; provider coordinates and signed URLs never reach customer code. See [Storage semantics](#storage-semantics). |
| `Users` | `GetAsync`, `CreateAsync`, `UpdateAsync`, `BanAsync`, `IssueTokenAsync` over this project's application users. A subject is a UUID (any spelling is normalised to the lower-case `D` form the gateway requires). See [Issuing a user token](#issuing-a-user-token). |
| `Realtime` | `PublishAsync(topic, payload)` and `PublishJsonAsync(topic, json)`. A topic is a Realtime binding id — `[a-z][a-z0-9._-]{0,95}` — and a payload is at most 64 KiB; both are checked here so a publish the gateway would refuse never leaves the Lambda. A Lambda publishes; it does not subscribe or hold a Hub. |
| `Actors` | `InvokeAsync(functionId, instanceKey, functionVersion, payload, idempotencyKey)`, through the gateway's existing actor ingress. |
| `Workflows` | `StartAsync(workflowId, definitionVersion, input, idempotencyKey)`, `GetAsync(instanceId)` and `CancelAsync(instanceId, idempotencyKey)`. |
| `Reminders` | `SetAsync(name, functionId, functionVersion, payload, dueAt: …)` or `cron: …`, and `ClearAsync(name)`, through the existing durable scheduler. |
| `Valkey` / `Cache` | The runtime's real `IConnectionMultiplexer` and its `IDatabase`. Key operations and pub/sub go directly to the bound Valkey service over TLS as this project's ACL identity. |
| `Messaging` | `PublishAsync(topic, payload, qos, protocol)` through the gateway. MQTT supports QoS 0/1 and payloads up to 32 KiB; unqualified Kafka produce is a typed refusal. |
| `Caller` | The `ClaimsPrincipal`, its raw `Subject`, and `TryGetSubjectId` / `RequireSubjectId`. |
| `Logger` | The runtime's `ILogger` for this invocation. |
| `Invocation` | `InvocationId`, `FunctionId`, `Version`, `EnvironmentId`, and the raw `Facts` dictionary. |

Every service except `Logger`, `Caller`, and `Invocation` is resolved on first use, so a Lambda that
only publishes to Realtime never touches the database, and a missing service is reported when it is
actually needed, naming the service.

### Actors, workflows, and reminders

Actor calls require an explicit function version; workflow starts require an explicit definition
version. The gateway checks the same admitted versions as the public routes. There is no implied
latest-version selection. Orleans stays outside the sandbox. The optional idempotency key identifies
one logical operation within this Lambda invocation; without it, identical requests to the same
target share a key. Supply distinct keys for repeated operations with the same payload.

Reminder names belong to the calling Lambda within its project. Choose a future `dueAt` for a
one-shot reminder or a supported cron expression and optional time zone for recurring work. For
example, `cron: "*/5 * * * *"` schedules every five minutes in UTC by default. Arbitrary interval
values are not supported. The wrapper reads the current ETag before changing or clearing a
reminder, then uses the existing scheduler's create/update/delete preconditions. A concurrent change
returns `LambdaApiException` with status 412 and code `reminder_changed`; it never silently overwrites
the new state. Clearing a missing reminder is already complete.

### Direct Valkey access and pub/sub

Bind a proven Valkey substrate to the application's runtime before resolving `Valkey` or `Cache`.
Every key and channel must start with `p:<project-id>:` using the project's canonical lowercase
UUID. Valkey enforces both key and channel ACL patterns; the wrapper does not prepend or rewrite
names. Pub/sub uses the multiplexer directly, with no gateway route:

```csharp
var prefix = "p:" + projectId.ToString("D") + ":";
await context.Cache.StringSetAsync(prefix + "counter", 42);
await context.Valkey.GetSubscriber().PublishAsync(
    StackExchange.Redis.RedisChannel.Literal(prefix + "events"), "counter-updated");
```

The runtime owns the multiplexer. Do not dispose it; remove subscriptions you register before the
invocation ends. An unbound Valkey service is refused when accessed with a reason naming
`valkey-lambda-access`.

### Messaging and consumer triggers

MQTT publishing uses the gateway's authenticated broker connection. Topics are logical,
project-local paths; the gateway adds the project's broker namespace. No MQTT/Kafka client or
broker credential is handed to customer code. Kafka produce remains refused with
`LambdaApiException.Code == LambdaMessaging.ProtocolUnqualified` while its mutual-TLS fact is
unproven. An unbound or unqualified MQTT substrate similarly returns its exact refusal.

Kafka topics reach a Lambda through a configured consumer trigger. The existing consumer runtime
records the Lambda's durable result before acknowledging the broker receipt. Delivery is
**at least once**: handlers must make external side effects idempotent because a retry can deliver
the same record again. MQTT QoS 1 can also deliver duplicates. The SDK exposes no Kafka consumer
inside the sandbox.

### Storage semantics

The mediated store is content-addressed and **create-if-absent**; there is no overwrite.

- `PutAsync(key, bytes, contentType)` creates the key (201). A retry of this same invocation's
  write of the same bytes is acknowledged as the existing object (200). Any other write to an
  existing key — different bytes, or the same bytes from a different invocation — is refused with
  a `LambdaApiException` of status 409 and code `object_conflict`.
- `ReplaceAsync(key, bytes, contentType)` is `DeleteAsync` then `PutAsync`: two operations, not
  one. Another writer creating the key between them is reported by the write as `object_conflict`.
- `DeleteAsync(key)` is idempotent: the gateway answers 204 whether or not the key existed, so a
  retried invocation's delete succeeds exactly as its first attempt did.
- `ListAsync(prefix, cursor, limit)` returns one page — `Keys` in ordinal order plus `NextCursor`,
  `null` on the last page — of up to 500 keys (the gateway's default page is 100). Pass
  `NextCursor` back unchanged. `ListAllAsync(prefix)` walks every page for you; for a large
  namespace, page with `ListAsync` instead so the whole listing is never held at once.
- A key is one byte to 32 MiB under a media type without parameters, and follows the gateway's
  own rules, checked here before transport: not empty, absolute, or longer than 1024 characters;
  no backslash, percent sign, NUL, empty segment, or `.`/`..` segment; not a physical `projects/`
  path or a URL. Segments are percent-encoded individually on the wire (a key's `/` travels as a
  `/`), so keys may contain spaces, `?`, `#`, and non-ASCII characters.
- `environments/…` is reserved for the product's own artifact layout. A key under it, or a listing
  prefix that would enumerate it (`environments`, `environments/`, or a separator-less prefix such
  as `environment` that string-matches into it), is refused before transport with the same
  `LambdaApiException` the gateway answers: status 403, code `key_reserved_prefix`. A directory
  that merely starts the same way — `environment/settings.json` — is an ordinary key, and
  `environment/` an ordinary prefix.

### Issuing a user token

`Users.IssueTokenAsync(subject, scopes)` returns a real project-user access token minted by the
project realm on the Lambda's authority. Every client-facing route validates it exactly as a token
from `/connect/token`; it differs in carrying `yogabase_issued_by = lambda:<functionId>` and in
never coming with a refresh or identity token. It lives for the realm's configured access-token
lifetime (`ExpiresAt`). It speaks for that one user, never for the project.

The scope set is one to sixteen distinct scopes from the realm's admitted set; duplicates are
collapsed. `openid` and `offline_access` are never issuable and are refused here, before transport,
with the same `LambdaApiException` the gateway answers: status 400, code `scope_not_allowed`. The
gateway answers that code for a scope outside the admitted set, `user_not_found` (404) for a subject
that is not in this project, `user_banned` and `user_email_unconfirmed` (403) for one that could not
sign in, and `user_token_issuance_failed` / `user_token_issuance_unavailable` (503) when the realm
could not issue or the host has no realm issuer.

### Refusals

Every failure the gateway reports is a `LambdaApiException` carrying its HTTP `StatusCode` and the
gateway's stable `Code`; `LambdaRefusalCodes` names the shared refusal codes. Two refusals the wrapper can decide locally — a reserved storage key and a never-issuable
scope — throw that same exception before any request is sent, with exactly the status and code the
gateway would have answered, so one catch path covers both. Argument-shape problems (an empty key,
an oversized payload, a subject that is not a UUID) are `ArgumentException`s.

### Scheduled, actor, and workflow invocations have no player

The runtime sets `sub` to `lambda:<functionId>` when Yogabase itself started the invocation.
`Caller.IsSystem` is then true, `SystemFunctionId` names the function, and `TryGetSubjectId` answers
`false` — so a Lambda never silently attributes scheduled work to a player. `RequireSubjectId()`
refuses with that reason rather than returning `Guid.Empty`.

## Trust boundary

A Lambda runs as a full-access database identity for its own project. What separates one project's
code from the installation is the sandbox's process, filesystem and network isolation, the admission
scan, and the per-invocation token — not a narrow API. Egress opens only to the project's own
database, gateway, and explicitly bound Valkey service. No cloud, Kubernetes, fleet, or Control Plane credential ever reaches customer
code, and this package exposes no way to ask for one.

Treat a password passed to `Users.CreateAsync` and a token returned by `Users.IssueTokenAsync` as
secrets: this package never logs them, and neither should the Lambda.

## Known limits of this preview

- `Database` exists only on the `net10.0` build. `System.Data.Common.DbDataSource` was introduced in
  .NET 7 and has no .NET Standard 2.1 form. A Lambda bundle targets the sandbox's own .NET runtime,
  so the `netstandard2.1` build is for sharing non-database code, not for running a Lambda.
- The internal Lambda runtime API is not a published contract. This package speaks the route table
  and shapes the gateway's `LambdaRuntimeApiEndpoints` serves on 2026-09-09 (the product
  repository's `docs/SIGNALR_LAMBDA_ORLEANS_BOUNDARIES.md` §5 records them): an object listing is
  read strictly as `{ "objects": [{ "key" }], "nextCursor" }` and any other shape is refused rather
  than read as empty, while unmodelled user fields are kept rather than dropped.
- No live installation has exercised this package. Its coverage is fake-provider and fake-transport
  unit tests only.

Third-party notice: `THIRD-PARTY-NOTICES.md` and `licenses/Apache-2.0-Nakama.txt`.
