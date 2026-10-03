package io.stackyoga.yogabase;

import java.time.OffsetDateTime;

/**
 * What a client asks the schedule trigger route to do: run the schedule once now.
 *
 * @param idempotencyKey the caller's key for this run: 1 to 256 characters, with no leading or trailing white
 *     space; the same key against the same version of the schedule is the same run, so retrying a trigger is safe
 * @param deadline when the invocation must finish: later than now and at most 3600 seconds from now, else the
 *     gateway refuses it; sent as a UTC instant
 */
public record YogabaseScheduleTriggerRequest(String idempotencyKey, OffsetDateTime deadline) {
}
