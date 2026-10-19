package io.yogabase.client.delivery.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DeliveryPromotionPreviewRequest implements Parsable {
    /**
     * The migrationEvidenceDigest property
     */
    private String migrationEvidenceDigest;
    /**
     * The sourceEnvironmentId property
     */
    private UUID sourceEnvironmentId;
    /**
     * The sourceReleaseId property
     */
    private UUID sourceReleaseId;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DeliveryPromotionPreviewRequest}
     */
    @jakarta.annotation.Nonnull
    public static DeliveryPromotionPreviewRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DeliveryPromotionPreviewRequest();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(3);
        deserializerMap.put("migrationEvidenceDigest", (n) -> { this.setMigrationEvidenceDigest(n.getStringValue()); });
        deserializerMap.put("sourceEnvironmentId", (n) -> { this.setSourceEnvironmentId(n.getUUIDValue()); });
        deserializerMap.put("sourceReleaseId", (n) -> { this.setSourceReleaseId(n.getUUIDValue()); });
        return deserializerMap;
    }
    /**
     * Gets the migrationEvidenceDigest property value. The migrationEvidenceDigest property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getMigrationEvidenceDigest() {
        return this.migrationEvidenceDigest;
    }
    /**
     * Gets the sourceEnvironmentId property value. The sourceEnvironmentId property
     * @return a {@link UUID}
     */
    @jakarta.annotation.Nullable
    public UUID getSourceEnvironmentId() {
        return this.sourceEnvironmentId;
    }
    /**
     * Gets the sourceReleaseId property value. The sourceReleaseId property
     * @return a {@link UUID}
     */
    @jakarta.annotation.Nullable
    public UUID getSourceReleaseId() {
        return this.sourceReleaseId;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeStringValue("migrationEvidenceDigest", this.getMigrationEvidenceDigest());
        writer.writeUUIDValue("sourceEnvironmentId", this.getSourceEnvironmentId());
        writer.writeUUIDValue("sourceReleaseId", this.getSourceReleaseId());
    }
    /**
     * Sets the migrationEvidenceDigest property value. The migrationEvidenceDigest property
     * @param value Value to set for the migrationEvidenceDigest property.
     */
    public void setMigrationEvidenceDigest(@jakarta.annotation.Nullable final String value) {
        this.migrationEvidenceDigest = value;
    }
    /**
     * Sets the sourceEnvironmentId property value. The sourceEnvironmentId property
     * @param value Value to set for the sourceEnvironmentId property.
     */
    public void setSourceEnvironmentId(@jakarta.annotation.Nullable final UUID value) {
        this.sourceEnvironmentId = value;
    }
    /**
     * Sets the sourceReleaseId property value. The sourceReleaseId property
     * @param value Value to set for the sourceReleaseId property.
     */
    public void setSourceReleaseId(@jakarta.annotation.Nullable final UUID value) {
        this.sourceReleaseId = value;
    }
}
