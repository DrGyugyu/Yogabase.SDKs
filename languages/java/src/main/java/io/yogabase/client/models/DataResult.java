package io.yogabase.client.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import com.microsoft.kiota.serialization.UntypedNode;
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
     * For a select, true when another page exists past this one.
     */
    private Boolean hasMore;
    /**
     * The rows property
     */
    private UntypedNode rows;
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
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(3);
        deserializerMap.put("affectedRows", (n) -> { this.setAffectedRows(n.getIntegerValue()); });
        deserializerMap.put("hasMore", (n) -> { this.setHasMore(n.getBooleanValue()); });
        deserializerMap.put("rows", (n) -> { this.setRows(n.getObjectValue(UntypedNode::createFromDiscriminatorValue)); });
        return deserializerMap;
    }
    /**
     * Gets the hasMore property value. For a select, true when another page exists past this one.
     * @return a {@link Boolean}
     */
    @jakarta.annotation.Nullable
    public Boolean getHasMore() {
        return this.hasMore;
    }
    /**
     * Gets the rows property value. The rows property
     * @return a {@link UntypedNode}
     */
    @jakarta.annotation.Nullable
    public UntypedNode getRows() {
        return this.rows;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeIntegerValue("affectedRows", this.getAffectedRows());
        writer.writeBooleanValue("hasMore", this.getHasMore());
        writer.writeObjectValue("rows", this.getRows());
    }
    /**
     * Sets the affectedRows property value. The affectedRows property
     * @param value Value to set for the affectedRows property.
     */
    public void setAffectedRows(@jakarta.annotation.Nullable final Integer value) {
        this.affectedRows = value;
    }
    /**
     * Sets the hasMore property value. For a select, true when another page exists past this one.
     * @param value Value to set for the hasMore property.
     */
    public void setHasMore(@jakarta.annotation.Nullable final Boolean value) {
        this.hasMore = value;
    }
    /**
     * Sets the rows property value. The rows property
     * @param value Value to set for the rows property.
     */
    public void setRows(@jakarta.annotation.Nullable final UntypedNode value) {
        this.rows = value;
    }
}
