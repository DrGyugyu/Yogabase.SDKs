using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Yogabase;

internal sealed class YogabaseStorageClient : IYogabaseStorage
{
    private readonly YogabaseClientConfiguration configuration;
    private readonly YogabaseHttpTransport transport;

    internal YogabaseStorageClient(YogabaseClientConfiguration configuration, YogabaseHttpTransport transport)
    {
        this.configuration = configuration;
        this.transport = transport;
    }

    public async Task<YogabaseObjectWriteResult> PutAsync(
        string key,
        Stream content,
        long contentLength,
        string contentType,
        string sha256Digest,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "storage");
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }
        if (!content.CanRead || contentLength < 1)
        {
            throw new ArgumentException("Storage uploads require a readable, non-empty bounded stream.", nameof(content));
        }
        ValidateContentType(contentType);
        ValidateDigest(sha256Digest);
        ValidateIdempotencyKey(idempotencyKey);

        var path = $"v1/storage/objects/{EscapeLogicalKey(key)}";
        var response = await transport.SendAsync(
            HttpMethod.Put,
            path,
            body: null,
            authenticated: true,
            cancellationToken,
            configure: request =>
            {
                request.Content = new StreamContent(new NonDisposingReadStream(content));
                request.Content.Headers.ContentLength = contentLength;
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                request.Headers.TryAddWithoutValidation("X-Yogabase-Content-Sha256", sha256Digest);
                request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
            }).ConfigureAwait(false);
        var result = await YogabaseHttpTransport.ReadJsonAsync<YogabaseObjectWriteResult>(response, cancellationToken)
            .ConfigureAwait(false);
        if (!string.Equals(result.Key, key, StringComparison.Ordinal)
            || !string.Equals(result.Digest, sha256Digest, StringComparison.Ordinal)
            || result.SizeBytes != contentLength
            || string.IsNullOrWhiteSpace(result.Encryption)
            || string.Equals(result.Encryption, "none", StringComparison.OrdinalIgnoreCase))
        {
            throw new YogabaseApiException(502, "storage_receipt_invalid", "Yogabase returned an invalid storage receipt.");
        }
        return result;
    }

    public Task<YogabaseObjectWriteResult> PutBytesAsync(
        string key,
        byte[] content,
        string contentType,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (content is null || content.Length == 0)
        {
            throw new ArgumentException("Storage uploads require non-empty content.", nameof(content));
        }
        string digest;
        using (var sha = SHA256.Create())
        {
            digest = "sha256:" + ToLowerHex(sha.ComputeHash(content));
        }

        return PutAsync(
            key,
            new MemoryStream(content, writable: false),
            content.Length,
            contentType,
            digest,
            idempotencyKey,
            cancellationToken);
    }

    public async Task<YogabaseObjectRead> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "storage");
        var path = $"v1/storage/objects/{EscapeLogicalKey(key)}";
        var response = await transport.SendAsync(
            HttpMethod.Get,
            path,
            body: null,
            authenticated: true,
            cancellationToken,
            HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        try
        {
            var stream = await YogabaseHttpContent.ReadAsStreamAsync(response.Content, cancellationToken)
                .ConfigureAwait(false);
            return new YogabaseObjectRead(
                stream,
                response.Content.Headers.ContentType?.MediaType,
                response.Content.Headers.ContentLength,
                response);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    private static string EscapeLogicalKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)
            || key.Length > 1024
            || key.StartsWith("/", StringComparison.Ordinal)
            || key.StartsWith("projects/", StringComparison.Ordinal)
            || key.Contains("://", StringComparison.Ordinal)
            || key.Contains('\\', StringComparison.Ordinal)
            || key.Contains('%', StringComparison.Ordinal)
            || key.Contains('\0', StringComparison.Ordinal)
            || key.Split('/').Any(segment => segment is "" or "." or ".."))
        {
            throw new ArgumentException("A bounded project-local logical object key is required.", nameof(key));
        }

        return string.Join("/", key.Split('/').Select(Uri.EscapeDataString));
    }

    private static void ValidateDigest(string value)
    {
        if (value is null
            || value.Length != 71
            || !value.StartsWith("sha256:", StringComparison.Ordinal)
            || value.Substring(7).Any(character => !(character is >= '0' and <= '9' or >= 'a' and <= 'f')))
        {
            throw new ArgumentException("A canonical lowercase sha256: digest is required.", nameof(value));
        }
    }

    private static void ValidateContentType(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > 128
            || value.Contains(";", StringComparison.Ordinal)
            || value.Any(char.IsControl))
        {
            throw new ArgumentException("A bounded media type without parameters is required.", nameof(value));
        }
    }

    private static void ValidateIdempotencyKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > 128
            || !string.Equals(value, value.Trim(), StringComparison.Ordinal)
            || value.Any(character => character is < '!' or > '~'))
        {
            throw new ArgumentException("A bounded printable ASCII idempotency key is required.", nameof(value));
        }
    }

    private static string ToLowerHex(byte[] bytes)
    {
        var chars = new char[bytes.Length * 2];
        const string alphabet = "0123456789abcdef";
        for (var index = 0; index < bytes.Length; index++)
        {
            chars[index * 2] = alphabet[bytes[index] >> 4];
            chars[index * 2 + 1] = alphabet[bytes[index] & 0x0f];
        }
        return new string(chars);
    }

    private sealed class NonDisposingReadStream : Stream
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
}
