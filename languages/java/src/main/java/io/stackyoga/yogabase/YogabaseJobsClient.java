package io.stackyoga.yogabase;

import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParseException;
import com.google.gson.JsonParser;
import com.google.gson.JsonPrimitive;
import com.microsoft.kiota.HttpMethod;
import com.microsoft.kiota.NativeResponseHandler;
import com.microsoft.kiota.RequestAdapter;
import com.microsoft.kiota.RequestInformation;
import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.math.BigDecimal;
import java.net.URI;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.time.OffsetDateTime;
import java.time.format.DateTimeParseException;
import java.util.Base64;
import java.util.HashMap;
import java.util.HexFormat;
import java.util.List;
import java.util.Map;
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
 * <p>Jobs are served by the product (owner decision, 2026-10-03) but its client OpenAPI document does not
 * describe the routes yet, so Kiota generates nothing for them and this class is written from the gateway's
 * source and developer documentation instead; {@code YogabaseJobsClientTest} pins every shape. It shares the
 * Kiota request adapter, and so the authentication and the origin, of the generated application client
 * ({@link YogabaseApplicationClient#jobs}), and reads the response through the adapter's native response
 * because the result route's digest is a response header: it needs the {@code OkHttpRequestAdapter} the
 * Kiota bundle provides, whose native response is an OkHttp {@link Response}.
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

    private static final int MAXIMUM_DOCUMENT_BYTES = 1_048_576;
    private static final int MAXIMUM_JOB_TYPE_LENGTH = 128;
    private static final int MAXIMUM_IDEMPOTENCY_KEY_LENGTH = 256;
    private static final Pattern JOB_TYPE = Pattern.compile("[A-Za-z0-9._:-]{1,128}");
    private static final Pattern FAILURE_CODE = Pattern.compile("[a-z0-9_]{1,128}");
    private static final Pattern RESULT_DIGEST = Pattern.compile("sha256:[0-9a-f]{64}");
    private static final Pattern WHOLE_NUMBER = Pattern.compile("-?[0-9]+");
    private static final String ROUTE = "{+baseurl}/v1/projects/{projectId}/jobs";
    private static final String DOCUMENT_ACCEPT = "application/json, application/problem+json";
    private static final String RESULT_ACCEPT = "application/octet-stream, application/problem+json";

    private final RequestAdapter requestAdapter;
    private final URI origin;
    private final UUID projectId;

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
        // The member order and the names are the gateway's submission, which refuses any other member. A job
        // with no payload and one with an empty payload are the same job to the gateway.
        final StringBuilder body = new StringBuilder("{\"jobType\":").append(quote(request.jobType()))
            .append(",\"idempotencyKey\":").append(quote(request.idempotencyKey()));
        if (request.payload() != null && request.payload().length > 0) {
            body.append(",\"payloadBase64\":\"")
                .append(Base64.getEncoder().encodeToString(request.payload())).append('"');
        }
        if (request.maxAttempts() != null) {
            body.append(",\"maxAttempts\":").append(request.maxAttempts().intValue());
        }
        body.append('}');
        try (Response response = send(HttpMethod.POST, "", null, body.toString().getBytes(StandardCharsets.UTF_8), DOCUMENT_ACCEPT)) {
            return readJob(response);
        }
    }

    /**
     * Reads the caller's own job. Poll this until {@link YogabaseJob#isTerminal()}.
     *
     * @throws YogabaseJobsException {@link YogabaseJobErrorCodes#NOT_FOUND} (404) for no such job and for another user's
     */
    public YogabaseJob get(UUID jobId) {
        try (Response response = send(HttpMethod.GET, "/{jobId}", requireId(jobId, "jobId"), null, DOCUMENT_ACCEPT)) {
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
        try (Response response = send(HttpMethod.POST, "/{jobId}/cancel", requireId(jobId, "jobId"), null, DOCUMENT_ACCEPT)) {
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
        try (Response response = send(HttpMethod.GET, "/{jobId}/result", requireId(jobId, "jobId"), null, RESULT_ACCEPT)) {
            // Headers joins nothing: a repeated header is a list of more than one value, which is not trusted.
            final List<String> declared = response.headers(RESULT_DIGEST_HEADER);
            if (response.code() != 200 || declared.size() != 1 || !RESULT_DIGEST.matcher(declared.get(0)).matches()) {
                throw invalid("Yogabase returned an invalid job result.", response);
            }
            final ResponseBody responseBody = response.body();
            if (responseBody == null || responseBody.contentLength() > MAXIMUM_RESULT_BYTES) {
                throw invalid("Yogabase returned an invalid or oversized job result.", response);
            }
            final byte[] content = readBounded(responseBody.byteStream(), MAXIMUM_RESULT_BYTES, response);
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
     * Sends one request through the adapter and hands back its native response, whatever its status. The
     * caller owns the response and closes it. A non-2xx status is turned into the exception it is here, so
     * every caller sees only an answer it may read.
     */
    private Response send(HttpMethod method, String template, UUID jobId, byte[] body, String accept) {
        final Map<String, Object> pathParameters = new HashMap<>();
        pathParameters.put("baseurl", requestAdapter.getBaseUrl());
        pathParameters.put("projectId", projectId.toString());
        if (jobId != null) {
            pathParameters.put("jobId", jobId.toString());
        }
        final RequestInformation information = new RequestInformation(method, ROUTE + template, pathParameters);
        information.headers.add("Accept", accept);
        if (body != null) {
            information.setStreamContent(new ByteArrayInputStream(body), "application/json");
        }
        final NativeResponseHandler handler = new NativeResponseHandler();
        information.setResponseHandler(handler);
        requestAdapter.sendPrimitive(information, new HashMap<>(), Void.class);
        if (!(handler.getValue() instanceof Response response)) {
            throw new IllegalStateException(
                "The request adapter did not hand back its native OkHttp response, which the Jobs client reads; "
                    + "use the OkHttpRequestAdapter of the Kiota bundle.");
        }
        if (response.code() < 200 || response.code() >= 300) {
            try (response) {
                throw problem(response);
            }
        }
        return response;
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

    /** A JSON string literal, ASCII only: everything outside printable ASCII travels as a \\u escape. */
    private static String quote(String value) {
        final StringBuilder text = new StringBuilder("\"");
        for (int index = 0; index < value.length(); index++) {
            final char character = value.charAt(index);
            switch (character) {
                case '"' -> text.append("\\\"");
                case '\\' -> text.append("\\\\");
                default -> {
                    if (character < 0x20 || character > 0x7e) {
                        text.append(String.format("\\u%04x", (int) character));
                    } else {
                        text.append(character);
                    }
                }
            }
        }
        return text.append('"').toString();
    }

    private static YogabaseJobsException invalid(String message, Response response) {
        return new YogabaseJobsException(
            message, 502, YogabaseJobErrorCodes.RESPONSE_INVALID, response.header("X-Correlation-ID"), null, null);
    }

    private static YogabaseJobsException problem(Response response) {
        JsonObject document = null;
        try {
            final ResponseBody responseBody = response.body();
            if (responseBody != null) {
                final JsonElement parsed = JsonParser.parseString(new String(
                    readBounded(responseBody.byteStream(), MAXIMUM_DOCUMENT_BYTES, response), StandardCharsets.UTF_8));
                if (parsed.isJsonObject()) {
                    document = parsed.getAsJsonObject();
                }
            }
        } catch (JsonParseException | IllegalStateException | YogabaseJobsException ignored) {
            // The status stands when the body is not a problem document.
        }
        final String fromStatus = switch (response.code()) {
            case 401 -> "authentication_required";
            case 403 -> "forbidden";
            case 404 -> "not_found";
            case 409 -> "conflict";
            // A gateway that predates the named refusal answers a bare 413 for a body over its listener's limit.
            case 413 -> "payload_too_large";
            case 429 -> "rate_limited";
            default -> "request_failed";
        };
        final String detail = text(document, "detail");
        final String title = text(document, "title");
        final String code = text(document, "code");
        final String trace = text(document, "traceId");
        final String correlation = response.header("X-Correlation-ID");
        return new YogabaseJobsException(
            detail != null ? detail : title != null ? title : "Yogabase returned HTTP " + response.code() + ".",
            response.code(),
            code != null ? code : fromStatus,
            correlation != null ? correlation : trace,
            positive(document, "limitBytes"),
            positive(document, "maxBodyBytes"));
    }

    // A job the gateway describes wrongly is worse than one it refuses: a caller that polls for a state it was
    // never told about waits for ever. Reject the answer instead of returning a guess. A member this client does
    // not know is ignored, as the generated clients keep unknown members aside.
    private static YogabaseJob readJob(Response response) {
        final JsonObject document;
        try {
            final ResponseBody responseBody = response.body();
            if (responseBody == null) {
                throw invalid("Yogabase returned an invalid job.", response);
            }
            final JsonElement parsed = JsonParser.parseString(new String(
                readBounded(responseBody.byteStream(), MAXIMUM_DOCUMENT_BYTES, response), StandardCharsets.UTF_8));
            if (!parsed.isJsonObject()) {
                throw invalid("Yogabase returned an invalid job.", response);
            }
            document = parsed.getAsJsonObject();
        } catch (JsonParseException exception) {
            throw invalid("Yogabase returned a job document that is not JSON.", response);
        }

        final String jobId = text(document, "jobId");
        final String jobType = text(document, "jobType");
        final YogabaseJobStatus status = YogabaseJobStatus.fromWireName(text(document, "status"));
        final Integer attempt = whole(document, "attempt");
        final Integer maxAttempts = whole(document, "maxAttempts");
        final Integer progress = whole(document, "progressPercent");
        final Boolean cancellation = flag(document, "cancellationRequested");
        final Nullable<String> resultReference = nullableText(document, "resultReference");
        final Nullable<String> failureCode = nullableText(document, "failureCode");
        final OffsetDateTime createdAt = timestamp(text(document, "createdAt"));
        final OffsetDateTime updatedAt = timestamp(text(document, "updatedAt"));
        final Nullable<String> completed = nullableText(document, "completedAt");
        final OffsetDateTime completedAt = completed.present() && completed.value() != null ? timestamp(completed.value()) : null;
        final UUID parsedJobId = canonicalUuid(jobId);
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
            throw invalid("Yogabase returned an invalid job.", response);
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

    /** A member that may be null: {@code present} is false for a member that is neither null nor text. */
    private record Nullable<T>(boolean present, T value) {
    }

    private static Nullable<String> nullableText(JsonObject document, String name) {
        final JsonElement element = document.get(name);
        if (element == null || element.isJsonNull()) {
            return new Nullable<>(true, null);
        }
        final String value = text(document, name);
        return new Nullable<>(value != null, value);
    }

    private static String text(JsonObject document, String name) {
        if (document == null) {
            return null;
        }
        final JsonElement element = document.get(name);
        return element != null && element.isJsonPrimitive() && element.getAsJsonPrimitive().isString()
            ? element.getAsString()
            : null;
    }

    private static Boolean flag(JsonObject document, String name) {
        final JsonElement element = document.get(name);
        return element != null && element.isJsonPrimitive() && element.getAsJsonPrimitive().isBoolean()
            ? element.getAsBoolean()
            : null;
    }

    private static Integer whole(JsonObject document, String name) {
        final Long value = wholeNumber(document, name);
        return value != null && value >= Integer.MIN_VALUE && value <= Integer.MAX_VALUE ? Integer.valueOf(value.intValue()) : null;
    }

    private static Long positive(JsonObject document, String name) {
        final Long value = document == null ? null : wholeNumber(document, name);
        return value != null && value > 0 ? value : null;
    }

    private static Long wholeNumber(JsonObject document, String name) {
        final JsonElement element = document.get(name);
        if (element == null || !element.isJsonPrimitive()) {
            return null;
        }
        final JsonPrimitive primitive = element.getAsJsonPrimitive();
        if (!primitive.isNumber() || !WHOLE_NUMBER.matcher(primitive.getAsString()).matches()) {
            return null;
        }
        try {
            return new BigDecimal(primitive.getAsString()).longValueExact();
        } catch (ArithmeticException exception) {
            return null;
        }
    }

    private static UUID canonicalUuid(String value) {
        if (value == null || value.length() != 36) {
            return null;
        }
        try {
            final UUID parsed = UUID.fromString(value);
            return parsed.toString().equalsIgnoreCase(value) && (parsed.getMostSignificantBits() != 0L || parsed.getLeastSignificantBits() != 0L)
                ? parsed
                : null;
        } catch (IllegalArgumentException exception) {
            return null;
        }
    }

    private static OffsetDateTime timestamp(String value) {
        if (value == null) {
            return null;
        }
        try {
            return OffsetDateTime.parse(value);
        } catch (DateTimeParseException exception) {
            return null;
        }
    }

    // The reference is opaque to a client: bounded, an object:// name, and nothing that could be read as more
    // than text. Its grammar belongs to the gateway and is not restated here.
    private static boolean isResultReference(String value) {
        return value.length() > 9
            && value.length() <= 2048
            && value.startsWith("object://")
            && value.chars().noneMatch(character -> Character.isWhitespace(character) || Character.isISOControl(character));
    }

    private static byte[] readBounded(InputStream stream, int maximumBytes, Response response) {
        try (InputStream input = stream) {
            final ByteArrayOutputStream output = new ByteArrayOutputStream();
            final byte[] buffer = new byte[16 * 1024];
            int total = 0;
            int read;
            while ((read = input.read(buffer)) != -1) {
                total += read;
                if (total > maximumBytes) {
                    throw invalid("Yogabase returned an oversized response.", response);
                }
                output.write(buffer, 0, read);
            }
            return output.toByteArray();
        } catch (IOException exception) {
            throw new YogabaseJobsException(
                "The response from Yogabase could not be read.",
                502,
                YogabaseJobErrorCodes.RESPONSE_INVALID,
                response.header("X-Correlation-ID"),
                null,
                null);
        }
    }

    private static byte[] sha256(byte[] content) {
        try {
            return MessageDigest.getInstance("SHA-256").digest(content);
        } catch (NoSuchAlgorithmException exception) {
            throw new IllegalStateException("SHA-256 is required of every Java platform", exception);
        }
    }
}
