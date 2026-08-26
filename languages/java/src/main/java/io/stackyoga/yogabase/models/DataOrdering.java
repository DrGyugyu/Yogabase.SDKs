package io.stackyoga.yogabase.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DataOrdering implements Parsable {
    /**
     * The column property
     */
    private String column;
    /**
     * The descending property
     */
    private Boolean descending;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DataOrdering}
     */
    @jakarta.annotation.Nonnull
    public static DataOrdering createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DataOrdering();
    }
    /**
     * Gets the column property value. The column property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getColumn() {
        return this.column;
    }
    /**
     * Gets the descending property value. The descending property
     * @return a {@link Boolean}
     */
    @jakarta.annotation.Nullable
    public Boolean getDescending() {
        return this.descending;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(2);
        deserializerMap.put("column", (n) -> { this.setColumn(n.getStringValue()); });
        deserializerMap.put("descending", (n) -> { this.setDescending(n.getBooleanValue()); });
        return deserializerMap;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeStringValue("column", this.getColumn());
        writer.writeBooleanValue("descending", this.getDescending());
    }
    /**
     * Sets the column property value. The column property
     * @param value Value to set for the column property.
     */
    public void setColumn(@jakarta.annotation.Nullable final String value) {
        this.column = value;
    }
    /**
     * Sets the descending property value. The descending property
     * @param value Value to set for the descending property.
     */
    public void setDescending(@jakarta.annotation.Nullable final Boolean value) {
        this.descending = value;
    }
}
