package io.yogabase.client.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.time.OffsetDateTime;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class ScheduleTriggerRequest implements Parsable {
    /**
     * When the invocation must finish: later than now, and at most 3,600 seconds from now.
     */
    private OffsetDateTime deadline;
    /**
     * The caller&apos;s key for this run: 1 to 256 characters, with no leading or trailing white space. The same key against the same version of the schedule is the same run.
     */
    private String idempotencyKey;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link ScheduleTriggerRequest}
     */
    @jakarta.annotation.Nonnull
    public static ScheduleTriggerRequest createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new ScheduleTriggerRequest();
    }
    /**
     * Gets the deadline property value. When the invocation must finish: later than now, and at most 3,600 seconds from now.
     * @return a {@link OffsetDateTime}
     */
    @jakarta.annotation.Nullable
    public OffsetDateTime getDeadline() {
        return this.deadline;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(2);
        deserializerMap.put("deadline", (n) -> { this.setDeadline(n.getOffsetDateTimeValue()); });
        deserializerMap.put("idempotencyKey", (n) -> { this.setIdempotencyKey(n.getStringValue()); });
        return deserializerMap;
    }
    /**
     * Gets the idempotencyKey property value. The caller&apos;s key for this run: 1 to 256 characters, with no leading or trailing white space. The same key against the same version of the schedule is the same run.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getIdempotencyKey() {
        return this.idempotencyKey;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeOffsetDateTimeValue("deadline", this.getDeadline());
        writer.writeStringValue("idempotencyKey", this.getIdempotencyKey());
    }
    /**
     * Sets the deadline property value. When the invocation must finish: later than now, and at most 3,600 seconds from now.
     * @param value Value to set for the deadline property.
     */
    public void setDeadline(@jakarta.annotation.Nullable final OffsetDateTime value) {
        this.deadline = value;
    }
    /**
     * Sets the idempotencyKey property value. The caller&apos;s key for this run: 1 to 256 characters, with no leading or trailing white space. The same key against the same version of the schedule is the same run.
     * @param value Value to set for the idempotencyKey property.
     */
    public void setIdempotencyKey(@jakarta.annotation.Nullable final String value) {
        this.idempotencyKey = value;
    }
}
