package io.stackyoga.yogabase;

import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParseException;
import com.google.gson.JsonParser;
import com.google.gson.JsonPrimitive;
import com.microsoft.kiota.NativeResponseHandler;
import com.microsoft.kiota.RequestAdapter;
import com.microsoft.kiota.RequestInformation;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.math.BigDecimal;
import java.nio.charset.StandardCharsets;
import java.time.OffsetDateTime;
import java.time.format.DateTimeParseException;
import java.util.HashMap;
import java.util.Objects;
import java.util.UUID;
import java.util.regex.Pattern;
import okhttp3.Response;
import okhttp3.ResponseBody;

/**
 * What the authored application-gateway clients ({@link YogabaseJobsClient}, {@link YogabaseSchedulesClient})
 * share: they build a request with the generated Kiota request builders, send it through the application
 * client's own request adapter, and read the native OkHttp response themselves, because what they need from it
 * (the result's digest header, the schedule's ETag) is a response header a generated builder cannot hand back.
 * This class sends that request, turns a refusal into the client's own exception, and holds the bounded reads
 * and the strict JSON member readers both clients validate an answer with.
 */
final class YogabaseGatewayChannel {
    private static final int MAXIMUM_DOCUMENT_BYTES = 1_048_576;
    private static final Pattern WHOLE_NUMBER = Pattern.compile("-?[0-9]+");

    /** Builds the exception one client raises. */
    @FunctionalInterface
    interface Failure {
        YogabaseGatewayException create(
            String message, int status, String code, String correlationId, Long limitBytes, Long maxBodyBytes);
    }

    private final RequestAdapter requestAdapter;
    private final Failure failure;
    private final String responseInvalidCode;

    YogabaseGatewayChannel(RequestAdapter requestAdapter, Failure failure, String responseInvalidCode) {
        this.requestAdapter = Objects.requireNonNull(requestAdapter, "requestAdapter");
        this.failure = failure;
        this.responseInvalidCode = responseInvalidCode;
    }

    /**
     * Sends one request through the adapter and hands back its native response if it succeeded. A non-2xx status
     * is turned into the client's exception, so every caller sees only an answer it may read. The caller owns the
     * response and closes it.
     */
    Response send(RequestInformation information) {
        final NativeResponseHandler handler = new NativeResponseHandler();
        information.setResponseHandler(handler);
        requestAdapter.sendPrimitive(information, new HashMap<>(), Void.class);
        if (!(handler.getValue() instanceof Response response)) {
            throw new IllegalStateException(
                "The request adapter did not hand back its native OkHttp response, which the application "
                    + "gateway clients read; use the OkHttpRequestAdapter of the Kiota bundle.");
        }
        if (response.code() < 200 || response.code() >= 300) {
            try (response) {
                throw problem(response);
            }
        }
        return response;
    }

    /** The exception for an answer that is not what the route declares: a 502 this client raises itself. */
    YogabaseGatewayException invalid(String message, Response response) {
        return failure.create(message, 502, responseInvalidCode, response.header("X-Correlation-ID"), null, null);
    }

    /** Reads the answer as one bounded JSON object, or refuses it as {@code invalidMessage}. */
    JsonObject readObject(Response response, String invalidMessage, String notJsonMessage) {
        try {
            final ResponseBody responseBody = response.body();
            if (responseBody == null) {
                throw invalid(invalidMessage, response);
            }
            final JsonElement parsed = JsonParser.parseString(new String(
                readBounded(responseBody.byteStream(), MAXIMUM_DOCUMENT_BYTES, response, "Yogabase returned an oversized response."),
                StandardCharsets.UTF_8));
            if (!parsed.isJsonObject()) {
                throw invalid(invalidMessage, response);
            }
            return parsed.getAsJsonObject();
        } catch (JsonParseException exception) {
            throw invalid(notJsonMessage, response);
        }
    }

    byte[] readBounded(InputStream stream, int maximumBytes, Response response, String oversizeMessage) {
        try (InputStream input = stream) {
            final ByteArrayOutputStream output = new ByteArrayOutputStream();
            final byte[] buffer = new byte[16 * 1024];
            int total = 0;
            int read;
            while ((read = input.read(buffer)) != -1) {
                total += read;
                if (total > maximumBytes) {
                    throw invalid(oversizeMessage, response);
                }
                output.write(buffer, 0, read);
            }
            return output.toByteArray();
        } catch (IOException exception) {
            throw failure.create(
                "The response from Yogabase could not be read.",
                502,
                responseInvalidCode,
                response.header("X-Correlation-ID"),
                null,
                null);
        }
    }

