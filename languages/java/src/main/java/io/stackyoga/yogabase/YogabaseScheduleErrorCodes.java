package io.stackyoga.yogabase;

/**
 * The codes the schedule routes refuse with, as {@link YogabaseSchedulesException#code()} carries them. Only one
 * refusal of the gateway's names a code; every other is a status with a fixed title, or no body at all, and its
 * code is derived from the status (see {@link YogabaseStatusErrorCodes}). The last is this client's own, for an
 * answer that is not what the route declares.
 */
public final class YogabaseScheduleErrorCodes {
    private YogabaseScheduleErrorCodes() {
    }

    /**
     * 413: the payload is over 1 MiB before base64 encoding, or the request body is over the largest the route
     * reads. {@link YogabaseSchedulesException#limitBytes()} and {@link YogabaseSchedulesException#maxBodyBytes()}
     * name both limits.
     */
    public static final String PAYLOAD_TOO_LARGE = "schedule_payload_too_large";

    /** This client: the gateway's answer to a schedule call is not what the route declares. */
    public static final String RESPONSE_INVALID = "schedule_response_invalid";
}
