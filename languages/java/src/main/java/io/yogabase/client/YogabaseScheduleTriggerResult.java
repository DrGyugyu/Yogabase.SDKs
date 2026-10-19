package io.yogabase.client;

import java.util.UUID;

/**
 * The invocation a manual run made, or the one the same key already made.
 *
 * @param outcome whether this request enqueued the invocation or found the one its key had already made
 * @param invocationId the invocation's id; it derives from the schedule's id, instance and version and the
 *     idempotency key, so a repeat has the same one
 */
public record YogabaseScheduleTriggerResult(YogabaseScheduleTriggerOutcome outcome, UUID invocationId) {
}
