package io.yogabase.client;

import com.google.gson.JsonObject;
import com.microsoft.kiota.RequestAdapter;
import io.yogabase.client.models.ConcurrencyPolicy;
import io.yogabase.client.models.MisfirePolicy;
import io.yogabase.client.models.ScheduleDefinitionRequest;
import io.yogabase.client.models.ScheduleTriggerRequest;
import io.yogabase.client.v1.projects.item.schedules.item.WithScheduleItemRequestBuilder;
import java.math.BigInteger;
import java.net.URI;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.Base64;
import java.util.List;
import java.util.Objects;
import java.util.UUID;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import okhttp3.Response;

/**
 * The signed-in user's own schedules: create, change, read, delete and run once a recurring or one-shot
 * schedule of a deployed stateless function, against {@code /v1/projects/{projectId}/schedules/{scheduleId}} on
 * the application gateway and behind the same project token (the {@code lambda.invoke} scope) as invoking a
 * function. A schedule belongs to the user who created it, and only that user reads, changes, deletes or runs
 * it: another user's schedule is answered as one that does not exist (except that updating it is a 403 and
 * creating a schedule under its id is a 412).
 *
 * <p>The product's client OpenAPI document describes the schedule routes, so Kiota generates their request
 * builders ({@code api().v1().projects().byProjectId(id).schedules().byScheduleId(id)}). This class wraps those
 * builders: each request is the one the generated builder builds (its route, its media types and its serialized
 * body), sent through the Kiota request adapter of the generated application client
 * ({@link YogabaseApplicationClient#schedules}), and so with its authentication and origin. What it adds is what
 * a generated builder cannot give: every write is conditional on the schedule's ETag, which it sends as the
 * header the route wants and reads back from the schedule, so it reads the response through the adapter's native
 * response and needs the {@code OkHttpRequestAdapter} the Kiota bundle provides, whose native response is an
 * OkHttp {@link Response}; it validates each call before it is sent; and it refuses an answer that is not what
 * the route declares and names every refusal. {@code YogabaseSchedulesClientTest} pins every shape.
 *
 * <p>The write is conditional. {@link #create} sends {@code If-None-Match: *}; {@link #update}, {@link #delete}
 * and {@link #trigger} send the ETag last read as {@code If-Match}. A stale ETag, creating an id that is already
 * held and updating one that does not exist are each a 412 that changes nothing, and the scheduler advances a
 * recurring schedule's version as it runs, so an ETag held for long is stale: {@link #get} reads it again.
 *
 * <p>No capability gates it: the bootstrap document carries no capability identifier for schedules, so the client
 * does not pretend to know whether an installation serves them. An installation without the Scheduler answers
 * every route with a 404 that has no body, which reads as {@code not_found}, as does a schedule that does not
 * exist. Calls block; run them off the UI thread.
 */
public final class YogabaseSchedulesClient {
    /** The most characters a schedule id may have. */
    public static final int MAXIMUM_SCHEDULE_ID_LENGTH = 128;

    /** The most characters a function id may have. */
    public static final int MAXIMUM_FUNCTION_ID_LENGTH = 256;

    /** The most characters a function version may have. */
    public static final int MAXIMUM_FUNCTION_VERSION_LENGTH = 64;

    /** The most characters a cron expression may have. */
    public static final int MAXIMUM_CRON_LENGTH = 128;

    /** The most characters a time zone id may have. */
    public static final int MAXIMUM_TIME_ZONE_ID_LENGTH = 128;

    /** The most missed occurrences a schedule may catch up. */
    public static final int MAXIMUM_CATCH_UP = 128;

    /** The most attempts an occurrence's invocation may be allowed. */
    public static final int MAXIMUM_ATTEMPTS = 16;

    /** The fewest seconds an occurrence's invocation may be given. */
    public static final int MINIMUM_DEADLINE_SECONDS = 1;

    /** The most seconds an occurrence's invocation may be given: 24 hours. */
    public static final int MAXIMUM_DEADLINE_SECONDS = 86_400;

    /** The most characters a manual run's idempotency key may have. */
    public static final int MAXIMUM_IDEMPOTENCY_KEY_LENGTH = 256;

    /** The furthest a manual run's deadline may be from now: one hour. */
    public static final int MAXIMUM_MANUAL_DEADLINE_SECONDS = 3_600;

