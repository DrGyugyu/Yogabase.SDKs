package io.yogabase.client.delivery.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DeliveryReleasePreviewRequest implements Parsable {
    /**
     * The artifactAdmissionIds property
     */
    private java.util.List<UUID> artifactAdmissionIds;
    /**
     * The configuration property
     */
    private DeliveryReleaseConfiguration configuration;
    /**
     * The migrationEvidenceDigest property
     */
    private String migrationEvidenceDigest;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DeliveryReleasePreviewRequest}
     */
    @jakarta.annotation.Nonnull
    public static DeliveryReleasePreviewRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DeliveryReleasePreviewRequest();
    }
    /**
     * Gets the artifactAdmissionIds property value. The artifactAdmissionIds property
     * @return a {@link java.util.List<UUID>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<UUID> getArtifactAdmissionIds() {
        return this.artifactAdmissionIds;
    }
    /**
     * Gets the configuration property value. The configuration property
     * @return a {@link DeliveryReleaseConfiguration}
     */
    @jakarta.annotation.Nullable
    public DeliveryReleaseConfiguration getConfiguration() {
        return this.configuration;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(3);
        deserializerMap.put("artifactAdmissionIds", (n) -> { this.setArtifactAdmissionIds(n.getCollectionOfPrimitiveValues(UUID.class)); });
        deserializerMap.put("configuration", (n) -> { this.setConfiguration(n.getObjectValue(DeliveryReleaseConfiguration::createFromDiscriminatorValue)); });
        deserializerMap.put("migrationEvidenceDigest", (n) -> { this.setMigrationEvidenceDigest(n.getStringValue()); });
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
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeCollectionOfPrimitiveValues("artifactAdmissionIds", this.getArtifactAdmissionIds());
        writer.writeObjectValue("configuration", this.getConfiguration());
        writer.writeStringValue("migrationEvidenceDigest", this.getMigrationEvidenceDigest());
    }
    /**
     * Sets the artifactAdmissionIds property value. The artifactAdmissionIds property
     * @param value Value to set for the artifactAdmissionIds property.
     */
    public void setArtifactAdmissionIds(@jakarta.annotation.Nullable final java.util.List<UUID> value) {
        this.artifactAdmissionIds = value;
    }
    /**
     * Sets the configuration property value. The configuration property
     * @param value Value to set for the configuration property.
     */
    public void setConfiguration(@jakarta.annotation.Nullable final DeliveryReleaseConfiguration value) {
        this.configuration = value;
    }
    /**
     * Sets the migrationEvidenceDigest property value. The migrationEvidenceDigest property
     * @param value Value to set for the migrationEvidenceDigest property.
     */
    public void setMigrationEvidenceDigest(@jakarta.annotation.Nullable final String value) {
        this.migrationEvidenceDigest = value;
    }
}
