using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Lambda;

/// <summary>
/// A refusal from the Yogabase Lambda runtime API. It carries the HTTP status and, when the
/// gateway answered RFC 7807, the stable refusal code and detail; <see cref="LambdaRefusalCodes"/>
/// names the codes. The wrapper throws the same exception before transport for the two refusals it
/// can decide locally — a reserved storage key and a never-issuable scope — with exactly the status
/// and code the gateway would have answered, so one catch path covers both.
/// </summary>
public sealed class LambdaApiException : Exception
{
    /// <summary>Creates a failure for one runtime API response.</summary>
    public LambdaApiException(int statusCode, string code, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }

    /// <summary>HTTP status the runtime API answered.</summary>
    public int StatusCode { get; }

    /// <summary>Stable refusal code, or a status-derived one when the body carried none.</summary>
    public string Code { get; }
}

/// <summary>
/// The refusal codes the Lambda runtime API answers on the routes this package calls, as
/// <see cref="LambdaApiException.Code"/> carries them. Every code is the gateway's own spelling;
/// the wrapper never invents one except the three response-shape codes at the end.
/// </summary>
public static class LambdaRefusalCodes
{
    // ---- runtime token (401): the per-invocation bearer the runtime attached was not accepted.

    /// <summary>No bearer was presented.</summary>
    public const string RuntimeTokenRequired = "runtime_token_required";

    /// <summary>The invocation's five-minute token has expired.</summary>
    public const string RuntimeTokenExpired = "runtime_token_expired";

    /// <summary>The token's validity has not begun.</summary>
    public const string RuntimeTokenNotYetValid = "runtime_token_not_yet_valid";

    /// <summary>The token names another project.</summary>
    public const string RuntimeTokenForeignProject = "runtime_token_foreign_project";

    /// <summary>The token names another environment.</summary>
    public const string RuntimeTokenForeignEnvironment = "runtime_token_foreign_environment";

    /// <summary>The token's signature did not verify.</summary>
    public const string RuntimeTokenSignatureInvalid = "runtime_token_signature_invalid";

    /// <summary>The token could not be read.</summary>
    public const string RuntimeTokenMalformed = "runtime_token_malformed";

    // ---- storage

    /// <summary>404: no object under the key.</summary>
    public const string ObjectNotFound = "object_not_found";

    /// <summary>409: the key already identifies an object this write may not replace.</summary>
    public const string ObjectConflict = "object_conflict";

    /// <summary>403: the key or prefix enters the reserved <c>environments/</c> layout. Also refused before transport.</summary>
    public const string KeyReservedPrefix = "key_reserved_prefix";

    /// <summary>413: a bounded positive Content-Length is required.</summary>
    public const string ObjectLengthInvalid = "object_length_invalid";

    /// <summary>400: the body did not match its Content-Length.</summary>
    public const string ObjectLengthMismatch = "object_length_mismatch";

    /// <summary>403: the media type is not permitted, or carries parameters.</summary>
    public const string ContentTypeNotAllowed = "content_type_not_allowed";

    /// <summary>400: a listing <c>limit</c> that is not a positive integer.</summary>
    public const string LimitInvalid = "limit_invalid";

    /// <summary>400: a listing <c>cursor</c> the gateway did not issue.</summary>
    public const string CursorInvalid = "cursor_invalid";

    /// <summary>503: the project's storage is not configured.</summary>
    public const string StorageUnavailable = "storage_unavailable";

    /// <summary>503: the installation's storage provider could not be reached.</summary>
    public const string StorageProviderUnavailable = "storage_provider_unavailable";

    // ---- users

    /// <summary>400: a subject that is not a canonical UUID. Also refused before transport, as an argument error.</summary>
    public const string SubjectInvalid = "subject_invalid";

    /// <summary>404: no such user in this project.</summary>
    public const string UserNotFound = "user_not_found";

    /// <summary>400: a create or update outside the bounded contract.</summary>
    public const string UserRequestInvalid = "user_request_invalid";

    /// <summary>409: the realm did not create the user.</summary>
    public const string UserNotCreated = "user_not_created";

    /// <summary>409: the realm did not update the user.</summary>
    public const string UserNotUpdated = "user_not_updated";

