using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Yogabase;

public sealed class YogabaseClientConfiguration
{
    // "data-rest" is deliberately absent: this package has no Data client, so IsAvailable answers
    // false for it whatever an installation publishes.
    private static readonly HashSet<string> KnownCapabilities = new(
        new[] { "auth", "storage", "functions-rest", "realtime-subscribe", YogabaseCapability.RealtimePresence },
        StringComparer.Ordinal);

    public string Schema { get; init; } = string.Empty;
    public Guid ProjectId { get; init; }
    public Guid EnvironmentId { get; init; }
    public string GatewayEndpoint { get; init; } = string.Empty;
    public string RealtimeEndpoint { get; init; } = string.Empty;
    public string OidcAuthority { get; init; } = string.Empty;
    public string OidcDiscoveryEndpoint { get; init; } = string.Empty;
    public string OidcPublicClientId { get; init; } = string.Empty;
    public IReadOnlyList<string> OidcScopes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> OidcRedirectUris { get; init; } = Array.Empty<string>();
    public string RealtimePath { get; init; } = "/v1/realtime";
    public IReadOnlyList<YogabaseCapability> Capabilities { get; init; } = Array.Empty<YogabaseCapability>();
    public DateTimeOffset IssuedAt { get; init; }

    public bool IsAvailable(string capabilityId)
    {
        if (string.IsNullOrWhiteSpace(capabilityId) || !KnownCapabilities.Contains(capabilityId))
        {
            return false;
        }

        foreach (var capability in Capabilities)
        {
            if (string.Equals(capability.Id, capabilityId, StringComparison.Ordinal))
            {
                return capability.Available;
            }
        }

        return false;
    }
}

public sealed class YogabaseCapability
{
    /// <summary>
    /// The client-facing Data API capability: a requested-only row since the Data API was restored
    /// (2026-09-23). This package has no Data client, so it never names the row in
    /// <c>X-Yogabase-Accept-Capabilities</c>; a bootstrap document that carries it anyway (an
    /// installation that predates the 2026-09-08 removal published it by default) is accepted, the
    /// row is ignored, and <see cref="YogabaseClientConfiguration.IsAvailable"/> answers
    /// <see langword="false"/> for it. The constant keeps the name it had when the module was
    /// withdrawn.
    /// </summary>
    public const string WithdrawnDataRest = "data-rest";

    /// <summary>
    /// Realtime presence. A requested-only row: the gateway adds it after the four core rows only
    /// for a client that names it in <c>X-Yogabase-Accept-Capabilities</c>, which
    /// <see cref="YogabaseClient.CreateAsync"/> does. A gateway that predates presence ignores the
    /// header and publishes no row, which reads as unavailable.
    /// </summary>
    public const string RealtimePresence = "realtime-presence";

    public string Id { get; init; } = string.Empty;
    public bool Available { get; init; }
    public string? UnavailableReason { get; init; }
}

public sealed class YogabaseClientOptions
{
    /// <summary>
    /// Optional direct transport handler for tests or platform integration. Redirect-capable
    /// handlers are rejected; custom handlers must return redirects without following them.
    /// </summary>
    public System.Net.Http.HttpMessageHandler? TransportHandler { get; set; }
    public IYogabaseSessionStore? SessionStore { get; set; }
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool AllowInsecureLoopback { get; set; }
}

public sealed class YogabaseTokenSet
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public DateTimeOffset ExpiresAt { get; set; }
    public string? Subject { get; set; }
}

public sealed class YogabaseSession
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTimeOffset ExpiresAt { get; set; }
    public string? Subject { get; set; }

    internal static YogabaseSession FromTokenSet(YogabaseTokenSet tokens) => new()
    {
        AccessToken = tokens.AccessToken,
        TokenType = tokens.TokenType,
        ExpiresAt = tokens.ExpiresAt,
        Subject = tokens.Subject,
    };
}

public interface IYogabaseSessionStore
{
    Task<YogabaseTokenSet?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(YogabaseTokenSet tokens, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}

public sealed class InMemoryYogabaseSessionStore : IYogabaseSessionStore
{
    private readonly object gate = new();
    private YogabaseTokenSet? tokens;

    public Task<YogabaseTokenSet?> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            return Task.FromResult(tokens is null ? null : Copy(tokens));
        }
    }

    public Task SaveAsync(YogabaseTokenSet tokens, CancellationToken cancellationToken = default)
    {
        if (tokens is null)
        {
            throw new ArgumentNullException(nameof(tokens));
        }

        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            this.tokens = Copy(tokens);
        }

        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            tokens = null;
        }

        return Task.CompletedTask;
    }

    private static YogabaseTokenSet Copy(YogabaseTokenSet source) => new()
    {
        AccessToken = source.AccessToken,
        RefreshToken = source.RefreshToken,
        TokenType = source.TokenType,
        ExpiresAt = source.ExpiresAt,
        Subject = source.Subject,
    };
}

