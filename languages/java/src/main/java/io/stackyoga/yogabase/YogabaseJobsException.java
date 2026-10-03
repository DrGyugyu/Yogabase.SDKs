package io.stackyoga.yogabase;

/**
 * A non-successful Jobs response, or an answer the Jobs client refuses, with the gateway's problem details.
 * It is unchecked, like the Kiota exceptions the generated clients throw.
 */
public final class YogabaseJobsException extends RuntimeException {
    private static final long serialVersionUID = 1L;

    private final int status;
    private final String code;
    private final String correlationId;
    private final Long limitBytes;
    private final Long maxBodyBytes;

    YogabaseJobsException(
        String message,
        int status,
        String code,
        String correlationId,
        Long limitBytes,
        Long maxBodyBytes) {
        super(message);
        this.status = status;
        this.code = code;
        this.correlationId = correlationId;
        this.limitBytes = limitBytes;
        this.maxBodyBytes = maxBodyBytes;
    }

    /** The HTTP status of the gateway's answer, or 502 for an answer this client refused. */
    public int status() {
        return status;
    }

    /**
     * The gateway's stable code (see {@link YogabaseJobErrorCodes}), or one derived from the status when the
     * body names none: {@code authentication_required}, {@code forbidden}, {@code not_found},
     * {@code conflict}, {@code payload_too_large}, {@code rate_limited} or {@code request_failed}.
     */
    public String code() {
        return code;
    }

    /** The correlation id the response carried, or {@code null}. */
    public String correlationId() {
        return correlationId;
    }

    /** The most bytes the refused payload may have, before base64, when a 413 named it; otherwise {@code null}. */
    public Long limitBytes() {
        return limitBytes;
    }

    /** The most bytes the whole request body may have, when a 413 named it; otherwise {@code null}. */
    public Long maxBodyBytes() {
        return maxBodyBytes;
    }
}
