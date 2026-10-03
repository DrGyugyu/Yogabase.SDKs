using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Yogabase;

internal static class YogabaseJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };
}

internal sealed class YogabaseHttpTransport : IDisposable
{
    private const int MaximumJsonResponseBytes = 16 * 1024 * 1024;
    private readonly HttpClient client;
    private readonly Func<CancellationToken, Task<string?>> accessToken;
    private readonly Uri gateway;

    internal YogabaseHttpTransport(
        Uri gateway,
        YogabaseClientOptions options,
        Func<CancellationToken, Task<string?>> accessToken)
    {
        this.accessToken = accessToken;
        this.gateway = gateway;
        client = YogabaseHttpClientFactory.Create(options);
    }

    internal async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? body,
        bool authenticated,
        CancellationToken cancellationToken,
        HttpCompletionOption completion = HttpCompletionOption.ResponseHeadersRead,
        Action<HttpRequestMessage>? configure = null,
        bool ensureSuccess = true)
    {
        using var request = new HttpRequestMessage(method, new Uri(gateway, path));
        if (body is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, YogabaseJson.Options),
                Encoding.UTF8,
                "application/json");
        }

        if (authenticated)
        {
            var token = await accessToken(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(token))
            {
                throw new YogabaseApiException(401, "authentication_required", "Sign in before calling this Yogabase capability.");
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        configure?.Invoke(request);
        var response = await client.SendAsync(request, completion, cancellationToken).ConfigureAwait(false);
        if (ensureSuccess && !response.IsSuccessStatusCode)
        {
            await ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    internal Task<HttpResponseMessage> SendExternalAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

    internal static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using (response)
        {
            try
            {
                var payload = await YogabaseHttpContent.ReadBoundedBytesAsync(
                    response.Content,
                    MaximumJsonResponseBytes,
                    cancellationToken).ConfigureAwait(false);
                var value = JsonSerializer.Deserialize<T>(payload, YogabaseJson.Options);
                return value ?? throw new YogabaseApiException(
                    (int)response.StatusCode,
                    "response_invalid",
                    "Yogabase returned an empty or invalid response.");
            }
            catch (Exception exception) when (exception is JsonException or InvalidDataException)
            {
                throw new YogabaseApiException(
                    (int)response.StatusCode,
                    "response_invalid",
                    "Yogabase returned an invalid or oversized response.");
            }
        }
    }

    internal static string EscapePath(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !string.Equals(value, value.Trim(), StringComparison.Ordinal)
            || value.Length > 256)
        {
            throw new ArgumentException("A bounded non-empty path value is required.", parameterName);
        }

        return Uri.EscapeDataString(value);
    }

    internal static void ValidateGateway(Uri gateway, bool allowInsecureLoopback)
    {
        if (!gateway.IsAbsoluteUri
            || !string.IsNullOrEmpty(gateway.UserInfo)
            || !string.IsNullOrEmpty(gateway.Query)
            || !string.IsNullOrEmpty(gateway.Fragment)
            || gateway.AbsolutePath != "/")
        {
            throw new ArgumentException("The Yogabase gateway must be an origin-only absolute URI.", nameof(gateway));
        }

        var secure = string.Equals(gateway.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal);
        var loopback = allowInsecureLoopback
            && string.Equals(gateway.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal)
            && (gateway.IsLoopback || string.Equals(gateway.Host, "localhost", StringComparison.OrdinalIgnoreCase));
        if (!secure && !loopback)
        {
            throw new ArgumentException("The Yogabase gateway must use HTTPS.", nameof(gateway));
        }
    }

    internal static async Task ThrowApiExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var code = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => YogabaseStatusErrors.AuthenticationRequired,
            HttpStatusCode.Forbidden => YogabaseStatusErrors.Forbidden,
            HttpStatusCode.NotFound => YogabaseStatusErrors.NotFound,
            HttpStatusCode.Conflict => YogabaseStatusErrors.Conflict,
            // The conditional schedule writes: a stale or unmet ETag, and a write that sent none.
            HttpStatusCode.PreconditionFailed => YogabaseStatusErrors.PreconditionFailed,
            (HttpStatusCode)428 => YogabaseStatusErrors.PreconditionRequired,
            // A gateway that predates the named refusal answers a bare 413 for a body over its
            // listener's limit; the status alone says what was too large, so it reads the same.
            HttpStatusCode.RequestEntityTooLarge => YogabaseInvocationErrors.PayloadTooLarge,
            HttpStatusCode.UnsupportedMediaType => YogabaseStatusErrors.UnsupportedMediaType,
            (HttpStatusCode)429 => YogabaseStatusErrors.RateLimited,
            _ => YogabaseStatusErrors.RequestFailed,
        };
        long? limitBytes = null;
        long? maxBodyBytes = null;
        var message = $"Yogabase returned HTTP {(int)response.StatusCode}.";
        // The Control Plane and the application gateway both answer X-Correlation-ID; the older
        // header name is still read so an installation one release behind is not blind here.
        string? traceId = response.Headers.TryGetValues("X-Correlation-ID", out var correlationIds)
            || response.Headers.TryGetValues("X-Yogabase-Correlation-Id", out correlationIds)
            ? correlationIds.FirstOrDefault()
            : null;
        try
        {
            var text = await YogabaseHttpContent.ReadBoundedStringAsync(
                response.Content,
                1_048_576,
                cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(text))
            {
                using var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 32 });
                var root = document.RootElement;
                if (root.TryGetProperty("code", out var directCode) && directCode.ValueKind == JsonValueKind.String)
                {
                    code = directCode.GetString() ?? code;
                }
                else if (root.TryGetProperty("extensions", out var extensions)
                    && extensions.ValueKind == JsonValueKind.Object
                    && extensions.TryGetProperty("code", out var extensionCode)
                    && extensionCode.ValueKind == JsonValueKind.String)
                {
                    code = extensionCode.GetString() ?? code;
                }

                if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                {
                    message = detail.GetString() ?? message;
                }
                else if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                {
                    message = title.GetString() ?? message;
                }

                // A 413 names the limits it applied: limitBytes for the payload and maxBodyBytes for the
                // whole request body, as plain numbers at the top level of the problem.
                if (root.TryGetProperty("limitBytes", out var limit)
                    && limit.ValueKind == JsonValueKind.Number
                    && limit.TryGetInt64(out var limitValue)
                    && limitValue > 0)
                {
                    limitBytes = limitValue;
                }

                if (root.TryGetProperty("maxBodyBytes", out var maxBody)
                    && maxBody.ValueKind == JsonValueKind.Number
                    && maxBody.TryGetInt64(out var maxBodyValue)
                    && maxBodyValue > 0)
                {
                    maxBodyBytes = maxBodyValue;
                }

                if (root.TryGetProperty("traceId", out var trace) && trace.ValueKind == JsonValueKind.String)
                {
                    traceId = trace.GetString();
                }
                else if (root.TryGetProperty("correlationId", out var correlation)
                    && correlation.ValueKind == JsonValueKind.String)
                {
                    traceId = correlation.GetString();
                }
            }
        }
        catch (Exception exception) when (
            exception is JsonException
                or InvalidOperationException
                or InvalidDataException
                or DecoderFallbackException)
        {
            // Stable status-derived error above is safer than leaking an untrusted response body.
        }
        finally
        {
            response.Dispose();
        }

        throw new YogabaseApiException((int)response.StatusCode, code, message, traceId, limitBytes, maxBodyBytes);
    }

    public void Dispose()
    {
        client.Dispose();
    }
}