public sealed class YogabasePkceOptions
{
    public Uri? RedirectUri { get; set; }
    public IReadOnlyList<string>? Scopes { get; set; }
    public Func<Uri, CancellationToken, Task<Uri>>? OpenBrowserAsync { get; set; }
}

public sealed class YogabaseApiException : Exception
{
    public YogabaseApiException(int statusCode, string code, string message, string? traceId = null)
        : this(statusCode, code, message, traceId, limitBytes: null, maxBodyBytes: null)
    {
    }

    public YogabaseApiException(
        int statusCode,
        string code,
        string message,
        string? traceId,
        long? limitBytes,
        long? maxBodyBytes)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        TraceId = traceId;
        LimitBytes = limitBytes;
        MaxBodyBytes = maxBodyBytes;
    }

    public int StatusCode { get; }
    public string Code { get; }
    public string? TraceId { get; }

    /// <summary>
    /// The most bytes the refused request's payload may have, before base64 encoding, when the gateway
    /// named it: a <c>413</c> from the function invoke route (<see cref="YogabaseInvocationErrors.PayloadTooLarge"/>),
    /// the actor invoke and workflow routes (the same code, at 480 KiB), the Jobs submit route
    /// (<see cref="YogabaseJobErrors.PayloadTooLarge"/>) or the schedule write route
    /// (<see cref="YogabaseScheduleErrors.PayloadTooLarge"/>) carries it as <c>limitBytes</c>.
    /// <see langword="null"/> for every other refusal.
    /// </summary>
    public long? LimitBytes { get; }

    /// <summary>
    /// The most bytes the route's whole request body may have, when the gateway named it
    /// (<c>maxBodyBytes</c> on the same <c>413</c> problem), so a caller can tell a payload over the
    /// limit from a body over it. <see langword="null"/> for every other refusal.
    /// </summary>
    public long? MaxBodyBytes { get; }
}

public interface IYogabaseAuth
{
    Task<YogabaseSession?> GetSessionAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the current session only when it has a usable access token. Expired sessions are
    /// refreshed through the configured OIDC authority when a refresh token is available.
    /// </summary>
    Task<YogabaseSession?> GetValidSessionAsync(CancellationToken cancellationToken = default);
    Task SignUpAsync(string email, string password, string? invitationToken = null, CancellationToken cancellationToken = default);
    Task<YogabaseInteractiveSignInOutcome> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default);
    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);
    Task CompletePasswordResetAsync(Guid userId, string token, string password, CancellationToken cancellationToken = default);
    Task<YogabaseMfaSetupResult> SetupMfaAsync(CancellationToken cancellationToken = default);
    Task ConfirmMfaAsync(string code, CancellationToken cancellationToken = default);
    Task VerifyMfaAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> RegenerateRecoveryCodesAsync(CancellationToken cancellationToken = default);
    Task UseRecoveryCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<YogabaseInteractiveSignInOutcome> CompleteExternalLoginAsync(
        string provider,
        string code,
        string state,
        CancellationToken cancellationToken = default);
    Task<YogabaseSession> SignInWithPkceAsync(YogabasePkceOptions options, CancellationToken cancellationToken = default);
    Task<YogabaseSession> RefreshAsync(CancellationToken cancellationToken = default);
    Task ClearLocalSessionAsync(CancellationToken cancellationToken = default);
}

public enum YogabaseInteractiveSignInOutcome
{
    SignedIn = 0,
    MfaRequired = 1,
}

public sealed class YogabaseMfaSetupResult
{
    public string SharedKey { get; set; } = string.Empty;
}

public sealed class YogabaseObjectWriteResult
{
    public string Key { get; set; } = string.Empty;
    public string Digest { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Encryption { get; set; } = string.Empty;
    public bool AlreadyExisted { get; set; }
}

public sealed class YogabaseObjectRead : IAsyncDisposable, IDisposable
{
    internal YogabaseObjectRead(Stream content, string? contentType, long? contentLength, System.Net.Http.HttpResponseMessage response)
    {
        Content = content;
        ContentType = contentType;
        ContentLength = contentLength;
        this.response = response;
    }

    private readonly System.Net.Http.HttpResponseMessage response;
    public Stream Content { get; }
    public string? ContentType { get; }
    public long? ContentLength { get; }

