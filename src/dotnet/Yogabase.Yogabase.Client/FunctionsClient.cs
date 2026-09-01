using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Yogabase;

internal sealed class YogabaseFunctionsClient : IYogabaseFunctions
{
    private const int MaximumInlinePayloadBytes = 1_048_576;
    private readonly YogabaseClientConfiguration configuration;
    private readonly YogabaseHttpTransport transport;

    internal YogabaseFunctionsClient(YogabaseClientConfiguration configuration, YogabaseHttpTransport transport)
    {
        this.configuration = configuration;
        this.transport = transport;
    }

    public async Task<YogabaseInvocationResult> InvokeAsync(
        string functionId,
        string functionVersion,
        YogabaseInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "functions-rest");
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }
        var hasInline = request.InlinePayload is not null;
        var hasReference = request.ObjectReference is not null;
        if (hasInline == hasReference)
        {
            throw new ArgumentException("Exactly one inline payload or immutable object reference is required.", nameof(request));
        }
        var now = DateTimeOffset.UtcNow;
        var maximumDeadline = request.Mode switch
        {
            YogabaseInvocationMode.Synchronous => TimeSpan.FromSeconds(30),
            YogabaseInvocationMode.Asynchronous => TimeSpan.FromMinutes(60),
            _ => throw new ArgumentOutOfRangeException(nameof(request), "The invocation mode is not supported."),
        };
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey)
            || request.IdempotencyKey.Length > 256
            || !string.Equals(request.IdempotencyKey, request.IdempotencyKey.Trim(), StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(request.CorrelationId)
            || request.CorrelationId.Length > 128
            || request.Deadline <= now
            || request.Deadline - now > maximumDeadline)
        {
            throw new ArgumentException("Invocation identity and a future deadline are required.", nameof(request));
        }

        var digest = request.PayloadDigest;
        string? inlineBase64 = null;
        if (hasInline)
        {
            if (request.InlinePayload!.Length > MaximumInlinePayloadBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "Inline payloads are limited to one MiB.");
            }
            using var sha = SHA256.Create();
            var computed = "sha256:" + ToLowerHex(sha.ComputeHash(request.InlinePayload));
            if (!string.IsNullOrEmpty(digest) && !string.Equals(digest, computed, StringComparison.Ordinal))
            {
                throw new ArgumentException("The inline payload does not match PayloadDigest.", nameof(request));
            }
            digest = computed;
            inlineBase64 = Convert.ToBase64String(request.InlinePayload);
        }
        else
        {
            if (request.ObjectReference is null
                || !IsStrictObjectReference(request.ObjectReference.Uri)
                || string.IsNullOrWhiteSpace(request.ObjectReference.Version)
                || request.ObjectReference.Version.Length > 256
                || !string.Equals(
                    request.ObjectReference.Version,
                    request.ObjectReference.Version.Trim(),
                    StringComparison.Ordinal)
                || request.ObjectReference.Version.Any(char.IsControl))
            {
                throw new ArgumentException("A credential-free immutable object:// reference is required.", nameof(request));
            }
            ValidateDigest(digest);
        }

        if (string.IsNullOrWhiteSpace(functionId)
            || functionId.Length > 256
            || string.IsNullOrWhiteSpace(functionVersion)
            || functionVersion.Length > 64)
        {
            throw new ArgumentException("Function identity is outside the gateway limits.");
        }

        var path = $"v1/projects/{configuration.ProjectId:D}/functions/"
            + $"{YogabaseHttpTransport.EscapePath(functionId, nameof(functionId))}/versions/"
            + $"{YogabaseHttpTransport.EscapePath(functionVersion, nameof(functionVersion))}/invoke";
        var body = new
        {
            request.IdempotencyKey,
            request.CorrelationId,
            request.Deadline,
            request.Mode,
            PayloadDigest = digest,
            InlinePayloadBase64 = inlineBase64,
            request.ObjectReference,
        };
        var response = await transport.SendAsync(
            HttpMethod.Post,
            path,
            body,
            authenticated: true,
            cancellationToken,
            ensureSuccess: false).ConfigureAwait(false);
        var responseStatus = response.StatusCode;
        if (!IsInvocationResultStatus(responseStatus)
            || string.Equals(
                response.Content.Headers.ContentType?.MediaType,
                "application/problem+json",
                StringComparison.OrdinalIgnoreCase))
        {
            await YogabaseHttpTransport.ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
        }
        var wire = await YogabaseHttpTransport.ReadJsonAsync<InvocationResponse>(response, cancellationToken)
            .ConfigureAwait(false);
#if NET10_0_OR_GREATER
        var isDefinedOutcome = Enum.IsDefined(wire.Outcome);
