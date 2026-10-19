package io.yogabase.client.delivery.models;

import com.microsoft.kiota.serialization.ComposedTypeWrapper;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.time.OffsetDateTime;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DeliveryOperationEnvelope implements Parsable {
    /**
     * The kind property
     */
    private String kind;
    /**
     * The operationId property
     */
    private UUID operationId;
    /**
     * The resourceId property
     */
    private DeliveryOperationEnvelopeResourceId resourceId;
    /**
     * The resourceType property
     */
    private DeliveryOperationEnvelopeResourceType resourceType;
    /**
     * The revision property
     */
    private Integer revision;
    /**
     * The state property
     */
    private String state;
    /**
     * The updatedAt property
     */
    private OffsetDateTime updatedAt;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DeliveryOperationEnvelope}
     */
    @jakarta.annotation.Nonnull
    public static DeliveryOperationEnvelope createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DeliveryOperationEnvelope();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(7);
        deserializerMap.put("kind", (n) -> { this.setKind(n.getStringValue()); });
        deserializerMap.put("operationId", (n) -> { this.setOperationId(n.getUUIDValue()); });
        deserializerMap.put("resourceId", (n) -> { this.setResourceId(n.getObjectValue(DeliveryOperationEnvelopeResourceId::createFromDiscriminatorValue)); });
        deserializerMap.put("resourceType", (n) -> { this.setResourceType(n.getObjectValue(DeliveryOperationEnvelopeResourceType::createFromDiscriminatorValue)); });
        deserializerMap.put("revision", (n) -> { this.setRevision(n.getIntegerValue()); });
        deserializerMap.put("state", (n) -> { this.setState(n.getStringValue()); });
        deserializerMap.put("updatedAt", (n) -> { this.setUpdatedAt(n.getOffsetDateTimeValue()); });
        return deserializerMap;
    }
    /**
     * Gets the kind property value. The kind property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getKind() {
        return this.kind;
    }
    /**
     * Gets the operationId property value. The operationId property
     * @return a {@link UUID}
     */
    @jakarta.annotation.Nullable
    public UUID getOperationId() {
        return this.operationId;
    }
    /**
     * Gets the resourceId property value. The resourceId property
     * @return a {@link DeliveryOperationEnvelopeResourceId}
     */
    @jakarta.annotation.Nullable
    public DeliveryOperationEnvelopeResourceId getResourceId() {
        return this.resourceId;
    }
    /**
     * Gets the resourceType property value. The resourceType property
     * @return a {@link DeliveryOperationEnvelopeResourceType}
     */
    @jakarta.annotation.Nullable
    public DeliveryOperationEnvelopeResourceType getResourceType() {
        return this.resourceType;
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
     * Gets the updatedAt property value. The updatedAt property
     * @return a {@link OffsetDateTime}
     */
    @jakarta.annotation.Nullable
    public OffsetDateTime getUpdatedAt() {
        return this.updatedAt;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeStringValue("kind", this.getKind());
        writer.writeUUIDValue("operationId", this.getOperationId());
        writer.writeObjectValue("resourceId", this.getResourceId());
        writer.writeObjectValue("resourceType", this.getResourceType());
        writer.writeIntegerValue("revision", this.getRevision());
        writer.writeStringValue("state", this.getState());
        writer.writeOffsetDateTimeValue("updatedAt", this.getUpdatedAt());
    }
    /**
     * Sets the kind property value. The kind property
     * @param value Value to set for the kind property.
     */
    public void setKind(@jakarta.annotation.Nullable final String value) {
        this.kind = value;
    }
    /**
     * Sets the operationId property value. The operationId property
     * @param value Value to set for the operationId property.
     */
    public void setOperationId(@jakarta.annotation.Nullable final UUID value) {
        this.operationId = value;
    }
    /**
     * Sets the resourceId property value. The resourceId property
     * @param value Value to set for the resourceId property.
     */
    public void setResourceId(@jakarta.annotation.Nullable final DeliveryOperationEnvelopeResourceId value) {
        this.resourceId = value;
    }
    /**
     * Sets the resourceType property value. The resourceType property
     * @param value Value to set for the resourceType property.
     */
    public void setResourceType(@jakarta.annotation.Nullable final DeliveryOperationEnvelopeResourceType value) {
        this.resourceType = value;
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
     * Sets the updatedAt property value. The updatedAt property
     * @param value Value to set for the updatedAt property.
     */
    public void setUpdatedAt(@jakarta.annotation.Nullable final OffsetDateTime value) {
        this.updatedAt = value;
    }
    /**
     * Composed type wrapper for classes {@link DeliveryOperationEnvelopeResourceIdMember1}, {@link Uuid}
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public static class DeliveryOperationEnvelopeResourceId implements ComposedTypeWrapper, Parsable {
        /**
         * Composed type representation for type {@link DeliveryOperationEnvelopeResourceIdMember1}
         */
        private DeliveryOperationEnvelopeResourceIdMember1 deliveryOperationEnvelopeResourceIdMember1;
        /**
         * Composed type representation for type {@link Uuid}
         */
        private Uuid uuid;
        /**
         * Creates a new instance of the appropriate class based on discriminator value
         * @param parseNode The parse node to use to read the discriminator value and create the object
         * @return a {@link DeliveryOperationEnvelopeResourceId}
         */
        @jakarta.annotation.Nonnull
        public static DeliveryOperationEnvelopeResourceId createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
            Objects.requireNonNull(parseNode);
            final DeliveryOperationEnvelopeResourceId result = new DeliveryOperationEnvelopeResourceId();
            final ParseNode mappingValueNode = parseNode.getChildNode("");
            if (mappingValueNode != null) {
                final String mappingValue = mappingValueNode.getStringValue();
            }
            return result;
        }
        /**
         * Gets the DeliveryOperationEnvelope_resourceIdMember1 property value. Composed type representation for type {@link DeliveryOperationEnvelopeResourceIdMember1}
         * @return a {@link DeliveryOperationEnvelopeResourceIdMember1}
         */
        @jakarta.annotation.Nullable
        public DeliveryOperationEnvelopeResourceIdMember1 getDeliveryOperationEnvelopeResourceIdMember1() {
            return this.deliveryOperationEnvelopeResourceIdMember1;
        }
        /**
         * The deserialization information for the current model
         * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
         */
        @jakarta.annotation.Nonnull
        public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
            if (this.getDeliveryOperationEnvelopeResourceIdMember1() != null) {
                return this.getDeliveryOperationEnvelopeResourceIdMember1().getFieldDeserializers();
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
            if (this.getDeliveryOperationEnvelopeResourceIdMember1() != null) {
                writer.writeObjectValue(null, this.getDeliveryOperationEnvelopeResourceIdMember1());
            } else if (this.getUuid() != null) {
                writer.writeObjectValue(null, this.getUuid());
            }
        }
        /**
         * Sets the DeliveryOperationEnvelope_resourceIdMember1 property value. Composed type representation for type {@link DeliveryOperationEnvelopeResourceIdMember1}
         * @param value Value to set for the DeliveryOperationEnvelope_resourceIdMember1 property.
         */
        public void setDeliveryOperationEnvelopeResourceIdMember1(@jakarta.annotation.Nullable final DeliveryOperationEnvelopeResourceIdMember1 value) {
            this.deliveryOperationEnvelopeResourceIdMember1 = value;
        }
        /**
         * Sets the Uuid property value. Composed type representation for type {@link Uuid}
         * @param value Value to set for the Uuid property.
         */
        public void setUuid(@jakarta.annotation.Nullable final Uuid value) {
            this.uuid = value;
        }
    }
    /**
     * Composed type wrapper for classes {@link DeliveryOperationEnvelopeResourceTypeMember1}, {@link String}
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public static class DeliveryOperationEnvelopeResourceType implements ComposedTypeWrapper, Parsable {
        /**
         * Composed type representation for type {@link DeliveryOperationEnvelopeResourceTypeMember1}
         */
        private DeliveryOperationEnvelopeResourceTypeMember1 deliveryOperationEnvelopeResourceTypeMember1;
        /**
         * Composed type representation for type {@link String}
         */
        private String string;
        /**
         * Creates a new instance of the appropriate class based on discriminator value
         * @param parseNode The parse node to use to read the discriminator value and create the object
         * @return a {@link DeliveryOperationEnvelopeResourceType}
         */
        @jakarta.annotation.Nonnull
        public static DeliveryOperationEnvelopeResourceType createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
            Objects.requireNonNull(parseNode);
            final DeliveryOperationEnvelopeResourceType result = new DeliveryOperationEnvelopeResourceType();
            final ParseNode mappingValueNode = parseNode.getChildNode("");
            if (mappingValueNode != null) {
                final String mappingValue = mappingValueNode.getStringValue();
            }
            if (parseNode.getStringValue() != null) {
                result.setString(parseNode.getStringValue());
            }
            return result;
        }
        /**
         * Gets the DeliveryOperationEnvelope_resourceTypeMember1 property value. Composed type representation for type {@link DeliveryOperationEnvelopeResourceTypeMember1}
         * @return a {@link DeliveryOperationEnvelopeResourceTypeMember1}
         */
        @jakarta.annotation.Nullable
        public DeliveryOperationEnvelopeResourceTypeMember1 getDeliveryOperationEnvelopeResourceTypeMember1() {
            return this.deliveryOperationEnvelopeResourceTypeMember1;
        }
        /**
         * The deserialization information for the current model
         * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
         */
        @jakarta.annotation.Nonnull
        public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
            if (this.getDeliveryOperationEnvelopeResourceTypeMember1() != null) {
                return this.getDeliveryOperationEnvelopeResourceTypeMember1().getFieldDeserializers();
            }
            return new HashMap<String, java.util.function.Consumer<ParseNode>>();
        }
        /**
         * Gets the string property value. Composed type representation for type {@link String}
         * @return a {@link String}
         */
        @jakarta.annotation.Nullable
        public String getString() {
            return this.string;
        }
        /**
         * Serializes information the current object
         * @param writer Serialization writer to use to serialize this model
         */
        public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
            Objects.requireNonNull(writer);
            if (this.getDeliveryOperationEnvelopeResourceTypeMember1() != null) {
                writer.writeObjectValue(null, this.getDeliveryOperationEnvelopeResourceTypeMember1());
            } else if (this.getString() != null) {
                writer.writeStringValue(null, this.getString());
            }
        }
        /**
         * Sets the DeliveryOperationEnvelope_resourceTypeMember1 property value. Composed type representation for type {@link DeliveryOperationEnvelopeResourceTypeMember1}
         * @param value Value to set for the DeliveryOperationEnvelope_resourceTypeMember1 property.
         */
        public void setDeliveryOperationEnvelopeResourceTypeMember1(@jakarta.annotation.Nullable final DeliveryOperationEnvelopeResourceTypeMember1 value) {
            this.deliveryOperationEnvelopeResourceTypeMember1 = value;
        }
        /**
         * Sets the string property value. Composed type representation for type {@link String}
         * @param value Value to set for the string property.
         */
        public void setString(@jakarta.annotation.Nullable final String value) {
            this.string = value;
        }
    }
}
