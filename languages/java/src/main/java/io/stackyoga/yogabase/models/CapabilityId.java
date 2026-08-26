package io.stackyoga.yogabase.models;

import com.microsoft.kiota.serialization.ValuedEnum;
import java.util.Objects;

@jakarta.annotation.Generated("com.microsoft.kiota")
public enum CapabilityId implements ValuedEnum {
    Auth("auth"),
    DataRest("data-rest"),
    Storage("storage"),
    FunctionsRest("functions-rest"),
    RealtimeSubscribe("realtime-subscribe");
    public final String value;
    CapabilityId(final String value) {
        this.value = value;
    }
    @jakarta.annotation.Nonnull
    public String getValue() { return this.value; }
    @jakarta.annotation.Nullable
    public static CapabilityId forValue(@jakarta.annotation.Nonnull final String searchValue) {
        Objects.requireNonNull(searchValue);
        switch(searchValue) {
            case "auth": return Auth;
            case "data-rest": return DataRest;
            case "storage": return Storage;
            case "functions-rest": return FunctionsRest;
            case "realtime-subscribe": return RealtimeSubscribe;
            default: return null;
        }
    }
}
