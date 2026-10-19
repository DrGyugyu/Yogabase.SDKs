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
public class Job implements Parsable {
    /**
     * How many attempts have started: 0 until a worker first claims the job, and never more than maxAttempts.
     */
    private Integer attempt;
    /**
     * True once the job&apos;s owner has asked for it to stop, and so on a cancelled job.
     */
    private Boolean cancellationRequested;
    /**
     * When the job became Succeeded, Cancelled or DeadLettered. Null before.
     */
    private OffsetDateTime completedAt;
    /**
     * The createdAt property
     */
    private OffsetDateTime createdAt;
    /**
     * Why the last failed attempt failed, as a stable code. Null until an attempt fails, and again once the job succeeds or is cancelled.
     */
    private String failureCode;
    /**
     * The jobId property
     */
    private UUID jobId;
    /**
     * The jobType property
     */
    private String jobType;
    /**
     * The maxAttempts property
     */
    private Integer maxAttempts;
    /**
     * How far the job reports it has got; 100 once it has succeeded.
     */
    private Integer progressPercent;
    /**
     * A name for the stored result, not a capability: the bytes are read from the result route. Null while there is none.
     */
    private String resultReference;
    /**
     * Queued and Running are in progress. Succeeded, Cancelled and DeadLettered are final: nothing about the job changes again. A failed attempt sends the job back to Queued for its next attempt, or on to DeadLettered once maxAttempts attempts have started. Failed names a failed attempt between those two, is never final, and is not a state the ledger records: a client that reads it treats the job as in progress.
     */
    private JobStatus status;
    /**
     * The updatedAt property
     */
    private OffsetDateTime updatedAt;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link Job}
     */
    @jakarta.annotation.Nonnull
    public static Job createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new Job();
    }
    /**
     * Gets the attempt property value. How many attempts have started: 0 until a worker first claims the job, and never more than maxAttempts.
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getAttempt() {
        return this.attempt;
    }
    /**
     * Gets the cancellationRequested property value. True once the job&apos;s owner has asked for it to stop, and so on a cancelled job.
     * @return a {@link Boolean}
     */
    @jakarta.annotation.Nullable
    public Boolean getCancellationRequested() {
        return this.cancellationRequested;
    }
    /**
     * Gets the completedAt property value. When the job became Succeeded, Cancelled or DeadLettered. Null before.
     * @return a {@link OffsetDateTime}
     */
    @jakarta.annotation.Nullable
    public OffsetDateTime getCompletedAt() {
        return this.completedAt;
    }
    /**
     * Gets the createdAt property value. The createdAt property
     * @return a {@link OffsetDateTime}
     */
    @jakarta.annotation.Nullable
    public OffsetDateTime getCreatedAt() {
        return this.createdAt;
    }
    /**
     * Gets the failureCode property value. Why the last failed attempt failed, as a stable code. Null until an attempt fails, and again once the job succeeds or is cancelled.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getFailureCode() {
        return this.failureCode;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(12);
        deserializerMap.put("attempt", (n) -> { this.setAttempt(n.getIntegerValue()); });
        deserializerMap.put("cancellationRequested", (n) -> { this.setCancellationRequested(n.getBooleanValue()); });
        deserializerMap.put("completedAt", (n) -> { this.setCompletedAt(n.getOffsetDateTimeValue()); });
        deserializerMap.put("createdAt", (n) -> { this.setCreatedAt(n.getOffsetDateTimeValue()); });
        deserializerMap.put("failureCode", (n) -> { this.setFailureCode(n.getStringValue()); });
        deserializerMap.put("jobId", (n) -> { this.setJobId(n.getUUIDValue()); });
        deserializerMap.put("jobType", (n) -> { this.setJobType(n.getStringValue()); });
        deserializerMap.put("maxAttempts", (n) -> { this.setMaxAttempts(n.getIntegerValue()); });
        deserializerMap.put("progressPercent", (n) -> { this.setProgressPercent(n.getIntegerValue()); });
        deserializerMap.put("resultReference", (n) -> { this.setResultReference(n.getStringValue()); });
        deserializerMap.put("status", (n) -> { this.setStatus(n.getEnumValue(JobStatus::forValue)); });
        deserializerMap.put("updatedAt", (n) -> { this.setUpdatedAt(n.getOffsetDateTimeValue()); });
        return deserializerMap;
    }
    /**
     * Gets the jobId property value. The jobId property
     * @return a {@link UUID}
     */
    @jakarta.annotation.Nullable
    public UUID getJobId() {
        return this.jobId;
    }
    /**
     * Gets the jobType property value. The jobType property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getJobType() {
        return this.jobType;
    }
    /**
     * Gets the maxAttempts property value. The maxAttempts property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getMaxAttempts() {
        return this.maxAttempts;
    }
    /**
     * Gets the progressPercent property value. How far the job reports it has got; 100 once it has succeeded.
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getProgressPercent() {
        return this.progressPercent;
    }
    /**
     * Gets the resultReference property value. A name for the stored result, not a capability: the bytes are read from the result route. Null while there is none.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getResultReference() {
        return this.resultReference;
    }
    /**
     * Gets the status property value. Queued and Running are in progress. Succeeded, Cancelled and DeadLettered are final: nothing about the job changes again. A failed attempt sends the job back to Queued for its next attempt, or on to DeadLettered once maxAttempts attempts have started. Failed names a failed attempt between those two, is never final, and is not a state the ledger records: a client that reads it treats the job as in progress.
     * @return a {@link JobStatus}
     */
    @jakarta.annotation.Nullable
    public JobStatus getStatus() {
        return this.status;
    }
    /**
     * Gets the updatedAt property value. The updatedAt property
     * @return a {@link OffsetDateTime}
     */
    @jakarta.annotation.Nullable
    public OffsetDateTime getUpdatedAt() {
        return this.updatedAt;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeIntegerValue("attempt", this.getAttempt());
        writer.writeBooleanValue("cancellationRequested", this.getCancellationRequested());
        writer.writeOffsetDateTimeValue("completedAt", this.getCompletedAt());
        writer.writeOffsetDateTimeValue("createdAt", this.getCreatedAt());
        writer.writeStringValue("failureCode", this.getFailureCode());
        writer.writeUUIDValue("jobId", this.getJobId());
        writer.writeStringValue("jobType", this.getJobType());
        writer.writeIntegerValue("maxAttempts", this.getMaxAttempts());
        writer.writeIntegerValue("progressPercent", this.getProgressPercent());
        writer.writeStringValue("resultReference", this.getResultReference());
        writer.writeEnumValue("status", this.getStatus());
        writer.writeOffsetDateTimeValue("updatedAt", this.getUpdatedAt());
    }
    /**
     * Sets the attempt property value. How many attempts have started: 0 until a worker first claims the job, and never more than maxAttempts.
     * @param value Value to set for the attempt property.
     */
    public void setAttempt(@jakarta.annotation.Nullable final Integer value) {
        this.attempt = value;
    }
    /**
     * Sets the cancellationRequested property value. True once the job&apos;s owner has asked for it to stop, and so on a cancelled job.
     * @param value Value to set for the cancellationRequested property.
     */
    public void setCancellationRequested(@jakarta.annotation.Nullable final Boolean value) {
        this.cancellationRequested = value;
    }
    /**
     * Sets the completedAt property value. When the job became Succeeded, Cancelled or DeadLettered. Null before.
     * @param value Value to set for the completedAt property.
     */
    public void setCompletedAt(@jakarta.annotation.Nullable final OffsetDateTime value) {
        this.completedAt = value;
    }
    /**
     * Sets the createdAt property value. The createdAt property
     * @param value Value to set for the createdAt property.
     */
    public void setCreatedAt(@jakarta.annotation.Nullable final OffsetDateTime value) {
        this.createdAt = value;
    }
    /**
     * Sets the failureCode property value. Why the last failed attempt failed, as a stable code. Null until an attempt fails, and again once the job succeeds or is cancelled.
     * @param value Value to set for the failureCode property.
     */
    public void setFailureCode(@jakarta.annotation.Nullable final String value) {
        this.failureCode = value;
    }
    /**
     * Sets the jobId property value. The jobId property
     * @param value Value to set for the jobId property.
     */
    public void setJobId(@jakarta.annotation.Nullable final UUID value) {
        this.jobId = value;
    }
    /**
     * Sets the jobType property value. The jobType property
     * @param value Value to set for the jobType property.
     */
    public void setJobType(@jakarta.annotation.Nullable final String value) {
        this.jobType = value;
    }
    /**
     * Sets the maxAttempts property value. The maxAttempts property
     * @param value Value to set for the maxAttempts property.
     */
    public void setMaxAttempts(@jakarta.annotation.Nullable final Integer value) {
        this.maxAttempts = value;
    }
    /**
     * Sets the progressPercent property value. How far the job reports it has got; 100 once it has succeeded.
     * @param value Value to set for the progressPercent property.
     */
    public void setProgressPercent(@jakarta.annotation.Nullable final Integer value) {
        this.progressPercent = value;
    }
    /**
     * Sets the resultReference property value. A name for the stored result, not a capability: the bytes are read from the result route. Null while there is none.
     * @param value Value to set for the resultReference property.
     */
    public void setResultReference(@jakarta.annotation.Nullable final String value) {
        this.resultReference = value;
    }
    /**
     * Sets the status property value. Queued and Running are in progress. Succeeded, Cancelled and DeadLettered are final: nothing about the job changes again. A failed attempt sends the job back to Queued for its next attempt, or on to DeadLettered once maxAttempts attempts have started. Failed names a failed attempt between those two, is never final, and is not a state the ledger records: a client that reads it treats the job as in progress.
     * @param value Value to set for the status property.
     */
    public void setStatus(@jakarta.annotation.Nullable final JobStatus value) {
        this.status = value;
    }
    /**
     * Sets the updatedAt property value. The updatedAt property
     * @param value Value to set for the updatedAt property.
     */
    public void setUpdatedAt(@jakarta.annotation.Nullable final OffsetDateTime value) {
        this.updatedAt = value;
    }
}
