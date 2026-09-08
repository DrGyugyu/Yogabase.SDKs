using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Yogabase;

internal sealed class YogabaseActorsClient : IYogabaseActors
{
    private readonly YogabaseClientConfiguration configuration;
    private readonly YogabaseHttpTransport transport;

    internal YogabaseActorsClient(YogabaseClientConfiguration configuration, YogabaseHttpTransport transport)
    {
        this.configuration = configuration;
        this.transport = transport;
    }

    public async Task<YogabaseActorInvocationResult> InvokeAsync(
        string functionId,
        string functionVersion,
        string instanceKey,
        YogabaseActorInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "functions-rest");
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        ExtendedLambdaValidation.ValidateRouteValue(functionId, 256, nameof(functionId));
        ExtendedLambdaValidation.ValidateRouteValue(functionVersion, 64, nameof(functionVersion));
        ExtendedLambdaValidation.ValidateInstanceKey(instanceKey, nameof(instanceKey));
        ExtendedLambdaValidation.ValidateBoundedValue(request.IdempotencyKey, 256, nameof(request.IdempotencyKey));
        ExtendedLambdaValidation.ValidateBoundedValue(request.CorrelationId, 128, nameof(request.CorrelationId));
        var now = DateTimeOffset.UtcNow;
        if (request.Deadline <= now || request.Deadline - now > TimeSpan.FromSeconds(30))
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Actor deadlines must be in the future and within 30 seconds.");
        }

        var payload = request.InlinePayload
            ?? throw new ArgumentException("An inline actor payload is required.", nameof(request));
        var digest = ExtendedLambdaValidation.ComputeAndValidateDigest(payload, request.PayloadDigest, nameof(request));
        var path = YogabaseRoutes.ActorInvoke(
            configuration.ProjectId,
            YogabaseHttpTransport.EscapePath(functionId, nameof(functionId)),
            YogabaseHttpTransport.EscapePath(functionVersion, nameof(functionVersion)),
            Uri.EscapeDataString(instanceKey));
        var response = await transport.SendAsync(
            HttpMethod.Post,
            path,
            new
            {
                request.IdempotencyKey,
                request.CorrelationId,
                request.Deadline,
                PayloadDigest = digest,
                InlinePayloadBase64 = Convert.ToBase64String(payload),
            },
            authenticated: true,
            cancellationToken,
            ensureSuccess: false).ConfigureAwait(false);
        var responseStatus = response.StatusCode;
        if (!IsActorResultStatus(responseStatus)
            || string.Equals(
                response.Content.Headers.ContentType?.MediaType,
                "application/problem+json",
                StringComparison.OrdinalIgnoreCase))
        {
            await YogabaseHttpTransport.ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
        }

        var wire = await YogabaseHttpTransport.ReadJsonAsync<ActorResponse>(response, cancellationToken)
            .ConfigureAwait(false);
        if (!Enum.TryParse(wire.Status, ignoreCase: false, out YogabaseActorInvocationStatus status)
            || !Guid.TryParseExact(wire.InvocationId, "D", out var invocationId)
            || !string.Equals(invocationId.ToString("D"), wire.InvocationId, StringComparison.Ordinal)
            || !string.Equals(wire.CorrelationId, request.CorrelationId, StringComparison.Ordinal)
            || wire.StateVersion < 0
            || wire.ResultReference is not null && !ExtendedLambdaValidation.IsStrictObjectReference(wire.ResultReference)
            || wire.FailureCode is not null && !ExtendedLambdaValidation.IsFailureCode(wire.FailureCode)
            || !StatusMatches(responseStatus, status, wire.FailureCode))
        {
            throw new YogabaseApiException(502, "actor_response_invalid", "Yogabase returned an invalid Actor Lambda result.");
        }

        return new YogabaseActorInvocationResult
        {
            Status = status,
            InvocationId = invocationId,
            CorrelationId = wire.CorrelationId ?? string.Empty,
            ResultReference = wire.ResultReference,
            FailureCode = wire.FailureCode,
            StateVersion = wire.StateVersion,
        };
    }

    private static bool IsActorResultStatus(HttpStatusCode status) =>
        status is HttpStatusCode.OK or HttpStatusCode.RequestTimeout or HttpStatusCode.Conflict
        || (int)status == 422;

    private static bool StatusMatches(
        HttpStatusCode httpStatus,
        YogabaseActorInvocationStatus status,
        string? failureCode) => httpStatus switch
        {
            HttpStatusCode.OK => status == YogabaseActorInvocationStatus.Completed,
            HttpStatusCode.Conflict => status == YogabaseActorInvocationStatus.InProgress
                || string.Equals(failureCode, "invocation_in_progress", StringComparison.Ordinal),
            HttpStatusCode.RequestTimeout => status == YogabaseActorInvocationStatus.Failed
                && string.Equals(failureCode, "deadline_exceeded", StringComparison.Ordinal),
            _ when (int)httpStatus == 422 => status == YogabaseActorInvocationStatus.Failed,
            _ => false,
        };

    private sealed class ActorResponse
    {
        public string? Status { get; set; }
        public string? InvocationId { get; set; }
        public string? CorrelationId { get; set; }
        public string? ResultReference { get; set; }
        public string? FailureCode { get; set; }
        public long StateVersion { get; set; }
    }
}

internal sealed class YogabaseWorkflowsClient : IYogabaseWorkflows
{
    private readonly YogabaseClientConfiguration configuration;
    private readonly YogabaseHttpTransport transport;

    internal YogabaseWorkflowsClient(YogabaseClientConfiguration configuration, YogabaseHttpTransport transport)
    {
        this.configuration = configuration;
        this.transport = transport;
    }

