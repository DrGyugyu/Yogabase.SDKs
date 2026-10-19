package io.yogabase.client.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class MfaSetupResponse implements Parsable {
    /**
     * The sharedKey property
     */
    private String sharedKey;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link MfaSetupResponse}
     */
    @jakarta.annotation.Nonnull
    public static MfaSetupResponse createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new MfaSetupResponse();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(1);
        deserializerMap.put("sharedKey", (n) -> { this.setSharedKey(n.getStringValue()); });
        return deserializerMap;
    }
    /**
     * Gets the sharedKey property value. The sharedKey property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getSharedKey() {
        return this.sharedKey;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeStringValue("sharedKey", this.getSharedKey());
    }
    /**
     * Sets the sharedKey property value. The sharedKey property
     * @param value Value to set for the sharedKey property.
     */
    public void setSharedKey(@jakarta.annotation.Nullable final String value) {
        this.sharedKey = value;
    }
}
