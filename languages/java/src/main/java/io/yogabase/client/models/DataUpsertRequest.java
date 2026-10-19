package io.yogabase.client.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DataUpsertRequest implements Parsable {
    /**
     * The ignoreDuplicates property
     */
    private Boolean ignoreDuplicates;
    /**
     * The onConflict property
     */
    private java.util.List<String> onConflict;
    /**
     * Column names, or [&quot;*&quot;] for every column the caller may read.
     */
    private java.util.List<String> returning;
    /**
     * The rows property
     */
    private java.util.List<DataRow> rows;
    /**
     * Instantiates a new {@link DataUpsertRequest} and sets the default values.
     */
    public DataUpsertRequest() {
        this.setIgnoreDuplicates(false);
    }
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DataUpsertRequest}
     */
    @jakarta.annotation.Nonnull
    public static DataUpsertRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DataUpsertRequest();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(4);
        deserializerMap.put("ignoreDuplicates", (n) -> { this.setIgnoreDuplicates(n.getBooleanValue()); });
        deserializerMap.put("onConflict", (n) -> { this.setOnConflict(n.getCollectionOfPrimitiveValues(String.class)); });
        deserializerMap.put("returning", (n) -> { this.setReturning(n.getCollectionOfPrimitiveValues(String.class)); });
        deserializerMap.put("rows", (n) -> { this.setRows(n.getCollectionOfObjectValues(DataRow::createFromDiscriminatorValue)); });
        return deserializerMap;
    }
    /**
     * Gets the ignoreDuplicates property value. The ignoreDuplicates property
     * @return a {@link Boolean}
     */
    @jakarta.annotation.Nullable
    public Boolean getIgnoreDuplicates() {
        return this.ignoreDuplicates;
    }
    /**
     * Gets the onConflict property value. The onConflict property
     * @return a {@link java.util.List<String>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<String> getOnConflict() {
        return this.onConflict;
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
        writer.writeBooleanValue("ignoreDuplicates", this.getIgnoreDuplicates());
        writer.writeCollectionOfPrimitiveValues("onConflict", this.getOnConflict());
        writer.writeCollectionOfPrimitiveValues("returning", this.getReturning());
        writer.writeCollectionOfObjectValues("rows", this.getRows());
    }
    /**
     * Sets the ignoreDuplicates property value. The ignoreDuplicates property
     * @param value Value to set for the ignoreDuplicates property.
     */
    public void setIgnoreDuplicates(@jakarta.annotation.Nullable final Boolean value) {
        this.ignoreDuplicates = value;
    }
    /**
     * Sets the onConflict property value. The onConflict property
     * @param value Value to set for the onConflict property.
     */
    public void setOnConflict(@jakarta.annotation.Nullable final java.util.List<String> value) {
        this.onConflict = value;
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
