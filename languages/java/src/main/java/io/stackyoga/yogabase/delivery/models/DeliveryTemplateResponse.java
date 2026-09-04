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
public class DeliveryTemplateResponse implements Parsable {
    /**
     * The hostedServicePolicyDigest property
     */
    private DeliveryTemplateResponseHostedServicePolicyDigest hostedServicePolicyDigest;
    /**
     * The id property
     */
    private UUID id;
    /**
     * The name property
     */
    private String name;
    /**
     * The revision property
     */
    private Integer revision;
    /**
     * The state property
     */
    private String state;
    /**
     * The targetId property
     */
    private UUID targetId;
    /**
     * The templateDigest property
     */
    private String templateDigest;
    /**
     * The version property
     */
    private Integer version;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DeliveryTemplateResponse}
     */
    @jakarta.annotation.Nonnull
    public static DeliveryTemplateResponse createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DeliveryTemplateResponse();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(8);
        deserializerMap.put("hostedServicePolicyDigest", (n) -> { this.setHostedServicePolicyDigest(n.getObjectValue(DeliveryTemplateResponseHostedServicePolicyDigest::createFromDiscriminatorValue)); });
        deserializerMap.put("id", (n) -> { this.setId(n.getUUIDValue()); });
        deserializerMap.put("name", (n) -> { this.setName(n.getStringValue()); });
        deserializerMap.put("revision", (n) -> { this.setRevision(n.getIntegerValue()); });
        deserializerMap.put("state", (n) -> { this.setState(n.getStringValue()); });
        deserializerMap.put("targetId", (n) -> { this.setTargetId(n.getUUIDValue()); });
        deserializerMap.put("templateDigest", (n) -> { this.setTemplateDigest(n.getStringValue()); });
        deserializerMap.put("version", (n) -> { this.setVersion(n.getIntegerValue()); });
        return deserializerMap;
    }
    /**
     * Gets the hostedServicePolicyDigest property value. The hostedServicePolicyDigest property
     * @return a {@link DeliveryTemplateResponseHostedServicePolicyDigest}
     */
    @jakarta.annotation.Nullable
    public DeliveryTemplateResponseHostedServicePolicyDigest getHostedServicePolicyDigest() {
        return this.hostedServicePolicyDigest;
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
     * Gets the state property value. The state property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getState() {
        return this.state;
    }
    /**
     * Gets the targetId property value. The targetId property
     * @return a {@link UUID}
     */
    @jakarta.annotation.Nullable
    public UUID getTargetId() {
        return this.targetId;
    }
    /**
     * Gets the templateDigest property value. The templateDigest property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getTemplateDigest() {
        return this.templateDigest;
    }
    /**
     * Gets the version property value. The version property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getVersion() {
        return this.version;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeObjectValue("hostedServicePolicyDigest", this.getHostedServicePolicyDigest());
        writer.writeUUIDValue("id", this.getId());
        writer.writeStringValue("name", this.getName());
        writer.writeIntegerValue("revision", this.getRevision());
        writer.writeStringValue("state", this.getState());
        writer.writeUUIDValue("targetId", this.getTargetId());
        writer.writeStringValue("templateDigest", this.getTemplateDigest());
        writer.writeIntegerValue("version", this.getVersion());
    }
    /**
     * Sets the hostedServicePolicyDigest property value. The hostedServicePolicyDigest property
     * @param value Value to set for the hostedServicePolicyDigest property.
     */
    public void setHostedServicePolicyDigest(@jakarta.annotation.Nullable final DeliveryTemplateResponseHostedServicePolicyDigest value) {
        this.hostedServicePolicyDigest = value;
    }
    /**
     * Sets the id property value. The id property
     * @param value Value to set for the id property.
     */
    public void setId(@jakarta.annotation.Nullable final UUID value) {
        this.id = value;
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
     * Sets the state property value. The state property
     * @param value Value to set for the state property.
     */
    public void setState(@jakarta.annotation.Nullable final String value) {
        this.state = value;
    }
    /**
     * Sets the targetId property value. The targetId property
     * @param value Value to set for the targetId property.
     */
    public void setTargetId(@jakarta.annotation.Nullable final UUID value) {
        this.targetId = value;
    }
    /**
     * Sets the templateDigest property value. The templateDigest property
     * @param value Value to set for the templateDigest property.
     */
    public void setTemplateDigest(@jakarta.annotation.Nullable final String value) {
        this.templateDigest = value;
    }
    /**
     * Sets the version property value. The version property
     * @param value Value to set for the version property.
     */
    public void setVersion(@jakarta.annotation.Nullable final Integer value) {
        this.version = value;
    }
    /**
     * Composed type wrapper for classes {@link DeliveryTemplateResponseHostedServicePolicyDigestMember1}, {@link Sha256Digest}
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public static class DeliveryTemplateResponseHostedServicePolicyDigest implements ComposedTypeWrapper, Parsable {
        /**
         * Composed type representation for type {@link DeliveryTemplateResponseHostedServicePolicyDigestMember1}
         */
        private DeliveryTemplateResponseHostedServicePolicyDigestMember1 deliveryTemplateResponseHostedServicePolicyDigestMember1;
        /**
         * Composed type representation for type {@link Sha256Digest}
         */
        private Sha256Digest sha256Digest;
        /**
         * Creates a new instance of the appropriate class based on discriminator value
         * @param parseNode The parse node to use to read the discriminator value and create the object
         * @return a {@link DeliveryTemplateResponseHostedServicePolicyDigest}
         */
        @jakarta.annotation.Nonnull
        public static DeliveryTemplateResponseHostedServicePolicyDigest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
            Objects.requireNonNull(parseNode);
            final DeliveryTemplateResponseHostedServicePolicyDigest result = new DeliveryTemplateResponseHostedServicePolicyDigest();
            final ParseNode mappingValueNode = parseNode.getChildNode("");
            if (mappingValueNode != null) {
                final String mappingValue = mappingValueNode.getStringValue();
            }
            return result;
        }
        /**
         * Gets the DeliveryTemplateResponse_hostedServicePolicyDigestMember1 property value. Composed type representation for type {@link DeliveryTemplateResponseHostedServicePolicyDigestMember1}
         * @return a {@link DeliveryTemplateResponseHostedServicePolicyDigestMember1}
         */
        @jakarta.annotation.Nullable
        public DeliveryTemplateResponseHostedServicePolicyDigestMember1 getDeliveryTemplateResponseHostedServicePolicyDigestMember1() {
            return this.deliveryTemplateResponseHostedServicePolicyDigestMember1;
        }
        /**
         * The deserialization information for the current model
         * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
         */
        @jakarta.annotation.Nonnull
        public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
            if (this.getDeliveryTemplateResponseHostedServicePolicyDigestMember1() != null) {
                return this.getDeliveryTemplateResponseHostedServicePolicyDigestMember1().getFieldDeserializers();
            } else if (this.getSha256Digest() != null) {
                return this.getSha256Digest().getFieldDeserializers();
            }
            return new HashMap<String, java.util.function.Consumer<ParseNode>>();
        }
        /**
         * Gets the Sha256Digest property value. Composed type representation for type {@link Sha256Digest}
         * @return a {@link Sha256Digest}
         */
        @jakarta.annotation.Nullable
        public Sha256Digest getSha256Digest() {
            return this.sha256Digest;
        }
        /**
         * Serializes information the current object
         * @param writer Serialization writer to use to serialize this model
         */
        public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
            Objects.requireNonNull(writer);
            if (this.getDeliveryTemplateResponseHostedServicePolicyDigestMember1() != null) {
                writer.writeObjectValue(null, this.getDeliveryTemplateResponseHostedServicePolicyDigestMember1());
            } else if (this.getSha256Digest() != null) {
                writer.writeObjectValue(null, this.getSha256Digest());
            }
        }
        /**
         * Sets the DeliveryTemplateResponse_hostedServicePolicyDigestMember1 property value. Composed type representation for type {@link DeliveryTemplateResponseHostedServicePolicyDigestMember1}
         * @param value Value to set for the DeliveryTemplateResponse_hostedServicePolicyDigestMember1 property.
         */
        public void setDeliveryTemplateResponseHostedServicePolicyDigestMember1(@jakarta.annotation.Nullable final DeliveryTemplateResponseHostedServicePolicyDigestMember1 value) {
            this.deliveryTemplateResponseHostedServicePolicyDigestMember1 = value;
        }
        /**
         * Sets the Sha256Digest property value. Composed type representation for type {@link Sha256Digest}
         * @param value Value to set for the Sha256Digest property.
         */
        public void setSha256Digest(@jakarta.annotation.Nullable final Sha256Digest value) {
            this.sha256Digest = value;
        }
    }
}
