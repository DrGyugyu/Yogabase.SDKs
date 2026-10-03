package io.stackyoga.yogabase.models;

import com.microsoft.kiota.serialization.ValuedEnum;
import java.util.Objects;

/**
 * realtime-presence appears only when requested; it is available exactly when realtime-subscribe is and the runtime&apos;s realtime projection enables presence. data-rest appears only when requested; it is available exactly when the runtime enables the Data API, which requires auth.
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public enum CapabilityId implements ValuedEnum {
    Auth("auth"),
    Storage("storage"),
    FunctionsRest("functions-rest"),
    RealtimeSubscribe("realtime-subscribe"),
    RealtimePresence("realtime-presence"),
    DataRest("data-rest");
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
            case "storage": return Storage;
            case "functions-rest": return FunctionsRest;
            case "realtime-subscribe": return RealtimeSubscribe;
            case "realtime-presence": return RealtimePresence;
            case "data-rest": return DataRest;
            default: return null;
        }
    }
}
