package io.yogabase.client.models;

import com.microsoft.kiota.serialization.ValuedEnum;
import java.util.Objects;

@jakarta.annotation.Generated("com.microsoft.kiota")
public enum DataFilterOp implements ValuedEnum {
    Eq("eq"),
    Neq("neq"),
    Gt("gt"),
    Gte("gte"),
    Lt("lt"),
    Lte("lte"),
    In("in"),
    Like("like"),
    Ilike("ilike"),
    IsNull("isNull"),
    NotNull("notNull");
    public final String value;
    DataFilterOp(final String value) {
        this.value = value;
    }
    @jakarta.annotation.Nonnull
    public String getValue() { return this.value; }
    @jakarta.annotation.Nullable
    public static DataFilterOp forValue(@jakarta.annotation.Nonnull final String searchValue) {
        Objects.requireNonNull(searchValue);
        switch(searchValue) {
            case "eq": return Eq;
            case "neq": return Neq;
            case "gt": return Gt;
            case "gte": return Gte;
            case "lt": return Lt;
            case "lte": return Lte;
            case "in": return In;
            case "like": return Like;
            case "ilike": return Ilike;
            case "isNull": return IsNull;
            case "notNull": return NotNull;
            default: return null;
        }
    }
}
