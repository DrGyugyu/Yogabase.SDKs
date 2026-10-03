package io.stackyoga.yogabase;

import java.time.OffsetDateTime;
import java.util.Arrays;
import java.util.Objects;

/**
 * What a client asks the schedule write route to hold: a recurring schedule ({@code cron} evaluated in
 * {@code timeZone}) or a one-shot ({@code oneShotAtUtc}) of one deployed stateless function version. The project
 * and the owner are never fields: the project is the client's own and the owner is the signed-in user. Every
 * member but {@code payload} and {@code oneShotAtUtc} is required, the two policies included: a request that
 * leaves one out is a 400 whose problem names the member, and this client refuses it before sending.
 *
 * @param scheduleId the schedule's id, chosen by the caller: 1 to 128 characters, a letter or a digit first and
 *     then letters, digits and {@code .} {@code _} {@code ~} {@code :} {@code -} only
 *     ({@code ^[A-Za-z0-9][A-Za-z0-9._~:-]{0,127}$}). The id is one segment of the schedule's URL path, so there
 *     is nothing to escape, and the console addresses the schedule by the same id. The id belongs to the project,
 *     so one that another user holds cannot be created. A schedule written before the rule under another id can
 *     still be read, run and deleted by that id, but a write of it is refused: delete it and create it again
 *     under an id that follows the rule
 * @param functionId the id of a deployed Stateless Lambda, at most 256 characters
 * @param functionVersion the exact released version of that function which every occurrence runs, at most 64
 *     characters; it must be admitted for the project, and an update checks it again
 * @param cron five fields, minute hour day-of-month month day-of-week, each a number, {@code *}, a list
 *     ({@code 1,15}), a range ({@code 1-5}), a step ({@code *}{@code /5} or {@code 8-18/2}) or, for the month and the
 *     day of the week, a name ({@code JAN}, {@code MON}); 7 is Sunday. No seconds field and no {@code @} macros; at
 *     most 128 characters. A one-shot still sends a valid expression, which does not decide when it runs
 * @param timeZone the time zone the cron expression is evaluated in, by the id the gateway's system knows it by
 *     ({@code UTC}, {@code Asia/Seoul}), at most 128 characters
 * @param misfirePolicy what to do about occurrences missed while the scheduler was unavailable
 * @param concurrencyPolicy what to do when an occurrence is due while an earlier one has not finished; see
 *     {@link YogabaseScheduleConcurrencyPolicy}
 * @param maxCatchUp the most missed occurrences {@link YogabaseScheduleMisfirePolicy#RUN_ALL_MISSED} runs after an
 *     outage, 1 to 128; the other policies ignore it but the gateway still requires it
 * @param maxAttempts how many times an occurrence's invocation may be attempted, 1 to 16
 * @param deadlineSeconds how long an occurrence's invocation has to finish, in seconds, 1 to 86400
 * @param payload the request every occurrence's invocation carries, at most
 *     {@link YogabaseSchedulesClient#MAXIMUM_PAYLOAD_BYTES} bytes, or {@code null} (or empty) for none
 * @param oneShotAtUtc makes the schedule a one-shot: it runs once at this instant, which is sent as UTC, and is
 *     then deleted; {@code null} is a recurring schedule. An update keeps a schedule's kind: it cannot turn a
 *     one-shot into a recurring schedule or the reverse (a 409)
 */
public record YogabaseScheduleDefinition(
    String scheduleId,
    String functionId,
    String functionVersion,
    String cron,
    String timeZone,
    YogabaseScheduleMisfirePolicy misfirePolicy,
    YogabaseScheduleConcurrencyPolicy concurrencyPolicy,
    int maxCatchUp,
    int maxAttempts,
    int deadlineSeconds,
    byte[] payload,
    OffsetDateTime oneShotAtUtc) {

    @Override
    public boolean equals(Object other) {
        return other instanceof YogabaseScheduleDefinition definition
            && Objects.equals(scheduleId, definition.scheduleId)
            && Objects.equals(functionId, definition.functionId)
            && Objects.equals(functionVersion, definition.functionVersion)
            && Objects.equals(cron, definition.cron)
            && Objects.equals(timeZone, definition.timeZone)
            && misfirePolicy == definition.misfirePolicy
            && concurrencyPolicy == definition.concurrencyPolicy
            && maxCatchUp == definition.maxCatchUp
            && maxAttempts == definition.maxAttempts
            && deadlineSeconds == definition.deadlineSeconds
            && Arrays.equals(payload, definition.payload)
            && Objects.equals(oneShotAtUtc, definition.oneShotAtUtc);
    }

    @Override
    public int hashCode() {
        return Objects.hash(
            scheduleId, functionId, functionVersion, cron, timeZone, misfirePolicy, concurrencyPolicy,
            maxCatchUp, maxAttempts, deadlineSeconds, Arrays.hashCode(payload), oneShotAtUtc);
    }

    @Override
    public String toString() {
        return "YogabaseScheduleDefinition[scheduleId=" + scheduleId + ", functionId=" + functionId
            + ", functionVersion=" + functionVersion + ", cron=" + cron + ", timeZone=" + timeZone
            + ", payload=" + (payload == null ? "none" : payload.length + " bytes")
            + ", oneShotAtUtc=" + oneShotAtUtc + "]";
    }
}
