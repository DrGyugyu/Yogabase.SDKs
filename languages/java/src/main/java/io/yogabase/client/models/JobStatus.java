package io.yogabase.client.models;

import com.microsoft.kiota.serialization.ValuedEnum;
import java.util.Objects;

/**
 * Queued and Running are in progress. Succeeded, Cancelled and DeadLettered are final: nothing about the job changes again. A failed attempt sends the job back to Queued for its next attempt, or on to DeadLettered once maxAttempts attempts have started. Failed names a failed attempt between those two, is never final, and is not a state the ledger records: a client that reads it treats the job as in progress.
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public enum JobStatus implements ValuedEnum {
    Queued("Queued"),
    Running("Running"),
    Succeeded("Succeeded"),
    Failed("Failed"),
    Cancelled("Cancelled"),
    DeadLettered("DeadLettered");
    public final String value;
    JobStatus(final String value) {
        this.value = value;
    }
    @jakarta.annotation.Nonnull
    public String getValue() { return this.value; }
    @jakarta.annotation.Nullable
    public static JobStatus forValue(@jakarta.annotation.Nonnull final String searchValue) {
        Objects.requireNonNull(searchValue);
        switch(searchValue) {
            case "Queued": return Queued;
            case "Running": return Running;
            case "Succeeded": return Succeeded;
            case "Failed": return Failed;
            case "Cancelled": return Cancelled;
            case "DeadLettered": return DeadLettered;
            default: return null;
        }
    }
}
