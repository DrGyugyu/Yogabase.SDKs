package io.yogabase.client.models;

import com.microsoft.kiota.serialization.ValuedEnum;
import java.util.Objects;

@jakarta.annotation.Generated("com.microsoft.kiota")
public enum ActorInvocationResultStatus implements ValuedEnum {
    InProgress("InProgress"),
    Completed("Completed"),
    Failed("Failed");
    public final String value;
    ActorInvocationResultStatus(final String value) {
        this.value = value;
    }
    @jakarta.annotation.Nonnull
    public String getValue() { return this.value; }
    @jakarta.annotation.Nullable
    public static ActorInvocationResultStatus forValue(@jakarta.annotation.Nonnull final String searchValue) {
        Objects.requireNonNull(searchValue);
        switch(searchValue) {
            case "InProgress": return InProgress;
            case "Completed": return Completed;
            case "Failed": return Failed;
            default: return null;
        }
    }
}
