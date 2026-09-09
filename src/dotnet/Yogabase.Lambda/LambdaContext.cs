using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Yogabase.Lambda;

/// <summary>
/// Everything a Yogabase Lambda is given, read from the runtime's service provider.
/// </summary>
/// <remarks>
/// <para>
/// The v3 Lambda entry contract is
/// <c>public static ValueTask&lt;ReadOnlyMemory&lt;byte&gt;&gt; InvokeAsync(IServiceProvider context,
/// ReadOnlyMemory&lt;byte&gt; input, CancellationToken cancellationToken)</c>. The provider resolves
/// BCL-typed services plus the admissible Valkey client, which keeps the contract free of any Yogabase assembly a bundle would
/// have to link. This class is a convenience over that provider, not a privileged path: it resolves
/// exactly the services the runtime already registered, and it can obtain nothing the raw
/// <see cref="IServiceProvider"/> could not.
/// </para>
/// <para>
/// <b>The Lambda is where a Yogabase application reads and writes data.</b> The client-facing Data
/// API module is withdrawn, so the project database under its full-access
/// <c>yogabase_lambda</c> identity is the data path — not a bounded typed query surface. That is a
/// deliberate trust change: the sandbox's process, filesystem, and network isolation, the admission
/// scan, and the per-invocation token are what separate one project's code from the installation.
/// Egress opens only to the project's own database, gateway and bound Valkey service, and no cloud, Kubernetes, fleet, or
/// Control Plane credential ever reaches customer code.
/// </para>
/// <para>
/// Services are resolved lazily and cached, so a Lambda that only publishes to Realtime never
/// touches the database, and a missing service is reported the first time it is actually needed,
/// naming the service.
/// </para>
/// </remarks>
public sealed partial class LambdaContext
{
    private readonly IServiceProvider services;
    private readonly object gate = new();
    private LambdaStorage? storage;
    private LambdaUsers? users;
    private LambdaRealtime? realtime;
#if NET10_0_OR_GREATER
    private System.Data.Common.DbDataSource? database;
#endif

    private LambdaContext(
        IServiceProvider services,
        ILogger logger,
        LambdaCaller caller,
        LambdaInvocation invocation)
    {
        this.services = services;
        Logger = logger;
        Caller = caller;
        Invocation = invocation;
    }

    /// <summary>
    /// Reads the context out of the provider the runtime handed <c>InvokeAsync</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// The provider is not a Yogabase Lambda runtime provider: it resolves no logger, no caller, or
    /// no per-invocation facts.
    /// </exception>
    public static LambdaContext From(IServiceProvider services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var logger = services.GetService(typeof(ILogger)) as ILogger
            ?? CreateLogger(services)
            ?? throw Missing("Microsoft.Extensions.Logging.ILogger");
        var principal = services.GetService(typeof(ClaimsPrincipal)) as ClaimsPrincipal
            ?? throw Missing("System.Security.Claims.ClaimsPrincipal");
        var facts = services.GetService(typeof(IReadOnlyDictionary<string, string>))
                as IReadOnlyDictionary<string, string>
            ?? throw Missing("IReadOnlyDictionary<string, string> of yogabase.* invocation facts");
        return new LambdaContext(services, logger, new LambdaCaller(principal), new LambdaInvocation(facts));
    }

    /// <summary>The runtime's logger for this invocation.</summary>
    public ILogger Logger { get; }

    /// <summary>Who initiated this invocation.</summary>
    public LambdaCaller Caller { get; }

    /// <summary>Which function, version, environment, and invocation this is.</summary>
    public LambdaInvocation Invocation { get; }

#if NET10_0_OR_GREATER
    /// <summary>
    /// The project's database, under the full-access <c>yogabase_lambda</c> identity. Open a
    /// connection from it per unit of work and dispose it; the runtime owns the pool.
    /// </summary>
    /// <remarks>
    /// This member exists only on the <c>net10.0</c> build. <see cref="System.Data.Common.DbDataSource"/>
    /// was introduced in .NET 7 and has no .NET Standard 2.1 form, so the <c>netstandard2.1</c>
    /// build carries the rest of the context without it. A Lambda bundle targets the sandbox's own
    /// .NET runtime, so this is a compile-time shape difference rather than a missing capability.
    /// </remarks>
    public System.Data.Common.DbDataSource Database
    {
        get
        {
            lock (gate)
            {
                return database ??= services.GetService(typeof(System.Data.Common.DbDataSource))
                        as System.Data.Common.DbDataSource
                    ?? throw Missing("System.Data.Common.DbDataSource");
            }
        }
    }
#endif

    /// <summary>Mediated object storage for this project.</summary>
    public LambdaStorage Storage
    {
        get
        {
            lock (gate)
            {
                return storage ??= new LambdaStorage(Client(LambdaRoutes.StorageClientName));
            }
        }
    }

    /// <summary>This project's application-user directory.</summary>
    public LambdaUsers Users
    {
        get
        {
            lock (gate)
            {
                return users ??= new LambdaUsers(Client(LambdaRoutes.UsersClientName));
            }
        }
    }

    /// <summary>Realtime topic publication for this project.</summary>
    public LambdaRealtime Realtime
    {
        get
        {
            lock (gate)
            {
                return realtime ??= new LambdaRealtime(Client(LambdaRoutes.RealtimeClientName));
            }
        }
    }

