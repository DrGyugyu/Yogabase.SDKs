# Yogabase.Lambda

The context a Yogabase Lambda runs with: the project database, mediated object storage, the
application-user directory, realtime publication, who called, and which invocation this is.

**This is where a Yogabase application reads and writes data.** The client-facing Data API module is
withdrawn. A client reaches data only through a Lambda, Auth, Storage and Realtime, so the database
handle below is the data path — not a bounded typed query surface layered over one.

The package targets .NET Standard 2.1 and .NET 10 and takes exactly one package dependency,
`Microsoft.Extensions.Logging.Abstractions`, because a bundle shares the sandbox host's assembly
load context and every package it adds is one the host must already carry at a compatible version.
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
resolves exactly the BCL-typed services the runtime already registered and can obtain nothing the
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
| `Storage` | `PutAsync`, `GetAsync`, `TryGetAsync`, `DeleteAsync`, `ListAsync` over the project's mediated object store. Keys are logical; provider coordinates and signed URLs never reach customer code. |
| `Users` | `GetAsync`, `CreateAsync`, `UpdateAsync`, `BanAsync`, `IssueTokenAsync` over this project's application users. An issued token speaks for that one user, never for the project. |
| `Realtime` | `PublishAsync(topic, payload)` and `PublishJsonAsync(topic, json)`. A Lambda publishes; it does not subscribe or hold a Hub. |
| `Caller` | The `ClaimsPrincipal`, its raw `Subject`, and `TryGetSubjectId` / `RequireSubjectId`. |
| `Logger` | The runtime's `ILogger` for this invocation. |
| `Invocation` | `InvocationId`, `FunctionId`, `Version`, `EnvironmentId`, and the raw `Facts` dictionary. |

Every service except `Logger`, `Caller`, and `Invocation` is resolved on first use, so a Lambda that
only publishes to Realtime never touches the database, and a missing service is reported when it is
actually needed, naming the service.

### Scheduled, actor, and workflow invocations have no player

The runtime sets `sub` to `lambda:<functionId>` when Yogabase itself started the invocation.
`Caller.IsSystem` is then true, `SystemFunctionId` names the function, and `TryGetSubjectId` answers
`false` — so a Lambda never silently attributes scheduled work to a player. `RequireSubjectId()`
refuses with that reason rather than returning `Guid.Empty`.

## Trust boundary

A Lambda runs as a full-access database identity for its own project. What separates one project's
code from the installation is the sandbox's process, filesystem and network isolation, the admission
scan, and the per-invocation token — not a narrow API. Egress opens only to the project's own
database and gateway. No cloud, Kubernetes, fleet, or Control Plane credential ever reaches customer
code, and this package exposes no way to ask for one.

Treat a password passed to `Users.CreateAsync` and a token returned by `Users.IssueTokenAsync` as
secrets: this package never logs them, and neither should the Lambda.

## Known limits of this preview

- `Database` exists only on the `net10.0` build. `System.Data.Common.DbDataSource` was introduced in
  .NET 7 and has no .NET Standard 2.1 form. A Lambda bundle targets the sandbox's own .NET runtime,
  so the `netstandard2.1` build is for sharing non-database code, not for running a Lambda.
- The internal Lambda runtime API is not a published contract. Listing shapes are read tolerantly
  (a bare array or a `{ "objects": [...] }` envelope), unmodelled user fields are kept rather than
  dropped, and an unrecognized shape is refused rather than silently read as empty.
- No live installation has exercised this package. Its coverage is fake-provider and fake-transport
  unit tests only.

Third-party notice: `THIRD-PARTY-NOTICES.md` and `licenses/Apache-2.0-Nakama.txt`.
