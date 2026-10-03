package io.stackyoga.yogabase;

/**
 * What a schedule does about occurrences it missed while the scheduler was unavailable. The gateway reads a
 * missing policy as the first name, which its contract does not promise, so a definition always names one.
 */
public enum YogabaseScheduleMisfirePolicy {
    /** Runs once, now, however many occurrences were missed, and resumes the cadence. */
    RUN_IMMEDIATELY("RunImmediately"),
    /** Abandons what it missed and resumes at the next occurrence. */
    SKIP_TO_NEXT_OCCURRENCE("SkipToNextOccurrence"),
    /** Runs each missed occurrence, oldest first, up to {@link YogabaseScheduleDefinition#maxCatchUp()}. */
    RUN_ALL_MISSED("RunAllMissed");

    private final String wireName;

    YogabaseScheduleMisfirePolicy(String wireName) {
        this.wireName = wireName;
    }

    /** The name the gateway reads, such as {@code SkipToNextOccurrence}. */
    public String wireName() {
        return wireName;
    }
}
