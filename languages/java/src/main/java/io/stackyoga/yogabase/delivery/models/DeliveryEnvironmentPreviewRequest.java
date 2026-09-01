package io.stackyoga.yogabase.delivery.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DeliveryEnvironmentPreviewRequest implements Parsable {
    /**
     * The name property
     */
    private String name;
    /**
     * The templateId property
     */
    private UUID templateId;
    /**
     * The templateVersion property
     */
    private Integer templateVersion;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DeliveryEnvironmentPreviewRequest}
     */
    @jakarta.annotation.Nonnull
    public static DeliveryEnvironmentPreviewRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DeliveryEnvironmentPreviewRequest();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(3);
        deserializerMap.put("name", (n) -> { this.setName(n.getStringValue()); });
        deserializerMap.put("templateId", (n) -> { this.setTemplateId(n.getUUIDValue()); });
        deserializerMap.put("templateVersion", (n) -> { this.setTemplateVersion(n.getIntegerValue()); });
        return deserializerMap;
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
     * Gets the templateId property value. The templateId property
     * @return a {@link UUID}
     */
    @jakarta.annotation.Nullable
    public UUID getTemplateId() {
        return this.templateId;
    }
    /**
     * Gets the templateVersion property value. The templateVersion property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getTemplateVersion() {
        return this.templateVersion;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeStringValue("name", this.getName());
        writer.writeUUIDValue("templateId", this.getTemplateId());
        writer.writeIntegerValue("templateVersion", this.getTemplateVersion());
    }
    /**
     * Sets the name property value. The name property
     * @param value Value to set for the name property.
     */
    public void setName(@jakarta.annotation.Nullable final String value) {
        this.name = value;
    }
    /**
     * Sets the templateId property value. The templateId property
     * @param value Value to set for the templateId property.
     */
    public void setTemplateId(@jakarta.annotation.Nullable final UUID value) {
        this.templateId = value;
    }
    /**
     * Sets the templateVersion property value. The templateVersion property
     * @param value Value to set for the templateVersion property.
     */
    public void setTemplateVersion(@jakarta.annotation.Nullable final Integer value) {
        this.templateVersion = value;
    }
}
