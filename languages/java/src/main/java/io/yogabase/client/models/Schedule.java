package io.yogabase.client.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.time.OffsetDateTime;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class Schedule implements Parsable {
    /**
     * The functionId property
     */
    private String functionId;
    /**
     * The functionVersion property
     */
    private String functionVersion;
    /**
     * Lasts until the schedule is deleted: a schedule deleted and created again has a new one.
     */
    private UUID instanceId;
    /**
     * How far the scheduler has got: when the schedule was created, then the last occurrence it ran or skipped past. Null for a one-shot.
     */
    private OffsetDateTime lastOccurrence;
    /**
     * The instant a one-shot runs; null for a recurring schedule.
     */
    private OffsetDateTime oneShotAtUtc;
    /**
     * The projectId property
     */
    private UUID projectId;
    /**
     * The id the schedule was written under. A write holds an id to the pattern of the definition&apos;s scheduleId member; a schedule written before the pattern was enforced may carry another.
     */
    private String scheduleId;
    /**
     * The owner: the sub of the user who created the schedule.
     */
    private UUID subject;
    /**
     * The schedule&apos;s version as decimal text, because it is a 64-bit number a JSON number cannot carry exactly. It changes whenever the schedule does and is not a counter: read it from the ETag, which carries it with the instance.
     */
    private String version;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link Schedule}
     */
    @jakarta.annotation.Nonnull
    public static Schedule createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new Schedule();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(9);
        deserializerMap.put("functionId", (n) -> { this.setFunctionId(n.getStringValue()); });
        deserializerMap.put("functionVersion", (n) -> { this.setFunctionVersion(n.getStringValue()); });
        deserializerMap.put("instanceId", (n) -> { this.setInstanceId(n.getUUIDValue()); });
        deserializerMap.put("lastOccurrence", (n) -> { this.setLastOccurrence(n.getOffsetDateTimeValue()); });
        deserializerMap.put("oneShotAtUtc", (n) -> { this.setOneShotAtUtc(n.getOffsetDateTimeValue()); });
        deserializerMap.put("projectId", (n) -> { this.setProjectId(n.getUUIDValue()); });
        deserializerMap.put("scheduleId", (n) -> { this.setScheduleId(n.getStringValue()); });
        deserializerMap.put("subject", (n) -> { this.setSubject(n.getUUIDValue()); });
        deserializerMap.put("version", (n) -> { this.setVersion(n.getStringValue()); });
        return deserializerMap;
    }
    /**
     * Gets the functionId property value. The functionId property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getFunctionId() {
        return this.functionId;
    }
    /**
     * Gets the functionVersion property value. The functionVersion property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getFunctionVersion() {
        return this.functionVersion;
    }
    /**
     * Gets the instanceId property value. Lasts until the schedule is deleted: a schedule deleted and created again has a new one.
     * @return a {@link UUID}
     */
    @jakarta.annotation.Nullable
    public UUID getInstanceId() {
        return this.instanceId;
    }
    /**
     * Gets the lastOccurrence property value. How far the scheduler has got: when the schedule was created, then the last occurrence it ran or skipped past. Null for a one-shot.
     * @return a {@link OffsetDateTime}
     */
    @jakarta.annotation.Nullable
    public OffsetDateTime getLastOccurrence() {
        return this.lastOccurrence;
    }
    /**
     * Gets the oneShotAtUtc property value. The instant a one-shot runs; null for a recurring schedule.
     * @return a {@link OffsetDateTime}
     */
    @jakarta.annotation.Nullable
    public OffsetDateTime getOneShotAtUtc() {
        return this.oneShotAtUtc;
    }
    /**
     * Gets the projectId property value. The projectId property
     * @return a {@link UUID}
     */
    @jakarta.annotation.Nullable
    public UUID getProjectId() {
        return this.projectId;
    }
    /**
     * Gets the scheduleId property value. The id the schedule was written under. A write holds an id to the pattern of the definition&apos;s scheduleId member; a schedule written before the pattern was enforced may carry another.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getScheduleId() {
        return this.scheduleId;
    }
    /**
     * Gets the subject property value. The owner: the sub of the user who created the schedule.
     * @return a {@link UUID}
     */
    @jakarta.annotation.Nullable
    public UUID getSubject() {
        return this.subject;
    }
    /**
     * Gets the version property value. The schedule&apos;s version as decimal text, because it is a 64-bit number a JSON number cannot carry exactly. It changes whenever the schedule does and is not a counter: read it from the ETag, which carries it with the instance.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getVersion() {
        return this.version;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeStringValue("functionId", this.getFunctionId());
        writer.writeStringValue("functionVersion", this.getFunctionVersion());
        writer.writeUUIDValue("instanceId", this.getInstanceId());
        writer.writeOffsetDateTimeValue("lastOccurrence", this.getLastOccurrence());
        writer.writeOffsetDateTimeValue("oneShotAtUtc", this.getOneShotAtUtc());
        writer.writeUUIDValue("projectId", this.getProjectId());
        writer.writeStringValue("scheduleId", this.getScheduleId());
        writer.writeUUIDValue("subject", this.getSubject());
        writer.writeStringValue("version", this.getVersion());
    }
    /**
     * Sets the functionId property value. The functionId property
     * @param value Value to set for the functionId property.
     */
    public void setFunctionId(@jakarta.annotation.Nullable final String value) {
        this.functionId = value;
    }
    /**
     * Sets the functionVersion property value. The functionVersion property
     * @param value Value to set for the functionVersion property.
     */
    public void setFunctionVersion(@jakarta.annotation.Nullable final String value) {
        this.functionVersion = value;
    }
    /**
     * Sets the instanceId property value. Lasts until the schedule is deleted: a schedule deleted and created again has a new one.
     * @param value Value to set for the instanceId property.
     */
    public void setInstanceId(@jakarta.annotation.Nullable final UUID value) {
        this.instanceId = value;
    }
    /**
     * Sets the lastOccurrence property value. How far the scheduler has got: when the schedule was created, then the last occurrence it ran or skipped past. Null for a one-shot.
     * @param value Value to set for the lastOccurrence property.
     */
    public void setLastOccurrence(@jakarta.annotation.Nullable final OffsetDateTime value) {
        this.lastOccurrence = value;
    }
    /**
     * Sets the oneShotAtUtc property value. The instant a one-shot runs; null for a recurring schedule.
     * @param value Value to set for the oneShotAtUtc property.
     */
    public void setOneShotAtUtc(@jakarta.annotation.Nullable final OffsetDateTime value) {
        this.oneShotAtUtc = value;
    }
    /**
     * Sets the projectId property value. The projectId property
     * @param value Value to set for the projectId property.
     */
    public void setProjectId(@jakarta.annotation.Nullable final UUID value) {
        this.projectId = value;
    }
    /**
     * Sets the scheduleId property value. The id the schedule was written under. A write holds an id to the pattern of the definition&apos;s scheduleId member; a schedule written before the pattern was enforced may carry another.
     * @param value Value to set for the scheduleId property.
     */
    public void setScheduleId(@jakarta.annotation.Nullable final String value) {
        this.scheduleId = value;
    }
    /**
     * Sets the subject property value. The owner: the sub of the user who created the schedule.
     * @param value Value to set for the subject property.
     */
    public void setSubject(@jakarta.annotation.Nullable final UUID value) {
        this.subject = value;
    }
    /**
     * Sets the version property value. The schedule&apos;s version as decimal text, because it is a 64-bit number a JSON number cannot carry exactly. It changes whenever the schedule does and is not a counter: read it from the ETag, which carries it with the instance.
     * @param value Value to set for the version property.
     */
    public void setVersion(@jakarta.annotation.Nullable final String value) {
        this.version = value;
    }
}