#else
        var isDefinedOutcome = Enum.IsDefined(typeof(YogabaseInvocationOutcome), wire.Outcome);
#endif
        if (!isDefinedOutcome
            || !Guid.TryParseExact(wire.InvocationId, "D", out var invocationId)
            || !string.Equals(invocationId.ToString("D"), wire.InvocationId, StringComparison.Ordinal)
            || !string.Equals(wire.CorrelationId, request.CorrelationId, StringComparison.Ordinal)
            || wire.InlineResultBase64 is not null && wire.ResultReference is not null
            || wire.ResultReference is not null && !IsStrictObjectReference(wire.ResultReference)
            || wire.FailureCode is not null && !IsFailureCode(wire.FailureCode)
            || !StatusMatchesOutcome(responseStatus, wire.Outcome))
        {
            throw new YogabaseApiException(502, "invocation_response_invalid", "Yogabase returned an invalid invocation result.");
        }
        byte[]? inlineResult = null;
        if (wire.InlineResultBase64 is not null)
        {
            var maximumBase64Length = ((MaximumInlinePayloadBytes + 2) / 3) * 4;
            if (wire.InlineResultBase64.Length > maximumBase64Length)
            {
                throw new YogabaseApiException(502, "invocation_response_invalid", "Yogabase returned an oversized inline result.");
            }
            try
            {
                inlineResult = Convert.FromBase64String(wire.InlineResultBase64);
            }
            catch (FormatException)
            {
                throw new YogabaseApiException(502, "invocation_response_invalid", "Yogabase returned invalid inline result data.");
            }
            if (inlineResult.Length > MaximumInlinePayloadBytes)
            {
                throw new YogabaseApiException(502, "invocation_response_invalid", "Yogabase returned an oversized inline result.");
            }
        }

        return new YogabaseInvocationResult
        {
            Outcome = wire.Outcome,
            InvocationId = wire.InvocationId ?? string.Empty,
            CorrelationId = wire.CorrelationId ?? string.Empty,
            InlineResult = inlineResult,
            ResultReference = wire.ResultReference,
            FailureCode = wire.FailureCode,
        };
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

    private static bool IsStrictObjectReference(string? value)
    {
        const string prefix = "object://";
        if (string.IsNullOrEmpty(value)
            || value.Length > 2_048
            || !value.StartsWith(prefix, StringComparison.Ordinal)
            || value.Length == prefix.Length
            || value.Contains('%')
            || value.Contains('?')
            || value.Contains('#')
            || value.Contains('@')
            || value.Contains('\\')
            || value.IndexOf(':', prefix.Length) >= 0
            || value.Any(char.IsWhiteSpace))
        {
            return false;
        }

        return value[prefix.Length..]
            .Split('/')
            .All(static segment => segment.Length > 0 && segment is not "." and not "..");
    }

    private static bool IsFailureCode(string value) =>
        value.Length is >= 1 and <= 128
        && value.All(static character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_');

    private static bool StatusMatchesOutcome(HttpStatusCode status, YogabaseInvocationOutcome outcome) =>
        outcome switch
        {
            YogabaseInvocationOutcome.Accepted or YogabaseInvocationOutcome.Replayed => status == HttpStatusCode.Accepted,
            YogabaseInvocationOutcome.Completed => status == HttpStatusCode.OK,
            YogabaseInvocationOutcome.Conflict => status == HttpStatusCode.Conflict,
            YogabaseInvocationOutcome.FunctionNotFound or YogabaseInvocationOutcome.ArtifactNotReady =>
                status == HttpStatusCode.NotFound,
            YogabaseInvocationOutcome.DeadlineExceeded => status == HttpStatusCode.RequestTimeout,
            YogabaseInvocationOutcome.Failed => (int)status == 422,
            YogabaseInvocationOutcome.Unavailable => status == HttpStatusCode.ServiceUnavailable,
            _ => false,
        };

    private static bool IsInvocationResultStatus(HttpStatusCode status) =>
        status is HttpStatusCode.OK
            or HttpStatusCode.Accepted
            or HttpStatusCode.NotFound
            or HttpStatusCode.RequestTimeout
            or HttpStatusCode.Conflict
            or HttpStatusCode.ServiceUnavailable
            or (HttpStatusCode)422;

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

    private sealed class InvocationResponse
    {
        public YogabaseInvocationOutcome Outcome { get; set; }
        public string? InvocationId { get; set; }
        public string? CorrelationId { get; set; }
        public string? InlineResultBase64 { get; set; }
        public string? ResultReference { get; set; }
        public string? FailureCode { get; set; }
    }
}
