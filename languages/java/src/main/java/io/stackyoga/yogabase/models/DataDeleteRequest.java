package io.stackyoga.yogabase.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DataDeleteRequest implements Parsable {
    /**
     * The filters property
     */
    private java.util.List<DataFilter> filters;
    /**
     * Column names, or [&quot;*&quot;] for every column the caller may read.
     */
    private java.util.List<String> returning;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DataDeleteRequest}
     */
    @jakarta.annotation.Nonnull
    public static DataDeleteRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DataDeleteRequest();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(2);
        deserializerMap.put("filters", (n) -> { this.setFilters(n.getCollectionOfObjectValues(DataFilter::createFromDiscriminatorValue)); });
        deserializerMap.put("returning", (n) -> { this.setReturning(n.getCollectionOfPrimitiveValues(String.class)); });
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
     * Gets the returning property value. Column names, or [&quot;*&quot;] for every column the caller may read.
     * @return a {@link java.util.List<String>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<String> getReturning() {
        return this.returning;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeCollectionOfObjectValues("filters", this.getFilters());
        writer.writeCollectionOfPrimitiveValues("returning", this.getReturning());
    }
    /**
     * Sets the filters property value. The filters property
     * @param value Value to set for the filters property.
     */
    public void setFilters(@jakarta.annotation.Nullable final java.util.List<DataFilter> value) {
        this.filters = value;
    }
    /**
     * Sets the returning property value. Column names, or [&quot;*&quot;] for every column the caller may read.
     * @param value Value to set for the returning property.
     */
    public void setReturning(@jakarta.annotation.Nullable final java.util.List<String> value) {
        this.returning = value;
    }
}
