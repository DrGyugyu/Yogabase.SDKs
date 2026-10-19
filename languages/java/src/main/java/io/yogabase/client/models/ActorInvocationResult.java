package io.yogabase.client.models;

import com.microsoft.kiota.ApiException;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class ActorInvocationResult extends ApiException implements Parsable {
    /**
     * The correlationId property
     */
    private String correlationId;
    /**
     * The failureCode property
     */
    private String failureCode;
    /**
     * The invocationId property
     */
    private UUID invocationId;
    /**
     * The resultReference property
     */
    private String resultReference;
    /**
     * The stateVersion property
     */
    private Long stateVersion;
    /**
     * The status property
     */
    private ActorInvocationResultStatus status;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link ActorInvocationResult}
     */
    @jakarta.annotation.Nonnull
    public static ActorInvocationResult createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new ActorInvocationResult();
    }
    /**
     * Gets the correlationId property value. The correlationId property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getCorrelationId() {
        return this.correlationId;
    }
    /**
     * Gets the failureCode property value. The failureCode property
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
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(6);
        deserializerMap.put("correlationId", (n) -> { this.setCorrelationId(n.getStringValue()); });
        deserializerMap.put("failureCode", (n) -> { this.setFailureCode(n.getStringValue()); });
        deserializerMap.put("invocationId", (n) -> { this.setInvocationId(n.getUUIDValue()); });
        deserializerMap.put("resultReference", (n) -> { this.setResultReference(n.getStringValue()); });
        deserializerMap.put("stateVersion", (n) -> { this.setStateVersion(n.getLongValue()); });
        deserializerMap.put("status", (n) -> { this.setStatus(n.getEnumValue(ActorInvocationResultStatus::forValue)); });
        return deserializerMap;
    }
    /**
     * Gets the invocationId property value. The invocationId property
     * @return a {@link UUID}
     */
    @jakarta.annotation.Nullable
    public UUID getInvocationId() {
        return this.invocationId;
    }
    /**
     * The primary error message.
     * @return a {@link String}
     */
    @jakarta.annotation.Nonnull
    @Override
    public String getMessage() {
        return super.getMessage();
    }
    /**
     * Gets the resultReference property value. The resultReference property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getResultReference() {
        return this.resultReference;
    }
    /**
     * Gets the stateVersion property value. The stateVersion property
     * @return a {@link Long}
     */
    @jakarta.annotation.Nullable
    public Long getStateVersion() {
        return this.stateVersion;
    }
    /**
     * Gets the status property value. The status property
     * @return a {@link ActorInvocationResultStatus}
     */
    @jakarta.annotation.Nullable
    public ActorInvocationResultStatus getStatus() {
        return this.status;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeStringValue("correlationId", this.getCorrelationId());
        writer.writeStringValue("failureCode", this.getFailureCode());
        writer.writeUUIDValue("invocationId", this.getInvocationId());
        writer.writeStringValue("resultReference", this.getResultReference());
        writer.writeLongValue("stateVersion", this.getStateVersion());
        writer.writeEnumValue("status", this.getStatus());
    }
    /**
     * Sets the correlationId property value. The correlationId property
     * @param value Value to set for the correlationId property.
     */
    public void setCorrelationId(@jakarta.annotation.Nullable final String value) {
        this.correlationId = value;
    }
    /**
     * Sets the failureCode property value. The failureCode property
     * @param value Value to set for the failureCode property.
     */
    public void setFailureCode(@jakarta.annotation.Nullable final String value) {
        this.failureCode = value;
    }
    /**
     * Sets the invocationId property value. The invocationId property
     * @param value Value to set for the invocationId property.
     */
    public void setInvocationId(@jakarta.annotation.Nullable final UUID value) {
        this.invocationId = value;
    }
    /**
     * Sets the resultReference property value. The resultReference property
     * @param value Value to set for the resultReference property.
     */
    public void setResultReference(@jakarta.annotation.Nullable final String value) {
        this.resultReference = value;
    }
    /**
     * Sets the stateVersion property value. The stateVersion property
     * @param value Value to set for the stateVersion property.
     */
    public void setStateVersion(@jakarta.annotation.Nullable final Long value) {
        this.stateVersion = value;
    }
    /**
     * Sets the status property value. The status property
     * @param value Value to set for the status property.
     */
    public void setStatus(@jakarta.annotation.Nullable final ActorInvocationResultStatus value) {
        this.status = value;
    }
}
