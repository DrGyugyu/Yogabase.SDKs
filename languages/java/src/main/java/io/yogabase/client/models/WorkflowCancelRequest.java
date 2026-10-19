package io.yogabase.client.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class WorkflowCancelRequest implements Parsable {
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
     * @return a {@link WorkflowCancelRequest}
     */
    @jakarta.annotation.Nonnull
    public static WorkflowCancelRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new WorkflowCancelRequest();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(3);
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
        writer.writeStringValue("idempotencyKey", this.getIdempotencyKey());
        writer.writeStringValue("inlinePayloadBase64", this.getInlinePayloadBase64());
        writer.writeStringValue("payloadDigest", this.getPayloadDigest());
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