    /** The most bytes a schedule's payload may have, before base64 encoding: the 1 MiB inline limit. */
    public static final int MAXIMUM_PAYLOAD_BYTES = 1_048_576;

    private static final Pattern ETAG = Pattern.compile("\"schedule-v([1-9][0-9]*)-([0-9a-f]{32})\"");
    private static final Pattern VERSION = Pattern.compile("[1-9][0-9]*");
    // The pattern of the contract's definition member scheduleId, which holds a write to what one segment of a URL
    // path carries without being escaped. It is matched whole (matches(), as $ would also accept a final line
    // terminator). A read, a run and a delete accept more (requireScheduleId), because a schedule written before
    // the rule is still found by its id.
    private static final Pattern WRITABLE_SCHEDULE_ID = Pattern.compile("[A-Za-z0-9][A-Za-z0-9._~:-]{0,127}");
    private static final BigInteger MAXIMUM_INT64 = BigInteger.valueOf(Long.MAX_VALUE);

    private final RequestAdapter requestAdapter;
    private final URI origin;
    private final UUID projectId;
    private final YogabaseGatewayChannel channel;

    /**
     * @param requestAdapter the Kiota request adapter that authenticates calls to the application gateway
     *     with the signed-in user's access token (the one the generated application client uses)
     * @param applicationOrigin the installation's application-gateway origin
     * @param projectId the project's id (the bootstrap document's {@code projectId})
     */
    public YogabaseSchedulesClient(RequestAdapter requestAdapter, URI applicationOrigin, UUID projectId) {
        this.requestAdapter = Objects.requireNonNull(requestAdapter, "requestAdapter");
        this.origin = YogabaseClientOrigin.requireOrigin(applicationOrigin, "applicationOrigin");
        this.projectId = requireId(projectId, "projectId");
        requestAdapter.setBaseUrl(YogabaseClientOrigin.baseUrl(this.origin));
        this.channel = new YogabaseGatewayChannel(
            requestAdapter, YogabaseSchedulesException::new, YogabaseScheduleErrorCodes.RESPONSE_INVALID);
    }

    public URI origin() {
        return origin;
    }

    public UUID projectId() {
        return projectId;
    }

    /**
     * Creates the schedule as the signed-in user, who then owns it (201), and answers it with its ETag.
     *
     * @throws YogabaseSchedulesException {@link YogabaseStatusErrorCodes#PRECONDITION_FAILED} (412) when the id
     *     is already held, {@link YogabaseStatusErrorCodes#CONFLICT} (409) when the function version is not an
     *     admitted Stateless one of the project, {@link YogabaseScheduleErrorCodes#PAYLOAD_TOO_LARGE} (413), a
     *     400 for a member the gateway refuses (a cron expression that does not parse, an unknown time zone, a
     *     payload that is not base64), a 403 or a 503
     */
    public YogabaseSchedule create(YogabaseScheduleDefinition definition) {
        return write(definition, null);
    }

    /**
     * Changes the schedule the {@code eTag} names (200) and answers it with its new ETag. The definition names
     * the same schedule id and keeps the schedule's kind: recurring or one-shot. A schedule written before the id
     * rule under an id that does not follow it cannot be changed: delete it and create it again under an id that
     * does.
     *
     * @throws YogabaseSchedulesException {@link YogabaseStatusErrorCodes#PRECONDITION_FAILED} (412) when the ETag
     *     is stale or the schedule does not exist, {@link YogabaseStatusErrorCodes#FORBIDDEN} (403) for another
     *     user's schedule, {@link YogabaseStatusErrorCodes#CONFLICT} (409) for a function version that is not
     *     admitted or a schedule that would change kind, and the refusals of {@link #create}
     */
    public YogabaseSchedule update(YogabaseScheduleDefinition definition, String eTag) {
        return write(definition, requireETag(eTag));
    }

    /**
     * Reads the caller's own schedule, with the ETag to change, delete or run it.
     *
     * @throws YogabaseSchedulesException {@link YogabaseStatusErrorCodes#NOT_FOUND} (404) for no such schedule and
     *     for another user's
     */
    public YogabaseSchedule get(String scheduleId) {
        final String id = requireScheduleId(scheduleId);
        try (Response response = channel.send(item(id).toGetRequestInformation())) {
            return readSchedule(response, id);
        }
    }

