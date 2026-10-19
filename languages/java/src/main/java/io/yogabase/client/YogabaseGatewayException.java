package io.yogabase.client;

/**
 * A non-successful response from an authored application-gateway client, or an answer such a client refuses,
 * with the gateway's problem details. {@link YogabaseJobsException} and {@link YogabaseSchedulesException} are
 * the two a caller meets; catching this class catches both. It is unchecked, like the Kiota exceptions the
 * generated clients throw.
 */
public class YogabaseGatewayException extends RuntimeException {
    private static final long serialVersionUID = 1L;

    private final int status;
    private final String code;
    private final String correlationId;
    private final Long limitBytes;
    private final Long maxBodyBytes;

    YogabaseGatewayException(
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
    public final int status() {
        return status;
    }

    /**
     * The gateway's stable code, or one derived from the status when the body names none: {@code
     * authentication_required}, {@code forbidden}, {@code not_found}, {@code conflict}, {@code
     * precondition_failed}, {@code payload_too_large}, {@code unsupported_media_type}, {@code
     * precondition_required}, {@code rate_limited} or {@code request_failed} (see {@link
     * YogabaseStatusErrorCodes}).
     */
    public final String code() {
        return code;
    }

    /** The correlation id the response carried, or {@code null}. */
    public final String correlationId() {
        return correlationId;
    }

    /** The most bytes the refused payload may have, before base64, when a 413 named it; otherwise {@code null}. */
    public final Long limitBytes() {
        return limitBytes;
    }

    /** The most bytes the whole request body may have, when a 413 named it; otherwise {@code null}. */
    public final Long maxBodyBytes() {
        return maxBodyBytes;
    }
}
