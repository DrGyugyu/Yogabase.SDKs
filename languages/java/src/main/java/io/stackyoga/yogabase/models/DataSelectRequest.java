package io.stackyoga.yogabase.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DataSelectRequest implements Parsable {
    /**
     * Column names, or [&quot;*&quot;] for every column the caller may read.
     */
    private java.util.List<String> columns;
    /**
     * The filters property
     */
    private java.util.List<DataFilter> filters;
    /**
     * The limit property
     */
    private Integer limit;
    /**
     * The offset property
     */
    private Integer offset;
    /**
     * The order property
     */
    private java.util.List<DataOrder> order;
    /**
     * Instantiates a new {@link DataSelectRequest} and sets the default values.
     */
    public DataSelectRequest() {
        this.setLimit(100);
        this.setOffset(0);
    }
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DataSelectRequest}
     */
    @jakarta.annotation.Nonnull
    public static DataSelectRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DataSelectRequest();
    }
    /**
     * Gets the columns property value. Column names, or [&quot;*&quot;] for every column the caller may read.
     * @return a {@link java.util.List<String>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<String> getColumns() {
        return this.columns;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(5);
        deserializerMap.put("columns", (n) -> { this.setColumns(n.getCollectionOfPrimitiveValues(String.class)); });
        deserializerMap.put("filters", (n) -> { this.setFilters(n.getCollectionOfObjectValues(DataFilter::createFromDiscriminatorValue)); });
        deserializerMap.put("limit", (n) -> { this.setLimit(n.getIntegerValue()); });
        deserializerMap.put("offset", (n) -> { this.setOffset(n.getIntegerValue()); });
        deserializerMap.put("order", (n) -> { this.setOrder(n.getCollectionOfObjectValues(DataOrder::createFromDiscriminatorValue)); });
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
     * Gets the limit property value. The limit property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getLimit() {
        return this.limit;
    }
    /**
     * Gets the offset property value. The offset property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getOffset() {
        return this.offset;
    }
    /**
     * Gets the order property value. The order property
     * @return a {@link java.util.List<DataOrder>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<DataOrder> getOrder() {
        return this.order;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeCollectionOfPrimitiveValues("columns", this.getColumns());
        writer.writeCollectionOfObjectValues("filters", this.getFilters());
        writer.writeIntegerValue("limit", this.getLimit());
        writer.writeIntegerValue("offset", this.getOffset());
        writer.writeCollectionOfObjectValues("order", this.getOrder());
    }
    /**
     * Sets the columns property value. Column names, or [&quot;*&quot;] for every column the caller may read.
     * @param value Value to set for the columns property.
     */
    public void setColumns(@jakarta.annotation.Nullable final java.util.List<String> value) {
        this.columns = value;
    }
    /**
     * Sets the filters property value. The filters property
     * @param value Value to set for the filters property.
     */
    public void setFilters(@jakarta.annotation.Nullable final java.util.List<DataFilter> value) {
        this.filters = value;
    }
    /**
     * Sets the limit property value. The limit property
     * @param value Value to set for the limit property.
     */
    public void setLimit(@jakarta.annotation.Nullable final Integer value) {
        this.limit = value;
    }
    /**
     * Sets the offset property value. The offset property
     * @param value Value to set for the offset property.
     */
    public void setOffset(@jakarta.annotation.Nullable final Integer value) {
        this.offset = value;
    }
    /**
     * Sets the order property value. The order property
     * @param value Value to set for the order property.
     */
    public void setOrder(@jakarta.annotation.Nullable final java.util.List<DataOrder> value) {
        this.order = value;
    }
}