    private YogabaseGatewayException problem(Response response) {
        JsonObject document = null;
        try {
            final ResponseBody responseBody = response.body();
            if (responseBody != null) {
                final JsonElement parsed = JsonParser.parseString(new String(
                    readBounded(responseBody.byteStream(), MAXIMUM_DOCUMENT_BYTES, response, "Yogabase returned an oversized response."),
                    StandardCharsets.UTF_8));
                if (parsed.isJsonObject()) {
                    document = parsed.getAsJsonObject();
                }
            }
        } catch (JsonParseException | IllegalStateException | YogabaseGatewayException ignored) {
            // The status stands when the body is not a problem document.
        }
        final String fromStatus = switch (response.code()) {
            case 401 -> YogabaseStatusErrorCodes.AUTHENTICATION_REQUIRED;
            case 403 -> YogabaseStatusErrorCodes.FORBIDDEN;
            case 404 -> YogabaseStatusErrorCodes.NOT_FOUND;
            case 409 -> YogabaseStatusErrorCodes.CONFLICT;
            case 412 -> YogabaseStatusErrorCodes.PRECONDITION_FAILED;
            // A gateway that predates the named refusal answers a bare 413 for a body over its listener's limit.
            case 413 -> YogabaseStatusErrorCodes.PAYLOAD_TOO_LARGE;
            case 415 -> YogabaseStatusErrorCodes.UNSUPPORTED_MEDIA_TYPE;
            case 428 -> YogabaseStatusErrorCodes.PRECONDITION_REQUIRED;
            case 429 -> YogabaseStatusErrorCodes.RATE_LIMITED;
            default -> YogabaseStatusErrorCodes.REQUEST_FAILED;
        };
        final String detail = text(document, "detail");
        final String title = text(document, "title");
        final String code = text(document, "code");
        final String trace = text(document, "traceId");
        final String correlation = response.header("X-Correlation-ID");
        return failure.create(
            detail != null ? detail : title != null ? title : "Yogabase returned HTTP " + response.code() + ".",
            response.code(),
            code != null ? code : fromStatus,
            correlation != null ? correlation : trace,
            positive(document, "limitBytes"),
            positive(document, "maxBodyBytes"));
    }

    // ---------------------------------------------------------------------------------------
    // Strict member readers
    // ---------------------------------------------------------------------------------------

    /** A member that may be null: {@code present} is false for a member that is neither null nor text. */
    record Nullable<T>(boolean present, T value) {
    }

    static Nullable<String> nullableText(JsonObject document, String name) {
        final JsonElement element = document.get(name);
        if (element == null || element.isJsonNull()) {
            return new Nullable<>(true, null);
        }
        final String value = text(document, name);
        return new Nullable<>(value != null, value);
    }

    static String text(JsonObject document, String name) {
        if (document == null) {
            return null;
        }
        final JsonElement element = document.get(name);
        return element != null && element.isJsonPrimitive() && element.getAsJsonPrimitive().isString()
            ? element.getAsString()
            : null;
    }

    static Boolean flag(JsonObject document, String name) {
        final JsonElement element = document.get(name);
        return element != null && element.isJsonPrimitive() && element.getAsJsonPrimitive().isBoolean()
            ? element.getAsBoolean()
            : null;
    }

    static Integer whole(JsonObject document, String name) {
        final Long value = wholeNumber(document, name);
        return value != null && value >= Integer.MIN_VALUE && value <= Integer.MAX_VALUE ? Integer.valueOf(value.intValue()) : null;
    }

    static Long positive(JsonObject document, String name) {
        final Long value = document == null ? null : wholeNumber(document, name);
        return value != null && value > 0 ? value : null;
    }

    static Long wholeNumber(JsonObject document, String name) {
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

    /** The UUID of a canonical hyphenated, non-zero text, or null. */
    static UUID canonicalUuid(String value) {
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

    static OffsetDateTime timestamp(String value) {
        if (value == null) {
            return null;
        }
        try {
            return OffsetDateTime.parse(value);
        } catch (DateTimeParseException exception) {
            return null;
        }
    }
}
