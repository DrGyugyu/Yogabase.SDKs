package io.yogabase.client;

import com.google.gson.JsonObject;
import com.microsoft.kiota.RequestAdapter;
import io.yogabase.client.models.JobSubmitRequest;
import io.yogabase.client.v1.projects.item.jobs.JobsRequestBuilder;
import java.net.URI;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.time.OffsetDateTime;
import java.util.Base64;
import java.util.HexFormat;
import java.util.List;
import java.util.Objects;
import java.util.UUID;
import java.util.regex.Pattern;
import okhttp3.Response;
import okhttp3.ResponseBody;

/**
 * Durable background jobs for the signed-in user: submit a run of a deployed stateless function, read it,
 * cancel it and fetch what it returned, against {@code /v1/projects/{projectId}/jobs} on the application
 * gateway and behind the same project token (the {@code lambda.invoke} scope) as invoking a function. A job
 * type is the id of a deployed stateless function. A job belongs to the user who submitted it, and another
 * user's job is answered exactly as one that does not exist.
 *
 * <p>The product's client OpenAPI document describes the Jobs routes, so Kiota generates their request builders
 * ({@code api().v1().projects().byProjectId(id).jobs()}). This class wraps those builders: each request is the
 * one the generated builder builds (its route, its media types and its serialized body), sent through the
 * Kiota request adapter of the generated application client ({@link YogabaseApplicationClient#jobs}), and so
 * with its authentication and origin. What it adds is what a generated builder cannot give: it reads the
 * response through the adapter's native response, because the result route's digest is a response header, so
 * it needs the {@code OkHttpRequestAdapter} the Kiota bundle provides, whose native response is an OkHttp
 * {@link Response}; it validates each call before it is sent; and it refuses an answer that is not what the
 * route declares and names every refusal by its code. {@code YogabaseJobsClientTest} pins every shape.
 *
 * <p>No capability gates it: the bootstrap document carries no capability identifier for Jobs, so the client
 * does not pretend to know whether an installation serves them. An installation without them answers a plain
 * 404 whose {@link YogabaseJobsException#code()} is {@code not_found}; a missing job is
 * {@link YogabaseJobErrorCodes#NOT_FOUND}, a different code. Calls block; run them off the UI thread.
 */
public final class YogabaseJobsClient {
    /** The most bytes a job's payload may have, before base64 encoding: the 1 MiB inline limit. */
    public static final int MAXIMUM_PAYLOAD_BYTES = 1_048_576;

    /** The most attempts a job may be allowed. */
    public static final int MAXIMUM_ATTEMPTS = 10;

    /** The most bytes a job's result may have: 16 MiB. */
    public static final int MAXIMUM_RESULT_BYTES = 16 * 1_048_576;

    /** The response header that carries the SHA-256 the result's bytes were verified against. */
    public static final String RESULT_DIGEST_HEADER = "X-Yogabase-Content-Sha256";

    private static final int MAXIMUM_JOB_TYPE_LENGTH = 128;
    private static final int MAXIMUM_IDEMPOTENCY_KEY_LENGTH = 256;
    private static final Pattern JOB_TYPE = Pattern.compile("[A-Za-z0-9._:-]{1,128}");
    private static final Pattern FAILURE_CODE = Pattern.compile("[a-z0-9_]{1,128}");
    private static final Pattern RESULT_DIGEST = Pattern.compile("sha256:[0-9a-f]{64}");

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
    public YogabaseJobsClient(RequestAdapter requestAdapter, URI applicationOrigin, UUID projectId) {
        this.requestAdapter = Objects.requireNonNull(requestAdapter, "requestAdapter");
        this.origin = YogabaseClientOrigin.requireOrigin(applicationOrigin, "applicationOrigin");
        this.projectId = requireId(projectId, "projectId");
        requestAdapter.setBaseUrl(YogabaseClientOrigin.baseUrl(this.origin));
        this.channel = new YogabaseGatewayChannel(requestAdapter, YogabaseJobsException::new, YogabaseJobErrorCodes.RESPONSE_INVALID);
    }

    public URI origin() {
        return origin;
    }

    public UUID projectId() {
        return projectId;
    }

    /**
     * Starts a job and returns it as the gateway accepted it (202), normally {@link YogabaseJobStatus#QUEUED}.
     * Idempotent per user and idempotency key: the same request again returns the job it already made,
     * whatever that job has become.
     *
     * @throws YogabaseJobsException {@link YogabaseJobErrorCodes#TYPE_NOT_REGISTERED} (422),
     *     {@link YogabaseJobErrorCodes#IDEMPOTENCY_CONFLICT} (409), {@link YogabaseJobErrorCodes#PAYLOAD_TOO_LARGE}
     *     (413), {@link YogabaseJobErrorCodes#REQUEST_INVALID} (400), {@link YogabaseJobErrorCodes#ACCESS_DENIED} (403)
     *     or {@link YogabaseJobErrorCodes#UNAVAILABLE} (503)
     */
    public YogabaseJob submit(YogabaseJobSubmitRequest request) {
        Objects.requireNonNull(request, "request");
        validate(request);
        // A job with no payload and one with an empty payload are the same job to the gateway.
        final JobSubmitRequest body = new JobSubmitRequest();
        body.setJobType(request.jobType());
        body.setIdempotencyKey(request.idempotencyKey());
        if (request.payload() != null && request.payload().length > 0) {
            body.setPayloadBase64(Base64.getEncoder().encodeToString(request.payload()));
        }
        if (request.maxAttempts() != null) {
            body.setMaxAttempts(request.maxAttempts());
        }
        try (Response response = channel.send(jobs().toPostRequestInformation(body))) {
            return readJob(response);
        }
    }

