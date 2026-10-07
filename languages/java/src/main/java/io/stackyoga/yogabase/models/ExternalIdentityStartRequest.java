package io.stackyoga.yogabase.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class ExternalIdentityStartRequest implements Parsable {
    /**
     * Base64url of the SHA-256 of the client&apos;s verifier, without padding.
     */
    private String codeChallenge;
    /**
     * The redirectUri property
     */
    private String redirectUri;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link ExternalIdentityStartRequest}
     */
    @jakarta.annotation.Nonnull
    public static ExternalIdentityStartRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new ExternalIdentityStartRequest();
    }
    /**
     * Gets the codeChallenge property value. Base64url of the SHA-256 of the client&apos;s verifier, without padding.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getCodeChallenge() {
        return this.codeChallenge;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(2);
        deserializerMap.put("codeChallenge", (n) -> { this.setCodeChallenge(n.getStringValue()); });
        deserializerMap.put("redirectUri", (n) -> { this.setRedirectUri(n.getStringValue()); });
        return deserializerMap;
    }
    /**
     * Gets the redirectUri property value. The redirectUri property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getRedirectUri() {
        return this.redirectUri;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeStringValue("codeChallenge", this.getCodeChallenge());
        writer.writeStringValue("redirectUri", this.getRedirectUri());
    }
    /**
     * Sets the codeChallenge property value. Base64url of the SHA-256 of the client&apos;s verifier, without padding.
     * @param value Value to set for the codeChallenge property.
     */
    public void setCodeChallenge(@jakarta.annotation.Nullable final String value) {
        this.codeChallenge = value;
    }
    /**
     * Sets the redirectUri property value. The redirectUri property
     * @param value Value to set for the redirectUri property.
     */
    public void setRedirectUri(@jakarta.annotation.Nullable final String value) {
        this.redirectUri = value;
    }
}
