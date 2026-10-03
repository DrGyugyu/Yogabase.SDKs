using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Yogabase;

/// <summary>
/// What a schedule does about occurrences it missed while the scheduler was unavailable. A definition
/// always names one: a request that leaves it out is a 400 whose problem names the member.
/// </summary>
public enum YogabaseScheduleMisfirePolicy
{
    /// <summary>Runs once, now, however many occurrences were missed, and resumes the cadence.</summary>
    RunImmediately = 0,

    /// <summary>Abandons what it missed and resumes at the next occurrence.</summary>
    SkipToNextOccurrence = 1,

    /// <summary>
    /// Runs each missed occurrence, oldest first, up to <see cref="YogabaseScheduleDefinition.MaxCatchUp"/>.
    /// </summary>
    RunAllMissed = 2,
}

/// <summary>
/// What a schedule does when an occurrence is due while an earlier occurrence of the same schedule has not
/// finished: its invocation is queued (waiting to start, or to be retried) or running, and its deadline has
/// not passed. A skipped occurrence is consumed, not deferred: the schedule moves on to its next occurrence.
/// A manual run is not an occurrence, so the policy neither holds it back nor is held back by it, and a
/// one-shot has no earlier occurrence, so the policy does not apply to it. A definition always names one: a
/// request that leaves it out is a 400 whose problem names the member.
/// </summary>
public enum YogabaseScheduleConcurrencyPolicy
{
    /// <summary>Starts the new occurrence alongside the earlier one.</summary>
    Allow = 0,

    /// <summary>
    /// Skips the new occurrence. When several are due at once
    /// (<see cref="YogabaseScheduleMisfirePolicy.RunAllMissed"/> after an outage), only the oldest starts.
    /// </summary>
    Forbid = 1,

    /// <summary>
    /// Ends an earlier occurrence that is still queued, with the failure code
    /// <c>schedule_occurrence_replaced</c>, and starts the new one. An occurrence that is already running
    /// cannot be stopped, because nothing can interrupt a Lambda that is executing, so while one is running
    /// this skips the new occurrence, as <see cref="Forbid"/> does. When several are due at once, only the
    /// newest starts.
    /// </summary>
    Replace = 2,
}

/// <summary>Whether a manual run enqueued a new invocation or found the one its key had already made.</summary>
public enum YogabaseScheduleTriggerOutcome
{
    /// <summary>This request enqueued the invocation.</summary>
    Enqueued = 0,

    /// <summary>The same key against the same version had already made the invocation, and this is it.</summary>
    Replayed = 1,
}

/// <summary>
/// What a client asks the schedule write route to hold: a recurring schedule (<see cref="Cron"/>
/// evaluated in <see cref="TimeZone"/>) or a one-shot (<see cref="OneShotAtUtc"/>) of one deployed
/// stateless function version. The project and the owner are never fields: the project is the client's
/// own and the owner is the signed-in user.
/// </summary>
public sealed class YogabaseScheduleDefinition
{
    /// <summary>
    /// The schedule's id, chosen by the caller: 1 to 128 characters, a letter or a digit first and then
    /// letters, digits and <c>.</c> <c>_</c> <c>~</c> <c>:</c> <c>-</c> only
    /// (<c>^[A-Za-z0-9][A-Za-z0-9._~:-]{0,127}$</c>). The id is one segment of the schedule's URL path, so
    /// there is nothing to escape, and the console addresses the schedule by the same id. The id belongs to
    /// the project, so one that another user holds cannot be created. A schedule written before the rule
    /// under another id can still be read, run and deleted by that id, but a write of it is refused: delete
    /// it and create it again under an id that follows the rule.
    /// </summary>
    public string ScheduleId { get; set; } = string.Empty;

    /// <summary>The id of a deployed Stateless Lambda, at most 256 characters.</summary>
    public string FunctionId { get; set; } = string.Empty;

    /// <summary>
    /// The exact released version of that function which every occurrence runs, at most 64 characters.
    /// It must be admitted for the project, and an update checks it again.
    /// </summary>
    public string FunctionVersion { get; set; } = string.Empty;

