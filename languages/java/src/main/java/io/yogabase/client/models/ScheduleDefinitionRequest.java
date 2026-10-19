package io.yogabase.client.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.time.OffsetDateTime;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class ScheduleDefinitionRequest implements Parsable {
    /**
     * What a schedule does when an occurrence is due while an earlier occurrence of the same schedule has not finished. An occurrence has not finished while its invocation is queued (waiting to start, or waiting for a retry) or running, and its deadline has not passed. Allow starts the new occurrence alongside. Forbid skips it. Replace ends an earlier occurrence that is still queued, with the failure code schedule_occurrence_replaced, and starts the new one; an occurrence that is already running cannot be stopped, because nothing can interrupt a Lambda that is executing, so while one is running Replace skips the new occurrence, as Forbid does. A skipped occurrence is consumed, not deferred: the schedule moves on to its next occurrence and the skipped one never runs. Forbid never starts an occurrence while an earlier one is in flight, and Replace never while one is running; when several are due at once (RunAllMissed after an outage) Forbid starts only the oldest and Replace only the newest. A manual run is not an occurrence: the policy neither holds it back nor is held back by it, and a one-shot has no earlier occurrence, so the policy does not apply to it. The decision is made in the transaction that enqueues the occurrence, so two schedulers cannot both decide. An unknown name is a bare 400. Required: a request that leaves it out is a 400 whose problem names the member.
     */
    private ConcurrencyPolicy concurrencyPolicy;
    /**
     * Five fields, minute hour day-of-month month day-of-week, each a number, *, a list (1,15), a range (1-5), a step (5 or 8-18/2) or, for the month and the day of the week, a name (JAN, MON); 7 is Sunday. No seconds field and no @ macros. It is evaluated in timeZone. A one-shot still sends a valid expression, which does not decide when it runs.
     */
    private String cron;
    /**
     * How long an occurrence&apos;s invocation has to finish, counted from when the scheduler starts it: the poll that finds the occurrence due, which is a little after its time.
     */
    private Integer deadlineSeconds;
    /**
     * The id of a deployed Stateless Lambda.
     */
    private String functionId;
    /**
     * The exact released version of that function which every occurrence runs. It must be admitted for the project, and an update checks it again.
     */
    private String functionVersion;
    /**
     * How many times an occurrence&apos;s invocation may be attempted.
     */
    private Integer maxAttempts;
    /**
     * The most missed occurrences RunAllMissed runs after an outage. The other policies ignore it.
     */
    private Integer maxCatchUp;
    /**
     * What the scheduler does about occurrences it missed while it was unavailable. RunImmediately runs once, now, however many were missed, and resumes the cadence. SkipToNextOccurrence abandons what it missed and resumes at the next occurrence. RunAllMissed runs each missed occurrence, oldest first, up to maxCatchUp. An unknown name is a bare 400. Required: a request that leaves it out is a 400 whose problem names the member.
     */
    private MisfirePolicy misfirePolicy;
    /**
     * Makes the schedule a one-shot: it runs once at this instant, which must be a UTC timestamp (Z or +00:00), and is then deleted. An instant already past runs at the scheduler&apos;s next poll. Absent or null is a recurring schedule. An update keeps a schedule&apos;s kind: it cannot turn a one-shot into a recurring schedule or the reverse (409).
     */
    private OffsetDateTime oneShotAtUtc;
    /**
     * The request every occurrence&apos;s invocation carries, at most 1,048,576 bytes before base64 encoding. Absent, null or empty is none.
     */
    private String payloadBase64;
    /**
     * The schedule&apos;s id, chosen by the caller and the same as the path&apos;s: 1 to 128 characters, a letter or a digit first and then letters, digits and . _ ~ : - only. The id is one segment of the schedule&apos;s URL path, so it is made of what a path segment carries without being escaped (a slash would end the segment, a space or a character outside ASCII would have to be escaped, and no id may be . or .., which URL clients resolve away), and the console addresses the schedule by the same id. An id that does not follow the rule is a 400 whose problem names the member and the rule in its detail.
     */
    private String scheduleId;
    /**
     * The time zone the cron expression is evaluated in, by the id the gateway&apos;s system knows it by (UTC, Asia/Seoul). An unknown one is 400.
     */
    private String timeZone;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link ScheduleDefinitionRequest}
     */
    @jakarta.annotation.Nonnull
    public static ScheduleDefinitionRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new ScheduleDefinitionRequest();
    }
    /**
     * Gets the concurrencyPolicy property value. What a schedule does when an occurrence is due while an earlier occurrence of the same schedule has not finished. An occurrence has not finished while its invocation is queued (waiting to start, or waiting for a retry) or running, and its deadline has not passed. Allow starts the new occurrence alongside. Forbid skips it. Replace ends an earlier occurrence that is still queued, with the failure code schedule_occurrence_replaced, and starts the new one; an occurrence that is already running cannot be stopped, because nothing can interrupt a Lambda that is executing, so while one is running Replace skips the new occurrence, as Forbid does. A skipped occurrence is consumed, not deferred: the schedule moves on to its next occurrence and the skipped one never runs. Forbid never starts an occurrence while an earlier one is in flight, and Replace never while one is running; when several are due at once (RunAllMissed after an outage) Forbid starts only the oldest and Replace only the newest. A manual run is not an occurrence: the policy neither holds it back nor is held back by it, and a one-shot has no earlier occurrence, so the policy does not apply to it. The decision is made in the transaction that enqueues the occurrence, so two schedulers cannot both decide. An unknown name is a bare 400. Required: a request that leaves it out is a 400 whose problem names the member.
     * @return a {@link ConcurrencyPolicy}
     */
    @jakarta.annotation.Nullable
    public ConcurrencyPolicy getConcurrencyPolicy() {
        return this.concurrencyPolicy;
    }
    /**
     * Gets the cron property value. Five fields, minute hour day-of-month month day-of-week, each a number, *, a list (1,15), a range (1-5), a step (5 or 8-18/2) or, for the month and the day of the week, a name (JAN, MON); 7 is Sunday. No seconds field and no @ macros. It is evaluated in timeZone. A one-shot still sends a valid expression, which does not decide when it runs.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getCron() {
        return this.cron;
    }
    /**
     * Gets the deadlineSeconds property value. How long an occurrence&apos;s invocation has to finish, counted from when the scheduler starts it: the poll that finds the occurrence due, which is a little after its time.
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getDeadlineSeconds() {
        return this.deadlineSeconds;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(12);
        deserializerMap.put("concurrencyPolicy", (n) -> { this.setConcurrencyPolicy(n.getEnumValue(ConcurrencyPolicy::forValue)); });
        deserializerMap.put("cron", (n) -> { this.setCron(n.getStringValue()); });
        deserializerMap.put("deadlineSeconds", (n) -> { this.setDeadlineSeconds(n.getIntegerValue()); });
        deserializerMap.put("functionId", (n) -> { this.setFunctionId(n.getStringValue()); });
        deserializerMap.put("functionVersion", (n) -> { this.setFunctionVersion(n.getStringValue()); });
        deserializerMap.put("maxAttempts", (n) -> { this.setMaxAttempts(n.getIntegerValue()); });
        deserializerMap.put("maxCatchUp", (n) -> { this.setMaxCatchUp(n.getIntegerValue()); });
        deserializerMap.put("misfirePolicy", (n) -> { this.setMisfirePolicy(n.getEnumValue(MisfirePolicy::forValue)); });
        deserializerMap.put("oneShotAtUtc", (n) -> { this.setOneShotAtUtc(n.getOffsetDateTimeValue()); });
        deserializerMap.put("payloadBase64", (n) -> { this.setPayloadBase64(n.getStringValue()); });
        deserializerMap.put("scheduleId", (n) -> { this.setScheduleId(n.getStringValue()); });
        deserializerMap.put("timeZone", (n) -> { this.setTimeZone(n.getStringValue()); });
        return deserializerMap;
    }
    /**
     * Gets the functionId property value. The id of a deployed Stateless Lambda.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getFunctionId() {
        return this.functionId;
    }
    /**
     * Gets the functionVersion property value. The exact released version of that function which every occurrence runs. It must be admitted for the project, and an update checks it again.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getFunctionVersion() {
        return this.functionVersion;
    }
    /**
     * Gets the maxAttempts property value. How many times an occurrence&apos;s invocation may be attempted.
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getMaxAttempts() {
        return this.maxAttempts;
    }
    /**
     * Gets the maxCatchUp property value. The most missed occurrences RunAllMissed runs after an outage. The other policies ignore it.
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getMaxCatchUp() {
        return this.maxCatchUp;
    }
    /**
     * Gets the misfirePolicy property value. What the scheduler does about occurrences it missed while it was unavailable. RunImmediately runs once, now, however many were missed, and resumes the cadence. SkipToNextOccurrence abandons what it missed and resumes at the next occurrence. RunAllMissed runs each missed occurrence, oldest first, up to maxCatchUp. An unknown name is a bare 400. Required: a request that leaves it out is a 400 whose problem names the member.
     * @return a {@link MisfirePolicy}
     */
    @jakarta.annotation.Nullable
    public MisfirePolicy getMisfirePolicy() {
        return this.misfirePolicy;
    }
    /**
     * Gets the oneShotAtUtc property value. Makes the schedule a one-shot: it runs once at this instant, which must be a UTC timestamp (Z or +00:00), and is then deleted. An instant already past runs at the scheduler&apos;s next poll. Absent or null is a recurring schedule. An update keeps a schedule&apos;s kind: it cannot turn a one-shot into a recurring schedule or the reverse (409).
     * @return a {@link OffsetDateTime}
     */
    @jakarta.annotation.Nullable
    public OffsetDateTime getOneShotAtUtc() {
        return this.oneShotAtUtc;
    }
    /**
     * Gets the payloadBase64 property value. The request every occurrence&apos;s invocation carries, at most 1,048,576 bytes before base64 encoding. Absent, null or empty is none.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getPayloadBase64() {
        return this.payloadBase64;
    }
    /**
     * Gets the scheduleId property value. The schedule&apos;s id, chosen by the caller and the same as the path&apos;s: 1 to 128 characters, a letter or a digit first and then letters, digits and . _ ~ : - only. The id is one segment of the schedule&apos;s URL path, so it is made of what a path segment carries without being escaped (a slash would end the segment, a space or a character outside ASCII would have to be escaped, and no id may be . or .., which URL clients resolve away), and the console addresses the schedule by the same id. An id that does not follow the rule is a 400 whose problem names the member and the rule in its detail.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getScheduleId() {
        return this.scheduleId;
    }
    /**
     * Gets the timeZone property value. The time zone the cron expression is evaluated in, by the id the gateway&apos;s system knows it by (UTC, Asia/Seoul). An unknown one is 400.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getTimeZone() {
        return this.timeZone;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeEnumValue("concurrencyPolicy", this.getConcurrencyPolicy());
        writer.writeStringValue("cron", this.getCron());
        writer.writeIntegerValue("deadlineSeconds", this.getDeadlineSeconds());
        writer.writeStringValue("functionId", this.getFunctionId());
        writer.writeStringValue("functionVersion", this.getFunctionVersion());
        writer.writeIntegerValue("maxAttempts", this.getMaxAttempts());
        writer.writeIntegerValue("maxCatchUp", this.getMaxCatchUp());
        writer.writeEnumValue("misfirePolicy", this.getMisfirePolicy());
        writer.writeOffsetDateTimeValue("oneShotAtUtc", this.getOneShotAtUtc());
        writer.writeStringValue("payloadBase64", this.getPayloadBase64());
        writer.writeStringValue("scheduleId", this.getScheduleId());
        writer.writeStringValue("timeZone", this.getTimeZone());
    }
    /**
     * Sets the concurrencyPolicy property value. What a schedule does when an occurrence is due while an earlier occurrence of the same schedule has not finished. An occurrence has not finished while its invocation is queued (waiting to start, or waiting for a retry) or running, and its deadline has not passed. Allow starts the new occurrence alongside. Forbid skips it. Replace ends an earlier occurrence that is still queued, with the failure code schedule_occurrence_replaced, and starts the new one; an occurrence that is already running cannot be stopped, because nothing can interrupt a Lambda that is executing, so while one is running Replace skips the new occurrence, as Forbid does. A skipped occurrence is consumed, not deferred: the schedule moves on to its next occurrence and the skipped one never runs. Forbid never starts an occurrence while an earlier one is in flight, and Replace never while one is running; when several are due at once (RunAllMissed after an outage) Forbid starts only the oldest and Replace only the newest. A manual run is not an occurrence: the policy neither holds it back nor is held back by it, and a one-shot has no earlier occurrence, so the policy does not apply to it. The decision is made in the transaction that enqueues the occurrence, so two schedulers cannot both decide. An unknown name is a bare 400. Required: a request that leaves it out is a 400 whose problem names the member.
     * @param value Value to set for the concurrencyPolicy property.
     */
    public void setConcurrencyPolicy(@jakarta.annotation.Nullable final ConcurrencyPolicy value) {
        this.concurrencyPolicy = value;
    }
    /**
     * Sets the cron property value. Five fields, minute hour day-of-month month day-of-week, each a number, *, a list (1,15), a range (1-5), a step (5 or 8-18/2) or, for the month and the day of the week, a name (JAN, MON); 7 is Sunday. No seconds field and no @ macros. It is evaluated in timeZone. A one-shot still sends a valid expression, which does not decide when it runs.
     * @param value Value to set for the cron property.
     */
    public void setCron(@jakarta.annotation.Nullable final String value) {
        this.cron = value;
    }
    /**
     * Sets the deadlineSeconds property value. How long an occurrence&apos;s invocation has to finish, counted from when the scheduler starts it: the poll that finds the occurrence due, which is a little after its time.
     * @param value Value to set for the deadlineSeconds property.
     */
    public void setDeadlineSeconds(@jakarta.annotation.Nullable final Integer value) {
        this.deadlineSeconds = value;
    }
    /**
     * Sets the functionId property value. The id of a deployed Stateless Lambda.
     * @param value Value to set for the functionId property.
     */
    public void setFunctionId(@jakarta.annotation.Nullable final String value) {
        this.functionId = value;
    }
    /**
     * Sets the functionVersion property value. The exact released version of that function which every occurrence runs. It must be admitted for the project, and an update checks it again.
     * @param value Value to set for the functionVersion property.
     */
    public void setFunctionVersion(@jakarta.annotation.Nullable final String value) {
        this.functionVersion = value;
    }
    /**
     * Sets the maxAttempts property value. How many times an occurrence&apos;s invocation may be attempted.
     * @param value Value to set for the maxAttempts property.
     */
    public void setMaxAttempts(@jakarta.annotation.Nullable final Integer value) {
        this.maxAttempts = value;
    }
    /**
     * Sets the maxCatchUp property value. The most missed occurrences RunAllMissed runs after an outage. The other policies ignore it.
     * @param value Value to set for the maxCatchUp property.
     */
    public void setMaxCatchUp(@jakarta.annotation.Nullable final Integer value) {
        this.maxCatchUp = value;
    }
    /**
     * Sets the misfirePolicy property value. What the scheduler does about occurrences it missed while it was unavailable. RunImmediately runs once, now, however many were missed, and resumes the cadence. SkipToNextOccurrence abandons what it missed and resumes at the next occurrence. RunAllMissed runs each missed occurrence, oldest first, up to maxCatchUp. An unknown name is a bare 400. Required: a request that leaves it out is a 400 whose problem names the member.
     * @param value Value to set for the misfirePolicy property.
     */
    public void setMisfirePolicy(@jakarta.annotation.Nullable final MisfirePolicy value) {
        this.misfirePolicy = value;
    }
    /**
     * Sets the oneShotAtUtc property value. Makes the schedule a one-shot: it runs once at this instant, which must be a UTC timestamp (Z or +00:00), and is then deleted. An instant already past runs at the scheduler&apos;s next poll. Absent or null is a recurring schedule. An update keeps a schedule&apos;s kind: it cannot turn a one-shot into a recurring schedule or the reverse (409).
     * @param value Value to set for the oneShotAtUtc property.
     */
    public void setOneShotAtUtc(@jakarta.annotation.Nullable final OffsetDateTime value) {
        this.oneShotAtUtc = value;
    }
    /**
     * Sets the payloadBase64 property value. The request every occurrence&apos;s invocation carries, at most 1,048,576 bytes before base64 encoding. Absent, null or empty is none.
     * @param value Value to set for the payloadBase64 property.
     */
    public void setPayloadBase64(@jakarta.annotation.Nullable final String value) {
        this.payloadBase64 = value;
    }
    /**
     * Sets the scheduleId property value. The schedule&apos;s id, chosen by the caller and the same as the path&apos;s: 1 to 128 characters, a letter or a digit first and then letters, digits and . _ ~ : - only. The id is one segment of the schedule&apos;s URL path, so it is made of what a path segment carries without being escaped (a slash would end the segment, a space or a character outside ASCII would have to be escaped, and no id may be . or .., which URL clients resolve away), and the console addresses the schedule by the same id. An id that does not follow the rule is a 400 whose problem names the member and the rule in its detail.
     * @param value Value to set for the scheduleId property.
     */
    public void setScheduleId(@jakarta.annotation.Nullable final String value) {
        this.scheduleId = value;
    }
    /**
     * Sets the timeZone property value. The time zone the cron expression is evaluated in, by the id the gateway&apos;s system knows it by (UTC, Asia/Seoul). An unknown one is 400.
     * @param value Value to set for the timeZone property.
     */
    public void setTimeZone(@jakarta.annotation.Nullable final String value) {
        this.timeZone = value;
    }
}
