package io.stackyoga.yogabase.models;

import com.microsoft.kiota.serialization.ComposedTypeWrapper;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DataSelectRequest implements Parsable {
    /**
     * The columns property
     */
    private java.util.List<String> columns;
    /**
     * The cursor property
     */
    private DataSelectRequestCursor cursor;
    /**
     * The filters property
     */
    private java.util.List<DataFilter> filters;
    /**
     * The ordering property
     */
    private java.util.List<DataOrdering> ordering;
    /**
     * The pageSize property
     */
    private Integer pageSize;
    /**
     * The queryDepth property
     */
    private Integer queryDepth;
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
     * Gets the columns property value. The columns property
     * @return a {@link java.util.List<String>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<String> getColumns() {
        return this.columns;
    }
    /**
     * Gets the cursor property value. The cursor property
     * @return a {@link DataSelectRequestCursor}
     */
    @jakarta.annotation.Nullable
    public DataSelectRequestCursor getCursor() {
        return this.cursor;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(6);
        deserializerMap.put("columns", (n) -> { this.setColumns(n.getCollectionOfPrimitiveValues(String.class)); });
        deserializerMap.put("cursor", (n) -> { this.setCursor(n.getObjectValue(DataSelectRequestCursor::createFromDiscriminatorValue)); });
        deserializerMap.put("filters", (n) -> { this.setFilters(n.getCollectionOfObjectValues(DataFilter::createFromDiscriminatorValue)); });
        deserializerMap.put("ordering", (n) -> { this.setOrdering(n.getCollectionOfObjectValues(DataOrdering::createFromDiscriminatorValue)); });
        deserializerMap.put("pageSize", (n) -> { this.setPageSize(n.getIntegerValue()); });
        deserializerMap.put("queryDepth", (n) -> { this.setQueryDepth(n.getIntegerValue()); });
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
     * Gets the ordering property value. The ordering property
     * @return a {@link java.util.List<DataOrdering>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<DataOrdering> getOrdering() {
        return this.ordering;
    }
    /**
     * Gets the pageSize property value. The pageSize property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getPageSize() {
        return this.pageSize;
    }
    /**
     * Gets the queryDepth property value. The queryDepth property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getQueryDepth() {
        return this.queryDepth;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeCollectionOfPrimitiveValues("columns", this.getColumns());
        writer.writeObjectValue("cursor", this.getCursor());
        writer.writeCollectionOfObjectValues("filters", this.getFilters());
        writer.writeCollectionOfObjectValues("ordering", this.getOrdering());
        writer.writeIntegerValue("pageSize", this.getPageSize());
        writer.writeIntegerValue("queryDepth", this.getQueryDepth());
    }
    /**
     * Sets the columns property value. The columns property
     * @param value Value to set for the columns property.
     */
    public void setColumns(@jakarta.annotation.Nullable final java.util.List<String> value) {
        this.columns = value;
    }
    /**
     * Sets the cursor property value. The cursor property
     * @param value Value to set for the cursor property.
     */
    public void setCursor(@jakarta.annotation.Nullable final DataSelectRequestCursor value) {
        this.cursor = value;
    }
    /**
     * Sets the filters property value. The filters property
     * @param value Value to set for the filters property.
     */
    public void setFilters(@jakarta.annotation.Nullable final java.util.List<DataFilter> value) {
        this.filters = value;
    }
    /**
     * Sets the ordering property value. The ordering property
     * @param value Value to set for the ordering property.
     */
    public void setOrdering(@jakarta.annotation.Nullable final java.util.List<DataOrdering> value) {
        this.ordering = value;
    }
    /**
     * Sets the pageSize property value. The pageSize property
     * @param value Value to set for the pageSize property.
     */
    public void setPageSize(@jakarta.annotation.Nullable final Integer value) {
        this.pageSize = value;
    }
    /**
     * Sets the queryDepth property value. The queryDepth property
     * @param value Value to set for the queryDepth property.
     */
    public void setQueryDepth(@jakarta.annotation.Nullable final Integer value) {
        this.queryDepth = value;
    }
    /**
     * Composed type wrapper for classes {@link DataCursor}, {@link DataSelectRequestCursorMember1}
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public static class DataSelectRequestCursor implements ComposedTypeWrapper, Parsable {
        /**
         * Composed type representation for type {@link DataCursor}
         */
        private DataCursor dataCursor;
        /**
         * Composed type representation for type {@link DataSelectRequestCursorMember1}
         */
        private DataSelectRequestCursorMember1 dataSelectRequestCursorMember1;
        /**
         * Creates a new instance of the appropriate class based on discriminator value
         * @param parseNode The parse node to use to read the discriminator value and create the object
         * @return a {@link DataSelectRequestCursor}
         */
        @jakarta.annotation.Nonnull
        public static DataSelectRequestCursor createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
            Objects.requireNonNull(parseNode);
            final DataSelectRequestCursor result = new DataSelectRequestCursor();
            final ParseNode mappingValueNode = parseNode.getChildNode("");
            if (mappingValueNode != null) {
                final String mappingValue = mappingValueNode.getStringValue();
            }
            return result;
        }
        /**
         * Gets the DataCursor property value. Composed type representation for type {@link DataCursor}
         * @return a {@link DataCursor}
         */
        @jakarta.annotation.Nullable
        public DataCursor getDataCursor() {
            return this.dataCursor;
        }
        /**
         * Gets the DataSelectRequest_cursorMember1 property value. Composed type representation for type {@link DataSelectRequestCursorMember1}
         * @return a {@link DataSelectRequestCursorMember1}
         */
        @jakarta.annotation.Nullable
        public DataSelectRequestCursorMember1 getDataSelectRequestCursorMember1() {
            return this.dataSelectRequestCursorMember1;
        }
        /**
         * The deserialization information for the current model
         * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
         */
        @jakarta.annotation.Nonnull
        public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
            if (this.getDataCursor() != null) {
                return this.getDataCursor().getFieldDeserializers();
            } else if (this.getDataSelectRequestCursorMember1() != null) {
                return this.getDataSelectRequestCursorMember1().getFieldDeserializers();
            }
            return new HashMap<String, java.util.function.Consumer<ParseNode>>();
        }
        /**
         * Serializes information the current object
         * @param writer Serialization writer to use to serialize this model
         */
        public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
            Objects.requireNonNull(writer);
            if (this.getDataCursor() != null) {
                writer.writeObjectValue(null, this.getDataCursor());
            } else if (this.getDataSelectRequestCursorMember1() != null) {
                writer.writeObjectValue(null, this.getDataSelectRequestCursorMember1());
            }
        }
        /**
         * Sets the DataCursor property value. Composed type representation for type {@link DataCursor}
         * @param value Value to set for the DataCursor property.
         */
        public void setDataCursor(@jakarta.annotation.Nullable final DataCursor value) {
            this.dataCursor = value;
        }
        /**
         * Sets the DataSelectRequest_cursorMember1 property value. Composed type representation for type {@link DataSelectRequestCursorMember1}
         * @param value Value to set for the DataSelectRequest_cursorMember1 property.
         */
        public void setDataSelectRequestCursorMember1(@jakarta.annotation.Nullable final DataSelectRequestCursorMember1 value) {
            this.dataSelectRequestCursorMember1 = value;
        }
    }
}
