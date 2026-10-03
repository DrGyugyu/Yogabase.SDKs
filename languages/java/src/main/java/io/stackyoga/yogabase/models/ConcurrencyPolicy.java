package io.stackyoga.yogabase.models;

import com.microsoft.kiota.serialization.ValuedEnum;
import java.util.Objects;

/**
 * What a schedule asks for when an occurrence is due while the one before it is still running: Allow starts it alongside, Forbid skips it, Replace stops the running one and starts it. The value is checked and stored with the schedule, but the scheduler does not yet look for a running occurrence, so every occurrence starts whichever is chosen. An unknown name is a bare 400. Always send it: the gateway reads a missing one as Allow, the first name, which this contract does not promise.
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public enum ConcurrencyPolicy implements ValuedEnum {
    Allow("Allow"),
    Forbid("Forbid"),
    Replace("Replace");
    public final String value;
    ConcurrencyPolicy(final String value) {
        this.value = value;
    }
    @jakarta.annotation.Nonnull
    public String getValue() { return this.value; }
    @jakarta.annotation.Nullable
    public static ConcurrencyPolicy forValue(@jakarta.annotation.Nonnull final String searchValue) {
        Objects.requireNonNull(searchValue);
        switch(searchValue) {
            case "Allow": return Allow;
            case "Forbid": return Forbid;
            case "Replace": return Replace;
            default: return null;
        }
    }
}
