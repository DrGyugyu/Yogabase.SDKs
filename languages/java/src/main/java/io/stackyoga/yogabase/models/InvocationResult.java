package io.stackyoga.yogabase.models;

import com.microsoft.kiota.ApiException;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class InvocationResult extends ApiException implements Parsable {
    /**
     * The correlationId property
     */
    private String correlationId;
    /**
     * The failureCode property
     */
    private String failureCode;
    /**
     * The inlineResultBase64 property
     */
    private String inlineResultBase64;
    /**
     * The invocationId property
     */
    private UUID invocationId;
    /**
     * The outcome property
     */
    private Integer outcome;
    /**
     * The resultReference property
     */
    private String resultReference;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link InvocationResult}
     */
    @jakarta.annotation.Nonnull
    public static InvocationResult createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new InvocationResult();
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
        deserializerMap.put("inlineResultBase64", (n) -> { this.setInlineResultBase64(n.getStringValue()); });
        deserializerMap.put("invocationId", (n) -> { this.setInvocationId(n.getUUIDValue()); });
        deserializerMap.put("outcome", (n) -> { this.setOutcome(n.getIntegerValue()); });
        deserializerMap.put("resultReference", (n) -> { this.setResultReference(n.getStringValue()); });
        return deserializerMap;
    }
    /**
     * Gets the inlineResultBase64 property value. The inlineResultBase64 property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getInlineResultBase64() {
        return this.inlineResultBase64;
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
     * Gets the outcome property value. The outcome property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getOutcome() {
        return this.outcome;
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
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeStringValue("correlationId", this.getCorrelationId());
        writer.writeStringValue("failureCode", this.getFailureCode());
        writer.writeStringValue("inlineResultBase64", this.getInlineResultBase64());
        writer.writeUUIDValue("invocationId", this.getInvocationId());
        writer.writeIntegerValue("outcome", this.getOutcome());
        writer.writeStringValue("resultReference", this.getResultReference());
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
     * Sets the inlineResultBase64 property value. The inlineResultBase64 property
     * @param value Value to set for the inlineResultBase64 property.
     */
    public void setInlineResultBase64(@jakarta.annotation.Nullable final String value) {
        this.inlineResultBase64 = value;
    }
    /**
     * Sets the invocationId property value. The invocationId property
     * @param value Value to set for the invocationId property.
     */
    public void setInvocationId(@jakarta.annotation.Nullable final UUID value) {
        this.invocationId = value;
    }
    /**
     * Sets the outcome property value. The outcome property
     * @param value Value to set for the outcome property.
     */
    public void setOutcome(@jakarta.annotation.Nullable final Integer value) {
        this.outcome = value;
    }
    /**
     * Sets the resultReference property value. The resultReference property
     * @param value Value to set for the resultReference property.
     */
    public void setResultReference(@jakarta.annotation.Nullable final String value) {
        this.resultReference = value;
    }
}
