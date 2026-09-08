using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Lambda;

/// <summary>
/// Mediated object storage for the Lambda's own project, over the runtime's pre-authenticated
/// <c>yogabase-storage</c> client. Provider coordinates, bucket names, and signed URLs never reach
/// customer code; a key is a logical key inside the project.
/// </summary>
public sealed class LambdaStorage
{
    private readonly HttpClient client;

    internal LambdaStorage(HttpClient client)
    {
        this.client = client;
    }

    /// <summary>Writes one object, replacing any existing value at that key.</summary>
    public async Task PutAsync(
        string key,
        ReadOnlyMemory<byte> content,
        string contentType = "application/octet-stream",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentType) || contentType.Length > 128)
        {
            throw new ArgumentException("A bounded media type is required.", nameof(contentType));
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            LambdaRoutes.StorageObject(LambdaTransport.EscapeSegment(key, nameof(key))));
        var body = new ByteArrayContent(content.ToArray());
        body.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        request.Content = body;
        using var response = await LambdaTransport.SendAsync(client, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Reads one object's bytes.</summary>
    public async Task<byte[]> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            LambdaRoutes.StorageObject(LambdaTransport.EscapeSegment(key, nameof(key))));
        var response = await LambdaTransport.SendAsync(client, request, cancellationToken)
            .ConfigureAwait(false);
        return await LambdaTransport.ReadBytesAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads one object's bytes, or <see langword="null"/> when the key does not exist. Prefer this
    /// to catching a 404 from <see cref="GetAsync"/>.
    /// </summary>
    public async Task<byte[]?> TryGetAsync(string key, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            LambdaRoutes.StorageObject(LambdaTransport.EscapeSegment(key, nameof(key))));
        var response = await LambdaTransport
            .SendAsync(client, request, cancellationToken, allowNotFound: true)
            .ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            response.Dispose();
            return null;
        }

        return await LambdaTransport.ReadBytesAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes one object.</summary>
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            LambdaRoutes.StorageObject(LambdaTransport.EscapeSegment(key, nameof(key))));
        using var response = await LambdaTransport.SendAsync(client, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Lists the object keys under an optional prefix.</summary>
    public async Task<IReadOnlyList<string>> ListAsync(
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        var route = prefix is null
            ? LambdaRoutes.StorageObjects
            : LambdaRoutes.StorageObjectsWithPrefix(prefix);
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        var response = await LambdaTransport.SendAsync(client, request, cancellationToken)
            .ConfigureAwait(false);
        var document = await LambdaTransport.ReadJsonAsync<JsonElement>(response, cancellationToken)
            .ConfigureAwait(false);
        var keys = new List<string>();
        foreach (var item in LambdaListing.Items(document, "objects", "items", "keys"))
        {
            keys.Add(item.ValueKind switch
            {
                JsonValueKind.String => item.GetString()!,
                JsonValueKind.Object when item.TryGetProperty("key", out var key)
                    && key.ValueKind == JsonValueKind.String => key.GetString()!,
                _ => throw new LambdaApiException(
                    502,
                    "response_invalid",
                    "The Yogabase Lambda runtime API returned an object listing entry without a key."),
            });
        }

        return keys;
    }
}
