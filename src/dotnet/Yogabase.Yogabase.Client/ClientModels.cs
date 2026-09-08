using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Yogabase;

public sealed class YogabaseClientConfiguration
{
    // "data-rest" is deliberately absent: the Data API module is withdrawn, so IsAvailable answers
    // false for it even on an installation that still publishes the capability row.
    private static readonly HashSet<string> KnownCapabilities = new(
        new[] { "auth", "storage", "functions-rest", "realtime-subscribe" },
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
    /// The withdrawn client-facing Data API capability. An installation predating its removal still
    /// publishes this row; the client accepts it in the bootstrap document and ignores it, and
    /// <see cref="YogabaseClientConfiguration.IsAvailable"/> answers <see langword="false"/> for it.
    /// Applications reach data through a Lambda instead.
    /// </summary>
    public const string WithdrawnDataRest = "data-rest";

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
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        TraceId = traceId;
    }

    public int StatusCode { get; }
    public string Code { get; }
    public string? TraceId { get; }
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
}

public interface IYogabaseRealtime
{
    bool IsConnected { get; }
    event Func<YogabaseRealtimeEvent, Task>? MessageReceived;
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<IAsyncDisposable> SubscribeAsync(string topic, CancellationToken cancellationToken = default);
    Task<YogabaseRealtimeLambdaInvocationReceipt> InvokeLambdaAsync(
        YogabaseRealtimeLambdaInvocationRequest request,
        CancellationToken cancellationToken = default);
}
