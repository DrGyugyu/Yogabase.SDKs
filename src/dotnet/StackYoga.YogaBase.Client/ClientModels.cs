using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StackYoga.YogaBase;

public sealed class YogaBaseClientConfiguration
{
    private static readonly HashSet<string> KnownCapabilities = new(
        new[] { "auth", "data-rest", "storage", "functions-rest", "realtime-subscribe" },
        StringComparer.Ordinal);

    public string Schema { get; init; } = string.Empty;
    public Guid ProjectId { get; init; }
    public Guid EnvironmentId { get; init; }
    public string GatewayEndpoint { get; init; } = string.Empty;
    public string OidcAuthority { get; init; } = string.Empty;
    public string OidcDiscoveryEndpoint { get; init; } = string.Empty;
    public string OidcPublicClientId { get; init; } = string.Empty;
    public IReadOnlyList<string> OidcScopes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> OidcRedirectUris { get; init; } = Array.Empty<string>();
    public string RealtimePath { get; init; } = "/v1/realtime";
    public IReadOnlyList<YogaBaseCapability> Capabilities { get; init; } = Array.Empty<YogaBaseCapability>();
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

public sealed class YogaBaseCapability
{
    public string Id { get; init; } = string.Empty;
    public bool Available { get; init; }
    public string? UnavailableReason { get; init; }
}

public sealed class YogaBaseClientOptions
{
    /// <summary>
    /// Optional direct transport handler for tests or platform integration. Redirect-capable
    /// handlers are rejected; custom handlers must return redirects without following them.
    /// </summary>
    public System.Net.Http.HttpMessageHandler? TransportHandler { get; set; }
    public IYogaBaseSessionStore? SessionStore { get; set; }
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool AllowInsecureLoopback { get; set; }
}

public sealed class YogaBaseTokenSet
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public DateTimeOffset ExpiresAt { get; set; }
    public string? Subject { get; set; }
}

public sealed class YogaBaseSession
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTimeOffset ExpiresAt { get; set; }
    public string? Subject { get; set; }

    internal static YogaBaseSession FromTokenSet(YogaBaseTokenSet tokens) => new()
    {
        AccessToken = tokens.AccessToken,
        TokenType = tokens.TokenType,
        ExpiresAt = tokens.ExpiresAt,
        Subject = tokens.Subject,
    };
}

public interface IYogaBaseSessionStore
{
    Task<YogaBaseTokenSet?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(YogaBaseTokenSet tokens, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}

public sealed class InMemoryYogaBaseSessionStore : IYogaBaseSessionStore
{
    private readonly object gate = new();
    private YogaBaseTokenSet? tokens;

    public Task<YogaBaseTokenSet?> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            return Task.FromResult(tokens is null ? null : Copy(tokens));
        }
    }

    public Task SaveAsync(YogaBaseTokenSet tokens, CancellationToken cancellationToken = default)
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

    private static YogaBaseTokenSet Copy(YogaBaseTokenSet source) => new()
    {
        AccessToken = source.AccessToken,
        RefreshToken = source.RefreshToken,
        TokenType = source.TokenType,
        ExpiresAt = source.ExpiresAt,
        Subject = source.Subject,
    };
}

public sealed class YogaBasePkceOptions
{
    public Uri? RedirectUri { get; set; }
    public IReadOnlyList<string>? Scopes { get; set; }
    public Func<Uri, CancellationToken, Task<Uri>>? OpenBrowserAsync { get; set; }
}

public sealed class YogaBaseApiException : Exception
{
    public YogaBaseApiException(int statusCode, string code, string message, string? traceId = null)
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

public interface IYogaBaseAuth
{
    Task<YogaBaseSession?> GetSessionAsync(CancellationToken cancellationToken = default);
    Task SignUpAsync(string email, string password, string? invitationToken = null, CancellationToken cancellationToken = default);
    Task<YogaBaseSession> SignInWithPkceAsync(YogaBasePkceOptions options, CancellationToken cancellationToken = default);
    Task<YogaBaseSession> RefreshAsync(CancellationToken cancellationToken = default);
    Task ClearLocalSessionAsync(CancellationToken cancellationToken = default);
}

public sealed class YogaBaseDataValue
{
    public string Type { get; set; } = string.Empty;
    public JsonElement Value { get; set; }

    public static YogaBaseDataValue Null() => Create("null", (object?)null);
    public static YogaBaseDataValue FromText(string value) => Create("string", value);
    public static YogaBaseDataValue FromBoolean(bool value) => Create("bool", value);
    public static YogaBaseDataValue FromInteger(long value) => Create("int64", value);
    public static YogaBaseDataValue FromFixedPoint(decimal value) => Create("decimal", value);
    public static YogaBaseDataValue FromFloatingPoint(double value) => Create("double", value);
    public static YogaBaseDataValue FromIdentifier(Guid value) => Create("guid", value.ToString("D"));
    public static YogaBaseDataValue FromTimestamp(DateTimeOffset value) => Create("timestamp", value.ToString("O"));
    public static YogaBaseDataValue FromJson(JsonElement value) => new() { Type = "json", Value = value.Clone() };