    public void Dispose()
    {
        Content.Dispose();
        response.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await Content.DisposeAsync().ConfigureAwait(false);
        response.Dispose();
    }
}

public interface IYogabaseStorage
{
    Task<YogabaseObjectWriteResult> PutAsync(string key, Stream content, long contentLength, string contentType, string sha256Digest, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<YogabaseObjectWriteResult> PutBytesAsync(string key, byte[] content, string contentType, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<YogabaseObjectRead> GetAsync(string key, CancellationToken cancellationToken = default);
}

public enum YogabaseInvocationMode
{
    Synchronous = 0,
    Asynchronous = 1,
}

public sealed class YogabaseInvocationRequest
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset Deadline { get; set; }
    public YogabaseInvocationMode Mode { get; set; }
    public string PayloadDigest { get; set; } = string.Empty;
    public byte[]? InlinePayload { get; set; }
    public YogabaseObjectReference? ObjectReference { get; set; }
}

public enum YogabaseInvocationOutcome
{
    Accepted = 0,
    Replayed = 1,
    Conflict = 2,
    Unavailable = 3,
    Completed = 4,
    Failed = 5,
    DeadlineExceeded = 6,
    FunctionNotFound = 7,
    ArtifactNotReady = 8,
}

public sealed class YogabaseObjectReference
{
    public string Uri { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

public sealed class YogabaseInvocationResult
{
    public YogabaseInvocationOutcome Outcome { get; set; }
    public string InvocationId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public byte[]? InlineResult { get; set; }
    public string? ResultReference { get; set; }
    public string? FailureCode { get; set; }
}

public interface IYogabaseFunctions
{
    Task<YogabaseInvocationResult> InvokeAsync(string functionId, string functionVersion, YogabaseInvocationRequest request, CancellationToken cancellationToken = default);
}

public sealed class YogabaseActorInvocationRequest
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset Deadline { get; set; }
    public string PayloadDigest { get; set; } = string.Empty;
    public byte[] InlinePayload { get; set; } = Array.Empty<byte>();
}

public enum YogabaseActorInvocationStatus
{
    InProgress = 0,
    Completed = 1,
    Failed = 2,
}

public sealed class YogabaseActorInvocationResult
{
    public YogabaseActorInvocationStatus Status { get; set; }
    public Guid InvocationId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? ResultReference { get; set; }
    public string? FailureCode { get; set; }
    public long StateVersion { get; set; }
}

public interface IYogabaseActors
{
    Task<YogabaseActorInvocationResult> InvokeAsync(
        string functionId,
        string functionVersion,
        string instanceKey,
        YogabaseActorInvocationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class YogabaseWorkflowStartRequest
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string PayloadDigest { get; set; } = string.Empty;
    public byte[] InlineInput { get; set; } = Array.Empty<byte>();
}

public sealed class YogabaseWorkflowStartResult
{
    public Guid WorkflowInstanceId { get; set; }
}

public sealed class YogabaseWorkflowCancelRequest
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string PayloadDigest { get; set; } = string.Empty;
    public byte[] InlinePayload { get; set; } = Array.Empty<byte>();
}

public interface IYogabaseWorkflows
{
    Task<YogabaseWorkflowStartResult> StartAsync(
        string workflowId,
        string definitionVersion,
        YogabaseWorkflowStartRequest request,
        CancellationToken cancellationToken = default);

