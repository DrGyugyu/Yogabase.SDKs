package io.stackyoga.yogabase;

/**
 * The codes the Jobs routes refuse with, as {@link YogabaseJobsException#code()} carries them. Every code up
 * to {@link #RESULT_REFERENCE_INVALID} is the gateway's; the last two are this client's own, for an answer
 * that is not what the route declares.
 */
public final class YogabaseJobErrorCodes {
    private YogabaseJobErrorCodes() {
    }

    /** 422: the job type is not the id of a deployed function. */
    public static final String TYPE_NOT_REGISTERED = "job_type_not_registered";

    /** 409: the idempotency key already belongs to a different request by this user. */
    public static final String IDEMPOTENCY_CONFLICT = "job_idempotency_conflict";

    /**
     * 413: the payload is over 1 MiB before base64 encoding, or the request body is over the largest the
     * route reads. {@link YogabaseJobsException#limitBytes()} and {@link YogabaseJobsException#maxBodyBytes()}
     * name both limits.
     */
    public static final String PAYLOAD_TOO_LARGE = "job_payload_too_large";

    /** 400: a field is malformed, or the payload is not base64. */
    public static final String REQUEST_INVALID = "job_request_invalid";

    /** 404: no such job, which is also another user's job (the two are never told apart). */
    public static final String NOT_FOUND = "job_not_found";

    /** 403: the token may not call the Jobs routes, or names another project. */
    public static final String ACCESS_DENIED = "job_access_denied";

    /** 503: the job store could not be reached. */
    public static final String UNAVAILABLE = "job_unavailable";

    /** 404: the job has no result to give (another user's, unknown, or finished without one). */
    public static final String RESULT_NOT_FOUND = "job_result_not_found";

    /** 409: the job is the caller's own and has not settled. */
    public static final String RESULT_NOT_READY = "job_result_not_ready";

    /** 502: the stored result is not what the job recorded; no byte of it was sent. */
    public static final String RESULT_INTEGRITY_FAILED = "job_result_integrity_failed";

    /** 500: the job's stored result reference is not that job's own; no byte of it was sent. */
    public static final String RESULT_REFERENCE_INVALID = "job_result_reference_invalid";

    /** This client: the gateway's answer to a Jobs call is not what the route declares. */
    public static final String RESPONSE_INVALID = "job_response_invalid";

    /** This client: the result's bytes do not hash to the digest the gateway sent with them. */
    public static final String RESULT_DIGEST_MISMATCH = "job_result_digest_mismatch";
}
