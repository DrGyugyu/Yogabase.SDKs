package io.stackyoga.yogabase.delivery.models;

import com.microsoft.kiota.serialization.ComposedTypeWrapper;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DeliveryReleaseResponse implements Parsable {
    /**
     * The artifactSetDigest property
     */
    private String artifactSetDigest;
    /**
     * The configurationDigest property
     */
    private String configurationDigest;
    /**
     * The desiredRuntimeRevision property
     */
    private Integer desiredRuntimeRevision;
    /**
     * The id property
     */
    private UUID id;
    /**
     * The migrationEvidenceDigest property
     */
    private String migrationEvidenceDigest;
    /**
     * The releaseType property
     */
    private String releaseType;
    /**
     * The revision property
     */
    private Integer revision;
    /**
     * The sourceReleaseId property
     */
    private DeliveryReleaseResponseSourceReleaseId sourceReleaseId;
    /**
     * The status property
     */
    private String status;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DeliveryReleaseResponse}
     */
    @jakarta.annotation.Nonnull
    public static DeliveryReleaseResponse createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DeliveryReleaseResponse();
    }
    /**
     * Gets the artifactSetDigest property value. The artifactSetDigest property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getArtifactSetDigest() {
        return this.artifactSetDigest;
    }
    /**
     * Gets the configurationDigest property value. The configurationDigest property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getConfigurationDigest() {
        return this.configurationDigest;
    }
    /**
     * Gets the desiredRuntimeRevision property value. The desiredRuntimeRevision property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getDesiredRuntimeRevision() {
        return this.desiredRuntimeRevision;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(9);
        deserializerMap.put("artifactSetDigest", (n) -> { this.setArtifactSetDigest(n.getStringValue()); });
        deserializerMap.put("configurationDigest", (n) -> { this.setConfigurationDigest(n.getStringValue()); });
        deserializerMap.put("desiredRuntimeRevision", (n) -> { this.setDesiredRuntimeRevision(n.getIntegerValue()); });
        deserializerMap.put("id", (n) -> { this.setId(n.getUUIDValue()); });
        deserializerMap.put("migrationEvidenceDigest", (n) -> { this.setMigrationEvidenceDigest(n.getStringValue()); });
        deserializerMap.put("releaseType", (n) -> { this.setReleaseType(n.getStringValue()); });
        deserializerMap.put("revision", (n) -> { this.setRevision(n.getIntegerValue()); });
        deserializerMap.put("sourceReleaseId", (n) -> { this.setSourceReleaseId(n.getObjectValue(DeliveryReleaseResponseSourceReleaseId::createFromDiscriminatorValue)); });
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
     * Gets the migrationEvidenceDigest property value. The migrationEvidenceDigest property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getMigrationEvidenceDigest() {
        return this.migrationEvidenceDigest;
    }
    /**
     * Gets the releaseType property value. The releaseType property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getReleaseType() {
        return this.releaseType;
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
     * Gets the sourceReleaseId property value. The sourceReleaseId property
     * @return a {@link DeliveryReleaseResponseSourceReleaseId}
     */
    @jakarta.annotation.Nullable
    public DeliveryReleaseResponseSourceReleaseId getSourceReleaseId() {
        return this.sourceReleaseId;
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
        writer.writeStringValue("artifactSetDigest", this.getArtifactSetDigest());
        writer.writeStringValue("configurationDigest", this.getConfigurationDigest());
        writer.writeIntegerValue("desiredRuntimeRevision", this.getDesiredRuntimeRevision());
        writer.writeUUIDValue("id", this.getId());
        writer.writeStringValue("migrationEvidenceDigest", this.getMigrationEvidenceDigest());
        writer.writeStringValue("releaseType", this.getReleaseType());
        writer.writeIntegerValue("revision", this.getRevision());
        writer.writeObjectValue("sourceReleaseId", this.getSourceReleaseId());
        writer.writeStringValue("status", this.getStatus());
    }
    /**
     * Sets the artifactSetDigest property value. The artifactSetDigest property
     * @param value Value to set for the artifactSetDigest property.
     */
    public void setArtifactSetDigest(@jakarta.annotation.Nullable final String value) {
        this.artifactSetDigest = value;
    }
    /**
     * Sets the configurationDigest property value. The configurationDigest property
     * @param value Value to set for the configurationDigest property.
     */
    public void setConfigurationDigest(@jakarta.annotation.Nullable final String value) {
        this.configurationDigest = value;
    }
    /**
     * Sets the desiredRuntimeRevision property value. The desiredRuntimeRevision property
     * @param value Value to set for the desiredRuntimeRevision property.
     */
    public void setDesiredRuntimeRevision(@jakarta.annotation.Nullable final Integer value) {
        this.desiredRuntimeRevision = value;
    }
    /**
     * Sets the id property value. The id property
     * @param value Value to set for the id property.
     */
    public void setId(@jakarta.annotation.Nullable final UUID value) {
        this.id = value;
    }
    /**
     * Sets the migrationEvidenceDigest property value. The migrationEvidenceDigest property
     * @param value Value to set for the migrationEvidenceDigest property.
     */
    public void setMigrationEvidenceDigest(@jakarta.annotation.Nullable final String value) {
        this.migrationEvidenceDigest = value;
    }
    /**
     * Sets the releaseType property value. The releaseType property
     * @param value Value to set for the releaseType property.
     */
    public void setReleaseType(@jakarta.annotation.Nullable final String value) {
        this.releaseType = value;
    }
    /**
     * Sets the revision property value. The revision property
     * @param value Value to set for the revision property.
     */
    public void setRevision(@jakarta.annotation.Nullable final Integer value) {
        this.revision = value;
    }
    /**
     * Sets the sourceReleaseId property value. The sourceReleaseId property
     * @param value Value to set for the sourceReleaseId property.
     */
    public void setSourceReleaseId(@jakarta.annotation.Nullable final DeliveryReleaseResponseSourceReleaseId value) {
        this.sourceReleaseId = value;
    }
    /**
     * Sets the status property value. The status property
     * @param value Value to set for the status property.
     */
    public void setStatus(@jakarta.annotation.Nullable final String value) {
        this.status = value;
    }
    /**
     * Composed type wrapper for classes {@link DeliveryReleaseResponseSourceReleaseIdMember1}, {@link Uuid}
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public static class DeliveryReleaseResponseSourceReleaseId implements ComposedTypeWrapper, Parsable {
        /**
         * Composed type representation for type {@link DeliveryReleaseResponseSourceReleaseIdMember1}
         */
        private DeliveryReleaseResponseSourceReleaseIdMember1 deliveryReleaseResponseSourceReleaseIdMember1;
        /**
         * Composed type representation for type {@link Uuid}
         */
        private Uuid uuid;
        /**
         * Creates a new instance of the appropriate class based on discriminator value
         * @param parseNode The parse node to use to read the discriminator value and create the object
         * @return a {@link DeliveryReleaseResponseSourceReleaseId}
         */
        @jakarta.annotation.Nonnull
        public static DeliveryReleaseResponseSourceReleaseId createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
            Objects.requireNonNull(parseNode);
            final DeliveryReleaseResponseSourceReleaseId result = new DeliveryReleaseResponseSourceReleaseId();
            final ParseNode mappingValueNode = parseNode.getChildNode("");
            if (mappingValueNode != null) {
                final String mappingValue = mappingValueNode.getStringValue();
            }
            return result;
        }
        /**
         * Gets the DeliveryReleaseResponse_sourceReleaseIdMember1 property value. Composed type representation for type {@link DeliveryReleaseResponseSourceReleaseIdMember1}
         * @return a {@link DeliveryReleaseResponseSourceReleaseIdMember1}
         */
        @jakarta.annotation.Nullable
        public DeliveryReleaseResponseSourceReleaseIdMember1 getDeliveryReleaseResponseSourceReleaseIdMember1() {
            return this.deliveryReleaseResponseSourceReleaseIdMember1;
        }
        /**
         * The deserialization information for the current model
         * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
         */
        @jakarta.annotation.Nonnull
        public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
            if (this.getDeliveryReleaseResponseSourceReleaseIdMember1() != null) {
                return this.getDeliveryReleaseResponseSourceReleaseIdMember1().getFieldDeserializers();
            } else if (this.getUuid() != null) {
                return this.getUuid().getFieldDeserializers();
            }
            return new HashMap<String, java.util.function.Consumer<ParseNode>>();
        }
        /**
         * Gets the Uuid property value. Composed type representation for type {@link Uuid}
         * @return a {@link Uuid}
         */
        @jakarta.annotation.Nullable
        public Uuid getUuid() {
            return this.uuid;
        }
        /**
         * Serializes information the current object
         * @param writer Serialization writer to use to serialize this model
         */
        public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
            Objects.requireNonNull(writer);
            if (this.getDeliveryReleaseResponseSourceReleaseIdMember1() != null) {
                writer.writeObjectValue(null, this.getDeliveryReleaseResponseSourceReleaseIdMember1());
            } else if (this.getUuid() != null) {
                writer.writeObjectValue(null, this.getUuid());
            }
        }
        /**
         * Sets the DeliveryReleaseResponse_sourceReleaseIdMember1 property value. Composed type representation for type {@link DeliveryReleaseResponseSourceReleaseIdMember1}
         * @param value Value to set for the DeliveryReleaseResponse_sourceReleaseIdMember1 property.
         */
        public void setDeliveryReleaseResponseSourceReleaseIdMember1(@jakarta.annotation.Nullable final DeliveryReleaseResponseSourceReleaseIdMember1 value) {
            this.deliveryReleaseResponseSourceReleaseIdMember1 = value;
        }
        /**
         * Sets the Uuid property value. Composed type representation for type {@link Uuid}
         * @param value Value to set for the Uuid property.
         */
        public void setUuid(@jakarta.annotation.Nullable final Uuid value) {
            this.uuid = value;
        }
    }
}
