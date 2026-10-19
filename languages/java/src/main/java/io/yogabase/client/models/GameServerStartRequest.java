package io.yogabase.client.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class GameServerStartRequest implements Parsable {
    /**
     * Bounded string map recorded on the allocation and carried to the instance. At most 4096 bytes as canonical JSON. It reaches customer code, so it must never carry a credential or another player&apos;s identity.
     */
    private GameServerStartRequestCustomData customData;
    /**
     * Canonical hosted-service id to allocate an instance of.
     */
    private String service;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link GameServerStartRequest}
     */
    @jakarta.annotation.Nonnull
    public static GameServerStartRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new GameServerStartRequest();
    }
    /**
     * Gets the customData property value. Bounded string map recorded on the allocation and carried to the instance. At most 4096 bytes as canonical JSON. It reaches customer code, so it must never carry a credential or another player&apos;s identity.
     * @return a {@link GameServerStartRequestCustomData}
     */
    @jakarta.annotation.Nullable
    public GameServerStartRequestCustomData getCustomData() {
        return this.customData;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(2);
        deserializerMap.put("customData", (n) -> { this.setCustomData(n.getObjectValue(GameServerStartRequestCustomData::createFromDiscriminatorValue)); });
        deserializerMap.put("service", (n) -> { this.setService(n.getStringValue()); });
        return deserializerMap;
    }
    /**
     * Gets the service property value. Canonical hosted-service id to allocate an instance of.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getService() {
        return this.service;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeObjectValue("customData", this.getCustomData());
        writer.writeStringValue("service", this.getService());
    }
    /**
     * Sets the customData property value. Bounded string map recorded on the allocation and carried to the instance. At most 4096 bytes as canonical JSON. It reaches customer code, so it must never carry a credential or another player&apos;s identity.
     * @param value Value to set for the customData property.
     */
    public void setCustomData(@jakarta.annotation.Nullable final GameServerStartRequestCustomData value) {
        this.customData = value;
    }
    /**
     * Sets the service property value. Canonical hosted-service id to allocate an instance of.
     * @param value Value to set for the service property.
     */
    public void setService(@jakarta.annotation.Nullable final String value) {
        this.service = value;
    }
}
