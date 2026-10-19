package io.yogabase.client.delivery.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DeliverySubmitRequest implements Parsable {
    /**
     * The previewDigest property
     */
    private String previewDigest;
    /**
     * The previewId property
     */
    private UUID previewId;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DeliverySubmitRequest}
     */
    @jakarta.annotation.Nonnull
    public static DeliverySubmitRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DeliverySubmitRequest();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(2);
        deserializerMap.put("previewDigest", (n) -> { this.setPreviewDigest(n.getStringValue()); });
        deserializerMap.put("previewId", (n) -> { this.setPreviewId(n.getUUIDValue()); });
        return deserializerMap;
    }
    /**
     * Gets the previewDigest property value. The previewDigest property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getPreviewDigest() {
        return this.previewDigest;
    }
    /**
     * Gets the previewId property value. The previewId property
     * @return a {@link UUID}
     */
    @jakarta.annotation.Nullable
    public UUID getPreviewId() {
        return this.previewId;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeStringValue("previewDigest", this.getPreviewDigest());
        writer.writeUUIDValue("previewId", this.getPreviewId());
    }
    /**
     * Sets the previewDigest property value. The previewDigest property
     * @param value Value to set for the previewDigest property.
     */
    public void setPreviewDigest(@jakarta.annotation.Nullable final String value) {
        this.previewDigest = value;
    }
    /**
     * Sets the previewId property value. The previewId property
     * @param value Value to set for the previewId property.
     */
    public void setPreviewId(@jakarta.annotation.Nullable final UUID value) {
        this.previewId = value;
    }
}
