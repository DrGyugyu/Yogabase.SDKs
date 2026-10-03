using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Yogabase;

/// <summary>
/// Where a durable job is in its lifecycle. A job is <see cref="Queued"/>, then <see cref="Running"/>,
/// then settles as <see cref="Succeeded"/>, <see cref="Cancelled"/> or <see cref="DeadLettered"/>.
/// <see cref="Failed"/> is a checkpoint a job passes through on the way to a retry
/// (<see cref="Queued"/> again) or to <see cref="DeadLettered"/>; it is not a state a job rests in, so
/// it is never terminal.
/// </summary>
public enum YogabaseJobStatus
{
    /// <summary>Waiting for a worker to claim it, or for its retry delay to pass.</summary>
    Queued = 0,

    /// <summary>A worker holds the job and its function is running.</summary>
    Running = 1,

    /// <summary>The function answered. The job has a result unless the function returned none.</summary>
    Succeeded = 2,

    /// <summary>An attempt failed and the job has not yet been requeued or dead-lettered.</summary>
    Failed = 3,

    /// <summary>The job was cancelled and will not run (again).</summary>
    Cancelled = 4,

    /// <summary>
    /// Every attempt failed, or the job could not be run at all. <see cref="YogabaseJob.FailureCode"/>
    /// says why.
    /// </summary>
    DeadLettered = 5,
}

/// <summary>
/// What a client asks the Jobs submit route to start: one durable run of a deployed stateless function.
/// The project and the subject are never fields: the project is the client's own and the subject is the
/// signed-in user.
/// </summary>
public sealed class YogabaseJobSubmitRequest
{
    /// <summary>
    /// The job type, which is the id of a deployed stateless function; its highest released version runs
    /// the job. 1 to 128 ASCII letters, digits, <c>.</c>, <c>-</c>, <c>_</c> or <c>:</c>, with no
    /// surrounding whitespace.
    /// </summary>
    public string JobType { get; set; } = string.Empty;

    /// <summary>
    /// The caller's key for this request, 1 to 256 characters, trimmed, with no control character. The
    /// same request under the same key is the job it already made (retrying a submit is safe, and
    /// answers the same job whatever it has become); a different request under that key is refused
    /// with <see cref="YogabaseJobErrors.IdempotencyConflict"/>. The key belongs to the signed-in user
    /// who used it, so another user's identical key makes that user's own job. Keep the key across
    /// retries of one submit and use a new one for a new job.
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>
    /// The function's request, at most <see cref="YogabaseJobsLimits.MaximumPayloadBytes"/> bytes, or
    /// <see langword="null"/> for a job that carries none. It is sent as base64 and is never part of
    /// what the gateway reads back.
    /// </summary>
    public byte[]? Payload { get; set; }

    /// <summary>
    /// How many times the job may be attempted, from 1 to
    /// <see cref="YogabaseJobsLimits.MaximumAttempts"/>, or <see langword="null"/> for the gateway's
    /// default of 5. A function's failure that asks for a retry is retried with backoff until this many
    /// attempts have been made, and the job is then dead-lettered.
    /// </summary>
    public int? MaxAttempts { get; set; }
}

/// <summary>
/// One durable job as the gateway reads it back: its state, how far it has got and how it ended. The
/// payload never travels back, and a result is read separately
/// (<see cref="IYogabaseJobs.GetResultAsync"/>).
/// </summary>
public sealed class YogabaseJob
{
    /// <summary>The job's id, a UUID in its canonical form. Keep it: every other call names the job by it.</summary>
    public Guid JobId { get; set; }

    /// <summary>The id of the function the job runs.</summary>
    public string JobType { get; set; } = string.Empty;

    /// <summary>The job's state.</summary>
    public YogabaseJobStatus Status { get; set; }

    /// <summary>How many attempts have started. 0 while the job has never run.</summary>
    public int Attempt { get; set; }

    /// <summary>How many attempts the job may have in all, from 1 to <see cref="YogabaseJobsLimits.MaximumAttempts"/>.</summary>
    public int MaxAttempts { get; set; }

    /// <summary>How far the function says it has got, from 0 to 100.</summary>
    public int ProgressPercent { get; set; }

    /// <summary>
    /// True once a cancel has been asked for. A queued job is cancelled at once; a running one is stopped
    /// by its worker, so this can be true while <see cref="Status"/> is still <see cref="YogabaseJobStatus.Running"/>.
    /// </summary>
    public bool CancellationRequested { get; set; }

