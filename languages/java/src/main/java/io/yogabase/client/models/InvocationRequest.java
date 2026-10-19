package io.yogabase.client.models;

import com.microsoft.kiota.serialization.ComposedTypeWrapper;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.time.OffsetDateTime;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class InvocationRequest implements Parsable {
    /**
     * The correlationId property
     */
    private String correlationId;
    /**
     * The deadline property
     */
    private OffsetDateTime deadline;
    /**
     * The idempotencyKey property
     */
    private String idempotencyKey;
    /**
     * The inlinePayloadBase64 property
     */
    private String inlinePayloadBase64;
    /**
     * The mode property
     */
    private Integer mode;
    /**
     * The objectReference property
     */
    private InvocationRequestObjectReference objectReference;
    /**
     * The payloadDigest property
     */
    private String payloadDigest;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link InvocationRequest}
     */
    @jakarta.annotation.Nonnull
    public static InvocationRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new InvocationRequest();
    }
    /**
     * Gets the correlationId property value. The correlationId property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getCorrelationId() {
        return this.correlationId;
    }
    /**
     * Gets the deadline property value. The deadline property
     * @return a {@link OffsetDateTime}
     */
    @jakarta.annotation.Nullable
    public OffsetDateTime getDeadline() {
        return this.deadline;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(7);
        deserializerMap.put("correlationId", (n) -> { this.setCorrelationId(n.getStringValue()); });
        deserializerMap.put("deadline", (n) -> { this.setDeadline(n.getOffsetDateTimeValue()); });
        deserializerMap.put("idempotencyKey", (n) -> { this.setIdempotencyKey(n.getStringValue()); });
        deserializerMap.put("inlinePayloadBase64", (n) -> { this.setInlinePayloadBase64(n.getStringValue()); });
        deserializerMap.put("mode", (n) -> { this.setMode(n.getIntegerValue()); });
        deserializerMap.put("objectReference", (n) -> { this.setObjectReference(n.getObjectValue(InvocationRequestObjectReference::createFromDiscriminatorValue)); });
        deserializerMap.put("payloadDigest", (n) -> { this.setPayloadDigest(n.getStringValue()); });
        return deserializerMap;
    }
    /**
     * Gets the idempotencyKey property value. The idempotencyKey property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getIdempotencyKey() {
        return this.idempotencyKey;
    }
    /**
     * Gets the inlinePayloadBase64 property value. The inlinePayloadBase64 property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getInlinePayloadBase64() {
        return this.inlinePayloadBase64;
    }
    /**
     * Gets the mode property value. The mode property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getMode() {
        return this.mode;
    }
    /**
     * Gets the objectReference property value. The objectReference property
     * @return a {@link InvocationRequestObjectReference}
     */
    @jakarta.annotation.Nullable
    public InvocationRequestObjectReference getObjectReference() {
        return this.objectReference;
    }
    /**
     * Gets the payloadDigest property value. The payloadDigest property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getPayloadDigest() {
        return this.payloadDigest;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeStringValue("correlationId", this.getCorrelationId());
        writer.writeOffsetDateTimeValue("deadline", this.getDeadline());
        writer.writeStringValue("idempotencyKey", this.getIdempotencyKey());
        writer.writeStringValue("inlinePayloadBase64", this.getInlinePayloadBase64());
        writer.writeIntegerValue("mode", this.getMode());
        writer.writeObjectValue("objectReference", this.getObjectReference());
        writer.writeStringValue("payloadDigest", this.getPayloadDigest());
    }
    /**
     * Sets the correlationId property value. The correlationId property
     * @param value Value to set for the correlationId property.
     */
    public void setCorrelationId(@jakarta.annotation.Nullable final String value) {
        this.correlationId = value;
    }
    /**
     * Sets the deadline property value. The deadline property
     * @param value Value to set for the deadline property.
     */
    public void setDeadline(@jakarta.annotation.Nullable final OffsetDateTime value) {
        this.deadline = value;
    }
    /**
     * Sets the idempotencyKey property value. The idempotencyKey property
     * @param value Value to set for the idempotencyKey property.
     */
    public void setIdempotencyKey(@jakarta.annotation.Nullable final String value) {
        this.idempotencyKey = value;
    }
    /**
     * Sets the inlinePayloadBase64 property value. The inlinePayloadBase64 property
     * @param value Value to set for the inlinePayloadBase64 property.
     */
    public void setInlinePayloadBase64(@jakarta.annotation.Nullable final String value) {
        this.inlinePayloadBase64 = value;
    }
    /**
     * Sets the mode property value. The mode property
     * @param value Value to set for the mode property.
     */
    public void setMode(@jakarta.annotation.Nullable final Integer value) {
        this.mode = value;
    }
    /**
     * Sets the objectReference property value. The objectReference property
     * @param value Value to set for the objectReference property.
     */
    public void setObjectReference(@jakarta.annotation.Nullable final InvocationRequestObjectReference value) {
        this.objectReference = value;
    }
    /**
     * Sets the payloadDigest property value. The payloadDigest property
     * @param value Value to set for the payloadDigest property.
     */
    public void setPayloadDigest(@jakarta.annotation.Nullable final String value) {
        this.payloadDigest = value;
    }
    /**
     * Composed type wrapper for classes {@link InvocationRequestObjectReferenceMember1}, {@link ObjectReference}
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public static class InvocationRequestObjectReference implements ComposedTypeWrapper, Parsable {
        /**
         * Composed type representation for type {@link InvocationRequestObjectReferenceMember1}
         */
        private InvocationRequestObjectReferenceMember1 invocationRequestObjectReferenceMember1;
        /**
         * Composed type representation for type {@link ObjectReference}
         */
        private ObjectReference objectReference;
        /**
         * Creates a new instance of the appropriate class based on discriminator value
         * @param parseNode The parse node to use to read the discriminator value and create the object
         * @return a {@link InvocationRequestObjectReference}
         */
        @jakarta.annotation.Nonnull
        public static InvocationRequestObjectReference createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
            Objects.requireNonNull(parseNode);
            final InvocationRequestObjectReference result = new InvocationRequestObjectReference();
            final ParseNode mappingValueNode = parseNode.getChildNode("");
            if (mappingValueNode != null) {
                final String mappingValue = mappingValueNode.getStringValue();
            }
            return result;
        }
        /**
         * The deserialization information for the current model
         * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
         */
        @jakarta.annotation.Nonnull
        public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
            if (this.getInvocationRequestObjectReferenceMember1() != null) {
                return this.getInvocationRequestObjectReferenceMember1().getFieldDeserializers();
            } else if (this.getObjectReference() != null) {
                return this.getObjectReference().getFieldDeserializers();
            }
            return new HashMap<String, java.util.function.Consumer<ParseNode>>();
        }
        /**
         * Gets the InvocationRequest_objectReferenceMember1 property value. Composed type representation for type {@link InvocationRequestObjectReferenceMember1}
         * @return a {@link InvocationRequestObjectReferenceMember1}
         */
        @jakarta.annotation.Nullable
        public InvocationRequestObjectReferenceMember1 getInvocationRequestObjectReferenceMember1() {
            return this.invocationRequestObjectReferenceMember1;
        }
        /**
         * Gets the ObjectReference property value. Composed type representation for type {@link ObjectReference}
         * @return a {@link ObjectReference}
         */
        @jakarta.annotation.Nullable
        public ObjectReference getObjectReference() {
            return this.objectReference;
        }
        /**
         * Serializes information the current object
         * @param writer Serialization writer to use to serialize this model
         */
        public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
            Objects.requireNonNull(writer);
            if (this.getInvocationRequestObjectReferenceMember1() != null) {
                writer.writeObjectValue(null, this.getInvocationRequestObjectReferenceMember1());
            } else if (this.getObjectReference() != null) {
                writer.writeObjectValue(null, this.getObjectReference());
            }
        }
        /**
         * Sets the InvocationRequest_objectReferenceMember1 property value. Composed type representation for type {@link InvocationRequestObjectReferenceMember1}
         * @param value Value to set for the InvocationRequest_objectReferenceMember1 property.
         */
        public void setInvocationRequestObjectReferenceMember1(@jakarta.annotation.Nullable final InvocationRequestObjectReferenceMember1 value) {
            this.invocationRequestObjectReferenceMember1 = value;
        }
        /**
         * Sets the ObjectReference property value. Composed type representation for type {@link ObjectReference}
         * @param value Value to set for the ObjectReference property.
         */
        public void setObjectReference(@jakarta.annotation.Nullable final ObjectReference value) {
            this.objectReference = value;
        }
    }
}