    /**
     * Reads the caller's own job. Poll this until {@link YogabaseJob#isTerminal()}.
     *
     * @throws YogabaseJobsException {@link YogabaseJobErrorCodes#NOT_FOUND} (404) for no such job and for another user's
     */
    public YogabaseJob get(UUID jobId) {
        try (Response response = channel.send(jobs().byJobId(requireId(jobId, "jobId")).toGetRequestInformation())) {
            return readJob(response);
        }
    }

    /**
     * Asks for the caller's own job to stop, idempotently, and returns it: a queued job is cancelled at once, a
     * running one is flagged ({@link YogabaseJob#cancellationRequested()}) and stopped by its worker, and a
     * settled one is returned as it is.
     *
     * @throws YogabaseJobsException {@link YogabaseJobErrorCodes#NOT_FOUND} (404) for no such job and for another user's
     */
    public YogabaseJob cancel(UUID jobId) {
        try (Response response = channel.send(jobs().byJobId(requireId(jobId, "jobId")).cancel().toPostRequestInformation())) {
            return readJob(response);
        }
    }

    /**
     * Reads the result of the caller's own job. The bytes are hashed here and compared with the
     * {@code X-Yogabase-Content-Sha256} the gateway sent; a result that does not match, or that arrives
     * without exactly one well-formed {@code sha256:} digest, is refused and never returned.
     *
     * @throws YogabaseJobsException {@link YogabaseJobErrorCodes#RESULT_NOT_READY} (409) while the job has not
     *     settled, {@link YogabaseJobErrorCodes#RESULT_NOT_FOUND} (404) for no result to give,
     *     {@link YogabaseJobErrorCodes#RESULT_INTEGRITY_FAILED} (502) or
     *     {@link YogabaseJobErrorCodes#RESULT_REFERENCE_INVALID} (500) from the gateway, and
     *     {@link YogabaseJobErrorCodes#RESULT_DIGEST_MISMATCH} or {@link YogabaseJobErrorCodes#RESPONSE_INVALID}
     *     (502) from this client
     */
    public YogabaseJobResult result(UUID jobId) {
        try (Response response = channel.send(jobs().byJobId(requireId(jobId, "jobId")).result().toGetRequestInformation())) {
            // Headers joins nothing: a repeated header is a list of more than one value, which is not trusted.
            final List<String> declared = response.headers(RESULT_DIGEST_HEADER);
            if (response.code() != 200 || declared.size() != 1 || !RESULT_DIGEST.matcher(declared.get(0)).matches()) {
                throw channel.invalid("Yogabase returned an invalid job result.", response);
            }
            final ResponseBody responseBody = response.body();
            if (responseBody == null || responseBody.contentLength() > MAXIMUM_RESULT_BYTES) {
                throw channel.invalid("Yogabase returned an invalid or oversized job result.", response);
            }
            final byte[] content = channel.readBounded(
                responseBody.byteStream(), MAXIMUM_RESULT_BYTES, response, "Yogabase returned an oversized response.");
            final String computed = "sha256:" + HexFormat.of().formatHex(sha256(content));
            if (!computed.equals(declared.get(0))) {
                throw new YogabaseJobsException(
                    "The job result does not match the digest Yogabase sent with it, so none of it was returned.",
                    502,
                    YogabaseJobErrorCodes.RESULT_DIGEST_MISMATCH,
                    response.header("X-Correlation-ID"),
                    null,
                    null);
            }
            return new YogabaseJobResult(content, declared.get(0));
        }
    }

    /**
     * The generated Jobs request builder of this client's project. It is built for each call from the generated
     * client's own navigation, so the route, the path parameters and the base URL the adapter holds now are the
     * generated code's and never restated here.
     */
    private JobsRequestBuilder jobs() {
        return new YogabaseApiClient(requestAdapter).v1().projects().byProjectId(projectId).jobs();
    }