    /**
     * Deletes the schedule the {@code eTag} names (204).
     *
     * @throws YogabaseSchedulesException {@link YogabaseStatusErrorCodes#PRECONDITION_FAILED} (412) when the ETag
     *     is stale, {@link YogabaseStatusErrorCodes#NOT_FOUND} (404) for no such schedule and for another user's
     */
    public void delete(String scheduleId, String eTag) {
        final String id = requireScheduleId(scheduleId);
        final String token = requireETag(eTag);
        // 204: nothing to read.
        channel.send(item(id).toDeleteRequestInformation(configuration -> configuration.headers.add("If-Match", token))).close();
    }

    /**
     * Runs the schedule the {@code eTag} names once now, outside its cadence: it enqueues one invocation of the
     * schedule's function with the schedule's payload, as the schedule's owner (202). The same idempotency key
     * against the same version answers the same invocation, with {@link YogabaseScheduleTriggerOutcome#REPLAYED}
     * where the first said {@link YogabaseScheduleTriggerOutcome#ENQUEUED}. A manual run is not an occurrence of the
     * schedule: its {@link YogabaseScheduleConcurrencyPolicy} neither holds it back nor is held back by it.
     *
     * @throws YogabaseSchedulesException {@link YogabaseStatusErrorCodes#PRECONDITION_FAILED} (412) when the ETag
     *     is stale, {@link YogabaseStatusErrorCodes#NOT_FOUND} (404), {@link YogabaseStatusErrorCodes#CONFLICT}
     *     (409) when the queue holds the key for a different request or the schedule's function version is no
     *     longer an admitted Stateless one of the project (as for a write of it), and a 400 for a deadline that is
     *     not after now or is more than an hour away
     */
    public YogabaseScheduleTriggerResult trigger(String scheduleId, String eTag, YogabaseScheduleTriggerRequest request) {
        final String id = requireScheduleId(scheduleId);
        final String token = requireETag(eTag);
        Objects.requireNonNull(request, "request");
        final String key = request.idempotencyKey();
        if (key == null
            || key.isBlank()
            || key.length() > MAXIMUM_IDEMPOTENCY_KEY_LENGTH
            || !key.equals(key.strip())) {
            throw new IllegalArgumentException(
                "an idempotency key is 1 to 256 characters with no leading or trailing white space");
        }
        if (request.deadline() == null) {
            throw new IllegalArgumentException("a manual run needs a deadline");
        }
        final ScheduleTriggerRequest body = new ScheduleTriggerRequest();
        body.setIdempotencyKey(key);
        body.setDeadline(request.deadline().withOffsetSameInstant(ZoneOffset.UTC));
        try (Response response = channel.send(item(id).trigger().toPostRequestInformation(
            body, configuration -> configuration.headers.add("If-Match", token)))) {
            return readTrigger(response);
        }
    }

    private YogabaseSchedule write(YogabaseScheduleDefinition definition, String eTag) {
        Objects.requireNonNull(definition, "definition");
        final ScheduleDefinitionRequest body = toRequest(definition);
        // Exactly one of the two preconditions, never both and never neither: a create must not overwrite what
        // another request made first, and an update must not overwrite what it never read.
        final String name = eTag == null ? "If-None-Match" : "If-Match";
        final String value = eTag == null ? "*" : eTag;
        try (Response response = channel.send(item(definition.scheduleId()).toPutRequestInformation(
            body, configuration -> configuration.headers.add(name, value)))) {
            return readSchedule(response, definition.scheduleId());
        }
    }

    /**
     * The generated request builder of one schedule of this client's project. It is built for each call from the
     * generated client's own navigation, so the route, the path parameters and the base URL the adapter holds now
     * are the generated code's and never restated here.
     */
    private WithScheduleItemRequestBuilder item(String scheduleId) {
        return new YogabaseApiClient(requestAdapter).v1().projects().byProjectId(projectId).schedules().byScheduleId(scheduleId);
    }

