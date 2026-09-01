package io.stackyoga.yogabase.delivery.models;

import com.microsoft.kiota.serialization.ComposedTypeWrapper;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DeliveryReleaseList implements Parsable {
    /**
     * The items property
     */
    private java.util.List<DeliveryReleaseResponse> items;
    /**
     * The nextCursor property
     */
    private DeliveryReleaseListNextCursor nextCursor;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DeliveryReleaseList}
     */
    @jakarta.annotation.Nonnull
    public static DeliveryReleaseList createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DeliveryReleaseList();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(2);
        deserializerMap.put("items", (n) -> { this.setItems(n.getCollectionOfObjectValues(DeliveryReleaseResponse::createFromDiscriminatorValue)); });
        deserializerMap.put("nextCursor", (n) -> { this.setNextCursor(n.getObjectValue(DeliveryReleaseListNextCursor::createFromDiscriminatorValue)); });
        return deserializerMap;
    }
    /**
     * Gets the items property value. The items property
     * @return a {@link java.util.List<DeliveryReleaseResponse>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<DeliveryReleaseResponse> getItems() {
        return this.items;
    }
    /**
     * Gets the nextCursor property value. The nextCursor property
     * @return a {@link DeliveryReleaseListNextCursor}
     */
    @jakarta.annotation.Nullable
    public DeliveryReleaseListNextCursor getNextCursor() {
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
    public void setItems(@jakarta.annotation.Nullable final java.util.List<DeliveryReleaseResponse> value) {
        this.items = value;
    }
    /**
     * Sets the nextCursor property value. The nextCursor property
     * @param value Value to set for the nextCursor property.
     */
    public void setNextCursor(@jakarta.annotation.Nullable final DeliveryReleaseListNextCursor value) {
        this.nextCursor = value;
    }
    /**
     * Composed type wrapper for classes {@link DeliveryReleaseListNextCursorMember1}, {@link String}
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public static class DeliveryReleaseListNextCursor implements ComposedTypeWrapper, Parsable {
        /**
         * Composed type representation for type {@link DeliveryReleaseListNextCursorMember1}
         */
        private DeliveryReleaseListNextCursorMember1 deliveryReleaseListNextCursorMember1;
        /**
         * Composed type representation for type {@link String}
         */
        private String string;
        /**
         * Creates a new instance of the appropriate class based on discriminator value
         * @param parseNode The parse node to use to read the discriminator value and create the object
         * @return a {@link DeliveryReleaseListNextCursor}
         */
        @jakarta.annotation.Nonnull
        public static DeliveryReleaseListNextCursor createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
            Objects.requireNonNull(parseNode);
            final DeliveryReleaseListNextCursor result = new DeliveryReleaseListNextCursor();
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
         * Gets the DeliveryReleaseList_nextCursorMember1 property value. Composed type representation for type {@link DeliveryReleaseListNextCursorMember1}
         * @return a {@link DeliveryReleaseListNextCursorMember1}
         */
        @jakarta.annotation.Nullable
        public DeliveryReleaseListNextCursorMember1 getDeliveryReleaseListNextCursorMember1() {
            return this.deliveryReleaseListNextCursorMember1;
        }
        /**
         * The deserialization information for the current model
         * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
         */
        @jakarta.annotation.Nonnull
        public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
            if (this.getDeliveryReleaseListNextCursorMember1() != null) {
                return this.getDeliveryReleaseListNextCursorMember1().getFieldDeserializers();
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
            if (this.getDeliveryReleaseListNextCursorMember1() != null) {
                writer.writeObjectValue(null, this.getDeliveryReleaseListNextCursorMember1());
            } else if (this.getString() != null) {
                writer.writeStringValue(null, this.getString());
            }
        }
        /**
         * Sets the DeliveryReleaseList_nextCursorMember1 property value. Composed type representation for type {@link DeliveryReleaseListNextCursorMember1}
         * @param value Value to set for the DeliveryReleaseList_nextCursorMember1 property.
         */
        public void setDeliveryReleaseListNextCursorMember1(@jakarta.annotation.Nullable final DeliveryReleaseListNextCursorMember1 value) {
            this.deliveryReleaseListNextCursorMember1 = value;
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
