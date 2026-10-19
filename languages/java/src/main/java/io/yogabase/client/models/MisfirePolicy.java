package io.yogabase.client.models;

import com.microsoft.kiota.serialization.ValuedEnum;
import java.util.Objects;

/**
 * What the scheduler does about occurrences it missed while it was unavailable. RunImmediately runs once, now, however many were missed, and resumes the cadence. SkipToNextOccurrence abandons what it missed and resumes at the next occurrence. RunAllMissed runs each missed occurrence, oldest first, up to maxCatchUp. An unknown name is a bare 400. Required: a request that leaves it out is a 400 whose problem names the member.
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public enum MisfirePolicy implements ValuedEnum {
    RunImmediately("RunImmediately"),
    SkipToNextOccurrence("SkipToNextOccurrence"),
    RunAllMissed("RunAllMissed");
    public final String value;
    MisfirePolicy(final String value) {
        this.value = value;
    }
    @jakarta.annotation.Nonnull
    public String getValue() { return this.value; }
    @jakarta.annotation.Nullable
    public static MisfirePolicy forValue(@jakarta.annotation.Nonnull final String searchValue) {
        Objects.requireNonNull(searchValue);
        switch(searchValue) {
            case "RunImmediately": return RunImmediately;
            case "SkipToNextOccurrence": return SkipToNextOccurrence;
            case "RunAllMissed": return RunAllMissed;
            default: return null;
        }
    }
}
