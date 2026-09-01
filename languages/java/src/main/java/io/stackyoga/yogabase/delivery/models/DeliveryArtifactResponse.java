package io.stackyoga.yogabase.delivery.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DeliveryArtifactResponse implements Parsable {
    /**
     * The artifactVersion property
     */
    private Integer artifactVersion;
    /**
     * The bundleDigest property
     */
    private String bundleDigest;
    /**
     * The id property
     */
    private UUID id;
    /**
     * The keyId property
     */
    private String keyId;
    /**
     * The manifestDigest property
     */
    private String manifestDigest;
    /**
     * The revision property
     */
    private Integer revision;
    /**
     * The sbomDigest property
     */
    private String sbomDigest;
    /**
     * The scanResultDigest property
     */
    private String scanResultDigest;
    /**
     * The signerId property
     */
    private String signerId;
    /**
     * The status property
     */
    private String status;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DeliveryArtifactResponse}
     */
    @jakarta.annotation.Nonnull
    public static DeliveryArtifactResponse createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DeliveryArtifactResponse();
    }
    /**
     * Gets the artifactVersion property value. The artifactVersion property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getArtifactVersion() {
        return this.artifactVersion;
    }
    /**
     * Gets the bundleDigest property value. The bundleDigest property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getBundleDigest() {
        return this.bundleDigest;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(10);
        deserializerMap.put("artifactVersion", (n) -> { this.setArtifactVersion(n.getIntegerValue()); });
        deserializerMap.put("bundleDigest", (n) -> { this.setBundleDigest(n.getStringValue()); });
        deserializerMap.put("id", (n) -> { this.setId(n.getUUIDValue()); });
        deserializerMap.put("keyId", (n) -> { this.setKeyId(n.getStringValue()); });
        deserializerMap.put("manifestDigest", (n) -> { this.setManifestDigest(n.getStringValue()); });
        deserializerMap.put("revision", (n) -> { this.setRevision(n.getIntegerValue()); });
        deserializerMap.put("sbomDigest", (n) -> { this.setSbomDigest(n.getStringValue()); });
        deserializerMap.put("scanResultDigest", (n) -> { this.setScanResultDigest(n.getStringValue()); });
        deserializerMap.put("signerId", (n) -> { this.setSignerId(n.getStringValue()); });
        deserializerMap.put("status", (n) -> { this.setStatus(n.getStringValue()); });
        return deserializerMap;
    }
    /**
     * Gets the id property value. The id property
     * @return a {@link UUID}
     */
    @jakarta.annotation.Nullable
    public UUID getId() {
        return this.id;
    }
    /**
     * Gets the keyId property value. The keyId property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getKeyId() {
        return this.keyId;
    }
    /**
     * Gets the manifestDigest property value. The manifestDigest property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getManifestDigest() {
        return this.manifestDigest;
    }
    /**
     * Gets the revision property value. The revision property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getRevision() {
        return this.revision;
    }
    /**
     * Gets the sbomDigest property value. The sbomDigest property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getSbomDigest() {
        return this.sbomDigest;
    }
    /**
     * Gets the scanResultDigest property value. The scanResultDigest property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getScanResultDigest() {
        return this.scanResultDigest;
    }
    /**
     * Gets the signerId property value. The signerId property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getSignerId() {
        return this.signerId;
    }
    /**
     * Gets the status property value. The status property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getStatus() {
        return this.status;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeIntegerValue("artifactVersion", this.getArtifactVersion());
        writer.writeStringValue("bundleDigest", this.getBundleDigest());
        writer.writeUUIDValue("id", this.getId());
        writer.writeStringValue("keyId", this.getKeyId());
        writer.writeStringValue("manifestDigest", this.getManifestDigest());
        writer.writeIntegerValue("revision", this.getRevision());
        writer.writeStringValue("sbomDigest", this.getSbomDigest());
        writer.writeStringValue("scanResultDigest", this.getScanResultDigest());
        writer.writeStringValue("signerId", this.getSignerId());
        writer.writeStringValue("status", this.getStatus());
    }
    /**
     * Sets the artifactVersion property value. The artifactVersion property
     * @param value Value to set for the artifactVersion property.
     */
    public void setArtifactVersion(@jakarta.annotation.Nullable final Integer value) {
        this.artifactVersion = value;
    }
    /**
     * Sets the bundleDigest property value. The bundleDigest property
     * @param value Value to set for the bundleDigest property.
     */
    public void setBundleDigest(@jakarta.annotation.Nullable final String value) {
        this.bundleDigest = value;
    }
    /**
     * Sets the id property value. The id property
     * @param value Value to set for the id property.
     */
    public void setId(@jakarta.annotation.Nullable final UUID value) {
        this.id = value;
    }
    /**
     * Sets the keyId property value. The keyId property
     * @param value Value to set for the keyId property.
     */
    public void setKeyId(@jakarta.annotation.Nullable final String value) {
        this.keyId = value;
    }
    /**
     * Sets the manifestDigest property value. The manifestDigest property
     * @param value Value to set for the manifestDigest property.
     */
    public void setManifestDigest(@jakarta.annotation.Nullable final String value) {
        this.manifestDigest = value;
    }
    /**
     * Sets the revision property value. The revision property
     * @param value Value to set for the revision property.
     */
    public void setRevision(@jakarta.annotation.Nullable final Integer value) {
        this.revision = value;
    }
    /**
     * Sets the sbomDigest property value. The sbomDigest property
     * @param value Value to set for the sbomDigest property.
     */
    public void setSbomDigest(@jakarta.annotation.Nullable final String value) {
        this.sbomDigest = value;
    }
    /**
     * Sets the scanResultDigest property value. The scanResultDigest property
     * @param value Value to set for the scanResultDigest property.
     */
    public void setScanResultDigest(@jakarta.annotation.Nullable final String value) {
        this.scanResultDigest = value;
    }
    /**
     * Sets the signerId property value. The signerId property
     * @param value Value to set for the signerId property.
     */
    public void setSignerId(@jakarta.annotation.Nullable final String value) {
        this.signerId = value;
    }
    /**
     * Sets the status property value. The status property
     * @param value Value to set for the status property.
     */
    public void setStatus(@jakarta.annotation.Nullable final String value) {
        this.status = value;
    }
}
