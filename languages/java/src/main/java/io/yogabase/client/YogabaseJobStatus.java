package io.yogabase.client;

/**
 * Where a durable job is in its lifecycle: {@link #QUEUED}, then {@link #RUNNING}, then it settles as
 * {@link #SUCCEEDED}, {@link #CANCELLED} or {@link #DEAD_LETTERED}. {@link #FAILED} is a checkpoint a job
 * passes through on the way to a retry ({@link #QUEUED} again) or to {@link #DEAD_LETTERED}; a job never
 * rests in it, so it is not terminal.
 */
public enum YogabaseJobStatus {
    QUEUED("Queued"),
    RUNNING("Running"),
    SUCCEEDED("Succeeded"),
    FAILED("Failed"),
    CANCELLED("Cancelled"),
    DEAD_LETTERED("DeadLettered");

    private final String wireName;

    YogabaseJobStatus(String wireName) {
        this.wireName = wireName;
    }

    /** The name the gateway writes, such as {@code DeadLettered}. */
    public String wireName() {
        return wireName;
    }

    /** True once the job has settled and will not change again. */
    public boolean isTerminal() {
        return this == SUCCEEDED || this == CANCELLED || this == DEAD_LETTERED;
    }

    /** The status the gateway named, or {@code null} for a name this client was never told of. */
    static YogabaseJobStatus fromWireName(String value) {
        for (final YogabaseJobStatus status : values()) {
            if (status.wireName.equals(value)) {
                return status;
            }
        }
        return null;
    }
}
