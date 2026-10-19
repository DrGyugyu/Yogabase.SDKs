package io.yogabase.client.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class ScheduleTriggerResult implements Parsable {
    /**
     * The invocation&apos;s id. It derives from the schedule&apos;s id, instance and version and the idempotency key, so a repeat has the same one.
     */
    private UUID invocationId;
    /**
     * 0 when this request enqueued the invocation, 1 when it found the one the same key had already made.
     */
    private Integer outcome;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link ScheduleTriggerResult}
     */
    @jakarta.annotation.Nonnull
    public static ScheduleTriggerResult createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new ScheduleTriggerResult();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(2);
        deserializerMap.put("invocationId", (n) -> { this.setInvocationId(n.getUUIDValue()); });
        deserializerMap.put("outcome", (n) -> { this.setOutcome(n.getIntegerValue()); });
        return deserializerMap;
    }
    /**
     * Gets the invocationId property value. The invocation&apos;s id. It derives from the schedule&apos;s id, instance and version and the idempotency key, so a repeat has the same one.
     * @return a {@link UUID}
     */
    @jakarta.annotation.Nullable
    public UUID getInvocationId() {
        return this.invocationId;
    }
    /**
     * Gets the outcome property value. 0 when this request enqueued the invocation, 1 when it found the one the same key had already made.
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getOutcome() {
        return this.outcome;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeUUIDValue("invocationId", this.getInvocationId());
        writer.writeIntegerValue("outcome", this.getOutcome());
    }
    /**
     * Sets the invocationId property value. The invocation&apos;s id. It derives from the schedule&apos;s id, instance and version and the idempotency key, so a repeat has the same one.
     * @param value Value to set for the invocationId property.
     */
    public void setInvocationId(@jakarta.annotation.Nullable final UUID value) {
        this.invocationId = value;
    }
    /**
     * Sets the outcome property value. 0 when this request enqueued the invocation, 1 when it found the one the same key had already made.
     * @param value Value to set for the outcome property.
     */
    public void setOutcome(@jakarta.annotation.Nullable final Integer value) {
        this.outcome = value;
    }
}