    /// <summary>
    /// A name for the stored result, or <see langword="null"/> while the job has none (a function that
    /// returns nothing leaves it <see langword="null"/>). The reference is a name and not a capability:
    /// read the bytes with <see cref="IYogabaseJobs.GetResultAsync"/>.
    /// </summary>
    public string? ResultReference { get; set; }

    /// <summary>
    /// Why the job last failed, as the function or the worker named it (lowercase letters, digits and
    /// underscores), or <see langword="null"/>. <c>job_duration_exceeded</c> is a function still running
    /// at the longest a job may run.
    /// </summary>
    public string? FailureCode { get; set; }

    /// <summary>When the job was accepted.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the job last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>When the job settled, or <see langword="null"/> while it has not.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// True once the job has settled and will not change again:
    /// <see cref="YogabaseJobStatus.Succeeded"/>, <see cref="YogabaseJobStatus.Cancelled"/> or
    /// <see cref="YogabaseJobStatus.DeadLettered"/>.
    /// </summary>
    public bool IsTerminal =>
        Status is YogabaseJobStatus.Succeeded or YogabaseJobStatus.Cancelled or YogabaseJobStatus.DeadLettered;
}

/// <summary>
/// The stored result of a job that succeeded with one: exactly the bytes the function returned, checked
/// against the digest the gateway sent with them before this was returned.
/// </summary>
public sealed class YogabaseJobResult
{
    /// <summary>The result bytes.</summary>
    public byte[] Content { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// The result's digest, <c>sha256:</c> and sixty-four lowercase hexadecimal digits, as the gateway sent
    /// it in <c>X-Yogabase-Content-Sha256</c> and as <see cref="Content"/> was hashed to here.
    /// </summary>
    public string Digest { get; set; } = string.Empty;
}

/// <summary>The bounds of the Jobs routes, which the client checks before it sends a request.</summary>
public static class YogabaseJobsLimits
{
    /// <summary>The most bytes a job's payload may have, before base64 encoding: the 1 MiB inline limit.</summary>
    public const int MaximumPayloadBytes = 1_048_576;

    /// <summary>The most attempts a job may be allowed.</summary>
    public const int MaximumAttempts = 10;

    /// <summary>The most characters a job type may have.</summary>
    public const int MaximumJobTypeLength = 128;

    /// <summary>The most characters an idempotency key may have.</summary>
    public const int MaximumIdempotencyKeyLength = 256;

    /// <summary>The most bytes a job's result may have: 16 MiB.</summary>
    public const int MaximumResultBytes = 16 * 1_048_576;
}

/// <summary>
/// The codes the Jobs routes refuse with, as <see cref="YogabaseApiException.Code"/> carries them. The
/// first group is the gateway's; the last two are this client's own, for an answer that is not what the
/// route declares.
/// </summary>
public static class YogabaseJobErrors
{
    /// <summary>422: the job type is not the id of a deployed function.</summary>
    public const string TypeNotRegistered = "job_type_not_registered";

    /// <summary>409: the idempotency key already belongs to a different request by this user.</summary>
    public const string IdempotencyConflict = "job_idempotency_conflict";

    /// <summary>
    /// 413: the payload is over 1 MiB before base64 encoding, or the request body is over the largest the
    /// route reads. <see cref="YogabaseApiException.LimitBytes"/> and
    /// <see cref="YogabaseApiException.MaxBodyBytes"/> name both limits.
    /// </summary>
    public const string PayloadTooLarge = "job_payload_too_large";

    /// <summary>400: a field is malformed, or the payload is not base64.</summary>
    public const string RequestInvalid = "job_request_invalid";

    /// <summary>
    /// 404: no such job, which is also another user's job (the two are never told apart). A
    /// <c>not_found</c> without this prefix is an installation that does not serve Jobs at all.
    /// </summary>
    public const string NotFound = "job_not_found";

    /// <summary>403: the token may not call the Jobs routes, or names another project.</summary>
    public const string AccessDenied = "job_access_denied";

    /// <summary>503: the job store could not be reached. Retrying later is safe.</summary>
    public const string Unavailable = "job_unavailable";

    /// <summary>
    /// 404: the job has no result to give. That is another user's job, an unknown one, and a job that
    /// finished without a result, which are never told apart.
    /// </summary>
    public const string ResultNotFound = "job_result_not_found";

    /// <summary>409: the job is the caller's own and has not settled, so it has no result yet.</summary>
    public const string ResultNotReady = "job_result_not_ready";

    /// <summary>502: the stored result is not what the job recorded. No byte of it was sent.</summary>
    public const string ResultIntegrityFailed = "job_result_integrity_failed";

