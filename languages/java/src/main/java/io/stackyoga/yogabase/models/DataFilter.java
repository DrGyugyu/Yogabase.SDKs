package io.stackyoga.yogabase.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DataFilter implements Parsable {
    /**
     * The column property
     */
    private String column;
    /**
     * The operator property
     */
    private String operator;
    /**
     * The values property
     */
    private java.util.List<DataValue> values;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DataFilter}
     */
    @jakarta.annotation.Nonnull
    public static DataFilter createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DataFilter();
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
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(3);
        deserializerMap.put("column", (n) -> { this.setColumn(n.getStringValue()); });
        deserializerMap.put("operator", (n) -> { this.setOperator(n.getStringValue()); });
        deserializerMap.put("values", (n) -> { this.setValues(n.getCollectionOfObjectValues(DataValue::createFromDiscriminatorValue)); });
        return deserializerMap;
    }
    /**
     * Gets the operator property value. The operator property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getOperator() {
        return this.operator;
    }
    /**
     * Gets the values property value. The values property
     * @return a {@link java.util.List<DataValue>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<DataValue> getValues() {
        return this.values;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeStringValue("column", this.getColumn());
        writer.writeStringValue("operator", this.getOperator());
        writer.writeCollectionOfObjectValues("values", this.getValues());
    }
    /**
     * Sets the column property value. The column property
     * @param value Value to set for the column property.
     */
    public void setColumn(@jakarta.annotation.Nullable final String value) {
        this.column = value;
    }
    /**
     * Sets the operator property value. The operator property
     * @param value Value to set for the operator property.
     */
    public void setOperator(@jakarta.annotation.Nullable final String value) {
        this.operator = value;
    }
    /**
     * Sets the values property value. The values property
     * @param value Value to set for the values property.
     */
    public void setValues(@jakarta.annotation.Nullable final java.util.List<DataValue> value) {
        this.values = value;
    }
}
