package io.yogabase.client.delivery.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.time.OffsetDateTime;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DeliveryPreviewResponse implements Parsable {
    /**
     * The action property
     */
    private String action;
    /**
     * The approvalRequired property
     */
    private Boolean approvalRequired;
    /**
     * The expectedRevision property
     */
    private Integer expectedRevision;
    /**
     * The expiresAt property
     */
    private OffsetDateTime expiresAt;
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
     * @return a {@link DeliveryPreviewResponse}
     */
    @jakarta.annotation.Nonnull
    public static DeliveryPreviewResponse createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DeliveryPreviewResponse();
    }
    /**
     * Gets the action property value. The action property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getAction() {
        return this.action;
    }
    /**
     * Gets the approvalRequired property value. The approvalRequired property
     * @return a {@link Boolean}
     */
    @jakarta.annotation.Nullable
    public Boolean getApprovalRequired() {
        return this.approvalRequired;
    }
    /**
     * Gets the expectedRevision property value. The expectedRevision property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getExpectedRevision() {
        return this.expectedRevision;
    }
    /**
     * Gets the expiresAt property value. The expiresAt property
     * @return a {@link OffsetDateTime}
     */
    @jakarta.annotation.Nullable
    public OffsetDateTime getExpiresAt() {
        return this.expiresAt;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(6);
        deserializerMap.put("action", (n) -> { this.setAction(n.getStringValue()); });
        deserializerMap.put("approvalRequired", (n) -> { this.setApprovalRequired(n.getBooleanValue()); });
        deserializerMap.put("expectedRevision", (n) -> { this.setExpectedRevision(n.getIntegerValue()); });
        deserializerMap.put("expiresAt", (n) -> { this.setExpiresAt(n.getOffsetDateTimeValue()); });
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
        writer.writeStringValue("action", this.getAction());
        writer.writeBooleanValue("approvalRequired", this.getApprovalRequired());
        writer.writeIntegerValue("expectedRevision", this.getExpectedRevision());
        writer.writeOffsetDateTimeValue("expiresAt", this.getExpiresAt());
        writer.writeStringValue("previewDigest", this.getPreviewDigest());
        writer.writeUUIDValue("previewId", this.getPreviewId());
    }
    /**
     * Sets the action property value. The action property
     * @param value Value to set for the action property.
     */
    public void setAction(@jakarta.annotation.Nullable final String value) {
        this.action = value;
    }
    /**
     * Sets the approvalRequired property value. The approvalRequired property
     * @param value Value to set for the approvalRequired property.
     */
    public void setApprovalRequired(@jakarta.annotation.Nullable final Boolean value) {
        this.approvalRequired = value;
    }
    /**
     * Sets the expectedRevision property value. The expectedRevision property
     * @param value Value to set for the expectedRevision property.
     */
    public void setExpectedRevision(@jakarta.annotation.Nullable final Integer value) {
        this.expectedRevision = value;
    }
    /**
     * Sets the expiresAt property value. The expiresAt property
     * @param value Value to set for the expiresAt property.
     */
    public void setExpiresAt(@jakarta.annotation.Nullable final OffsetDateTime value) {
        this.expiresAt = value;
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