    /// <summary>500: the job's stored result reference is not that job's own. No byte of it was sent.</summary>
    public const string ResultReferenceInvalid = "job_result_reference_invalid";

    /// <summary>This client: the gateway's answer to a Jobs call is not what the route declares.</summary>
    public const string ResponseInvalid = "job_response_invalid";

    /// <summary>This client: the result's bytes do not hash to the digest the gateway sent with them.</summary>
    public const string ResultDigestMismatch = "job_result_digest_mismatch";
}

/// <summary>
/// Durable background jobs for the signed-in user: submit a run of a deployed stateless function, read it,
/// cancel it and fetch what it returned. Every call is authenticated with the user's access token against
/// the application gateway, behind the project token's <c>lambda.invoke</c> scope, and a job belongs to
/// the user who submitted it: another user's job is answered exactly as one that does not exist.
/// </summary>
/// <remarks>
/// The bootstrap document carries no capability identifier for Jobs, so the client does not pretend to
/// know whether an installation serves them: an installation without them answers the gateway's own
/// <c>not_found</c> for the route (a missing job is <see cref="YogabaseJobErrors.NotFound"/>, a different
/// code). Jobs need the project's Authentication, as invoking a function does.
/// </remarks>
public interface IYogabaseJobs
{
    /// <summary>
    /// Starts a job and returns it as the gateway accepted it (202), normally
    /// <see cref="YogabaseJobStatus.Queued"/>. Idempotent per user and
    /// <see cref="YogabaseJobSubmitRequest.IdempotencyKey"/>: the same request again returns the job it
    /// already made, whatever that job has become.
    /// </summary>
    /// <exception cref="YogabaseApiException">
    /// <see cref="YogabaseJobErrors.TypeNotRegistered"/> (422), <see cref="YogabaseJobErrors.IdempotencyConflict"/>
    /// (409), <see cref="YogabaseJobErrors.PayloadTooLarge"/> (413), <see cref="YogabaseJobErrors.RequestInvalid"/> (400),
    /// <see cref="YogabaseJobErrors.AccessDenied"/> (403) or <see cref="YogabaseJobErrors.Unavailable"/> (503).
    /// </exception>
    Task<YogabaseJob> SubmitAsync(YogabaseJobSubmitRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the caller's own job. Poll this until <see cref="YogabaseJob.IsTerminal"/>.
    /// </summary>
    /// <exception cref="YogabaseApiException"><see cref="YogabaseJobErrors.NotFound"/> (404) for no such job and for another user's.</exception>
    Task<YogabaseJob> GetAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks for the caller's own job to stop, idempotently, and returns it: a queued job is cancelled at
    /// once, a running one is flagged (<see cref="YogabaseJob.CancellationRequested"/>) and stopped by its
    /// worker, and a settled one is returned as it is.
    /// </summary>
    /// <exception cref="YogabaseApiException"><see cref="YogabaseJobErrors.NotFound"/> (404) for no such job and for another user's.</exception>
    Task<YogabaseJob> CancelAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the result of the caller's own job. The bytes are hashed here and compared with the
    /// <c>X-Yogabase-Content-Sha256</c> the gateway sent; a result that does not match, or that arrives
    /// without a well-formed digest, is refused and never returned.
    /// </summary>
    /// <exception cref="YogabaseApiException">
    /// <see cref="YogabaseJobErrors.ResultNotReady"/> (409) while the job has not settled,
    /// <see cref="YogabaseJobErrors.ResultNotFound"/> (404) for no result to give,
    /// <see cref="YogabaseJobErrors.ResultIntegrityFailed"/> (502) or <see cref="YogabaseJobErrors.ResultReferenceInvalid"/>
    /// (500) from the gateway, and <see cref="YogabaseJobErrors.ResultDigestMismatch"/> or
    /// <see cref="YogabaseJobErrors.ResponseInvalid"/> (502) from this client.
    /// </exception>
    Task<YogabaseJobResult> GetResultAsync(Guid jobId, CancellationToken cancellationToken = default);
}

internal sealed class YogabaseJobsClient : IYogabaseJobs
{
    /// <summary>The response header that carries the SHA-256 the result's bytes were verified against.</summary>
    internal const string ResultDigestHeader = "X-Yogabase-Content-Sha256";

    private readonly YogabaseClientConfiguration configuration;
    private readonly YogabaseHttpTransport transport;

    internal YogabaseJobsClient(YogabaseClientConfiguration configuration, YogabaseHttpTransport transport)
    {
        this.configuration = configuration;
        this.transport = transport;
    }

