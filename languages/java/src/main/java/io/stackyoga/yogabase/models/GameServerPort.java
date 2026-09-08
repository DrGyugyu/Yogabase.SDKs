package io.stackyoga.yogabase.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class GameServerPort implements Parsable {
    /**
     * Port to connect to on host.
     */
    private Integer externalPort;
    /**
     * Node address the member observed. Never composed by the Control Plane.
     */
    private String host;
    /**
     * Port the workload listens on inside its Pod.
     */
    private Integer internalPort;
    /**
     * The name property
     */
    private String name;
    /**
     * The protocol property
     */
    private GameServerPortProtocol protocol;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link GameServerPort}
     */
    @jakarta.annotation.Nonnull
    public static GameServerPort createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new GameServerPort();
    }
    /**
     * Gets the externalPort property value. Port to connect to on host.
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getExternalPort() {
        return this.externalPort;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(5);
        deserializerMap.put("externalPort", (n) -> { this.setExternalPort(n.getIntegerValue()); });
        deserializerMap.put("host", (n) -> { this.setHost(n.getStringValue()); });
        deserializerMap.put("internalPort", (n) -> { this.setInternalPort(n.getIntegerValue()); });
        deserializerMap.put("name", (n) -> { this.setName(n.getStringValue()); });
        deserializerMap.put("protocol", (n) -> { this.setProtocol(n.getEnumValue(GameServerPortProtocol::forValue)); });
        return deserializerMap;
    }
    /**
     * Gets the host property value. Node address the member observed. Never composed by the Control Plane.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getHost() {
        return this.host;
    }
    /**
     * Gets the internalPort property value. Port the workload listens on inside its Pod.
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getInternalPort() {
        return this.internalPort;
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
     * Gets the protocol property value. The protocol property
     * @return a {@link GameServerPortProtocol}
     */
    @jakarta.annotation.Nullable
    public GameServerPortProtocol getProtocol() {
        return this.protocol;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeIntegerValue("externalPort", this.getExternalPort());
        writer.writeStringValue("host", this.getHost());
        writer.writeIntegerValue("internalPort", this.getInternalPort());
        writer.writeStringValue("name", this.getName());
        writer.writeEnumValue("protocol", this.getProtocol());
    }
    /**
     * Sets the externalPort property value. Port to connect to on host.
     * @param value Value to set for the externalPort property.
     */
    public void setExternalPort(@jakarta.annotation.Nullable final Integer value) {
        this.externalPort = value;
    }
    /**
     * Sets the host property value. Node address the member observed. Never composed by the Control Plane.
     * @param value Value to set for the host property.
     */
    public void setHost(@jakarta.annotation.Nullable final String value) {
        this.host = value;
    }
    /**
     * Sets the internalPort property value. Port the workload listens on inside its Pod.
     * @param value Value to set for the internalPort property.
     */
    public void setInternalPort(@jakarta.annotation.Nullable final Integer value) {
        this.internalPort = value;
    }
    /**
     * Sets the name property value. The name property
     * @param value Value to set for the name property.
     */
    public void setName(@jakarta.annotation.Nullable final String value) {
        this.name = value;
    }
    /**
     * Sets the protocol property value. The protocol property
     * @param value Value to set for the protocol property.
     */
    public void setProtocol(@jakarta.annotation.Nullable final GameServerPortProtocol value) {
        this.protocol = value;
    }
}