    /// <summary>400: the scope set is empty, larger than sixteen, outside the realm's admitted set, or names <c>openid</c> or <c>offline_access</c>. The last two are also refused before transport.</summary>
    public const string ScopeNotAllowed = "scope_not_allowed";

    /// <summary>403: the user is banned or locked out.</summary>
    public const string UserBanned = "user_banned";

    /// <summary>403: the user has not confirmed the email address the realm requires.</summary>
    public const string UserEmailUnconfirmed = "user_email_unconfirmed";

    /// <summary>503: the realm refused to issue, or produced no token.</summary>
    public const string UserTokenIssuanceFailed = "user_token_issuance_failed";

    /// <summary>503: this host composed the runtime API without the realm's issuer.</summary>
    public const string UserTokenIssuanceUnavailable = "user_token_issuance_unavailable";

    // ---- realtime

    /// <summary>400: the topic is not a Realtime binding id. Also refused before transport, as an argument error.</summary>
    public const string TopicInvalid = "topic_invalid";

    /// <summary>413: the payload is empty or larger than 64 KiB. Also refused before transport, as an argument error.</summary>
    public const string PayloadTooLarge = "payload_too_large";

    /// <summary>400: the payload is not JSON.</summary>
    public const string PayloadNotJson = "payload_not_json";

    /// <summary>503: the durable Realtime outbox is not configured for this runtime.</summary>
    public const string RealtimeOutboxUnavailable = "realtime_outbox_unavailable";

    /// <summary>409: the invocation this token names is no longer running.</summary>
    public const string InvocationNotInProgress = "invocation_not_in_progress";

    /// <summary>429: this invocation has emitted its maximum number of Realtime events.</summary>
    public const string RealtimePublishLimitExhausted = "realtime_publish_limit_exhausted";

    // ---- this wrapper's own, for a response it could not use (502)

    /// <summary>The response was empty, unreadable, or not the shape the gateway serves.</summary>
    public const string ResponseInvalid = "response_invalid";

    /// <summary>The response was larger than the wrapper will read.</summary>
    public const string ResponseTooLarge = "response_too_large";

    /// <summary>A failure with a body that carried no code; <see cref="LambdaApiException.Code"/> is then status-derived.</summary>
    public const string RequestFailed = "request_failed";
}

/// <summary>
/// Locates the runtime's <c>IHttpClientFactory</c> without referencing it.
/// </summary>
/// <remarks>
/// A Lambda bundle shares the sandbox host's assembly load context, so every package this wrapper
/// declares is a package the host must already carry at a compatible version — and a bundle pinning
/// an incompatible major is refused at admission. <c>Microsoft.Extensions.Http</c> is therefore
/// deliberately not referenced: the factory the runtime already registered is found by type name
/// and invoked through reflection, so a bundle's dependency closure stays at exactly one package.
/// </remarks>
internal static class LambdaHttpClients
{
    private const string FactoryTypeName = "System.Net.Http.IHttpClientFactory";
    private static Type? cachedFactoryType;
    private static MethodInfo? cachedCreateClient;

    internal static HttpClient Resolve(IServiceProvider services, string name)
    {
        var factoryType = cachedFactoryType ??= FindFactoryType()
            ?? throw new InvalidOperationException(
                $"The Yogabase Lambda runtime did not load '{FactoryTypeName}', so the '{name}' "
                + "client cannot be resolved. This wrapper must run inside the Yogabase sandbox.");
        var factory = services.GetService(factoryType)
            ?? throw new InvalidOperationException(
                $"The Yogabase Lambda runtime resolved no '{FactoryTypeName}', so the '{name}' "
                + "client is unavailable.");
        var create = cachedCreateClient ??= factoryType.GetMethod("CreateClient", [typeof(string)])
            ?? throw new InvalidOperationException(
                $"'{FactoryTypeName}' carries no CreateClient(string) method.");
        if (create.Invoke(factory, [name]) is not HttpClient client)
        {
            throw new InvalidOperationException($"The Yogabase Lambda runtime returned no '{name}' client.");
        }
        if (client.BaseAddress is null)
        {
            throw new InvalidOperationException(
                $"The '{name}' client has no BaseAddress. The runtime is expected to point it at the "
                + "gateway's internal Lambda API root.");
        }

        return client;
    }

