using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Lambda;

/// <summary>One page of a project's application objects, as logical keys in ordinal order.</summary>
public sealed class LambdaObjectPage
{
    internal LambdaObjectPage(IReadOnlyList<string> keys, string? nextCursor)
    {
        Keys = keys;
        NextCursor = nextCursor;
    }

    /// <summary>The keys on this page; empty when nothing sits under the prefix.</summary>
    public IReadOnlyList<string> Keys { get; }

    /// <summary>
    /// The cursor of the next page, or <see langword="null"/> when this was the last one. Pass it
    /// back to <see cref="LambdaStorage.ListAsync"/> unchanged; it is the gateway's, not a key.
    /// </summary>
    public string? NextCursor { get; }
}

/// <summary>
/// Mediated object storage for the Lambda's own project, over the runtime's pre-authenticated
/// <c>yogabase-storage</c> client. Provider coordinates, bucket names, and signed URLs never reach
/// customer code; a key is a logical key inside the project, and the product's reserved
/// <c>environments/</c> layout is refused for every verb before a request is sent.
/// </summary>
public sealed class LambdaStorage
{
    /// <summary>The gateway's bound on one object body; the store's own configured cap still applies on top.</summary>
    public const int MaximumObjectBytes = 32 * 1024 * 1024;

    /// <summary>The page size the gateway uses when <see cref="ListAsync"/> names none.</summary>
    public const int DefaultListPageSize = 100;

    /// <summary>The largest page the gateway returns; a larger <c>limit</c> is refused here rather than silently clamped there.</summary>
    public const int MaximumListPageSize = 500;

    private readonly HttpClient client;

    internal LambdaStorage(HttpClient client)
    {
        this.client = client;
    }