    private static ScheduleDefinitionRequest toRequest(YogabaseScheduleDefinition definition) {
        requireWritableScheduleId(definition.scheduleId());
        requireBounded(definition.functionId(), MAXIMUM_FUNCTION_ID_LENGTH, "a function id");
        requireBounded(definition.functionVersion(), MAXIMUM_FUNCTION_VERSION_LENGTH, "a function version");
        requireBounded(definition.cron(), MAXIMUM_CRON_LENGTH, "a cron expression");
        requireBounded(definition.timeZone(), MAXIMUM_TIME_ZONE_ID_LENGTH, "a time zone id");
        if (definition.misfirePolicy() == null) {
            throw new IllegalArgumentException(
                "a misfire policy is required: RunImmediately, SkipToNextOccurrence or RunAllMissed");
        }
        if (definition.concurrencyPolicy() == null) {
            throw new IllegalArgumentException(
                "a concurrency policy is required: Allow, Forbid or Replace");
        }
        if (definition.maxCatchUp() < 1 || definition.maxCatchUp() > MAXIMUM_CATCH_UP) {
            throw new IllegalArgumentException("a schedule catches up between 1 and 128 missed occurrences");
        }
        if (definition.maxAttempts() < 1 || definition.maxAttempts() > MAXIMUM_ATTEMPTS) {
            throw new IllegalArgumentException("a schedule allows between 1 and 16 attempts");
        }
        if (definition.deadlineSeconds() < MINIMUM_DEADLINE_SECONDS || definition.deadlineSeconds() > MAXIMUM_DEADLINE_SECONDS) {
            throw new IllegalArgumentException("a schedule's deadline is between 1 second and 24 hours");
        }
        if (definition.payload() != null && definition.payload().length > MAXIMUM_PAYLOAD_BYTES) {
            throw new IllegalArgumentException("a schedule payload is limited to one MiB before base64 encoding");
        }
        final ScheduleDefinitionRequest body = new ScheduleDefinitionRequest();
        body.setScheduleId(definition.scheduleId());
        body.setFunctionId(definition.functionId());
        body.setFunctionVersion(definition.functionVersion());
        body.setCron(definition.cron());
        body.setTimeZone(definition.timeZone());
        body.setMisfirePolicy(MisfirePolicy.forValue(definition.misfirePolicy().wireName()));
        body.setConcurrencyPolicy(ConcurrencyPolicy.forValue(definition.concurrencyPolicy().wireName()));
        body.setMaxCatchUp(definition.maxCatchUp());
        body.setMaxAttempts(definition.maxAttempts());
        body.setDeadlineSeconds(definition.deadlineSeconds());
        // A schedule with no payload and one with an empty payload are the same schedule to the gateway.
        if (definition.payload() != null && definition.payload().length > 0) {
            body.setPayloadBase64(Base64.getEncoder().encodeToString(definition.payload()));
        }
        if (definition.oneShotAtUtc() != null) {
            body.setOneShotAtUtc(definition.oneShotAtUtc().withOffsetSameInstant(ZoneOffset.UTC));
        }
        return body;
    }

    private static String requireWritableScheduleId(String value) {
        if (value == null || !WRITABLE_SCHEDULE_ID.matcher(value).matches()) {
            throw new IllegalArgumentException(
                "a schedule id to write is 1 to 128 characters, a letter or a digit first and then letters, digits and . _ ~ : - only");
        }
        return value;
    }

    private static String requireScheduleId(String value) {
        if (value == null
            || value.isBlank()
            || value.length() > MAXIMUM_SCHEDULE_ID_LENGTH
            || !value.equals(value.strip())
            || value.chars().anyMatch(Character::isISOControl)) {
            throw new IllegalArgumentException("a schedule id is 1 to 128 characters, trimmed, with no control character");
        }
        // The id is a path segment, and a URL parser collapses a segment of dots (percent-encoded or not) into the
        // one before it, so these two would address another route altogether.
        if (value.equals(".") || value.equals("..")) {
            throw new IllegalArgumentException("a schedule id of only dots cannot be addressed in a URL path; choose another id");
        }
        return value;
    }

    private static String requireETag(String value) {
        final Matcher match = value == null ? null : ETAG.matcher(value);
        if (match == null
            || !match.matches()
            || new BigInteger(match.group(1)).compareTo(MAXIMUM_INT64) > 0
            || match.group(2).chars().allMatch(digit -> digit == '0')) {
            throw new IllegalArgumentException(
                "an ETag is the quoted token a schedule read or write answered, \"schedule-v<version>-<instance>\", exactly as sent");
        }
        return value;
    }

    private static UUID requireId(UUID value, String name) {
        Objects.requireNonNull(value, name);
        if (value.getMostSignificantBits() == 0L && value.getLeastSignificantBits() == 0L) {
            throw new IllegalArgumentException(name + " must be a non-empty UUID");
        }
        return value;
    }

    private static void requireBounded(String value, int maximumLength, String what) {
        if (value == null || value.isBlank() || value.length() > maximumLength) {
            throw new IllegalArgumentException(what + " is required and at most " + maximumLength + " characters");
        }
    }

