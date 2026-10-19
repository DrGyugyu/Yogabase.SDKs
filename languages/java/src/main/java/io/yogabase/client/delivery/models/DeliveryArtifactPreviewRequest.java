package io.yogabase.client.delivery.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.time.OffsetDateTime;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DeliveryArtifactPreviewRequest implements Parsable {
    /**
     * The artifactVersion property
     */
    private Integer artifactVersion;
    /**
     * The bundleDigest property
     */
    private String bundleDigest;
    /**
     * The criticalFindings property
     */
    private Integer criticalFindings;
    /**
     * The highFindings property
     */
    private Integer highFindings;
    /**
     * The manifestDigest property
     */
    private String manifestDigest;
    /**
     * The manifestUri property
     */
    private String manifestUri;
    /**
     * The sbomDigest property
     */
    private String sbomDigest;
    /**
     * The sbomFormat property
     */
    private String sbomFormat;
    /**
     * The sbomUri property
     */
    private String sbomUri;
    /**
     * The scanEvidenceDigest property
     */
    private String scanEvidenceDigest;
    /**
     * The scannedAt property
     */
    private OffsetDateTime scannedAt;
    /**
     * The scanner property
     */
    private String scanner;
    /**
     * The scanUri property
     */
    private String scanUri;
    /**
     * Credential-free native provider URI; percent escapes are rejected.
     */
    private String sourceLocator;
    /**
     * The sourceMediaType property
     */
    private String sourceMediaType;
    /**
     * The sourceSizeBytes property
     */
    private Integer sourceSizeBytes;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DeliveryArtifactPreviewRequest}
     */
    @jakarta.annotation.Nonnull
    public static DeliveryArtifactPreviewRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DeliveryArtifactPreviewRequest();
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
     * Gets the criticalFindings property value. The criticalFindings property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getCriticalFindings() {
        return this.criticalFindings;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(16);
        deserializerMap.put("artifactVersion", (n) -> { this.setArtifactVersion(n.getIntegerValue()); });
        deserializerMap.put("bundleDigest", (n) -> { this.setBundleDigest(n.getStringValue()); });
        deserializerMap.put("criticalFindings", (n) -> { this.setCriticalFindings(n.getIntegerValue()); });
        deserializerMap.put("highFindings", (n) -> { this.setHighFindings(n.getIntegerValue()); });
        deserializerMap.put("manifestDigest", (n) -> { this.setManifestDigest(n.getStringValue()); });
        deserializerMap.put("manifestUri", (n) -> { this.setManifestUri(n.getStringValue()); });
        deserializerMap.put("sbomDigest", (n) -> { this.setSbomDigest(n.getStringValue()); });
        deserializerMap.put("sbomFormat", (n) -> { this.setSbomFormat(n.getStringValue()); });
        deserializerMap.put("sbomUri", (n) -> { this.setSbomUri(n.getStringValue()); });
        deserializerMap.put("scanEvidenceDigest", (n) -> { this.setScanEvidenceDigest(n.getStringValue()); });
        deserializerMap.put("scannedAt", (n) -> { this.setScannedAt(n.getOffsetDateTimeValue()); });
        deserializerMap.put("scanner", (n) -> { this.setScanner(n.getStringValue()); });
        deserializerMap.put("scanUri", (n) -> { this.setScanUri(n.getStringValue()); });
        deserializerMap.put("sourceLocator", (n) -> { this.setSourceLocator(n.getStringValue()); });
        deserializerMap.put("sourceMediaType", (n) -> { this.setSourceMediaType(n.getStringValue()); });
        deserializerMap.put("sourceSizeBytes", (n) -> { this.setSourceSizeBytes(n.getIntegerValue()); });
        return deserializerMap;
    }
    /**
     * Gets the highFindings property value. The highFindings property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getHighFindings() {
        return this.highFindings;
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
     * Gets the manifestUri property value. The manifestUri property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getManifestUri() {
        return this.manifestUri;
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
     * Gets the sbomFormat property value. The sbomFormat property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getSbomFormat() {
        return this.sbomFormat;
    }
    /**
     * Gets the sbomUri property value. The sbomUri property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getSbomUri() {
        return this.sbomUri;
    }
    /**
     * Gets the scanEvidenceDigest property value. The scanEvidenceDigest property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getScanEvidenceDigest() {
        return this.scanEvidenceDigest;
    }
    /**
     * Gets the scannedAt property value. The scannedAt property
     * @return a {@link OffsetDateTime}
     */
    @jakarta.annotation.Nullable
    public OffsetDateTime getScannedAt() {
        return this.scannedAt;
    }
    /**
     * Gets the scanner property value. The scanner property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getScanner() {
        return this.scanner;
    }
    /**
     * Gets the scanUri property value. The scanUri property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getScanUri() {
        return this.scanUri;
    }
    /**
     * Gets the sourceLocator property value. Credential-free native provider URI; percent escapes are rejected.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getSourceLocator() {
        return this.sourceLocator;
    }
    /**
     * Gets the sourceMediaType property value. The sourceMediaType property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getSourceMediaType() {
        return this.sourceMediaType;
    }
    /**
     * Gets the sourceSizeBytes property value. The sourceSizeBytes property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getSourceSizeBytes() {
        return this.sourceSizeBytes;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeIntegerValue("artifactVersion", this.getArtifactVersion());
        writer.writeStringValue("bundleDigest", this.getBundleDigest());
        writer.writeIntegerValue("criticalFindings", this.getCriticalFindings());
        writer.writeIntegerValue("highFindings", this.getHighFindings());
        writer.writeStringValue("manifestDigest", this.getManifestDigest());
        writer.writeStringValue("manifestUri", this.getManifestUri());
        writer.writeStringValue("sbomDigest", this.getSbomDigest());
        writer.writeStringValue("sbomFormat", this.getSbomFormat());
        writer.writeStringValue("sbomUri", this.getSbomUri());
        writer.writeStringValue("scanEvidenceDigest", this.getScanEvidenceDigest());
        writer.writeOffsetDateTimeValue("scannedAt", this.getScannedAt());
        writer.writeStringValue("scanner", this.getScanner());
        writer.writeStringValue("scanUri", this.getScanUri());
        writer.writeStringValue("sourceLocator", this.getSourceLocator());
        writer.writeStringValue("sourceMediaType", this.getSourceMediaType());
        writer.writeIntegerValue("sourceSizeBytes", this.getSourceSizeBytes());
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
     * Sets the criticalFindings property value. The criticalFindings property
     * @param value Value to set for the criticalFindings property.
     */
    public void setCriticalFindings(@jakarta.annotation.Nullable final Integer value) {
        this.criticalFindings = value;
    }
    /**
     * Sets the highFindings property value. The highFindings property
     * @param value Value to set for the highFindings property.
     */
    public void setHighFindings(@jakarta.annotation.Nullable final Integer value) {
        this.highFindings = value;
    }
    /**
     * Sets the manifestDigest property value. The manifestDigest property
     * @param value Value to set for the manifestDigest property.
     */
    public void setManifestDigest(@jakarta.annotation.Nullable final String value) {
        this.manifestDigest = value;
    }
    /**
     * Sets the manifestUri property value. The manifestUri property
     * @param value Value to set for the manifestUri property.
     */
    public void setManifestUri(@jakarta.annotation.Nullable final String value) {
        this.manifestUri = value;
    }
    /**
     * Sets the sbomDigest property value. The sbomDigest property
     * @param value Value to set for the sbomDigest property.
     */
    public void setSbomDigest(@jakarta.annotation.Nullable final String value) {
        this.sbomDigest = value;
    }
    /**
     * Sets the sbomFormat property value. The sbomFormat property
     * @param value Value to set for the sbomFormat property.
     */
    public void setSbomFormat(@jakarta.annotation.Nullable final String value) {
        this.sbomFormat = value;
    }
    /**
     * Sets the sbomUri property value. The sbomUri property
     * @param value Value to set for the sbomUri property.
     */
    public void setSbomUri(@jakarta.annotation.Nullable final String value) {
        this.sbomUri = value;
    }
    /**
     * Sets the scanEvidenceDigest property value. The scanEvidenceDigest property
     * @param value Value to set for the scanEvidenceDigest property.
     */
    public void setScanEvidenceDigest(@jakarta.annotation.Nullable final String value) {
        this.scanEvidenceDigest = value;
    }
    /**
     * Sets the scannedAt property value. The scannedAt property
     * @param value Value to set for the scannedAt property.
     */
    public void setScannedAt(@jakarta.annotation.Nullable final OffsetDateTime value) {
        this.scannedAt = value;
    }
    /**
     * Sets the scanner property value. The scanner property
     * @param value Value to set for the scanner property.
     */
    public void setScanner(@jakarta.annotation.Nullable final String value) {
        this.scanner = value;
    }
    /**
     * Sets the scanUri property value. The scanUri property
     * @param value Value to set for the scanUri property.
     */
    public void setScanUri(@jakarta.annotation.Nullable final String value) {
        this.scanUri = value;
    }
    /**
     * Sets the sourceLocator property value. Credential-free native provider URI; percent escapes are rejected.
     * @param value Value to set for the sourceLocator property.
     */
    public void setSourceLocator(@jakarta.annotation.Nullable final String value) {
        this.sourceLocator = value;
    }
    /**
     * Sets the sourceMediaType property value. The sourceMediaType property
     * @param value Value to set for the sourceMediaType property.
     */
    public void setSourceMediaType(@jakarta.annotation.Nullable final String value) {
        this.sourceMediaType = value;
    }
    /**
     * Sets the sourceSizeBytes property value. The sourceSizeBytes property
     * @param value Value to set for the sourceSizeBytes property.
     */
    public void setSourceSizeBytes(@jakarta.annotation.Nullable final Integer value) {
        this.sourceSizeBytes = value;
    }
}
