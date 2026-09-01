package io.stackyoga.yogabase.delivery.models;

import com.microsoft.kiota.serialization.ComposedTypeWrapper;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DeliveryEnvironmentList implements Parsable {
    /**
     * The items property
     */
    private java.util.List<DeliveryEnvironmentResponse> items;
    /**
     * The nextCursor property
     */
    private DeliveryEnvironmentListNextCursor nextCursor;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DeliveryEnvironmentList}
     */
    @jakarta.annotation.Nonnull
    public static DeliveryEnvironmentList createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DeliveryEnvironmentList();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(2);
        deserializerMap.put("items", (n) -> { this.setItems(n.getCollectionOfObjectValues(DeliveryEnvironmentResponse::createFromDiscriminatorValue)); });
        deserializerMap.put("nextCursor", (n) -> { this.setNextCursor(n.getObjectValue(DeliveryEnvironmentListNextCursor::createFromDiscriminatorValue)); });
        return deserializerMap;
    }
    /**
     * Gets the items property value. The items property
     * @return a {@link java.util.List<DeliveryEnvironmentResponse>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<DeliveryEnvironmentResponse> getItems() {
        return this.items;
    }
    /**
     * Gets the nextCursor property value. The nextCursor property
     * @return a {@link DeliveryEnvironmentListNextCursor}
     */
    @jakarta.annotation.Nullable
    public DeliveryEnvironmentListNextCursor getNextCursor() {
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
    public void setItems(@jakarta.annotation.Nullable final java.util.List<DeliveryEnvironmentResponse> value) {
        this.items = value;
    }
    /**
     * Sets the nextCursor property value. The nextCursor property
     * @param value Value to set for the nextCursor property.
     */
    public void setNextCursor(@jakarta.annotation.Nullable final DeliveryEnvironmentListNextCursor value) {
        this.nextCursor = value;
    }
    /**
     * Composed type wrapper for classes {@link DeliveryEnvironmentListNextCursorMember1}, {@link String}
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public static class DeliveryEnvironmentListNextCursor implements ComposedTypeWrapper, Parsable {
        /**
         * Composed type representation for type {@link DeliveryEnvironmentListNextCursorMember1}
         */
        private DeliveryEnvironmentListNextCursorMember1 deliveryEnvironmentListNextCursorMember1;
        /**
         * Composed type representation for type {@link String}
         */
        private String string;
        /**
         * Creates a new instance of the appropriate class based on discriminator value
         * @param parseNode The parse node to use to read the discriminator value and create the object
         * @return a {@link DeliveryEnvironmentListNextCursor}
         */
        @jakarta.annotation.Nonnull
        public static DeliveryEnvironmentListNextCursor createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
            Objects.requireNonNull(parseNode);
            final DeliveryEnvironmentListNextCursor result = new DeliveryEnvironmentListNextCursor();
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
         * Gets the DeliveryEnvironmentList_nextCursorMember1 property value. Composed type representation for type {@link DeliveryEnvironmentListNextCursorMember1}
         * @return a {@link DeliveryEnvironmentListNextCursorMember1}
         */
        @jakarta.annotation.Nullable
        public DeliveryEnvironmentListNextCursorMember1 getDeliveryEnvironmentListNextCursorMember1() {
            return this.deliveryEnvironmentListNextCursorMember1;
        }
        /**
         * The deserialization information for the current model
         * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
         */
        @jakarta.annotation.Nonnull
        public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
            if (this.getDeliveryEnvironmentListNextCursorMember1() != null) {
                return this.getDeliveryEnvironmentListNextCursorMember1().getFieldDeserializers();
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
            if (this.getDeliveryEnvironmentListNextCursorMember1() != null) {
                writer.writeObjectValue(null, this.getDeliveryEnvironmentListNextCursorMember1());
            } else if (this.getString() != null) {
                writer.writeStringValue(null, this.getString());
            }
        }
        /**
         * Sets the DeliveryEnvironmentList_nextCursorMember1 property value. Composed type representation for type {@link DeliveryEnvironmentListNextCursorMember1}
         * @param value Value to set for the DeliveryEnvironmentList_nextCursorMember1 property.
         */
        public void setDeliveryEnvironmentListNextCursorMember1(@jakarta.annotation.Nullable final DeliveryEnvironmentListNextCursorMember1 value) {
            this.deliveryEnvironmentListNextCursorMember1 = value;
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
