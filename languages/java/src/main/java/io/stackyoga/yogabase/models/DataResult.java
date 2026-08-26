package io.stackyoga.yogabase.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DataResult implements Parsable {
    /**
     * The affectedRows property
     */
    private Integer affectedRows;
    /**
     * The rows property
     */
    private java.util.List<DataResultRows> rows;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DataResult}
     */
    @jakarta.annotation.Nonnull
    public static DataResult createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DataResult();
    }
    /**
     * Gets the affectedRows property value. The affectedRows property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getAffectedRows() {
        return this.affectedRows;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(2);
        deserializerMap.put("affectedRows", (n) -> { this.setAffectedRows(n.getIntegerValue()); });
        deserializerMap.put("rows", (n) -> { this.setRows(n.getCollectionOfObjectValues(DataResultRows::createFromDiscriminatorValue)); });
        return deserializerMap;
    }
    /**
     * Gets the rows property value. The rows property
     * @return a {@link java.util.List<DataResultRows>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<DataResultRows> getRows() {
        return this.rows;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeIntegerValue("affectedRows", this.getAffectedRows());
        writer.writeCollectionOfObjectValues("rows", this.getRows());
    }
    /**
     * Sets the affectedRows property value. The affectedRows property
     * @param value Value to set for the affectedRows property.
     */
    public void setAffectedRows(@jakarta.annotation.Nullable final Integer value) {
        this.affectedRows = value;
    }
    /**
     * Sets the rows property value. The rows property
     * @param value Value to set for the rows property.
     */
    public void setRows(@jakarta.annotation.Nullable final java.util.List<DataResultRows> value) {
        this.rows = value;
    }
}