    private static YogaBaseDataValue Create(string type, object? value) => new()
    {
        Type = type,
        Value = JsonSerializer.SerializeToElement(value, YogaBaseJson.Options),
    };
}

public sealed class YogaBaseFilter
{
    public string Column { get; set; } = string.Empty;
    public string Operator { get; set; } = "eq";
    public IReadOnlyList<YogaBaseDataValue> Values { get; set; } = Array.Empty<YogaBaseDataValue>();
}

public sealed class YogaBaseOrdering
{
    public string Column { get; set; } = string.Empty;
    public bool Descending { get; set; }
}

public sealed class YogaBaseCursor
{
    public string Column { get; set; } = string.Empty;
    public YogaBaseDataValue LastValue { get; set; } = YogaBaseDataValue.Null();
    public bool Descending { get; set; }
}

public sealed class YogaBaseSelectRequest
{
    public IReadOnlyList<string> Columns { get; set; } = Array.Empty<string>();
    public IReadOnlyList<YogaBaseFilter> Filters { get; set; } = Array.Empty<YogaBaseFilter>();
    public IReadOnlyList<YogaBaseOrdering> Ordering { get; set; } = Array.Empty<YogaBaseOrdering>();
    public int PageSize { get; set; }
    public int QueryDepth { get; set; } = 1;
    public YogaBaseCursor? Cursor { get; set; }
}

public sealed class YogaBaseMutationRequest
{
    public IReadOnlyDictionary<string, YogaBaseDataValue> Values { get; set; } =
        new Dictionary<string, YogaBaseDataValue>(StringComparer.Ordinal);
    public IReadOnlyList<YogaBaseFilter> Filters { get; set; } = Array.Empty<YogaBaseFilter>();
    public IReadOnlyList<string> ReturningColumns { get; set; } = Array.Empty<string>();
}

public sealed class YogaBaseDataResult
{
    public IReadOnlyList<JsonElement> Rows { get; set; } = Array.Empty<JsonElement>();
    public int AffectedRows { get; set; }

    public IReadOnlyList<T> DeserializeRows<T>(System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
    {
        if (typeInfo is null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        var rows = new List<T>(Rows.Count);
        foreach (var row in Rows)
        {
            var item = row.Deserialize(typeInfo);
            if (item is not null)
            {
                rows.Add(item);
            }
        }

        return rows;
    }
}

public interface IYogaBaseData
{
    Task<YogaBaseDataResult> SelectAsync(string resource, YogaBaseSelectRequest request, CancellationToken cancellationToken = default);
    Task<YogaBaseDataResult> InsertAsync(string resource, YogaBaseMutationRequest request, CancellationToken cancellationToken = default);
    Task<YogaBaseDataResult> UpdateAsync(string resource, YogaBaseMutationRequest request, CancellationToken cancellationToken = default);
    Task<YogaBaseDataResult> DeleteAsync(string resource, YogaBaseMutationRequest request, CancellationToken cancellationToken = default);
}

public sealed class YogaBaseObjectWriteResult
{
    public string Key { get; set; } = string.Empty;
    public string Digest { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Encryption { get; set; } = string.Empty;
    public bool AlreadyExisted { get; set; }
}

public sealed class YogaBaseObjectRead : IAsyncDisposable, IDisposable
{
    internal YogaBaseObjectRead(Stream content, string? contentType, long? contentLength, System.Net.Http.HttpResponseMessage response)
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

public interface IYogaBaseStorage
{
    Task<YogaBaseObjectWriteResult> PutAsync(string key, Stream content, long contentLength, string contentType, string sha256Digest, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<YogaBaseObjectWriteResult> PutBytesAsync(string key, byte[] content, string contentType, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<YogaBaseObjectRead> GetAsync(string key, CancellationToken cancellationToken = default);
}

public enum YogaBaseInvocationMode
{
    Synchronous = 0,
    Asynchronous = 1,
}

public sealed class YogaBaseInvocationRequest
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset Deadline { get; set; }
    public YogaBaseInvocationMode Mode { get; set; }
    public string PayloadDigest { get; set; } = string.Empty;
    public byte[]? InlinePayload { get; set; }
    public YogaBaseObjectReference? ObjectReference { get; set; }
}

public enum YogaBaseInvocationOutcome
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

public sealed class YogaBaseObjectReference
{
    public string Uri { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

public sealed class YogaBaseInvocationResult
{
    public YogaBaseInvocationOutcome Outcome { get; set; }
    public string InvocationId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public byte[]? InlineResult { get; set; }
    public string? ResultReference { get; set; }
    public string? FailureCode { get; set; }
}

public interface IYogaBaseFunctions
{
    Task<YogaBaseInvocationResult> InvokeAsync(string functionId, string functionVersion, YogaBaseInvocationRequest request, CancellationToken cancellationToken = default);
}

public interface IYogaBaseRealtime
{
    bool IsConnected { get; }
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<IAsyncDisposable> SubscribeAsync(string topic, CancellationToken cancellationToken = default);
}
