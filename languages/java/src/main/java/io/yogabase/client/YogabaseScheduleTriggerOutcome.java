package io.yogabase.client;

/** Whether a manual run enqueued a new invocation or found the one its key had already made. */
public enum YogabaseScheduleTriggerOutcome {
    /** This request enqueued the invocation (the gateway's outcome 0). */
    ENQUEUED,
    /** The same key against the same version had already made the invocation, and this is it (outcome 1). */
    REPLAYED;

    /** The outcome the gateway numbered, or {@code null} for a number this client was never told of. */
    static YogabaseScheduleTriggerOutcome fromWireValue(int value) {
        return switch (value) {
            case 0 -> ENQUEUED;
            case 1 -> REPLAYED;
            default -> null;
        };
    }
}
