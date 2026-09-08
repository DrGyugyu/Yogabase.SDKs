using System;
using System.Collections.Generic;
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
/// Failure reported by the Yogabase Lambda runtime API. It carries the HTTP status and, when the
/// gateway answered RFC 7807, the stable refusal code and detail.
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
                        "response_invalid",
                        "The Yogabase Lambda runtime API returned an empty response.");
            }
            catch (JsonException)
            {
                throw new LambdaApiException(
                    (int)response.StatusCode,
                    "response_invalid",
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

    internal static string EscapeSegment(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > 512
            || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("A bounded non-empty value is required.", parameterName);
        }

        return Uri.EscapeDataString(value);
    }

    private static async Task<byte[]> ReadBoundedBytesAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is long declared && declared > MaximumResponseBytes)
        {
            throw new LambdaApiException(502, "response_too_large", "The Yogabase Lambda runtime API returned an oversized response.");
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
                throw new LambdaApiException(502, "response_too_large", "The Yogabase Lambda runtime API returned an oversized response.");
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
            _ => "request_failed",
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

/// <summary>
/// Reads a listing that may arrive as a bare array or wrapped in an object, because the internal
/// Lambda API is not a published contract and its envelope is still settling.
/// </summary>
internal static class LambdaListing
{
    internal static IReadOnlyList<JsonElement> Items(JsonElement root, params string[] propertyNames)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return Collect(root);
        }
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in propertyNames)
            {
                if (root.TryGetProperty(name, out var array) && array.ValueKind == JsonValueKind.Array)
                {
                    return Collect(array);
                }
            }
        }

        throw new LambdaApiException(
            502,
            "response_invalid",
            "The Yogabase Lambda runtime API returned an unrecognized listing shape.");
    }

    private static List<JsonElement> Collect(JsonElement array)
    {
        var items = new List<JsonElement>();
        foreach (var item in array.EnumerateArray())
        {
            items.Add(item.Clone());
        }

        return items;
    }
}
