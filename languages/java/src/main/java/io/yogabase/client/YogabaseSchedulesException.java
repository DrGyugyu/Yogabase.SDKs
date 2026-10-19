package io.yogabase.client;

/**
 * A non-successful schedule response, or an answer the schedule client refuses, with the gateway's problem
 * details ({@link #status()}, {@link #code()} with the codes of {@link YogabaseScheduleErrorCodes} and, where the
 * gateway names none, the status-derived ones of {@link YogabaseStatusErrorCodes}, {@link #correlationId()},
 * {@link #limitBytes()} and {@link #maxBodyBytes()}). It is unchecked, like the Kiota exceptions the generated
 * clients throw.
 */
public final class YogabaseSchedulesException extends YogabaseGatewayException {
    private static final long serialVersionUID = 1L;

    YogabaseSchedulesException(
        String message,
        int status,
        String code,
        String correlationId,
        Long limitBytes,
        Long maxBodyBytes) {
        super(message, status, code, correlationId, limitBytes, maxBodyBytes);
    }
}
