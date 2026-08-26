package io.stackyoga.yogabase.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DataMutationRequest implements Parsable {
    /**
     * The filters property
     */
    private java.util.List<DataFilter> filters;
    /**
     * The returningColumns property
     */
    private java.util.List<String> returningColumns;
    /**
     * The values property
     */
    private DataMutationRequestValues values;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DataMutationRequest}
     */
    @jakarta.annotation.Nonnull
    public static DataMutationRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DataMutationRequest();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(3);
        deserializerMap.put("filters", (n) -> { this.setFilters(n.getCollectionOfObjectValues(DataFilter::createFromDiscriminatorValue)); });
        deserializerMap.put("returningColumns", (n) -> { this.setReturningColumns(n.getCollectionOfPrimitiveValues(String.class)); });
        deserializerMap.put("values", (n) -> { this.setValues(n.getObjectValue(DataMutationRequestValues::createFromDiscriminatorValue)); });
        return deserializerMap;
    }
    /**
     * Gets the filters property value. The filters property
     * @return a {@link java.util.List<DataFilter>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<DataFilter> getFilters() {
        return this.filters;
    }
    /**
     * Gets the returningColumns property value. The returningColumns property
     * @return a {@link java.util.List<String>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<String> getReturningColumns() {
        return this.returningColumns;
    }
    /**
     * Gets the values property value. The values property
     * @return a {@link DataMutationRequestValues}
     */
    @jakarta.annotation.Nullable
    public DataMutationRequestValues getValues() {
        return this.values;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeCollectionOfObjectValues("filters", this.getFilters());
        writer.writeCollectionOfPrimitiveValues("returningColumns", this.getReturningColumns());
        writer.writeObjectValue("values", this.getValues());
    }
    /**
     * Sets the filters property value. The filters property
     * @param value Value to set for the filters property.
     */
    public void setFilters(@jakarta.annotation.Nullable final java.util.List<DataFilter> value) {
        this.filters = value;
    }
    /**
     * Sets the returningColumns property value. The returningColumns property
     * @param value Value to set for the returningColumns property.
     */
    public void setReturningColumns(@jakarta.annotation.Nullable final java.util.List<String> value) {
        this.returningColumns = value;
    }
    /**
     * Sets the values property value. The values property
     * @param value Value to set for the values property.
     */
    public void setValues(@jakarta.annotation.Nullable final DataMutationRequestValues value) {
        this.values = value;
    }
}
