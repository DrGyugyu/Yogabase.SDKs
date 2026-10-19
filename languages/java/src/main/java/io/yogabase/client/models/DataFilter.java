package io.yogabase.client.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import com.microsoft.kiota.serialization.UntypedNode;
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
     * The op property
     */
    private DataFilterOp op;
    /**
     * Absent for isNull and notNull; an array of 1-100 non-null values for in; a string pattern for like and ilike; otherwise one non-null JSON value.
     */
    private UntypedNode value;
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
        deserializerMap.put("op", (n) -> { this.setOp(n.getEnumValue(DataFilterOp::forValue)); });
        deserializerMap.put("value", (n) -> { this.setValue(n.getObjectValue(UntypedNode::createFromDiscriminatorValue)); });
        return deserializerMap;
    }
    /**
     * Gets the op property value. The op property
     * @return a {@link DataFilterOp}
     */
    @jakarta.annotation.Nullable
    public DataFilterOp getOp() {
        return this.op;
    }
    /**
     * Gets the value property value. Absent for isNull and notNull; an array of 1-100 non-null values for in; a string pattern for like and ilike; otherwise one non-null JSON value.
     * @return a {@link UntypedNode}
     */
    @jakarta.annotation.Nullable
    public UntypedNode getValue() {
        return this.value;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeStringValue("column", this.getColumn());
        writer.writeEnumValue("op", this.getOp());
        writer.writeObjectValue("value", this.getValue());
    }
    /**
     * Sets the column property value. The column property
     * @param value Value to set for the column property.
     */
    public void setColumn(@jakarta.annotation.Nullable final String value) {
        this.column = value;
    }
    /**
     * Sets the op property value. The op property
     * @param value Value to set for the op property.
     */
    public void setOp(@jakarta.annotation.Nullable final DataFilterOp value) {
        this.op = value;
    }
    /**
     * Sets the value property value. Absent for isNull and notNull; an array of 1-100 non-null values for in; a string pattern for like and ilike; otherwise one non-null JSON value.
     * @param value Value to set for the value property.
     */
    public void setValue(@jakarta.annotation.Nullable final UntypedNode value) {
        this.value = value;
    }
}
