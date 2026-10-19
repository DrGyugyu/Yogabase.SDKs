package io.yogabase.client;

/**
 * What a schedule does when an occurrence is due while an earlier occurrence of the same schedule has not
 * finished: its invocation is queued (waiting to start, or to be retried) or running, and its deadline has not
 * passed. A skipped occurrence is consumed, not deferred: the schedule moves on to its next occurrence. A manual
 * run is not an occurrence, so the policy neither holds it back nor is held back by it, and a one-shot has no
 * earlier occurrence, so the policy does not apply to it. A definition always names one: a request that leaves
 * it out is a 400 whose problem names the member.
 */
public enum YogabaseScheduleConcurrencyPolicy {
    /** Starts the new occurrence alongside the earlier one. */
    ALLOW("Allow"),
    /**
     * Skips the new occurrence. When several are due at once ({@link YogabaseScheduleMisfirePolicy#RUN_ALL_MISSED}
     * after an outage), only the oldest starts.
     */
    FORBID("Forbid"),
    /**
     * Ends an earlier occurrence that is still queued, with the failure code {@code schedule_occurrence_replaced},
     * and starts the new one. An occurrence that is already running cannot be stopped, because nothing can
     * interrupt a Lambda that is executing, so while one is running this skips the new occurrence, as
     * {@link #FORBID} does. When several are due at once, only the newest starts.
     */
    REPLACE("Replace");

    private final String wireName;

    YogabaseScheduleConcurrencyPolicy(String wireName) {
        this.wireName = wireName;
    }

    /** The name the gateway reads, such as {@code Forbid}. */
    public String wireName() {
        return wireName;
    }
}