    /// <summary>
    /// Five fields, minute hour day-of-month month day-of-week, each a number, <c>*</c>, a list
    /// (<c>1,15</c>), a range (<c>1-5</c>), a step (<c>*/5</c> or <c>8-18/2</c>) or, for the month and
    /// the day of the week, a name (<c>JAN</c>, <c>MON</c>); 7 is Sunday. No seconds field and no
    /// <c>@</c> macros; at most 128 characters. It is evaluated in <see cref="TimeZone"/>. A one-shot
    /// still sends a valid expression, which does not decide when it runs.
    /// </summary>
    public string Cron { get; set; } = string.Empty;

    /// <summary>
    /// The time zone the cron expression is evaluated in, by the id the gateway's system knows it by
    /// (<c>UTC</c>, <c>Asia/Seoul</c>), at most 128 characters. An unknown one is refused.
    /// </summary>
    public string TimeZone { get; set; } = string.Empty;

    /// <summary>What to do about occurrences missed while the scheduler was unavailable. Required.</summary>
    public YogabaseScheduleMisfirePolicy? MisfirePolicy { get; set; }

    /// <summary>
    /// What to do when an occurrence is due while an earlier one has not finished; see
    /// <see cref="YogabaseScheduleConcurrencyPolicy"/>. Required.
    /// </summary>
    public YogabaseScheduleConcurrencyPolicy? ConcurrencyPolicy { get; set; }

    /// <summary>
    /// The most missed occurrences <see cref="YogabaseScheduleMisfirePolicy.RunAllMissed"/> runs after an
    /// outage, from 1 to <see cref="YogabaseSchedulesLimits.MaximumCatchUp"/>. The other policies ignore it
    /// but the gateway still requires it.
    /// </summary>
    public int MaxCatchUp { get; set; }

    /// <summary>
    /// How many times an occurrence's invocation may be attempted, from 1 to
    /// <see cref="YogabaseSchedulesLimits.MaximumAttempts"/>.
    /// </summary>
    public int MaxAttempts { get; set; }

    /// <summary>
    /// How long an occurrence's invocation has to finish, in seconds, from 1 to
    /// <see cref="YogabaseSchedulesLimits.MaximumDeadlineSeconds"/>, counted from when the scheduler starts
    /// it: the poll that finds the occurrence due, which is a little after its time.
    /// </summary>
    public int DeadlineSeconds { get; set; }

    /// <summary>
    /// The request every occurrence's invocation carries, at most
    /// <see cref="YogabaseSchedulesLimits.MaximumPayloadBytes"/> bytes, or <see langword="null"/> (or empty)
    /// for none. It is sent as base64.
    /// </summary>
    public byte[]? Payload { get; set; }

    /// <summary>
    /// Makes the schedule a one-shot: it runs once at this instant and is then deleted. The instant is
    /// sent as UTC. An instant already past runs at the scheduler's next poll. <see langword="null"/> is a
    /// recurring schedule. An update keeps a schedule's kind: it cannot turn a one-shot into a recurring
    /// schedule or the reverse (a 409).
    /// </summary>
    public DateTimeOffset? OneShotAtUtc { get; set; }
}

/// <summary>
/// One schedule as the gateway holds it, with the ETag of this version of it. Send the
/// <see cref="ETag"/> back to change, delete or run exactly this version.
/// </summary>
public sealed class YogabaseSchedule
{
    /// <summary>The schedule's id.</summary>
    public string ScheduleId { get; set; } = string.Empty;

    /// <summary>The project the schedule belongs to: the client's own.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>The owner: the sub of the user who created the schedule.</summary>
    public Guid Subject { get; set; }

    /// <summary>The id of the function every occurrence runs.</summary>
    public string FunctionId { get; set; } = string.Empty;

    /// <summary>The exact function version every occurrence runs.</summary>
    public string FunctionVersion { get; set; } = string.Empty;

    /// <summary>
    /// The schedule's version as decimal text: a 64-bit number a JSON number cannot carry exactly. It
    /// changes whenever the schedule does, including when the scheduler records an occurrence, and is not
    /// a counter; the <see cref="ETag"/> carries it with the instance.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Lasts until the schedule is deleted: a schedule deleted and created again has a new one.</summary>
    public Guid InstanceId { get; set; }

    /// <summary>
    /// How far the scheduler has got: when the schedule was created, then the last occurrence it ran or
    /// skipped past. <see langword="null"/> for a one-shot.
    /// </summary>
    public DateTimeOffset? LastOccurrence { get; set; }

    /// <summary>The instant a one-shot runs; <see langword="null"/> for a recurring schedule.</summary>
    public DateTimeOffset? OneShotAtUtc { get; set; }

