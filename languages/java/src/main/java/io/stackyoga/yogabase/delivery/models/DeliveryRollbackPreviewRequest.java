package io.stackyoga.yogabase.delivery.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DeliveryRollbackPreviewRequest implements Parsable {
    /**
     * The migrationEvidenceDigest property
     */
    private String migrationEvidenceDigest;
    /**
     * The releaseId property
     */
    private UUID releaseId;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DeliveryRollbackPreviewRequest}
     */
    @jakarta.annotation.Nonnull
    public static DeliveryRollbackPreviewRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DeliveryRollbackPreviewRequest();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(2);
        deserializerMap.put("migrationEvidenceDigest", (n) -> { this.setMigrationEvidenceDigest(n.getStringValue()); });
        deserializerMap.put("releaseId", (n) -> { this.setReleaseId(n.getUUIDValue()); });
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
     * Gets the releaseId property value. The releaseId property
     * @return a {@link UUID}
     */
    @jakarta.annotation.Nullable
    public UUID getReleaseId() {
        return this.releaseId;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeStringValue("migrationEvidenceDigest", this.getMigrationEvidenceDigest());
        writer.writeUUIDValue("releaseId", this.getReleaseId());
    }
    /**
     * Sets the migrationEvidenceDigest property value. The migrationEvidenceDigest property
     * @param value Value to set for the migrationEvidenceDigest property.
     */
    public void setMigrationEvidenceDigest(@jakarta.annotation.Nullable final String value) {
        this.migrationEvidenceDigest = value;
    }
    /**
     * Sets the releaseId property value. The releaseId property
     * @param value Value to set for the releaseId property.
     */
    public void setReleaseId(@jakarta.annotation.Nullable final UUID value) {
        this.releaseId = value;
    }
}