    Task CancelAsync(
        Guid workflowInstanceId,
        YogabaseWorkflowCancelRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class YogabaseRealtimeLambdaInvocationRequest
{
    public string BindingId { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset Deadline { get; set; }
    public byte[] Payload { get; set; } = Array.Empty<byte>();
    public string? InstanceKey { get; set; }
}

public enum YogabaseRealtimeLambdaInvocationOutcome
{
    Accepted = 0,
    Replayed = 1,
    Completed = 2,
    Rejected = 3,
    Unavailable = 4,
}

public sealed class YogabaseRealtimeLambdaInvocationReceipt
{
    public YogabaseRealtimeLambdaInvocationOutcome Outcome { get; set; }
    public string InvocationId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string? FailureCode { get; set; }
}

public sealed class YogabaseRealtimeEvent
{
    public const string CurrentSchema = "yogabase.lambda-realtime-event.v1";

    public string Schema { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string InvocationId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public JsonElement Payload { get; set; }

    /// <summary>
    /// The payload as the JSON text it arrived as, or <see langword="null"/> when the event carried
    /// none. Beside <see cref="Payload"/> for callers that parse with their own serializer (Unity's
    /// <c>JsonUtility</c>, Newtonsoft) rather than walking a <see cref="JsonElement"/>. It is read
    /// from <see cref="Payload"/> on each access and is never part of the wire shape.
    /// </summary>
    [JsonIgnore]
    public string? PayloadJson => Payload.ValueKind == JsonValueKind.Undefined ? null : Payload.GetRawText();

    /// <summary>
    /// The topic this event was published on, or <see langword="null"/> from a server that predates
    /// the field.
    ///
    /// <para>
    /// A broadcast could be attributed by which subscription fired, but an event a Lambda addressed
    /// to this user arrives without any subscription at all — it is routed to the signed-in
    /// identity, not to a topic group — so this is how a handler tells <c>inventory.updated</c> from
    /// <c>match.found</c>. It is nullable rather than required because it was added to the existing
    /// schema version: an older gateway simply does not send it, and treating that as a protocol
    /// error would break clients against installations that have not been upgraded yet.
    /// </para>
    /// </summary>
    public string? Topic { get; set; }
}

/// <summary>
/// The <c>presence</c> client method's payload: <see cref="Subject"/> became present on
/// (<see cref="Join"/>) or absent from (<see cref="Leave"/>) <see cref="Topic"/>. Delivered at least
/// once across gateway replicas; <see cref="IYogabaseRealtime.GetPresenceAsync"/> is authoritative.
/// </summary>
public sealed class YogabaseRealtimePresenceEvent
{
    public const string Join = "join";
    public const string Leave = "leave";

    public string Topic { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
}

/// <summary>One subject present on a topic, and how many of its connections track it.</summary>
public sealed class YogabaseRealtimePresenceMember
{
    public string Subject { get; set; } = string.Empty;
    public int Connections { get; set; }
}

/// <summary>
/// The answer to <c>GetPresence</c>: the subjects present on one topic, at most
/// <see cref="YogabaseRealtimePresenceLimits.MaximumSnapshotMembers"/> of them, and whether more existed.
/// </summary>
public sealed class YogabaseRealtimePresenceSnapshot
{
    public string Topic { get; set; } = string.Empty;
    public IReadOnlyList<YogabaseRealtimePresenceMember> Members { get; set; } = Array.Empty<YogabaseRealtimePresenceMember>();
    public bool Truncated { get; set; }
}

/// <summary>The realtime hub's presence bounds and the codes its presence methods refuse with.</summary>
public static class YogabaseRealtimePresenceLimits
{
    public const int MaximumTrackedTopicsPerConnection = 16;
    public const int MaximumSnapshotMembers = 1000;

    /// <summary>Presence is not enabled for this runtime.</summary>
    public const string Unavailable = "presence_unavailable";

    /// <summary>The topic is outside the realtime topic grammar.</summary>
    public const string TopicInvalid = "presence_topic_invalid";

    /// <summary>The connection already tracks the maximum number of topics.</summary>
    public const string TopicLimit = "presence_topic_limit";

    /// <summary>The shared presence store did not answer; established connections carry on.</summary>
    public const string StoreUnavailable = "presence_store_unavailable";
}

public interface IYogabaseRealtime
{
    bool IsConnected { get; }
    event Func<YogabaseRealtimeEvent, Task>? MessageReceived;

    /// <summary>A subject joined or left a topic this connection tracks or is subscribed to.</summary>
    event Func<YogabaseRealtimePresenceEvent, Task>? PresenceChanged;

    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<IAsyncDisposable> SubscribeAsync(string topic, CancellationToken cancellationToken = default);
    Task<YogabaseRealtimeLambdaInvocationReceipt> InvokeLambdaAsync(
        YogabaseRealtimeLambdaInvocationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes the signed-in subject present on <paramref name="topic"/> and subscribes this
    /// connection to it. Tracking a tracked topic is a no-op. Needs the
    /// <see cref="YogabaseCapability.RealtimePresence"/> capability; refused with
    /// <see cref="YogabaseRealtimePresenceLimits.TopicLimit"/> before it reaches the hub when this
    /// connection already tracks <see cref="YogabaseRealtimePresenceLimits.MaximumTrackedTopicsPerConnection"/>
    /// topics. Tracked topics are tracked again whenever the connection is re-established.
    /// </summary>
    Task TrackPresenceAsync(string topic, CancellationToken cancellationToken = default);

    /// <summary>
    /// Withdraws presence on <paramref name="topic"/>. The hub leaves the connection subscribed; that
    /// implicit subscription is not restored after a reconnect, so use
    /// <see cref="SubscribeAsync"/> to keep receiving the topic.
    /// </summary>
    Task UntrackPresenceAsync(string topic, CancellationToken cancellationToken = default);

    /// <summary>The subjects present on <paramref name="topic"/>, validated against the hub's bounds.</summary>
    Task<YogabaseRealtimePresenceSnapshot> GetPresenceAsync(string topic, CancellationToken cancellationToken = default);
}
