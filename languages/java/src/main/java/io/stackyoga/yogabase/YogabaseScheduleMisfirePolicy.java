package io.stackyoga.yogabase;

/**
 * What a schedule does about occurrences it missed while the scheduler was unavailable. A definition always
 * names one: a request that leaves it out is a 400 whose problem names the member.
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