    public async Task<YogabaseJob> SubmitAsync(
        YogabaseJobSubmitRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        ValidateJobType(request.JobType, nameof(request));
        ValidateIdempotencyKey(request.IdempotencyKey, nameof(request));
        if (request.Payload is { Length: > YogabaseJobsLimits.MaximumPayloadBytes })
        {
            throw new ArgumentOutOfRangeException(nameof(request), "A job payload is limited to one MiB before base64 encoding.");
        }
        if (request.MaxAttempts is < 1 or > YogabaseJobsLimits.MaximumAttempts)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "A job allows between 1 and 10 attempts.");
        }

        var response = await transport.SendAsync(
            HttpMethod.Post,
            YogabaseRoutes.Jobs(configuration.ProjectId),
            new SubmitBody
            {
                JobType = request.JobType,
                IdempotencyKey = request.IdempotencyKey,
                // A job with no payload and one with an empty payload are the same job to the gateway.
                PayloadBase64 = request.Payload is { Length: > 0 } ? Convert.ToBase64String(request.Payload) : null,
                MaxAttempts = request.MaxAttempts,
            },
            authenticated: true,
            cancellationToken).ConfigureAwait(false);
        return Validate(await YogabaseHttpTransport.ReadJsonAsync<JobResponse>(response, cancellationToken)
            .ConfigureAwait(false));
    }

