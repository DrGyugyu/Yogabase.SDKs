package io.stackyoga.yogabase.models;

import com.microsoft.kiota.serialization.ValuedEnum;
import java.util.Objects;

@jakarta.annotation.Generated("com.microsoft.kiota")
public enum DataValueType implements ValuedEnum {
    Null("null"),
    String("string"),
    Bool("bool"),
    Int64("int64"),
    Decimal("decimal"),
    Double("double"),
    Guid("guid"),
    Timestamp("timestamp"),
    Json("json");
    public final String value;
    DataValueType(final String value) {
        this.value = value;
    }
    @jakarta.annotation.Nonnull
    public String getValue() { return this.value; }
    @jakarta.annotation.Nullable
    public static DataValueType forValue(@jakarta.annotation.Nonnull final String searchValue) {
        Objects.requireNonNull(searchValue);
        switch(searchValue) {
            case "null": return Null;
            case "string": return String;
            case "bool": return Bool;
            case "int64": return Int64;
            case "decimal": return Decimal;
            case "double": return Double;
            case "guid": return Guid;
            case "timestamp": return Timestamp;
            case "json": return Json;
            default: return null;
        }
    }
}
