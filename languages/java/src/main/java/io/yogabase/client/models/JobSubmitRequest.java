package io.yogabase.client.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class JobSubmitRequest implements Parsable {
    /**
     * The caller&apos;s key for this request: trimmed, with no control character. It belongs to the signed-in user, who may repeat it for the same request and for no other.
     */
    private String idempotencyKey;
    /**
     * The id of a deployed stateless function; its highest released version runs the job.
     */
    private String jobType;
    /**
     * How many times the job may be attempted. Absent or null is 5. The idempotency key binds it, with the job type and the payload.
     */
    private Integer maxAttempts;
    /**
     * The function&apos;s request, at most 1,048,576 bytes before base64 encoding. Absent, null or empty is a job that carries none.
     */
    private String payloadBase64;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link JobSubmitRequest}
     */
    @jakarta.annotation.Nonnull
    public static JobSubmitRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new JobSubmitRequest();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(4);
        deserializerMap.put("idempotencyKey", (n) -> { this.setIdempotencyKey(n.getStringValue()); });
        deserializerMap.put("jobType", (n) -> { this.setJobType(n.getStringValue()); });
        deserializerMap.put("maxAttempts", (n) -> { this.setMaxAttempts(n.getIntegerValue()); });
        deserializerMap.put("payloadBase64", (n) -> { this.setPayloadBase64(n.getStringValue()); });
        return deserializerMap;
    }
    /**
     * Gets the idempotencyKey property value. The caller&apos;s key for this request: trimmed, with no control character. It belongs to the signed-in user, who may repeat it for the same request and for no other.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getIdempotencyKey() {
        return this.idempotencyKey;
    }
    /**
     * Gets the jobType property value. The id of a deployed stateless function; its highest released version runs the job.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getJobType() {
        return this.jobType;
    }
    /**
     * Gets the maxAttempts property value. How many times the job may be attempted. Absent or null is 5. The idempotency key binds it, with the job type and the payload.
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getMaxAttempts() {
        return this.maxAttempts;
    }
    /**
     * Gets the payloadBase64 property value. The function&apos;s request, at most 1,048,576 bytes before base64 encoding. Absent, null or empty is a job that carries none.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getPayloadBase64() {
        return this.payloadBase64;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeStringValue("idempotencyKey", this.getIdempotencyKey());
        writer.writeStringValue("jobType", this.getJobType());
        writer.writeIntegerValue("maxAttempts", this.getMaxAttempts());
        writer.writeStringValue("payloadBase64", this.getPayloadBase64());
    }
    /**
     * Sets the idempotencyKey property value. The caller&apos;s key for this request: trimmed, with no control character. It belongs to the signed-in user, who may repeat it for the same request and for no other.
     * @param value Value to set for the idempotencyKey property.
     */
    public void setIdempotencyKey(@jakarta.annotation.Nullable final String value) {
        this.idempotencyKey = value;
    }
    /**
     * Sets the jobType property value. The id of a deployed stateless function; its highest released version runs the job.
     * @param value Value to set for the jobType property.
     */
    public void setJobType(@jakarta.annotation.Nullable final String value) {
        this.jobType = value;
    }
    /**
     * Sets the maxAttempts property value. How many times the job may be attempted. Absent or null is 5. The idempotency key binds it, with the job type and the payload.
     * @param value Value to set for the maxAttempts property.
     */
    public void setMaxAttempts(@jakarta.annotation.Nullable final Integer value) {
        this.maxAttempts = value;
    }
    /**
     * Sets the payloadBase64 property value. The function&apos;s request, at most 1,048,576 bytes before base64 encoding. Absent, null or empty is a job that carries none.
     * @param value Value to set for the payloadBase64 property.
     */
    public void setPayloadBase64(@jakarta.annotation.Nullable final String value) {
        this.payloadBase64 = value;
    }
}
