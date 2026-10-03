package io.stackyoga.yogabase;

/**
 * A non-successful Jobs response, or an answer the Jobs client refuses, with the gateway's problem details
 * ({@link #status()}, {@link #code()} with the codes of {@link YogabaseJobErrorCodes}, {@link #correlationId()},
 * {@link #limitBytes()} and {@link #maxBodyBytes()}). It is unchecked, like the Kiota exceptions the generated
 * clients throw.
 */
public final class YogabaseJobsException extends YogabaseGatewayException {
    private static final long serialVersionUID = 1L;

    YogabaseJobsException(
        String message,
        int status,
        String code,
        String correlationId,
        Long limitBytes,
        Long maxBodyBytes) {
        super(message, status, code, correlationId, limitBytes, maxBodyBytes);
    }
}