    private static void validate(YogabaseJobSubmitRequest request) {
        final String jobType = request.jobType();
        if (jobType == null || jobType.length() > MAXIMUM_JOB_TYPE_LENGTH || !JOB_TYPE.matcher(jobType).matches()) {
            throw new IllegalArgumentException(
                "a job type is 1 to 128 ASCII letters, digits, '.', '-', '_' or ':', with no surrounding whitespace");
        }
        final String key = request.idempotencyKey();
        if (key == null
            || key.isBlank()
            || key.length() > MAXIMUM_IDEMPOTENCY_KEY_LENGTH
            || !key.equals(key.strip())
            || key.chars().anyMatch(Character::isISOControl)) {
            throw new IllegalArgumentException(
                "an idempotency key is 1 to 256 characters, trimmed, with no control character");
        }
        if (request.payload() != null && request.payload().length > MAXIMUM_PAYLOAD_BYTES) {
            throw new IllegalArgumentException("a job payload is limited to one MiB before base64 encoding");
        }
        if (request.maxAttempts() != null && (request.maxAttempts() < 1 || request.maxAttempts() > MAXIMUM_ATTEMPTS)) {
            throw new IllegalArgumentException("a job allows between 1 and 10 attempts");
        }
    }

    private static UUID requireId(UUID value, String name) {
        Objects.requireNonNull(value, name);
        if (value.getMostSignificantBits() == 0L && value.getLeastSignificantBits() == 0L) {
            throw new IllegalArgumentException(name + " must be a non-empty UUID");
        }
        return value;
    }

    // A job the gateway describes wrongly is worse than one it refuses: a caller that polls for a state it was
    // never told about waits for ever. Reject the answer instead of returning a guess. A member this client does
    // not know is ignored, as the generated clients keep unknown members aside.
    private YogabaseJob readJob(Response response) {
        final JsonObject document = channel.readObject(
            response, "Yogabase returned an invalid job.", "Yogabase returned a job document that is not JSON.");

        final String jobId = YogabaseGatewayChannel.text(document, "jobId");
        final String jobType = YogabaseGatewayChannel.text(document, "jobType");
        final YogabaseJobStatus status = YogabaseJobStatus.fromWireName(YogabaseGatewayChannel.text(document, "status"));
        final Integer attempt = YogabaseGatewayChannel.whole(document, "attempt");
        final Integer maxAttempts = YogabaseGatewayChannel.whole(document, "maxAttempts");
        final Integer progress = YogabaseGatewayChannel.whole(document, "progressPercent");
        final Boolean cancellation = YogabaseGatewayChannel.flag(document, "cancellationRequested");
        final YogabaseGatewayChannel.Nullable<String> resultReference = YogabaseGatewayChannel.nullableText(document, "resultReference");
        final YogabaseGatewayChannel.Nullable<String> failureCode = YogabaseGatewayChannel.nullableText(document, "failureCode");
        final OffsetDateTime createdAt = YogabaseGatewayChannel.timestamp(YogabaseGatewayChannel.text(document, "createdAt"));
        final OffsetDateTime updatedAt = YogabaseGatewayChannel.timestamp(YogabaseGatewayChannel.text(document, "updatedAt"));
        final YogabaseGatewayChannel.Nullable<String> completed = YogabaseGatewayChannel.nullableText(document, "completedAt");
        final OffsetDateTime completedAt = completed.present() && completed.value() != null
            ? YogabaseGatewayChannel.timestamp(completed.value())
            : null;
        final UUID parsedJobId = YogabaseGatewayChannel.canonicalUuid(jobId);
        if (parsedJobId == null
            || jobType == null
            || !JOB_TYPE.matcher(jobType).matches()
            || status == null
            || attempt == null
            || attempt < 0
            || maxAttempts == null
            || maxAttempts < 1
            || maxAttempts > MAXIMUM_ATTEMPTS
            || progress == null
            || progress < 0
            || progress > 100
            || cancellation == null
            || !resultReference.present()
            || resultReference.value() != null && !isResultReference(resultReference.value())
            || !failureCode.present()
            || failureCode.value() != null && !FAILURE_CODE.matcher(failureCode.value()).matches()
            || createdAt == null
            || updatedAt == null
            || !completed.present()
            || completed.value() != null && completedAt == null) {
            throw channel.invalid("Yogabase returned an invalid job.", response);
        }
        return new YogabaseJob(
            parsedJobId,
            jobType,
            status,
            attempt,
            maxAttempts,
            progress,
            cancellation,
            resultReference.value(),
            failureCode.value(),
            createdAt,
            updatedAt,
            completedAt);
    }

    // The reference is opaque to a client: bounded, an object:// name, and nothing that could be read as more
    // than text. Its grammar belongs to the gateway and is not restated here.
    private static boolean isResultReference(String value) {
        return value.length() > 9
            && value.length() <= 2048
            && value.startsWith("object://")
            && value.chars().noneMatch(character -> Character.isWhitespace(character) || Character.isISOControl(character));
    }

    private static byte[] sha256(byte[] content) {
        try {
            return MessageDigest.getInstance("SHA-256").digest(content);
        } catch (NoSuchAlgorithmException exception) {
            throw new IllegalStateException("SHA-256 is required of every Java platform", exception);
        }
    }
}
