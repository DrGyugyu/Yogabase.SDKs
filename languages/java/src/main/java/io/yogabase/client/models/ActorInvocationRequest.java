package io.yogabase.client.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.time.OffsetDateTime;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class ActorInvocationRequest implements Parsable {
    /**
     * The correlationId property
     */
    private String correlationId;
    /**
     * Must be within 30 seconds of admission.
     */
    private OffsetDateTime deadline;
    /**
     * The idempotencyKey property
     */
    private String idempotencyKey;
    /**
     * The inlinePayloadBase64 property
     */
    private String inlinePayloadBase64;
    /**
     * The payloadDigest property
     */
    private String payloadDigest;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link ActorInvocationRequest}
     */
    @jakarta.annotation.Nonnull
    public static ActorInvocationRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new ActorInvocationRequest();
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
     * Gets the deadline property value. Must be within 30 seconds of admission.
     * @return a {@link OffsetDateTime}
     */
    @jakarta.annotation.Nullable
    public OffsetDateTime getDeadline() {
        return this.deadline;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(5);
        deserializerMap.put("correlationId", (n) -> { this.setCorrelationId(n.getStringValue()); });
        deserializerMap.put("deadline", (n) -> { this.setDeadline(n.getOffsetDateTimeValue()); });
        deserializerMap.put("idempotencyKey", (n) -> { this.setIdempotencyKey(n.getStringValue()); });
        deserializerMap.put("inlinePayloadBase64", (n) -> { this.setInlinePayloadBase64(n.getStringValue()); });
        deserializerMap.put("payloadDigest", (n) -> { this.setPayloadDigest(n.getStringValue()); });
        return deserializerMap;
    }
    /**
     * Gets the idempotencyKey property value. The idempotencyKey property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getIdempotencyKey() {
        return this.idempotencyKey;
    }
    /**
     * Gets the inlinePayloadBase64 property value. The inlinePayloadBase64 property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getInlinePayloadBase64() {
        return this.inlinePayloadBase64;
    }
    /**
     * Gets the payloadDigest property value. The payloadDigest property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getPayloadDigest() {
        return this.payloadDigest;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeStringValue("correlationId", this.getCorrelationId());
        writer.writeOffsetDateTimeValue("deadline", this.getDeadline());
        writer.writeStringValue("idempotencyKey", this.getIdempotencyKey());
        writer.writeStringValue("inlinePayloadBase64", this.getInlinePayloadBase64());
        writer.writeStringValue("payloadDigest", this.getPayloadDigest());
    }
    /**
     * Sets the correlationId property value. The correlationId property
     * @param value Value to set for the correlationId property.
     */
    public void setCorrelationId(@jakarta.annotation.Nullable final String value) {
        this.correlationId = value;
    }
    /**
     * Sets the deadline property value. Must be within 30 seconds of admission.
     * @param value Value to set for the deadline property.
     */
    public void setDeadline(@jakarta.annotation.Nullable final OffsetDateTime value) {
        this.deadline = value;
    }
    /**
     * Sets the idempotencyKey property value. The idempotencyKey property
     * @param value Value to set for the idempotencyKey property.
     */
    public void setIdempotencyKey(@jakarta.annotation.Nullable final String value) {
        this.idempotencyKey = value;
    }
    /**
     * Sets the inlinePayloadBase64 property value. The inlinePayloadBase64 property
     * @param value Value to set for the inlinePayloadBase64 property.
     */
    public void setInlinePayloadBase64(@jakarta.annotation.Nullable final String value) {
        this.inlinePayloadBase64 = value;
    }
    /**
     * Sets the payloadDigest property value. The payloadDigest property
     * @param value Value to set for the payloadDigest property.
     */
    public void setPayloadDigest(@jakarta.annotation.Nullable final String value) {
        this.payloadDigest = value;
    }
}
