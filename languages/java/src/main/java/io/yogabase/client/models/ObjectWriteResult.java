package io.yogabase.client.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class ObjectWriteResult implements Parsable {
    /**
     * The alreadyExisted property
     */
    private Boolean alreadyExisted;
    /**
     * The digest property
     */
    private String digest;
    /**
     * The encryption property
     */
    private String encryption;
    /**
     * The key property
     */
    private String key;
    /**
     * The sizeBytes property
     */
    private Long sizeBytes;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link ObjectWriteResult}
     */
    @jakarta.annotation.Nonnull
    public static ObjectWriteResult createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new ObjectWriteResult();
    }
    /**
     * Gets the alreadyExisted property value. The alreadyExisted property
     * @return a {@link Boolean}
     */
    @jakarta.annotation.Nullable
    public Boolean getAlreadyExisted() {
        return this.alreadyExisted;
    }
    /**
     * Gets the digest property value. The digest property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getDigest() {
        return this.digest;
    }
    /**
     * Gets the encryption property value. The encryption property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getEncryption() {
        return this.encryption;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(5);
        deserializerMap.put("alreadyExisted", (n) -> { this.setAlreadyExisted(n.getBooleanValue()); });
        deserializerMap.put("digest", (n) -> { this.setDigest(n.getStringValue()); });
        deserializerMap.put("encryption", (n) -> { this.setEncryption(n.getStringValue()); });
        deserializerMap.put("key", (n) -> { this.setKey(n.getStringValue()); });
        deserializerMap.put("sizeBytes", (n) -> { this.setSizeBytes(n.getLongValue()); });
        return deserializerMap;
    }
    /**
     * Gets the key property value. The key property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getKey() {
        return this.key;
    }
    /**
     * Gets the sizeBytes property value. The sizeBytes property
     * @return a {@link Long}
     */
    @jakarta.annotation.Nullable
    public Long getSizeBytes() {
        return this.sizeBytes;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeBooleanValue("alreadyExisted", this.getAlreadyExisted());
        writer.writeStringValue("digest", this.getDigest());
        writer.writeStringValue("encryption", this.getEncryption());
        writer.writeStringValue("key", this.getKey());
        writer.writeLongValue("sizeBytes", this.getSizeBytes());
    }
    /**
     * Sets the alreadyExisted property value. The alreadyExisted property
     * @param value Value to set for the alreadyExisted property.
     */
    public void setAlreadyExisted(@jakarta.annotation.Nullable final Boolean value) {
        this.alreadyExisted = value;
    }
    /**
     * Sets the digest property value. The digest property
     * @param value Value to set for the digest property.
     */
    public void setDigest(@jakarta.annotation.Nullable final String value) {
        this.digest = value;
    }
    /**
     * Sets the encryption property value. The encryption property
     * @param value Value to set for the encryption property.
     */
    public void setEncryption(@jakarta.annotation.Nullable final String value) {
        this.encryption = value;
    }
    /**
     * Sets the key property value. The key property
     * @param value Value to set for the key property.
     */
    public void setKey(@jakarta.annotation.Nullable final String value) {
        this.key = value;
    }
    /**
     * Sets the sizeBytes property value. The sizeBytes property
     * @param value Value to set for the sizeBytes property.
     */
    public void setSizeBytes(@jakarta.annotation.Nullable final Long value) {
        this.sizeBytes = value;
    }
}
