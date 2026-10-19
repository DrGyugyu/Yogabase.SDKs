package io.yogabase.client.delivery.models;

import com.microsoft.kiota.serialization.ComposedTypeWrapper;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DeliveryTemplateList implements Parsable {
    /**
     * The items property
     */
    private java.util.List<DeliveryTemplateResponse> items;
    /**
     * The nextCursor property
     */
    private DeliveryTemplateListNextCursor nextCursor;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DeliveryTemplateList}
     */
    @jakarta.annotation.Nonnull
    public static DeliveryTemplateList createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DeliveryTemplateList();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(2);
        deserializerMap.put("items", (n) -> { this.setItems(n.getCollectionOfObjectValues(DeliveryTemplateResponse::createFromDiscriminatorValue)); });
        deserializerMap.put("nextCursor", (n) -> { this.setNextCursor(n.getObjectValue(DeliveryTemplateListNextCursor::createFromDiscriminatorValue)); });
        return deserializerMap;
    }
    /**
     * Gets the items property value. The items property
     * @return a {@link java.util.List<DeliveryTemplateResponse>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<DeliveryTemplateResponse> getItems() {
        return this.items;
    }
    /**
     * Gets the nextCursor property value. The nextCursor property
     * @return a {@link DeliveryTemplateListNextCursor}
     */
    @jakarta.annotation.Nullable
    public DeliveryTemplateListNextCursor getNextCursor() {
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
    public void setItems(@jakarta.annotation.Nullable final java.util.List<DeliveryTemplateResponse> value) {
        this.items = value;
    }
    /**
     * Sets the nextCursor property value. The nextCursor property
     * @param value Value to set for the nextCursor property.
     */
    public void setNextCursor(@jakarta.annotation.Nullable final DeliveryTemplateListNextCursor value) {
        this.nextCursor = value;
    }
    /**
     * Composed type wrapper for classes {@link DeliveryTemplateListNextCursorMember1}, {@link String}
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public static class DeliveryTemplateListNextCursor implements ComposedTypeWrapper, Parsable {
        /**
         * Composed type representation for type {@link DeliveryTemplateListNextCursorMember1}
         */
        private DeliveryTemplateListNextCursorMember1 deliveryTemplateListNextCursorMember1;
        /**
         * Composed type representation for type {@link String}
         */
        private String string;
        /**
         * Creates a new instance of the appropriate class based on discriminator value
         * @param parseNode The parse node to use to read the discriminator value and create the object
         * @return a {@link DeliveryTemplateListNextCursor}
         */
        @jakarta.annotation.Nonnull
        public static DeliveryTemplateListNextCursor createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
            Objects.requireNonNull(parseNode);
            final DeliveryTemplateListNextCursor result = new DeliveryTemplateListNextCursor();
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
         * Gets the DeliveryTemplateList_nextCursorMember1 property value. Composed type representation for type {@link DeliveryTemplateListNextCursorMember1}
         * @return a {@link DeliveryTemplateListNextCursorMember1}
         */
        @jakarta.annotation.Nullable
        public DeliveryTemplateListNextCursorMember1 getDeliveryTemplateListNextCursorMember1() {
            return this.deliveryTemplateListNextCursorMember1;
        }
        /**
         * The deserialization information for the current model
         * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
         */
        @jakarta.annotation.Nonnull
        public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
            if (this.getDeliveryTemplateListNextCursorMember1() != null) {
                return this.getDeliveryTemplateListNextCursorMember1().getFieldDeserializers();
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
            if (this.getDeliveryTemplateListNextCursorMember1() != null) {
                writer.writeObjectValue(null, this.getDeliveryTemplateListNextCursorMember1());
            } else if (this.getString() != null) {
                writer.writeStringValue(null, this.getString());
            }
        }
        /**
         * Sets the DeliveryTemplateList_nextCursorMember1 property value. Composed type representation for type {@link DeliveryTemplateListNextCursorMember1}
         * @param value Value to set for the DeliveryTemplateList_nextCursorMember1 property.
         */
        public void setDeliveryTemplateListNextCursorMember1(@jakarta.annotation.Nullable final DeliveryTemplateListNextCursorMember1 value) {
            this.deliveryTemplateListNextCursorMember1 = value;
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
