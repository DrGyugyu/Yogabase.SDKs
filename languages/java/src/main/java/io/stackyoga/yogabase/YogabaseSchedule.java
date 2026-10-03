package io.stackyoga.yogabase;

import java.time.OffsetDateTime;
import java.util.UUID;

/**
 * One schedule as the gateway holds it, with the ETag of this version of it. Send {@link #eTag()} back to
 * change, delete or run exactly this version.
 *
 * @param scheduleId the schedule's id
 * @param projectId the project the schedule belongs to: the client's own
 * @param subject the owner: the sub of the user who created the schedule
 * @param functionId the id of the function every occurrence runs
 * @param functionVersion the exact function version every occurrence runs
 * @param version the schedule's version as decimal text: a 64-bit number a JSON number cannot carry exactly; it
 *     changes whenever the schedule does, including when the scheduler records an occurrence, and the
 *     {@code eTag} carries it with the instance
 * @param instanceId lasts until the schedule is deleted: a schedule deleted and created again has a new one
 * @param lastOccurrence how far the scheduler has got, or {@code null} for a one-shot
 * @param oneShotAtUtc the instant a one-shot runs, or {@code null} for a recurring schedule
 * @param eTag the ETag to send as {@code If-Match} to change, delete or run exactly this version: the quotes
 *     included, {@code "schedule-v<version>-<instance>"} with the instance as 32 lowercase hexadecimal digits;
 *     it is built from {@code version} and {@code instanceId}, which the gateway's ETag header carries too (a
 *     strong header that disagrees is refused). The scheduler advances a recurring schedule's version as it
 *     runs, so an ETag held for long is stale: read the schedule again
 */
public record YogabaseSchedule(
    String scheduleId,
    UUID projectId,
    UUID subject,
    String functionId,
    String functionVersion,
    String version,
    UUID instanceId,
    OffsetDateTime lastOccurrence,
    OffsetDateTime oneShotAtUtc,
    String eTag) {

    /** True for a one-shot, which runs once and is then deleted. */
    public boolean isOneShot() {
        return oneShotAtUtc != null;
    }
}
