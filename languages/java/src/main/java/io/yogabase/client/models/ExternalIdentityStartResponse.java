package io.yogabase.client.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class ExternalIdentityStartResponse implements Parsable {
    /**
     * The authorizationUrl property
     */
    private String authorizationUrl;
    /**
     * The state property
     */
    private String state;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link ExternalIdentityStartResponse}
     */
    @jakarta.annotation.Nonnull
    public static ExternalIdentityStartResponse createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new ExternalIdentityStartResponse();
    }
    /**
     * Gets the authorizationUrl property value. The authorizationUrl property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getAuthorizationUrl() {
        return this.authorizationUrl;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(2);
        deserializerMap.put("authorizationUrl", (n) -> { this.setAuthorizationUrl(n.getStringValue()); });
        deserializerMap.put("state", (n) -> { this.setState(n.getStringValue()); });
        return deserializerMap;
    }
    /**
     * Gets the state property value. The state property
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
        writer.writeStringValue("authorizationUrl", this.getAuthorizationUrl());
        writer.writeStringValue("state", this.getState());
    }
    /**
     * Sets the authorizationUrl property value. The authorizationUrl property
     * @param value Value to set for the authorizationUrl property.
     */
    public void setAuthorizationUrl(@jakarta.annotation.Nullable final String value) {
        this.authorizationUrl = value;
    }
    /**
     * Sets the state property value. The state property
     * @param value Value to set for the state property.
     */
    public void setState(@jakarta.annotation.Nullable final String value) {
        this.state = value;
    }
}
