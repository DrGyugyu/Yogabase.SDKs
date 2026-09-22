package io.stackyoga.yogabase.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class Capability implements Parsable {
    /**
     * The available property
     */
    private Boolean available;
    /**
     * realtime-presence appears only when requested; it is available exactly when realtime-subscribe is and the runtime&apos;s realtime projection enables presence.
     */
    private CapabilityId id;
    /**
     * The unavailableReason property
     */
    private String unavailableReason;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link Capability}
     */
    @jakarta.annotation.Nonnull
    public static Capability createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new Capability();
    }
    /**
     * Gets the available property value. The available property
     * @return a {@link Boolean}
     */
    @jakarta.annotation.Nullable
    public Boolean getAvailable() {
        return this.available;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(3);
        deserializerMap.put("available", (n) -> { this.setAvailable(n.getBooleanValue()); });
        deserializerMap.put("id", (n) -> { this.setId(n.getEnumValue(CapabilityId::forValue)); });
        deserializerMap.put("unavailableReason", (n) -> { this.setUnavailableReason(n.getStringValue()); });
        return deserializerMap;
    }
    /**
     * Gets the id property value. realtime-presence appears only when requested; it is available exactly when realtime-subscribe is and the runtime&apos;s realtime projection enables presence.
     * @return a {@link CapabilityId}
     */
    @jakarta.annotation.Nullable
    public CapabilityId getId() {
        return this.id;
    }
    /**
     * Gets the unavailableReason property value. The unavailableReason property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getUnavailableReason() {
        return this.unavailableReason;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeBooleanValue("available", this.getAvailable());
        writer.writeEnumValue("id", this.getId());
        writer.writeStringValue("unavailableReason", this.getUnavailableReason());
    }
    /**
     * Sets the available property value. The available property
     * @param value Value to set for the available property.
     */
    public void setAvailable(@jakarta.annotation.Nullable final Boolean value) {
        this.available = value;
    }
    /**
     * Sets the id property value. realtime-presence appears only when requested; it is available exactly when realtime-subscribe is and the runtime&apos;s realtime projection enables presence.
     * @param value Value to set for the id property.
     */
    public void setId(@jakarta.annotation.Nullable final CapabilityId value) {
        this.id = value;
    }
    /**
     * Sets the unavailableReason property value. The unavailableReason property
     * @param value Value to set for the unavailableReason property.
     */
    public void setUnavailableReason(@jakarta.annotation.Nullable final String value) {
        this.unavailableReason = value;
    }
}