    public async Task<YogabaseJob> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var response = await transport.SendAsync(
            HttpMethod.Get,
            YogabaseRoutes.Job(configuration.ProjectId, RequireJobId(jobId)),
            body: null,
            authenticated: true,
            cancellationToken).ConfigureAwait(false);
        return Validate(await YogabaseHttpTransport.ReadJsonAsync<JobResponse>(response, cancellationToken)
            .ConfigureAwait(false));
    }

    public async Task<YogabaseJob> CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var response = await transport.SendAsync(
            HttpMethod.Post,
            YogabaseRoutes.JobCancel(configuration.ProjectId, RequireJobId(jobId)),
            body: null,
            authenticated: true,
            cancellationToken).ConfigureAwait(false);
        return Validate(await YogabaseHttpTransport.ReadJsonAsync<JobResponse>(response, cancellationToken)
            .ConfigureAwait(false));
    }

    public async Task<YogabaseJobResult> GetResultAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        using var response = await transport.SendAsync(
            HttpMethod.Get,
            YogabaseRoutes.JobResult(configuration.ProjectId, RequireJobId(jobId)),
            body: null,
            authenticated: true,
            cancellationToken,
            HttpCompletionOption.ResponseHeadersRead,
            configure: static request =>
            {
                request.Headers.TryAddWithoutValidation("Accept", "application/octet-stream, application/problem+json");
            }).ConfigureAwait(false);

        var declared = ReadDeclaredDigest(response);
        if (response.StatusCode != HttpStatusCode.OK || declared is null)
        {
            throw ResultInvalid();
        }

        byte[] content;
        try
        {
            content = await YogabaseHttpContent.ReadBoundedBytesAsync(
                response.Content,
                YogabaseJobsLimits.MaximumResultBytes,
                cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidDataException)
        {
            throw ResultInvalid();
        }

        string computed;
        using (var sha = SHA256.Create())
        {
            computed = "sha256:" + ToLowerHex(sha.ComputeHash(content));
        }

        if (!string.Equals(computed, declared, StringComparison.Ordinal))
        {
            throw new YogabaseApiException(
                502,
                YogabaseJobErrors.ResultDigestMismatch,
                "The job result does not match the digest Yogabase sent with it, so none of it was returned.");
        }

        return new YogabaseJobResult { Content = content, Digest = declared };
    }

    /// <summary>
    /// The digest the gateway states for the result: exactly one header, <c>sha256:</c> and sixty-four
    /// lowercase hexadecimal digits. Anything else, or none, is null: a result that does not name its
    /// digest cannot be checked, so it is not trusted.
    /// </summary>
    private static string? ReadDeclaredDigest(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues(ResultDigestHeader, out var values))
        {
            return null;
        }

        var declared = values.Take(2).ToArray();
        if (declared.Length != 1)
        {
            return null;
        }

        var value = declared[0];
        return value.Length == 71
            && value.StartsWith("sha256:", StringComparison.Ordinal)
            && value.Substring(7).All(static character => character is >= '0' and <= '9' or >= 'a' and <= 'f')
                ? value
                : null;
    }

    private static YogabaseApiException ResultInvalid() => new(
        502,
        YogabaseJobErrors.ResponseInvalid,
        "Yogabase returned an invalid job result.");

    private static Guid RequireJobId(Guid jobId)
    {
        if (jobId == Guid.Empty)
        {
            throw new ArgumentException("A non-empty job id is required.", nameof(jobId));
        }

        return jobId;
    }

    private static void ValidateJobType(string jobType, string parameterName)
    {
        if (!IsJobType(jobType))
        {
            throw new ArgumentException(
                "A job type is 1 to 128 ASCII letters, digits, '.', '-', '_' or ':', with no surrounding whitespace.",
                parameterName);
        }
    }

    private static bool IsJobType(string? jobType) =>
        !string.IsNullOrEmpty(jobType)
        && jobType!.Length <= YogabaseJobsLimits.MaximumJobTypeLength
        && jobType.All(static character =>
            character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '.' or '-' or '_' or ':');

    private static void ValidateIdempotencyKey(string idempotencyKey, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)
            || idempotencyKey.Length > YogabaseJobsLimits.MaximumIdempotencyKeyLength
            || !string.Equals(idempotencyKey, idempotencyKey.Trim(), StringComparison.Ordinal)
            || idempotencyKey.Any(char.IsControl))
        {
            throw new ArgumentException(
                "An idempotency key is 1 to 256 characters, trimmed, with no control character.",
                parameterName);
        }
    }

    // A job the gateway describes wrongly is worse than one it refuses: a caller that polls for a state
    // it was never told about waits for ever. Reject the answer instead of returning a guess.
    private static YogabaseJob Validate(JobResponse wire)
    {
        if (!Guid.TryParseExact(wire.JobId, "D", out var jobId)
            || jobId == Guid.Empty
            || !IsJobType(wire.JobType)
            || !TryParseStatus(wire.Status, out var status)
            || wire.Attempt < 0
            || wire.MaxAttempts is < 1 or > YogabaseJobsLimits.MaximumAttempts
            || wire.ProgressPercent is < 0 or > 100
            || wire.ResultReference is not null && !IsResultReference(wire.ResultReference)
            || wire.FailureCode is not null && !ExtendedLambdaValidation.IsFailureCode(wire.FailureCode)
            || wire.CreatedAt == default
            || wire.UpdatedAt == default)
        {
            throw new YogabaseApiException(502, YogabaseJobErrors.ResponseInvalid, "Yogabase returned an invalid job.");
        }

        return new YogabaseJob
        {
            JobId = jobId,
            JobType = wire.JobType!,
            Status = status,
            Attempt = wire.Attempt,
            MaxAttempts = wire.MaxAttempts,
            ProgressPercent = wire.ProgressPercent,
            CancellationRequested = wire.CancellationRequested,
            ResultReference = wire.ResultReference,
            FailureCode = wire.FailureCode,
            CreatedAt = wire.CreatedAt,
            UpdatedAt = wire.UpdatedAt,
            CompletedAt = wire.CompletedAt,
        };
    }

    // The exact names only: Enum.TryParse would also accept a number, and a state this client was never
    // told of must be refused, not read as whichever one that number happens to be.
    private static bool TryParseStatus(string? value, out YogabaseJobStatus status)
    {
        switch (value)
        {
            case "Queued":
                status = YogabaseJobStatus.Queued;
                return true;
            case "Running":
                status = YogabaseJobStatus.Running;
                return true;
            case "Succeeded":
                status = YogabaseJobStatus.Succeeded;
                return true;
            case "Failed":
                status = YogabaseJobStatus.Failed;
                return true;
            case "Cancelled":
                status = YogabaseJobStatus.Cancelled;
                return true;
            case "DeadLettered":
                status = YogabaseJobStatus.DeadLettered;
                return true;
            default:
                status = default;
                return false;
        }
    }

    // The reference is opaque to a client: bounded, an object:// name, and nothing that could be read as
    // more than text. Its grammar belongs to the gateway and is not restated here.
    private static bool IsResultReference(string value) =>
        value.Length is > 9 and <= 2_048
        && value.StartsWith("object://", StringComparison.Ordinal)
        && value.All(static character => !char.IsWhiteSpace(character) && !char.IsControl(character));

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

    private sealed class SubmitBody
    {
        public string JobType { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;
        public string? PayloadBase64 { get; set; }
        public int? MaxAttempts { get; set; }
    }

    private sealed class JobResponse
    {
        public string? JobId { get; set; }
        public string? JobType { get; set; }
        public string? Status { get; set; }
        public int Attempt { get; set; }
        public int MaxAttempts { get; set; }
        public int ProgressPercent { get; set; }
        public bool CancellationRequested { get; set; }
        public string? ResultReference { get; set; }
        public string? FailureCode { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