internal static class YogabaseHttpClientFactory
{
    /// <summary>
    /// Builds the package's one HTTP client shape. <paramref name="maximumTimeout"/> defaults to
    /// the five minutes every interactive gateway call is bounded by; only the build upload raises
    /// it, because a multi-hundred-megabyte bundle on a slow link legitimately outlives that.
    /// </summary>
    internal static HttpClient Create(YogabaseClientOptions options, TimeSpan? maximumTimeout = null)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var upperBound = maximumTimeout ?? TimeSpan.FromMinutes(5);
        if (options.RequestTimeout <= TimeSpan.Zero || options.RequestTimeout > upperBound)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                FormattableString.Invariant(
                    $"Request timeout must be between zero and {upperBound.TotalMinutes} minutes."));
        }

        HttpMessageHandler handler;
        var disposeHandler = false;
        if (options.TransportHandler is null)
        {
            handler = new HttpClientHandler { AllowAutoRedirect = false };
            disposeHandler = true;
        }
        else
        {
            handler = options.TransportHandler;
            if (handler is HttpClientHandler httpHandler && httpHandler.AllowAutoRedirect)
            {
                throw new ArgumentException("The Yogabase transport must not follow HTTP redirects.", nameof(options));
            }
        }

        return new HttpClient(handler, disposeHandler)
        {
            Timeout = options.RequestTimeout,
        };
    }
}

/// <summary>
/// Wraps a caller-owned upload stream so <see cref="HttpContent"/> may read it without taking
/// ownership: the caller keeps its own stream open, and reuses or disposes it itself.
/// </summary>
internal sealed class NonDisposingReadStream : Stream
{
    private readonly Stream inner;

    internal NonDisposingReadStream(Stream inner) => this.inner = inner;
    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => inner.CanSeek;
    public override bool CanWrite => false;
    public override long Length => inner.Length;
    public override long Position { get => inner.Position; set => inner.Position = value; }
    public override void Flush() => inner.Flush();
    public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        inner.ReadAsync(buffer, offset, count, cancellationToken);
    public override int Read(Span<byte> buffer) => inner.Read(buffer);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        inner.ReadAsync(buffer, cancellationToken);
    public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    protected override void Dispose(bool disposing) => base.Dispose(disposing);
}

internal static class YogabaseHttpContent
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    internal static Task<Stream> ReadAsStreamAsync(HttpContent content, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
#if NETSTANDARD2_1
        return content.ReadAsStreamAsync();
#else
        return content.ReadAsStreamAsync(cancellationToken);
#endif
    }

    internal static async Task<byte[]> ReadBoundedBytesAsync(
        HttpContent content,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }
        if (maximumBytes < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        }
        if (content.Headers.ContentLength is long declaredLength && declaredLength > maximumBytes)
        {
            throw new InvalidDataException("Yogabase returned an oversized response.");
        }

        await using var input = await ReadAsStreamAsync(content, cancellationToken).ConfigureAwait(false);
        using var output = new MemoryStream(Math.Min(maximumBytes, 64 * 1024));
        var buffer = new byte[16 * 1024];
        while (true)
        {
            var remaining = maximumBytes - checked((int)output.Length);
            var requested = Math.Min(buffer.Length, remaining + 1);
            var read = await input.ReadAsync(buffer.AsMemory(0, requested), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return output.ToArray();
            }
            if (read > remaining)
            {
                throw new InvalidDataException("Yogabase returned an oversized response.");
            }
            output.Write(buffer, 0, read);
        }
    }

    internal static async Task<string> ReadBoundedStringAsync(
        HttpContent content,
        int maximumBytes,
        CancellationToken cancellationToken) =>
        StrictUtf8.GetString(await ReadBoundedBytesAsync(content, maximumBytes, cancellationToken)
            .ConfigureAwait(false));
}