    /// <summary>
    /// The ETag to send as <c>If-Match</c> to change, delete or run exactly this version: the quotes
    /// included, <c>"schedule-v&lt;version&gt;-&lt;instance&gt;"</c> with the instance as 32 lowercase
    /// hexadecimal digits. It is built from <see cref="Version"/> and <see cref="InstanceId"/>, which the
    /// gateway's ETag header carries too (a header that disagrees is refused). The scheduler advances a
    /// recurring schedule's version as it runs, so an ETag held for long is stale: read the schedule again.
    /// </summary>
    public string ETag { get; set; } = string.Empty;

    /// <summary>True for a one-shot, which runs once and is then deleted.</summary>
    public bool IsOneShot => OneShotAtUtc.HasValue;
}

/// <summary>What a client asks the schedule trigger route to do: run the schedule once now.</summary>
public sealed class YogabaseScheduleTriggerRequest
{
    /// <summary>
    /// The caller's key for this run: 1 to 256 characters, with no leading or trailing white space. The
    /// same key against the same version of the schedule is the same run, so retrying a trigger is safe.
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>
    /// When the invocation must finish: later than now and at most
    /// <see cref="YogabaseSchedulesLimits.MaximumManualDeadlineSeconds"/> seconds from now, else the
    /// gateway refuses it. The instant is sent as UTC.
    /// </summary>
    public DateTimeOffset Deadline { get; set; }
}

/// <summary>The invocation a manual run made, or the one the same key already made.</summary>
public sealed class YogabaseScheduleTriggerResult
{
    /// <summary>Whether this request enqueued the invocation or found the one its key had already made.</summary>
    public YogabaseScheduleTriggerOutcome Outcome { get; set; }

    /// <summary>
    /// The invocation's id. It derives from the schedule's id, instance and version and the idempotency
    /// key, so a repeat has the same one.
    /// </summary>
    public Guid InvocationId { get; set; }
}

/// <summary>The bounds of the schedule routes, which the client checks before it sends a request.</summary>
public static class YogabaseSchedulesLimits
{
    /// <summary>The most characters a schedule id may have.</summary>
    public const int MaximumScheduleIdLength = 128;

    /// <summary>The most characters a function id may have.</summary>
    public const int MaximumFunctionIdLength = 256;

    /// <summary>The most characters a function version may have.</summary>
    public const int MaximumFunctionVersionLength = 64;

    /// <summary>The most characters a cron expression may have.</summary>
    public const int MaximumCronLength = 128;

    /// <summary>The most characters a time zone id may have.</summary>
    public const int MaximumTimeZoneIdLength = 128;

    /// <summary>The most missed occurrences a schedule may catch up.</summary>
    public const int MaximumCatchUp = 128;

    /// <summary>The most attempts an occurrence's invocation may be allowed.</summary>
    public const int MaximumAttempts = 16;

    /// <summary>The fewest seconds an occurrence's invocation may be given.</summary>
    public const int MinimumDeadlineSeconds = 1;

    /// <summary>The most seconds an occurrence's invocation may be given: 24 hours.</summary>
    public const int MaximumDeadlineSeconds = 86_400;

    /// <summary>The most characters a manual run's idempotency key may have.</summary>
    public const int MaximumIdempotencyKeyLength = 256;

    /// <summary>The furthest a manual run's deadline may be from now: one hour.</summary>
    public const int MaximumManualDeadlineSeconds = 3_600;

    /// <summary>The most bytes a schedule's payload may have, before base64 encoding: the 1 MiB inline limit.</summary>
    public const int MaximumPayloadBytes = 1_048_576;
}

/// <summary>
/// The codes the schedule routes refuse with, as <see cref="YogabaseApiException.Code"/> carries them. Only
/// one refusal of the gateway's names a code; every other is a status with a fixed title, or no body at all,
/// and its code is derived from the status (see <see cref="YogabaseStatusErrors"/>).
/// </summary>
public static class YogabaseScheduleErrors
{
    /// <summary>
    /// 413: the payload is over 1 MiB before base64 encoding, or the request body is over the largest the
    /// route reads. <see cref="YogabaseApiException.LimitBytes"/> and
    /// <see cref="YogabaseApiException.MaxBodyBytes"/> name both limits.
    /// </summary>
    public const string PayloadTooLarge = "schedule_payload_too_large";

