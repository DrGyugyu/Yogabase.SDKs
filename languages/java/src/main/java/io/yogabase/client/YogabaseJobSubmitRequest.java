package io.yogabase.client;

import java.util.Arrays;
import java.util.Objects;

/**
 * What a client asks the Jobs submit route to start: one durable run of a deployed stateless function. The
 * project and the subject are never fields: the project is the client's own and the subject is the
 * signed-in user.
 *
 * @param jobType the id of a deployed stateless function; its highest released version runs the job. 1 to
 *     128 ASCII letters, digits, {@code .}, {@code -}, {@code _} or {@code :}, with no surrounding whitespace
 * @param idempotencyKey the caller's key for this request, 1 to 256 characters, trimmed, with no control
 *     character. The same request under the same key is the job it already made (retrying a submit is
 *     safe); a different request under that key is refused with
 *     {@link YogabaseJobErrorCodes#IDEMPOTENCY_CONFLICT}. The key belongs to the signed-in user. Keep one
 *     key across the retries of one submit and use a new one for a new job
 * @param payload the function's request, at most {@link YogabaseJobsClient#MAXIMUM_PAYLOAD_BYTES} bytes, or
 *     {@code null} for a job that carries none
 * @param maxAttempts how many times the job may be attempted, 1 to 10, or {@code null} for the gateway's
 *     default of 5
 */
public record YogabaseJobSubmitRequest(String jobType, String idempotencyKey, byte[] payload, Integer maxAttempts) {
    /** A job with no payload and the gateway's default attempt budget. */
    public YogabaseJobSubmitRequest(String jobType, String idempotencyKey) {
        this(jobType, idempotencyKey, null, null);
    }

    @Override
    public boolean equals(Object other) {
        return other instanceof YogabaseJobSubmitRequest request
            && Objects.equals(jobType, request.jobType)
            && Objects.equals(idempotencyKey, request.idempotencyKey)
            && Arrays.equals(payload, request.payload)
            && Objects.equals(maxAttempts, request.maxAttempts);
    }

    @Override
    public int hashCode() {
        return Objects.hash(jobType, idempotencyKey, Arrays.hashCode(payload), maxAttempts);
    }

    @Override
    public String toString() {
        return "YogabaseJobSubmitRequest[jobType=" + jobType + ", payload="
            + (payload == null ? "none" : payload.length + " bytes") + ", maxAttempts=" + maxAttempts + "]";
    }
}