    private HttpClient Client(string name) => LambdaHttpClients.Resolve(services, name);

    private static ILogger? CreateLogger(IServiceProvider services) =>
        services.GetService(typeof(ILoggerFactory)) is ILoggerFactory factory
            ? factory.CreateLogger("Yogabase.Lambda")
            : null;

    private static InvalidOperationException Missing(string service) => new(
        $"The Yogabase Lambda runtime resolved no {service}. LambdaContext.From must be given the "
        + "IServiceProvider that the runtime passes to InvokeAsync.");
}

/// <summary>Who initiated an invocation.</summary>
public sealed class LambdaCaller
{
    internal LambdaCaller(ClaimsPrincipal principal)
    {
        Principal = principal;
        Subject = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;
        InvocationKind = principal.FindFirst(LambdaRoutes.InvocationKindClaimType)?.Value;
    }

    /// <summary>The caller as the runtime resolved it.</summary>
    public ClaimsPrincipal Principal { get; }

    /// <summary>
    /// What triggered this invocation, as the runtime's <c>yogabase:invocation-kind</c> claim names
    /// it: <c>Caller</c>, <c>Scheduled</c>, <c>Actor</c>, <c>Workflow</c> or <c>Consumer</c>. Null on a
    /// host older than the claim.
    /// </summary>
    public string? InvocationKind { get; }

    /// <summary>
    /// Whether a managed broker consumer trigger started this invocation. The Lambda's
    /// <c>input</c> is then exactly one Kafka record's value bytes, delivered at least once: the
    /// same record can arrive again after a retry, so external side effects must be idempotent.
    /// </summary>
    public bool IsConsumerTrigger =>
        string.Equals(InvocationKind, LambdaRoutes.ConsumerInvocationKind, StringComparison.Ordinal);

    /// <summary>
    /// The raw <c>sub</c> claim. For a scheduled, actor, or workflow invocation the runtime sets it
    /// to <c>lambda:&lt;functionId&gt;</c> rather than to an application user.
    /// </summary>
    public string? Subject { get; }

    /// <summary>
    /// Whether this invocation was started by Yogabase itself — a schedule, an actor tick, or a
    /// workflow step — rather than by an application user.
    /// </summary>
    public bool IsSystem =>
        Subject is not null && Subject.StartsWith(LambdaRoutes.SystemSubjectPrefix, StringComparison.Ordinal);

    /// <summary>
    /// The function id behind a system invocation, or <see langword="null"/> for a user call.
    /// </summary>
    public string? SystemFunctionId =>
        IsSystem ? Subject![LambdaRoutes.SystemSubjectPrefix.Length..] : null;

    /// <summary>
    /// The caller's subject as a <see cref="Guid"/> when an application user made this call.
    /// Answers <see langword="false"/> for a system invocation or a non-GUID subject, so a Lambda
    /// never silently attributes scheduled work to a player.
    /// </summary>
    public bool TryGetSubjectId(out Guid subjectId)
    {
        if (!IsSystem && Subject is not null && Guid.TryParse(Subject, out subjectId))
        {
            return true;
        }

        subjectId = Guid.Empty;
        return false;
    }

    /// <summary>
    /// The calling application user's subject, or a refusal naming why there is not one. Use this
    /// where the Lambda's whole purpose is to act for a player.
    /// </summary>
    public Guid RequireSubjectId() => TryGetSubjectId(out var subjectId)
        ? subjectId
        : throw new InvalidOperationException(
            IsSystem
                ? "This invocation was started by Yogabase, not by an application user, so it has no subject id."
                : $"The caller's subject '{Subject}' is not an application user identifier.");
}

/// <summary>Which function, version, environment, and invocation this is.</summary>
public sealed class LambdaInvocation
{
    internal LambdaInvocation(IReadOnlyDictionary<string, string> facts)
    {
        Facts = facts;
        InvocationId = Require(facts, LambdaRoutes.InvocationIdKey);
        FunctionId = Require(facts, LambdaRoutes.FunctionIdKey);
        Version = Require(facts, LambdaRoutes.VersionKey);
        EnvironmentId = Require(facts, LambdaRoutes.EnvironmentIdKey);
        ProjectId = facts.TryGetValue(LambdaRoutes.ProjectIdKey, out var projectId) ? projectId : null;
    }

    /// <summary>Identifier of this one invocation. Log it; it is how a run is found again.</summary>
    public string InvocationId { get; }

    /// <summary>Identifier of the function being invoked.</summary>
    public string FunctionId { get; }

    /// <summary>Exact registered version of the function being invoked.</summary>
    public string Version { get; }

    /// <summary>Environment the invocation belongs to.</summary>
    public string EnvironmentId { get; }

    /// <summary>Canonical project UUID for scoped keys and channels; null on older hosts that do not supply it.</summary>
    public string? ProjectId { get; }

    /// <summary>
    /// Every fact the runtime supplied, including any this version does not model by name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Facts { get; }

    private static string Require(IReadOnlyDictionary<string, string> facts, string key) =>
        facts.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException(
                $"The Yogabase Lambda runtime supplied no '{key}' invocation fact.");
}