    /// <summary>This client: the gateway's answer to a schedule call is not what the route declares.</summary>
    public const string ResponseInvalid = "schedule_response_invalid";
}

/// <summary>
/// The signed-in user's own schedules: create, change, read, delete and run once a recurring or one-shot
/// schedule of a deployed stateless function. Every call is authenticated with the user's access token
/// against the application gateway, behind the project token's <c>lambda.invoke</c> scope, and a schedule
/// belongs to the user who created it: another user's schedule is answered as one that does not exist
/// (except that updating it is a 403 and creating a schedule under its id is a 412).
/// </summary>
/// <remarks>
/// <para>
/// Writes are conditional. <see cref="CreateAsync"/> sends <c>If-None-Match: *</c>; the others send the
/// ETag last read as <c>If-Match</c>: a stale ETag, creating an id that is already held and updating one
/// that does not exist are each a 412 (<see cref="YogabaseStatusErrors.PreconditionFailed"/>) that changes
/// nothing, and the scheduler advances a recurring schedule's version as it runs, so an ETag held for long
/// is stale: <see cref="GetAsync"/> reads it again. Every refusal here is a <see cref="YogabaseApiException"/>.
/// </para>
/// <para>
/// The bootstrap document carries no capability identifier for schedules, so the client does not pretend to
/// know whether an installation serves them: an installation without the Scheduler answers every route with
/// a 404 that has no body, which reads as <see cref="YogabaseStatusErrors.NotFound"/>, as does a schedule
/// that does not exist.
/// </para>
/// </remarks>
public interface IYogabaseSchedules
{
    /// <summary>
    /// Creates the schedule as the signed-in user, who then owns it (201). The answer is the schedule with
    /// its ETag.
    /// </summary>
    /// <exception cref="YogabaseApiException">
    /// <see cref="YogabaseStatusErrors.PreconditionFailed"/> (412) when the id is already held,
    /// <see cref="YogabaseStatusErrors.Conflict"/> (409) when the function version is not an admitted Stateless
    /// one of the project, <see cref="YogabaseScheduleErrors.PayloadTooLarge"/> (413), a 400 for a member the
    /// gateway refuses (a cron expression that does not parse, an unknown time zone, a payload that is not
    /// base64), a 403 or a 503.
    /// </exception>
    Task<YogabaseSchedule> CreateAsync(YogabaseScheduleDefinition definition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the schedule the <paramref name="eTag"/> names (200) and answers it with its new ETag. The
    /// definition names the same <see cref="YogabaseScheduleDefinition.ScheduleId"/> and keeps the schedule's
    /// kind: recurring or one-shot. A schedule written before the id rule under an id that does not follow it
    /// cannot be changed: delete it and create it again under an id that does.
    /// </summary>
    /// <exception cref="YogabaseApiException">
    /// <see cref="YogabaseStatusErrors.PreconditionFailed"/> (412) when the ETag is stale or the schedule does
    /// not exist, <see cref="YogabaseStatusErrors.Forbidden"/> (403) for another user's schedule,
    /// <see cref="YogabaseStatusErrors.Conflict"/> (409) for a function version that is not admitted or a
    /// schedule that would change kind, and the refusals of <see cref="CreateAsync"/>.
    /// </exception>
    Task<YogabaseSchedule> UpdateAsync(
        YogabaseScheduleDefinition definition,
        string eTag,
        CancellationToken cancellationToken = default);

    /// <summary>Reads the caller's own schedule, with the ETag to change, delete or run it.</summary>
    /// <exception cref="YogabaseApiException">
    /// <see cref="YogabaseStatusErrors.NotFound"/> (404) for no such schedule and for another user's.
    /// </exception>
    Task<YogabaseSchedule> GetAsync(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>Deletes the schedule the <paramref name="eTag"/> names (204).</summary>
    /// <exception cref="YogabaseApiException">
    /// <see cref="YogabaseStatusErrors.PreconditionFailed"/> (412) when the ETag is stale,
    /// <see cref="YogabaseStatusErrors.NotFound"/> (404) for no such schedule and for another user's.
    /// </exception>
    Task DeleteAsync(string scheduleId, string eTag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the schedule the <paramref name="eTag"/> names once now, outside its cadence: it enqueues one
    /// invocation of the schedule's function with the schedule's payload, as the schedule's owner (202). The
    /// same idempotency key against the same version answers the same invocation, with
    /// <see cref="YogabaseScheduleTriggerOutcome.Replayed"/> where the first said
    /// <see cref="YogabaseScheduleTriggerOutcome.Enqueued"/>. A manual run is not an occurrence of the
    /// schedule: its <see cref="YogabaseScheduleConcurrencyPolicy"/> neither holds it back nor is held back by it.
    /// </summary>
    /// <exception cref="YogabaseApiException">
    /// <see cref="YogabaseStatusErrors.PreconditionFailed"/> (412) when the ETag is stale,
    /// <see cref="YogabaseStatusErrors.NotFound"/> (404), <see cref="YogabaseStatusErrors.Conflict"/> (409) when
    /// the queue holds the key for a different request or the schedule's function version is no longer an
    /// admitted Stateless one of the project (as for a write of it), and a 400 for a deadline that is not
    /// after now or is more than an hour away.
    /// </exception>
    Task<YogabaseScheduleTriggerResult> TriggerAsync(
        string scheduleId,
        string eTag,
        YogabaseScheduleTriggerRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed class YogabaseSchedulesClient : IYogabaseSchedules
{
    private static readonly Regex ETagPattern = new(
        "^\"schedule-v(?<version>[1-9][0-9]*)-(?<instance>[0-9a-f]{32})\"$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

    private static readonly Regex VersionPattern = new("^[1-9][0-9]*$", RegexOptions.CultureInvariant);

    // A write holds the id to the contract's one rule, the pattern of the definition's scheduleId member: the
    // id is one segment of the URL path, so it is made of what a segment carries without being escaped. The
    // anchor is \z, not $: in .NET $ also matches before a final newline, which the contract's pattern does
    // not. A read, a run and a delete accept more (ValidateScheduleId), because a schedule written before
    // the rule is still found by its id.
    private static readonly Regex WritableScheduleIdPattern = new(
        "^[A-Za-z0-9][A-Za-z0-9._~:-]{0,127}\\z",
        RegexOptions.CultureInvariant);

    private readonly YogabaseClientConfiguration configuration;
    private readonly YogabaseHttpTransport transport;

    internal YogabaseSchedulesClient(YogabaseClientConfiguration configuration, YogabaseHttpTransport transport)
    {
        this.configuration = configuration;
        this.transport = transport;
    }

    public Task<YogabaseSchedule> CreateAsync(
        YogabaseScheduleDefinition definition,
        CancellationToken cancellationToken = default) =>
        WriteAsync(definition, eTag: null, cancellationToken);

    public Task<YogabaseSchedule> UpdateAsync(
        YogabaseScheduleDefinition definition,
        string eTag,
        CancellationToken cancellationToken = default) =>
        WriteAsync(definition, RequireETag(eTag, nameof(eTag)), cancellationToken);

    public async Task<YogabaseSchedule> GetAsync(string scheduleId, CancellationToken cancellationToken = default)
    {
        ValidateScheduleId(scheduleId, nameof(scheduleId));
        var response = await SendAsync(
            HttpMethod.Get,
            YogabaseRoutes.Schedule(configuration.ProjectId, EscapeScheduleId(scheduleId)),
            body: null,
            configure: null,
            cancellationToken).ConfigureAwait(false);
        return await ReadScheduleAsync(response, scheduleId, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string scheduleId, string eTag, CancellationToken cancellationToken = default)
    {
        ValidateScheduleId(scheduleId, nameof(scheduleId));
        var token = RequireETag(eTag, nameof(eTag));
        var response = await SendAsync(
            HttpMethod.Delete,
            YogabaseRoutes.Schedule(configuration.ProjectId, EscapeScheduleId(scheduleId)),
            body: null,
            configure: request => request.Headers.TryAddWithoutValidation("If-Match", token),
            cancellationToken).ConfigureAwait(false);
        response.Dispose();
    }

    public async Task<YogabaseScheduleTriggerResult> TriggerAsync(
        string scheduleId,
        string eTag,
        YogabaseScheduleTriggerRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateScheduleId(scheduleId, nameof(scheduleId));
        var token = RequireETag(eTag, nameof(eTag));
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        ValidateIdempotencyKey(request.IdempotencyKey, nameof(request));
        if (request.Deadline == default)
        {
            throw new ArgumentException("A manual run needs a deadline.", nameof(request));
        }

        var response = await SendAsync(
            HttpMethod.Post,
            YogabaseRoutes.ScheduleTrigger(configuration.ProjectId, EscapeScheduleId(scheduleId)),
            new TriggerBody
            {
                IdempotencyKey = request.IdempotencyKey,
                Deadline = request.Deadline.ToUniversalTime(),
            },
            configure: message => message.Headers.TryAddWithoutValidation("If-Match", token),
            cancellationToken).ConfigureAwait(false);
        return ValidateTrigger(await YogabaseHttpTransport.ReadJsonAsync<TriggerResponse>(response, cancellationToken)
            .ConfigureAwait(false));
    }

    private async Task<YogabaseSchedule> WriteAsync(
        YogabaseScheduleDefinition definition,
        string? eTag,
        CancellationToken cancellationToken)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        var body = ValidateDefinition(definition);
        var response = await SendAsync(
            HttpMethod.Put,
            YogabaseRoutes.Schedule(configuration.ProjectId, EscapeScheduleId(definition.ScheduleId)),
            body,
            // Exactly one of the two preconditions, never both and never neither: a create must not
            // overwrite what another request made first, and an update must not overwrite what it never read.
            configure: eTag is null
                ? message => message.Headers.TryAddWithoutValidation("If-None-Match", "*")
                : message => message.Headers.TryAddWithoutValidation("If-Match", eTag),
            cancellationToken).ConfigureAwait(false);
        return await ReadScheduleAsync(response, definition.ScheduleId, cancellationToken).ConfigureAwait(false);
    }

    private Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? body,
        Action<HttpRequestMessage>? configure,
        CancellationToken cancellationToken) =>
        transport.SendAsync(
            method,
            path,
            body,
            authenticated: true,
            cancellationToken,
            HttpCompletionOption.ResponseHeadersRead,
            configure);

    // The schedule the answer describes must be the one asked for, in this client's own project: a gateway
    // that describes another is not trusted, and a caller that holds the wrong ETag for the wrong schedule
    // would delete it. The ETag header, where the gateway sends a strong one, must say what the body says.
    private async Task<YogabaseSchedule> ReadScheduleAsync(
        HttpResponseMessage response,
        string requestedScheduleId,
        CancellationToken cancellationToken)
    {
        var header = ReadStrongETagHeader(response);
        var wire = await YogabaseHttpTransport.ReadJsonAsync<ScheduleResponse>(response, cancellationToken)
            .ConfigureAwait(false);
        if (wire.ScheduleId is null
            || !string.Equals(wire.ScheduleId, requestedScheduleId, StringComparison.Ordinal)
            || !Guid.TryParseExact(wire.ProjectId, "D", out var projectId)
            || projectId != configuration.ProjectId
            || !Guid.TryParseExact(wire.Subject, "D", out var subject)
            || subject == Guid.Empty
            || !IsBounded(wire.FunctionId, YogabaseSchedulesLimits.MaximumFunctionIdLength)
            || !IsBounded(wire.FunctionVersion, YogabaseSchedulesLimits.MaximumFunctionVersionLength)
            || wire.Version is null
            || !IsVersion(wire.Version)
            || !Guid.TryParseExact(wire.InstanceId, "D", out var instanceId)
            || instanceId == Guid.Empty)
        {
            throw Invalid("Yogabase returned an invalid schedule.");
        }

        var eTag = "\"schedule-v" + wire.Version + "-" + instanceId.ToString("N", CultureInfo.InvariantCulture) + "\"";
        if (header is not null && !string.Equals(header, eTag, StringComparison.Ordinal))
        {
            throw Invalid("Yogabase returned a schedule whose ETag header does not match the schedule.");
        }

        return new YogabaseSchedule
        {
            ScheduleId = wire.ScheduleId,
            ProjectId = projectId,
            Subject = subject,
            FunctionId = wire.FunctionId!,
            FunctionVersion = wire.FunctionVersion!,
            Version = wire.Version,
            InstanceId = instanceId,
            LastOccurrence = wire.LastOccurrence,
            OneShotAtUtc = wire.OneShotAtUtc,
            ETag = eTag,
        };
    }

    /// <summary>
    /// The ETag header when the gateway sent exactly one and it is a strong token; a weak or repeated one is
    /// ignored, because a proxy that compresses a response may weaken its ETag and the body carries the
    /// same facts.
    /// </summary>
    private static string? ReadStrongETagHeader(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("ETag", out var values))
        {
            return null;
        }

        var declared = values.Take(2).ToArray();
        return declared.Length == 1 && !declared[0].StartsWith("W/", StringComparison.Ordinal) ? declared[0] : null;
    }

    private static YogabaseScheduleTriggerResult ValidateTrigger(TriggerResponse wire)
    {
        if (wire.Outcome is not (0 or 1) || !Guid.TryParseExact(wire.InvocationId, "D", out var invocationId) || invocationId == Guid.Empty)
        {
            throw Invalid("Yogabase returned an invalid schedule run.");
        }

        return new YogabaseScheduleTriggerResult
        {
            Outcome = wire.Outcome == 0 ? YogabaseScheduleTriggerOutcome.Enqueued : YogabaseScheduleTriggerOutcome.Replayed,
            InvocationId = invocationId,
        };
    }

    private static DefinitionBody ValidateDefinition(YogabaseScheduleDefinition definition)
    {
        ValidateWritableScheduleId(definition.ScheduleId, nameof(definition));
        RequireBounded(definition.FunctionId, YogabaseSchedulesLimits.MaximumFunctionIdLength, "A function id", nameof(definition));
        RequireBounded(definition.FunctionVersion, YogabaseSchedulesLimits.MaximumFunctionVersionLength, "A function version", nameof(definition));
        RequireBounded(definition.Cron, YogabaseSchedulesLimits.MaximumCronLength, "A cron expression", nameof(definition));
        RequireBounded(definition.TimeZone, YogabaseSchedulesLimits.MaximumTimeZoneIdLength, "A time zone id", nameof(definition));
        if (definition.MisfirePolicy is not { } misfire || !Enum.IsDefined(typeof(YogabaseScheduleMisfirePolicy), misfire))
        {
            throw new ArgumentException(
                "A misfire policy is required: RunImmediately, SkipToNextOccurrence or RunAllMissed.",
                nameof(definition));
        }

        if (definition.ConcurrencyPolicy is not { } concurrency
            || !Enum.IsDefined(typeof(YogabaseScheduleConcurrencyPolicy), concurrency))
        {
            throw new ArgumentException(
                "A concurrency policy is required: Allow, Forbid or Replace.",
                nameof(definition));
        }

        if (definition.MaxCatchUp is < 1 or > YogabaseSchedulesLimits.MaximumCatchUp)
        {
            throw new ArgumentOutOfRangeException(nameof(definition), "A schedule catches up between 1 and 128 missed occurrences.");
        }

        if (definition.MaxAttempts is < 1 or > YogabaseSchedulesLimits.MaximumAttempts)
        {
            throw new ArgumentOutOfRangeException(nameof(definition), "A schedule allows between 1 and 16 attempts.");
        }

        if (definition.DeadlineSeconds is < YogabaseSchedulesLimits.MinimumDeadlineSeconds or > YogabaseSchedulesLimits.MaximumDeadlineSeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(definition), "A schedule's deadline is between 1 second and 24 hours.");
        }

        if (definition.Payload is { Length: > YogabaseSchedulesLimits.MaximumPayloadBytes })
        {
            throw new ArgumentOutOfRangeException(nameof(definition), "A schedule payload is limited to one MiB before base64 encoding.");
        }

        if (definition.OneShotAtUtc == default(DateTimeOffset))
        {
            throw new ArgumentException("A one-shot's time must be set, as an instant.", nameof(definition));
        }

        return new DefinitionBody
        {
            ScheduleId = definition.ScheduleId,
            FunctionId = definition.FunctionId,
            FunctionVersion = definition.FunctionVersion,
            Cron = definition.Cron,
            TimeZone = definition.TimeZone,
            MisfirePolicy = misfire.ToString(),
            ConcurrencyPolicy = concurrency.ToString(),
            MaxCatchUp = definition.MaxCatchUp,
            MaxAttempts = definition.MaxAttempts,
            DeadlineSeconds = definition.DeadlineSeconds,
            // A schedule with no payload and one with an empty payload are the same schedule to the gateway.
            PayloadBase64 = definition.Payload is { Length: > 0 } ? Convert.ToBase64String(definition.Payload) : null,
            OneShotAtUtc = definition.OneShotAtUtc?.ToUniversalTime(),
        };
    }

    private static void ValidateWritableScheduleId(string? scheduleId, string parameterName)
    {
        if (scheduleId is null || !WritableScheduleIdPattern.IsMatch(scheduleId))
        {
            throw new ArgumentException(
                "A schedule id to write is 1 to 128 characters, a letter or a digit first and then letters, digits and . _ ~ : - only.",
                parameterName);
        }
    }

    private static void ValidateScheduleId(string? scheduleId, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(scheduleId)
            || scheduleId!.Length > YogabaseSchedulesLimits.MaximumScheduleIdLength
            || !string.Equals(scheduleId, scheduleId.Trim(), StringComparison.Ordinal)
            || scheduleId.Any(char.IsControl))
        {
            throw new ArgumentException(
                "A schedule id is 1 to 128 characters, trimmed, with no control character.",
                parameterName);
        }

        // The id is a path segment, and a URL parser collapses a segment of dots (percent-encoded or not)
        // into the one before it, so these two would address another route altogether.
        if (scheduleId is "." or "..")
        {
            throw new ArgumentException(
                "A schedule id of only dots cannot be addressed in a URL path; choose another id.",
                parameterName);
        }
    }

    private static string EscapeScheduleId(string scheduleId) => Uri.EscapeDataString(scheduleId);

    private static void ValidateIdempotencyKey(string? idempotencyKey, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)
            || idempotencyKey!.Length > YogabaseSchedulesLimits.MaximumIdempotencyKeyLength
            || !string.Equals(idempotencyKey, idempotencyKey.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "An idempotency key is 1 to 256 characters with no leading or trailing white space.",
                parameterName);
        }
    }

