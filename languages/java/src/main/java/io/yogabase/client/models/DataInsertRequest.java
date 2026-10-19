package io.yogabase.client.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DataInsertRequest implements Parsable {
    /**
     * Column names, or [&quot;*&quot;] for every column the caller may read.
     */
    private java.util.List<String> returning;
    /**
     * The rows property
     */
    private java.util.List<DataRow> rows;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DataInsertRequest}
     */
    @jakarta.annotation.Nonnull
    public static DataInsertRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DataInsertRequest();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(2);
        deserializerMap.put("returning", (n) -> { this.setReturning(n.getCollectionOfPrimitiveValues(String.class)); });
        deserializerMap.put("rows", (n) -> { this.setRows(n.getCollectionOfObjectValues(DataRow::createFromDiscriminatorValue)); });
        return deserializerMap;
    }
    /**
     * Gets the returning property value. Column names, or [&quot;*&quot;] for every column the caller may read.
     * @return a {@link java.util.List<String>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<String> getReturning() {
        return this.returning;
    }
    /**
     * Gets the rows property value. The rows property
     * @return a {@link java.util.List<DataRow>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<DataRow> getRows() {
        return this.rows;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeCollectionOfPrimitiveValues("returning", this.getReturning());
        writer.writeCollectionOfObjectValues("rows", this.getRows());
    }
    /**
     * Sets the returning property value. Column names, or [&quot;*&quot;] for every column the caller may read.
     * @param value Value to set for the returning property.
     */
    public void setReturning(@jakarta.annotation.Nullable final java.util.List<String> value) {
        this.returning = value;
    }
    /**
     * Sets the rows property value. The rows property
     * @param value Value to set for the rows property.
     */
    public void setRows(@jakarta.annotation.Nullable final java.util.List<DataRow> value) {
        this.rows = value;
    }
}