    /// <summary>
    /// Writes one object. The mediated store is content-addressed and create-if-absent: the key
    /// is created when it does not exist (201), and a retry of this same invocation's write of the
    /// same bytes is acknowledged as the existing object (200). Any other write to an existing key
    /// — different bytes, or the same bytes from a different invocation — is refused with a
    /// <see cref="LambdaApiException"/> of status 409 and code
    /// <see cref="LambdaRefusalCodes.ObjectConflict"/> rather than overwriting it; to replace an
    /// object, use <see cref="ReplaceAsync"/>.
    /// </summary>
    /// <param name="key">The logical key inside the project; see <see cref="LambdaRoutes.StorageKey"/> for what one may be.</param>
    /// <param name="content">One to 32 MiB of bytes; the gateway requires a declared, positive length.</param>
    /// <param name="contentType">The media type recorded with the object, without parameters.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task PutAsync(
        string key,
        ReadOnlyMemory<byte> content,
        string contentType = "application/octet-stream",
        CancellationToken cancellationToken = default)
    {
        var route = WriteRoute(key, content, contentType);
        await WriteAsync(route, content, contentType, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces one object: <see cref="DeleteAsync"/>, then <see cref="PutAsync"/>. The store has
    /// no overwrite, so this is two operations, not one; another writer creating the key between
    /// them is reported by the write as <see cref="LambdaRefusalCodes.ObjectConflict"/>.
    /// </summary>
    /// <param name="key">The logical key inside the project.</param>
    /// <param name="content">One to 32 MiB of bytes.</param>
    /// <param name="contentType">The media type recorded with the object, without parameters.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task ReplaceAsync(
        string key,
        ReadOnlyMemory<byte> content,
        string contentType = "application/octet-stream",
        CancellationToken cancellationToken = default)
    {
        var route = WriteRoute(key, content, contentType);
        using (var delete = new HttpRequestMessage(HttpMethod.Delete, route))
        using (await LambdaTransport.SendAsync(client, delete, cancellationToken).ConfigureAwait(false))
        {
        }

        await WriteAsync(route, content, contentType, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads one object's bytes.</summary>
    public async Task<byte[]> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ObjectRoute(key));
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
        using var request = new HttpRequestMessage(HttpMethod.Get, ObjectRoute(key));
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

    /// <summary>
    /// Deletes one object. Idempotent: the gateway answers 204 whether or not the key existed, so a
    /// retried invocation's delete succeeds exactly as its first attempt did, and there is no
    /// "not found" to handle.
    /// </summary>
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, ObjectRoute(key));
        using var response = await LambdaTransport.SendAsync(client, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Lists one page of the project's object keys under an optional prefix, in ordinal order.
    /// The gateway pages by <see cref="DefaultListPageSize"/> unless <paramref name="limit"/>
    /// names a size up to <see cref="MaximumListPageSize"/>; continue with the page's
    /// <see cref="LambdaObjectPage.NextCursor"/> until it is <see langword="null"/>, or let
    /// <see cref="ListAllAsync"/> do that. The listing only ever sees this project's own namespace,
    /// and the reserved <c>environments/</c> layout is neither listable as a prefix nor present on
    /// any page.
    /// </summary>
    /// <param name="prefix">A logical prefix, or <see langword="null"/> for the whole project namespace; see <see cref="LambdaRoutes.StoragePrefix"/>.</param>
    /// <param name="cursor">The previous page's <see cref="LambdaObjectPage.NextCursor"/>, or <see langword="null"/> for the first page.</param>
    /// <param name="limit">Keys per page, 1 to <see cref="MaximumListPageSize"/>, or <see langword="null"/> for the gateway's default.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<LambdaObjectPage> ListAsync(
        string? prefix = null,
        string? cursor = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var validatedPrefix = LambdaRoutes.StoragePrefix(prefix, nameof(prefix));
        if (limit is < 1 or > MaximumListPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                limit,
                "A listing page is between 1 and 500 keys.");
        }
        if (cursor is { Length: > LambdaRoutes.MaximumStorageCursorLength } || HasControlCharacter(cursor))
        {
            throw new ArgumentException(
                "A listing cursor is the previous page's NextCursor, unchanged.",
                nameof(cursor));
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            LambdaRoutes.StorageObjectListing(validatedPrefix, string.IsNullOrEmpty(cursor) ? null : cursor, limit));
        var response = await LambdaTransport.SendAsync(client, request, cancellationToken)
            .ConfigureAwait(false);
        var document = await LambdaTransport.ReadJsonAsync<JsonElement>(response, cancellationToken)
            .ConfigureAwait(false);
        return ReadPage(document);
    }

    /// <summary>
    /// Lists every object key under an optional prefix by walking <see cref="ListAsync"/> to the
    /// last page. Convenient for a bounded namespace; for a large one, page with
    /// <see cref="ListAsync"/> instead so the whole listing is never held at once.
    /// </summary>
    public async Task<IReadOnlyList<string>> ListAllAsync(
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        var keys = new List<string>();
        string? cursor = null;
        while (true)
        {
            var page = await ListAsync(prefix, cursor, MaximumListPageSize, cancellationToken).ConfigureAwait(false);
            keys.AddRange(page.Keys);
            if (page.NextCursor is null)
            {
                return keys;
            }
            if (string.Equals(page.NextCursor, cursor, StringComparison.Ordinal))
            {
                throw new LambdaApiException(
                    502,
                    LambdaRefusalCodes.ResponseInvalid,
                    "The Yogabase Lambda runtime API repeated a listing cursor, so the listing cannot advance.");
            }

            cursor = page.NextCursor;
        }
    }

    private static string ObjectRoute(string key) =>
        LambdaRoutes.StorageObject(LambdaRoutes.StorageKey(key, nameof(key)));

    private static string WriteRoute(string key, ReadOnlyMemory<byte> content, string contentType)
    {
        var route = ObjectRoute(key);
        if (string.IsNullOrWhiteSpace(contentType)
            || contentType.Length > 128
            || contentType.Contains(';')
            || HasControlCharacter(contentType))
        {
            // The gateway records the media type as given and refuses a parameterised one
            // (content_type_not_allowed), so a charset would only carry the refusal to the wire.
            throw new ArgumentException("A bounded media type without parameters is required.", nameof(contentType));
        }
        if (content.Length is 0 or > MaximumObjectBytes)
        {
            throw new ArgumentException("An object is between one byte and 32 MiB.", nameof(content));
        }

        return route;
    }

    private async Task WriteAsync(
        string route,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, route);
        var body = new ByteArrayContent(content.ToArray());
        body.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        request.Content = body;
        using var response = await LambdaTransport.SendAsync(client, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the gateway's page shape, <c>{ "objects": [{ "key": "…" }], "nextCursor": "…" | null }</c>,
    /// and nothing looser: an entry without a string key, or a cursor that is not a string, is an
    /// unusable response rather than an empty listing.
    /// </summary>
    private static LambdaObjectPage ReadPage(JsonElement document)
    {
        if (document.ValueKind != JsonValueKind.Object
            || !document.TryGetProperty("objects", out var objects)
            || objects.ValueKind != JsonValueKind.Array)
        {
            throw Unusable("an object listing without an objects array");
        }

        var keys = new List<string>();
        foreach (var entry in objects.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object
                || !entry.TryGetProperty("key", out var key)
                || key.ValueKind != JsonValueKind.String)
            {
                throw Unusable("an object listing entry without a key");
            }

            keys.Add(key.GetString()!);
        }

        string? nextCursor = null;
        if (document.TryGetProperty("nextCursor", out var cursor) && cursor.ValueKind != JsonValueKind.Null)
        {
            if (cursor.ValueKind != JsonValueKind.String)
            {
                throw Unusable("an object listing with a cursor that is not a string");
            }

            nextCursor = cursor.GetString();
            if (string.IsNullOrEmpty(nextCursor))
            {
                nextCursor = null;
            }
        }

        return new LambdaObjectPage(keys, nextCursor);
    }

    private static LambdaApiException Unusable(string what) => new(
        502,
        LambdaRefusalCodes.ResponseInvalid,
        "The Yogabase Lambda runtime API returned " + what + ".");

    private static bool HasControlCharacter(string? value)
    {
        if (value is null)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (char.IsControl(character))
            {
                return true;
            }
        }

        return false;
    }
}
