package io.stackyoga.yogabase.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class RecoveryCodesResponse implements Parsable {
    /**
     * The recoveryCodes property
     */
    private java.util.List<String> recoveryCodes;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link RecoveryCodesResponse}
     */
    @jakarta.annotation.Nonnull
    public static RecoveryCodesResponse createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new RecoveryCodesResponse();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(1);
        deserializerMap.put("recoveryCodes", (n) -> { this.setRecoveryCodes(n.getCollectionOfPrimitiveValues(String.class)); });
        return deserializerMap;
    }
    /**
     * Gets the recoveryCodes property value. The recoveryCodes property
     * @return a {@link java.util.List<String>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<String> getRecoveryCodes() {
        return this.recoveryCodes;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeCollectionOfPrimitiveValues("recoveryCodes", this.getRecoveryCodes());
    }
    /**
     * Sets the recoveryCodes property value. The recoveryCodes property
     * @param value Value to set for the recoveryCodes property.
     */
    public void setRecoveryCodes(@jakarta.annotation.Nullable final java.util.List<String> value) {
        this.recoveryCodes = value;
    }
}
