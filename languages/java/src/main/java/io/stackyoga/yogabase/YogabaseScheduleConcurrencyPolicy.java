package io.stackyoga.yogabase;

/**
 * What a schedule asks for when an occurrence is due while the one before it is still running. The gateway
 * checks and stores the choice, but its scheduler does not yet look for a running occurrence, so every
 * occurrence starts whichever is chosen; a definition still names one, because the gateway reads a missing
 * policy as the first name, which its contract does not promise.
 */
public enum YogabaseScheduleConcurrencyPolicy {
    /** Starts the occurrence alongside the running one. */
    ALLOW("Allow"),
    /** Skips the occurrence. */
    FORBID("Forbid"),
    /** Stops the running occurrence and starts this one. */
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