    // The schedule the answer describes must be the one asked for, in this client's own project: a gateway that
    // describes another is not trusted, and a caller that holds the wrong ETag for the wrong schedule would
    // delete it. The ETag header, where the gateway sends a strong one, must say what the body says; a weak or
    // repeated one is ignored, because a proxy that compresses a response may weaken its ETag and the body
    // carries the same facts. A member this client does not know is ignored, as the generated clients keep
    // unknown members aside.
    private YogabaseSchedule readSchedule(Response response, String requestedScheduleId) {
        final JsonObject document = channel.readObject(
            response,
            "Yogabase returned an invalid schedule.",
            "Yogabase returned a schedule document that is not JSON.");

        final String scheduleId = YogabaseGatewayChannel.text(document, "scheduleId");
        final UUID project = YogabaseGatewayChannel.canonicalUuid(YogabaseGatewayChannel.text(document, "projectId"));
        final UUID subject = YogabaseGatewayChannel.canonicalUuid(YogabaseGatewayChannel.text(document, "subject"));
        final String functionId = YogabaseGatewayChannel.text(document, "functionId");
        final String functionVersion = YogabaseGatewayChannel.text(document, "functionVersion");
        final String version = YogabaseGatewayChannel.text(document, "version");
        final UUID instanceId = YogabaseGatewayChannel.canonicalUuid(YogabaseGatewayChannel.text(document, "instanceId"));
        final YogabaseGatewayChannel.Nullable<String> lastText = YogabaseGatewayChannel.nullableText(document, "lastOccurrence");
        final YogabaseGatewayChannel.Nullable<String> oneShotText = YogabaseGatewayChannel.nullableText(document, "oneShotAtUtc");
        final OffsetDateTime lastOccurrence = lastText.present() && lastText.value() != null
            ? YogabaseGatewayChannel.timestamp(lastText.value())
            : null;
        final OffsetDateTime oneShot = oneShotText.present() && oneShotText.value() != null
            ? YogabaseGatewayChannel.timestamp(oneShotText.value())
            : null;
        if (scheduleId == null
            || !scheduleId.equals(requestedScheduleId)
            || project == null
            || !project.equals(projectId)
            || subject == null
            || functionId == null
            || functionId.isBlank()
            || functionId.length() > MAXIMUM_FUNCTION_ID_LENGTH
            || functionVersion == null
            || functionVersion.isBlank()
            || functionVersion.length() > MAXIMUM_FUNCTION_VERSION_LENGTH
            || version == null
            || !VERSION.matcher(version).matches()
            || new BigInteger(version).compareTo(MAXIMUM_INT64) > 0
            || instanceId == null
            || !lastText.present()
            || lastText.value() != null && lastOccurrence == null
            || !oneShotText.present()
            || oneShotText.value() != null && oneShot == null) {
            throw channel.invalid("Yogabase returned an invalid schedule.", response);
        }
        final String eTag = "\"schedule-v" + version + "-" + instanceId.toString().replace("-", "").toLowerCase(java.util.Locale.ROOT) + "\"";
        final List<String> declared = response.headers("ETag");
        if (declared.size() == 1
            && !declared.get(0).startsWith("W/")
            && !declared.get(0).contains(",")
            && !declared.get(0).equals(eTag)) {
            throw channel.invalid("Yogabase returned a schedule whose ETag header does not match the schedule.", response);
        }
        return new YogabaseSchedule(
            scheduleId, project, subject, functionId, functionVersion, version, instanceId, lastOccurrence, oneShot, eTag);
    }

    private YogabaseScheduleTriggerResult readTrigger(Response response) {
        final JsonObject document = channel.readObject(
            response,
            "Yogabase returned an invalid schedule run.",
            "Yogabase returned a schedule run document that is not JSON.");
        final Integer outcome = YogabaseGatewayChannel.whole(document, "outcome");
        final UUID invocationId = YogabaseGatewayChannel.canonicalUuid(YogabaseGatewayChannel.text(document, "invocationId"));
        final YogabaseScheduleTriggerOutcome named = outcome == null ? null : YogabaseScheduleTriggerOutcome.fromWireValue(outcome);
        if (named == null || invocationId == null) {
            throw channel.invalid("Yogabase returned an invalid schedule run.", response);
        }
        return new YogabaseScheduleTriggerResult(named, invocationId);
    }
}
