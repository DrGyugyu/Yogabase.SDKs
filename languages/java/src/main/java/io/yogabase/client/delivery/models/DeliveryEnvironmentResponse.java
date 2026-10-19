package io.yogabase.client.delivery.models;

import com.microsoft.kiota.serialization.ComposedTypeWrapper;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DeliveryEnvironmentResponse implements Parsable {
    /**
     * The currentReleaseId property
     */
    private DeliveryEnvironmentResponseCurrentReleaseId currentReleaseId;
    /**
     * The id property
     */
    private UUID id;
    /**
     * The isProduction property
     */
    private Boolean isProduction;
    /**
     * The lifecycle property
     */
    private String lifecycle;
    /**
     * The name property
     */
    private String name;
    /**
     * The revision property
     */
    private Integer revision;
    /**
     * The runtimeRevision property
     */
    private Integer runtimeRevision;
    /**
     * The slug property
     */
    private String slug;
    /**
     * The templateId property
     */
    private UUID templateId;
    /**
     * The templateVersion property
     */
    private Integer templateVersion;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DeliveryEnvironmentResponse}
     */
    @jakarta.annotation.Nonnull
    public static DeliveryEnvironmentResponse createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DeliveryEnvironmentResponse();
    }
    /**
     * Gets the currentReleaseId property value. The currentReleaseId property
     * @return a {@link DeliveryEnvironmentResponseCurrentReleaseId}
     */
    @jakarta.annotation.Nullable
    public DeliveryEnvironmentResponseCurrentReleaseId getCurrentReleaseId() {
        return this.currentReleaseId;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(10);
        deserializerMap.put("currentReleaseId", (n) -> { this.setCurrentReleaseId(n.getObjectValue(DeliveryEnvironmentResponseCurrentReleaseId::createFromDiscriminatorValue)); });
        deserializerMap.put("id", (n) -> { this.setId(n.getUUIDValue()); });
        deserializerMap.put("isProduction", (n) -> { this.setIsProduction(n.getBooleanValue()); });
        deserializerMap.put("lifecycle", (n) -> { this.setLifecycle(n.getStringValue()); });
        deserializerMap.put("name", (n) -> { this.setName(n.getStringValue()); });
        deserializerMap.put("revision", (n) -> { this.setRevision(n.getIntegerValue()); });
        deserializerMap.put("runtimeRevision", (n) -> { this.setRuntimeRevision(n.getIntegerValue()); });
        deserializerMap.put("slug", (n) -> { this.setSlug(n.getStringValue()); });
        deserializerMap.put("templateId", (n) -> { this.setTemplateId(n.getUUIDValue()); });
        deserializerMap.put("templateVersion", (n) -> { this.setTemplateVersion(n.getIntegerValue()); });
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
     * Gets the isProduction property value. The isProduction property
     * @return a {@link Boolean}
     */
    @jakarta.annotation.Nullable
    public Boolean getIsProduction() {
        return this.isProduction;
    }
    /**
     * Gets the lifecycle property value. The lifecycle property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getLifecycle() {
        return this.lifecycle;
    }
    /**
     * Gets the name property value. The name property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getName() {
        return this.name;
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
     * Gets the runtimeRevision property value. The runtimeRevision property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getRuntimeRevision() {
        return this.runtimeRevision;
    }
    /**
     * Gets the slug property value. The slug property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getSlug() {
        return this.slug;
    }
    /**
     * Gets the templateId property value. The templateId property
     * @return a {@link UUID}
     */
    @jakarta.annotation.Nullable
    public UUID getTemplateId() {
        return this.templateId;
    }
    /**
     * Gets the templateVersion property value. The templateVersion property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getTemplateVersion() {
        return this.templateVersion;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeObjectValue("currentReleaseId", this.getCurrentReleaseId());
        writer.writeUUIDValue("id", this.getId());
        writer.writeBooleanValue("isProduction", this.getIsProduction());
        writer.writeStringValue("lifecycle", this.getLifecycle());
        writer.writeStringValue("name", this.getName());
        writer.writeIntegerValue("revision", this.getRevision());
        writer.writeIntegerValue("runtimeRevision", this.getRuntimeRevision());
        writer.writeStringValue("slug", this.getSlug());
        writer.writeUUIDValue("templateId", this.getTemplateId());
        writer.writeIntegerValue("templateVersion", this.getTemplateVersion());
    }
    /**
     * Sets the currentReleaseId property value. The currentReleaseId property
     * @param value Value to set for the currentReleaseId property.
     */
    public void setCurrentReleaseId(@jakarta.annotation.Nullable final DeliveryEnvironmentResponseCurrentReleaseId value) {
        this.currentReleaseId = value;
    }
    /**
     * Sets the id property value. The id property
     * @param value Value to set for the id property.
     */
    public void setId(@jakarta.annotation.Nullable final UUID value) {
        this.id = value;
    }
    /**
     * Sets the isProduction property value. The isProduction property
     * @param value Value to set for the isProduction property.
     */
    public void setIsProduction(@jakarta.annotation.Nullable final Boolean value) {
        this.isProduction = value;
    }
    /**
     * Sets the lifecycle property value. The lifecycle property
     * @param value Value to set for the lifecycle property.
     */
    public void setLifecycle(@jakarta.annotation.Nullable final String value) {
        this.lifecycle = value;
    }
    /**
     * Sets the name property value. The name property
     * @param value Value to set for the name property.
     */
    public void setName(@jakarta.annotation.Nullable final String value) {
        this.name = value;
    }
    /**
     * Sets the revision property value. The revision property
     * @param value Value to set for the revision property.
     */
    public void setRevision(@jakarta.annotation.Nullable final Integer value) {
        this.revision = value;
    }
    /**
     * Sets the runtimeRevision property value. The runtimeRevision property
     * @param value Value to set for the runtimeRevision property.
     */
    public void setRuntimeRevision(@jakarta.annotation.Nullable final Integer value) {
        this.runtimeRevision = value;
    }
    /**
     * Sets the slug property value. The slug property
     * @param value Value to set for the slug property.
     */
    public void setSlug(@jakarta.annotation.Nullable final String value) {
        this.slug = value;
    }
    /**
     * Sets the templateId property value. The templateId property
     * @param value Value to set for the templateId property.
     */
    public void setTemplateId(@jakarta.annotation.Nullable final UUID value) {
        this.templateId = value;
    }
    /**
     * Sets the templateVersion property value. The templateVersion property
     * @param value Value to set for the templateVersion property.
     */
    public void setTemplateVersion(@jakarta.annotation.Nullable final Integer value) {
        this.templateVersion = value;
    }
    /**
     * Composed type wrapper for classes {@link DeliveryEnvironmentResponseCurrentReleaseIdMember1}, {@link Uuid}
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public static class DeliveryEnvironmentResponseCurrentReleaseId implements ComposedTypeWrapper, Parsable {
        /**
         * Composed type representation for type {@link DeliveryEnvironmentResponseCurrentReleaseIdMember1}
         */
        private DeliveryEnvironmentResponseCurrentReleaseIdMember1 deliveryEnvironmentResponseCurrentReleaseIdMember1;
        /**
         * Composed type representation for type {@link Uuid}
         */
        private Uuid uuid;
        /**
         * Creates a new instance of the appropriate class based on discriminator value
         * @param parseNode The parse node to use to read the discriminator value and create the object
         * @return a {@link DeliveryEnvironmentResponseCurrentReleaseId}
         */
        @jakarta.annotation.Nonnull
        public static DeliveryEnvironmentResponseCurrentReleaseId createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
            Objects.requireNonNull(parseNode);
            final DeliveryEnvironmentResponseCurrentReleaseId result = new DeliveryEnvironmentResponseCurrentReleaseId();
            final ParseNode mappingValueNode = parseNode.getChildNode("");
            if (mappingValueNode != null) {
                final String mappingValue = mappingValueNode.getStringValue();
            }
            return result;
        }
        /**
         * Gets the DeliveryEnvironmentResponse_currentReleaseIdMember1 property value. Composed type representation for type {@link DeliveryEnvironmentResponseCurrentReleaseIdMember1}
         * @return a {@link DeliveryEnvironmentResponseCurrentReleaseIdMember1}
         */
        @jakarta.annotation.Nullable
        public DeliveryEnvironmentResponseCurrentReleaseIdMember1 getDeliveryEnvironmentResponseCurrentReleaseIdMember1() {
            return this.deliveryEnvironmentResponseCurrentReleaseIdMember1;
        }
        /**
         * The deserialization information for the current model
         * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
         */
        @jakarta.annotation.Nonnull
        public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
            if (this.getDeliveryEnvironmentResponseCurrentReleaseIdMember1() != null) {
                return this.getDeliveryEnvironmentResponseCurrentReleaseIdMember1().getFieldDeserializers();
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
            if (this.getDeliveryEnvironmentResponseCurrentReleaseIdMember1() != null) {
                writer.writeObjectValue(null, this.getDeliveryEnvironmentResponseCurrentReleaseIdMember1());
            } else if (this.getUuid() != null) {
                writer.writeObjectValue(null, this.getUuid());
            }
        }
        /**
         * Sets the DeliveryEnvironmentResponse_currentReleaseIdMember1 property value. Composed type representation for type {@link DeliveryEnvironmentResponseCurrentReleaseIdMember1}
         * @param value Value to set for the DeliveryEnvironmentResponse_currentReleaseIdMember1 property.
         */
        public void setDeliveryEnvironmentResponseCurrentReleaseIdMember1(@jakarta.annotation.Nullable final DeliveryEnvironmentResponseCurrentReleaseIdMember1 value) {
            this.deliveryEnvironmentResponseCurrentReleaseIdMember1 = value;
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