    private static void RequireBounded(string? value, int maximumLength, string what, string parameterName)
    {
        if (!IsBounded(value, maximumLength))
        {
            throw new ArgumentException(
                what + " is required and at most " + maximumLength.ToString(CultureInfo.InvariantCulture) + " characters.",
                parameterName);
        }
    }

    private static bool IsBounded(string? value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value!.Length <= maximumLength;

    // A version is decimal text for a number a JSON number cannot carry exactly: it must fit 64 bits, as the
    // gateway's own parser requires of the token.
    private static bool IsVersion(string value) =>
        VersionPattern.IsMatch(value) && long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out _);

    private static string RequireETag(string? eTag, string parameterName)
    {
        var match = eTag is null ? null : ETagPattern.Match(eTag);
        if (match is not { Success: true }
            || !long.TryParse(match.Groups["version"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out _)
            || match.Groups["instance"].Value.All(static digit => digit == '0'))
        {
            throw new ArgumentException(
                "An ETag is the quoted token a schedule read or write answered, \"schedule-v<version>-<instance>\", exactly as sent.",
                parameterName);
        }

        return eTag!;
    }

    private static YogabaseApiException Invalid(string message) =>
        new(502, YogabaseScheduleErrors.ResponseInvalid, message);

    private sealed class DefinitionBody
    {
        public string ScheduleId { get; set; } = string.Empty;
        public string FunctionId { get; set; } = string.Empty;
        public string FunctionVersion { get; set; } = string.Empty;
        public string Cron { get; set; } = string.Empty;
        public string TimeZone { get; set; } = string.Empty;
        public string MisfirePolicy { get; set; } = string.Empty;
        public string ConcurrencyPolicy { get; set; } = string.Empty;
        public int MaxCatchUp { get; set; }
        public int MaxAttempts { get; set; }
        public int DeadlineSeconds { get; set; }
        public string? PayloadBase64 { get; set; }
        public DateTimeOffset? OneShotAtUtc { get; set; }
    }

    private sealed class TriggerBody
    {
        public string IdempotencyKey { get; set; } = string.Empty;
        public DateTimeOffset Deadline { get; set; }
    }

    private sealed class ScheduleResponse
    {
        public string? ScheduleId { get; set; }
        public string? ProjectId { get; set; }
        public string? Subject { get; set; }
        public string? FunctionId { get; set; }
        public string? FunctionVersion { get; set; }
        public string? Version { get; set; }
        public string? InstanceId { get; set; }
        public DateTimeOffset? LastOccurrence { get; set; }
        public DateTimeOffset? OneShotAtUtc { get; set; }
    }

    private sealed class TriggerResponse
    {
        public int Outcome { get; set; } = -1;
        public string? InvocationId { get; set; }
    }
}
