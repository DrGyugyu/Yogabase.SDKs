package io.yogabase.client.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class WorkflowStartResult implements Parsable {
    /**
     * The workflowInstanceId property
     */
    private UUID workflowInstanceId;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link WorkflowStartResult}
     */
    @jakarta.annotation.Nonnull
    public static WorkflowStartResult createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new WorkflowStartResult();
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(1);
        deserializerMap.put("workflowInstanceId", (n) -> { this.setWorkflowInstanceId(n.getUUIDValue()); });
        return deserializerMap;
    }
    /**
     * Gets the workflowInstanceId property value. The workflowInstanceId property
     * @return a {@link UUID}
     */
    @jakarta.annotation.Nullable
    public UUID getWorkflowInstanceId() {
        return this.workflowInstanceId;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeUUIDValue("workflowInstanceId", this.getWorkflowInstanceId());
    }
    /**
     * Sets the workflowInstanceId property value. The workflowInstanceId property
     * @param value Value to set for the workflowInstanceId property.
     */
    public void setWorkflowInstanceId(@jakarta.annotation.Nullable final UUID value) {
        this.workflowInstanceId = value;
    }
}