    private static Type? FindFactoryType()
    {
        var direct = Type.GetType(FactoryTypeName + ", Microsoft.Extensions.Http", throwOnError: false);
        if (direct is not null)
        {
            return direct;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var candidate = assembly.GetType(FactoryTypeName, throwOnError: false);
                if (candidate is not null)
                {
                    return candidate;
                }
            }
            catch (NotSupportedException)
            {
                // A dynamic assembly cannot be searched by name. Keep looking through the rest.
            }
        }

        return null;
    }
}

internal static class LambdaJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

internal static class LambdaTransport
{
    private const int MaximumResponseBytes = 64 * 1024 * 1024;

    internal static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        bool allowNotFound = false)
    {
        var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        if (response.IsSuccessStatusCode
            || (allowNotFound && response.StatusCode == System.Net.HttpStatusCode.NotFound))
        {
            return response;
        }

        throw await ToExceptionAsync(response, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using (response)
        {
            var payload = await ReadBoundedBytesAsync(response.Content, cancellationToken).ConfigureAwait(false);
            try
            {
                return JsonSerializer.Deserialize<T>(payload, LambdaJson.Options)
                    ?? throw new LambdaApiException(
                        (int)response.StatusCode,
                        LambdaRefusalCodes.ResponseInvalid,
                        "The Yogabase Lambda runtime API returned an empty response.");
            }
            catch (JsonException)
            {
                throw new LambdaApiException(
                    (int)response.StatusCode,
                    LambdaRefusalCodes.ResponseInvalid,
                    "The Yogabase Lambda runtime API returned an unreadable response.");
            }
        }
    }

    internal static async Task<byte[]> ReadBytesAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        using (response)
        {
            return await ReadBoundedBytesAsync(response.Content, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<byte[]> ReadBoundedBytesAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is long declared && declared > MaximumResponseBytes)
        {
            throw new LambdaApiException(502, LambdaRefusalCodes.ResponseTooLarge, "The Yogabase Lambda runtime API returned an oversized response.");
        }

        cancellationToken.ThrowIfCancellationRequested();
#if NET10_0_OR_GREATER
        using var input = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
        using var input = await content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
        using var output = new MemoryStream();
        var buffer = new byte[64 * 1024];
        while (true)
        {
            var remaining = MaximumResponseBytes - checked((int)output.Length);
            var requested = Math.Min(buffer.Length, remaining + 1);
            var read = await input.ReadAsync(buffer.AsMemory(0, requested), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return output.ToArray();
            }
            if (read > remaining)
            {
                throw new LambdaApiException(502, LambdaRefusalCodes.ResponseTooLarge, "The Yogabase Lambda runtime API returned an oversized response.");
            }

            output.Write(buffer, 0, read);
        }
    }

    private static async Task<LambdaApiException> ToExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var status = (int)response.StatusCode;
        var code = status switch
        {
            401 => "authentication_required",
            403 => "forbidden",
            404 => "not_found",
            409 => "conflict",
            429 => "rate_limited",
            _ => LambdaRefusalCodes.RequestFailed,
        };
        var message = "The Yogabase Lambda runtime API returned HTTP "
            + status.ToString(CultureInfo.InvariantCulture) + ".";
        try
        {
            var text = Encoding.UTF8.GetString(
                await ReadBoundedBytesAsync(response.Content, cancellationToken).ConfigureAwait(false));
            if (!string.IsNullOrWhiteSpace(text))
            {
                using var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 32 });
                var root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("code", out var declared) && declared.ValueKind == JsonValueKind.String)
                    {
                        code = declared.GetString() ?? code;
                    }
                    if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                    {
                        message = detail.GetString() ?? message;
                    }
                    else if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                    {
                        message = title.GetString() ?? message;
                    }
                }
            }
        }
        catch (Exception exception) when (
            exception is JsonException or LambdaApiException or DecoderFallbackException or IOException)
        {
            // The status-derived code above is safer than surfacing an untrusted body verbatim.
        }
        finally
        {
            response.Dispose();
        }

        return new LambdaApiException(status, code, message);
    }
}
