package io.stackyoga.yogabase.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class GameServer implements Parsable {
    /**
     * Empty when no member has observed an address for this instance; a client must not connect to such an instance.
     */
    private java.util.List<GameServerPort> networkPorts;
    /**
     * The serverId property
     */
    private String serverId;
    /**
     * Lifecycle state, verbatim. Read it as text: a state Yogabase adds later must stay readable rather than fail a client.
     */
    private String state;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link GameServer}
     */
    @jakarta.annotation.Nonnull
    public static GameServer createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new GameServer();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(3);
        deserializerMap.put("networkPorts", (n) -> { this.setNetworkPorts(n.getCollectionOfObjectValues(GameServerPort::createFromDiscriminatorValue)); });
        deserializerMap.put("serverId", (n) -> { this.setServerId(n.getStringValue()); });
        deserializerMap.put("state", (n) -> { this.setState(n.getStringValue()); });
        return deserializerMap;
    }
    /**
     * Gets the networkPorts property value. Empty when no member has observed an address for this instance; a client must not connect to such an instance.
     * @return a {@link java.util.List<GameServerPort>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<GameServerPort> getNetworkPorts() {
        return this.networkPorts;
    }
    /**
     * Gets the serverId property value. The serverId property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getServerId() {
        return this.serverId;
    }
    /**
     * Gets the state property value. Lifecycle state, verbatim. Read it as text: a state Yogabase adds later must stay readable rather than fail a client.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getState() {
        return this.state;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeCollectionOfObjectValues("networkPorts", this.getNetworkPorts());
        writer.writeStringValue("serverId", this.getServerId());
        writer.writeStringValue("state", this.getState());
    }
    /**
     * Sets the networkPorts property value. Empty when no member has observed an address for this instance; a client must not connect to such an instance.
     * @param value Value to set for the networkPorts property.
     */
    public void setNetworkPorts(@jakarta.annotation.Nullable final java.util.List<GameServerPort> value) {
        this.networkPorts = value;
    }
    /**
     * Sets the serverId property value. The serverId property
     * @param value Value to set for the serverId property.
     */
    public void setServerId(@jakarta.annotation.Nullable final String value) {
        this.serverId = value;
    }
    /**
     * Sets the state property value. Lifecycle state, verbatim. Read it as text: a state Yogabase adds later must stay readable rather than fail a client.
     * @param value Value to set for the state property.
     */
    public void setState(@jakarta.annotation.Nullable final String value) {
        this.state = value;
    }
}