    public async Task<YogabaseWorkflowStartResult> StartAsync(
        string workflowId,
        string definitionVersion,
        YogabaseWorkflowStartRequest request,
        CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "functions-rest");
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        ExtendedLambdaValidation.ValidateRouteValue(workflowId, 256, nameof(workflowId));
        ExtendedLambdaValidation.ValidateDefinitionVersion(definitionVersion);
        ExtendedLambdaValidation.ValidateBoundedValue(request.IdempotencyKey, 256, nameof(request.IdempotencyKey));
        var input = request.InlineInput
            ?? throw new ArgumentException("An inline workflow input is required.", nameof(request));
        var digest = ExtendedLambdaValidation.ComputeAndValidateDigest(input, request.PayloadDigest, nameof(request));
        var path = YogabaseRoutes.WorkflowStart(
            configuration.ProjectId,
            YogabaseHttpTransport.EscapePath(workflowId, nameof(workflowId)),
            Uri.EscapeDataString(definitionVersion));
        var response = await transport.SendAsync(
            HttpMethod.Post,
            path,
            new
            {
                request.IdempotencyKey,
                PayloadDigest = digest,
                InlineInputBase64 = Convert.ToBase64String(input),
            },
            authenticated: true,
            cancellationToken).ConfigureAwait(false);
        var wire = await YogabaseHttpTransport.ReadJsonAsync<WorkflowStartResponse>(response, cancellationToken)
            .ConfigureAwait(false);
        if (!Guid.TryParseExact(wire.WorkflowInstanceId, "D", out var workflowInstanceId)
            || !string.Equals(workflowInstanceId.ToString("D"), wire.WorkflowInstanceId, StringComparison.Ordinal))
        {
            throw new YogabaseApiException(502, "workflow_response_invalid", "Yogabase returned an invalid Workflow Lambda result.");
        }

        return new YogabaseWorkflowStartResult { WorkflowInstanceId = workflowInstanceId };
    }

    public async Task CancelAsync(
        Guid workflowInstanceId,
        YogabaseWorkflowCancelRequest request,
        CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "functions-rest");
        if (workflowInstanceId == Guid.Empty)
        {
            throw new ArgumentException("A non-empty workflow instance id is required.", nameof(workflowInstanceId));
        }
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        ExtendedLambdaValidation.ValidateBoundedValue(request.IdempotencyKey, 256, nameof(request.IdempotencyKey));
        var payload = request.InlinePayload
            ?? throw new ArgumentException("An inline workflow cancellation payload is required.", nameof(request));
        var digest = ExtendedLambdaValidation.ComputeAndValidateDigest(payload, request.PayloadDigest, nameof(request));
        var path = YogabaseRoutes.WorkflowCancel(
            configuration.ProjectId,
            workflowInstanceId.ToString("D", System.Globalization.CultureInfo.InvariantCulture));
        using var response = await transport.SendAsync(
            HttpMethod.Post,
            path,
            new
            {
                request.IdempotencyKey,
                PayloadDigest = digest,
                InlinePayloadBase64 = Convert.ToBase64String(payload),
            },
            authenticated: true,
            cancellationToken).ConfigureAwait(false);
    }

    private sealed class WorkflowStartResponse
    {
        public string? WorkflowInstanceId { get; set; }
    }
}

internal static class ExtendedLambdaValidation
{
    internal const int MaximumInlinePayloadBytes = 480 * 1024;

    internal static void ValidateBoundedValue(string value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > maximumLength
            || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("A bounded non-empty value is required.", parameterName);
        }
    }

    internal static void ValidateRouteValue(string value, int maximumLength, string parameterName)
    {
        ValidateBoundedValue(value, maximumLength, parameterName);
        if (value.Any(char.IsControl))
        {
            throw new ArgumentException("Route values cannot contain control characters.", parameterName);
        }
    }

    internal static void ValidateInstanceKey(string value, string parameterName)
    {
        ValidateRouteValue(value, 256, parameterName);
        if (value.Any(static character => character is '/' or '\\' or '?' or '#' or '%'))
        {
            throw new ArgumentException("Actor instance keys cannot contain route delimiters.", parameterName);
        }
    }

    internal static void ValidateDefinitionVersion(string value)
    {
        if (string.IsNullOrEmpty(value)
            || value.Length > 64
            || value[0] is < '1' or > '9'
            || value.Skip(1).Any(static character => character is < '0' or > '9'))
        {
            throw new ArgumentException("Workflow definition versions are positive decimal strings up to 64 characters.", nameof(value));
        }
    }

    internal static string ComputeAndValidateDigest(byte[] payload, string suppliedDigest, string parameterName)
    {
        if (payload.Length > MaximumInlinePayloadBytes)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Inline Actor and Workflow payloads are limited to 480 KiB.");
        }

        string computed;
        using (var sha = SHA256.Create())
        {
            computed = "sha256:" + ToLowerHex(sha.ComputeHash(payload));
        }
        if (!string.IsNullOrEmpty(suppliedDigest)
            && !string.Equals(suppliedDigest, computed, StringComparison.Ordinal))
        {
            throw new ArgumentException("The inline payload does not match PayloadDigest.", parameterName);
        }

        return computed;
    }

    internal static bool IsFailureCode(string value) =>
        value.Length is >= 1 and <= 128
        && value.All(static character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_');

    internal static bool IsStrictObjectReference(string value)
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

        return value.Substring(prefix.Length)
            .Split('/')
            .All(static segment => segment.Length > 0 && segment is not "." and not "..");
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
}
