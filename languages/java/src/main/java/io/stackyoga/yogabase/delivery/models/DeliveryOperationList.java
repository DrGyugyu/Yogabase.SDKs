package io.stackyoga.yogabase.delivery.models;

import com.microsoft.kiota.serialization.ComposedTypeWrapper;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DeliveryOperationList implements Parsable {
    /**
     * The items property
     */
    private java.util.List<DeliveryOperationEnvelope> items;
    /**
     * The nextCursor property
     */
    private DeliveryOperationListNextCursor nextCursor;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DeliveryOperationList}
     */
    @jakarta.annotation.Nonnull
    public static DeliveryOperationList createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DeliveryOperationList();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(2);
        deserializerMap.put("items", (n) -> { this.setItems(n.getCollectionOfObjectValues(DeliveryOperationEnvelope::createFromDiscriminatorValue)); });
        deserializerMap.put("nextCursor", (n) -> { this.setNextCursor(n.getObjectValue(DeliveryOperationListNextCursor::createFromDiscriminatorValue)); });
        return deserializerMap;
    }
    /**
     * Gets the items property value. The items property
     * @return a {@link java.util.List<DeliveryOperationEnvelope>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<DeliveryOperationEnvelope> getItems() {
        return this.items;
    }
    /**
     * Gets the nextCursor property value. The nextCursor property
     * @return a {@link DeliveryOperationListNextCursor}
     */
    @jakarta.annotation.Nullable
    public DeliveryOperationListNextCursor getNextCursor() {
        return this.nextCursor;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeCollectionOfObjectValues("items", this.getItems());
        writer.writeObjectValue("nextCursor", this.getNextCursor());
    }
    /**
     * Sets the items property value. The items property
     * @param value Value to set for the items property.
     */
    public void setItems(@jakarta.annotation.Nullable final java.util.List<DeliveryOperationEnvelope> value) {
        this.items = value;
    }
    /**
     * Sets the nextCursor property value. The nextCursor property
     * @param value Value to set for the nextCursor property.
     */
    public void setNextCursor(@jakarta.annotation.Nullable final DeliveryOperationListNextCursor value) {
        this.nextCursor = value;
    }
    /**
     * Composed type wrapper for classes {@link DeliveryOperationListNextCursorMember1}, {@link String}
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public static class DeliveryOperationListNextCursor implements ComposedTypeWrapper, Parsable {
        /**
         * Composed type representation for type {@link DeliveryOperationListNextCursorMember1}
         */
        private DeliveryOperationListNextCursorMember1 deliveryOperationListNextCursorMember1;
        /**
         * Composed type representation for type {@link String}
         */
        private String string;
        /**
         * Creates a new instance of the appropriate class based on discriminator value
         * @param parseNode The parse node to use to read the discriminator value and create the object
         * @return a {@link DeliveryOperationListNextCursor}
         */
        @jakarta.annotation.Nonnull
        public static DeliveryOperationListNextCursor createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
            Objects.requireNonNull(parseNode);
            final DeliveryOperationListNextCursor result = new DeliveryOperationListNextCursor();
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
         * Gets the DeliveryOperationList_nextCursorMember1 property value. Composed type representation for type {@link DeliveryOperationListNextCursorMember1}
         * @return a {@link DeliveryOperationListNextCursorMember1}
         */
        @jakarta.annotation.Nullable
        public DeliveryOperationListNextCursorMember1 getDeliveryOperationListNextCursorMember1() {
            return this.deliveryOperationListNextCursorMember1;
        }
        /**
         * The deserialization information for the current model
         * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
         */
        @jakarta.annotation.Nonnull
        public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
            if (this.getDeliveryOperationListNextCursorMember1() != null) {
                return this.getDeliveryOperationListNextCursorMember1().getFieldDeserializers();
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
            if (this.getDeliveryOperationListNextCursorMember1() != null) {
                writer.writeObjectValue(null, this.getDeliveryOperationListNextCursorMember1());
            } else if (this.getString() != null) {
                writer.writeStringValue(null, this.getString());
            }
        }
        /**
         * Sets the DeliveryOperationList_nextCursorMember1 property value. Composed type representation for type {@link DeliveryOperationListNextCursorMember1}
         * @param value Value to set for the DeliveryOperationList_nextCursorMember1 property.
         */
        public void setDeliveryOperationListNextCursorMember1(@jakarta.annotation.Nullable final DeliveryOperationListNextCursorMember1 value) {
            this.deliveryOperationListNextCursorMember1 = value;
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
