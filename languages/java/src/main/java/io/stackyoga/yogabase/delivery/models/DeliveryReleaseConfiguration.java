package io.stackyoga.yogabase.delivery.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DeliveryReleaseConfiguration implements Parsable {
    /**
     * The allowedTimeZones property
     */
    private java.util.List<String> allowedTimeZones;
    /**
     * The jobs property
     */
    private Boolean jobs;
    /**
     * The maxCatchUp property
     */
    private Integer maxCatchUp;
    /**
     * The maxConcurrentExecutions property
     */
    private Integer maxConcurrentExecutions;
    /**
     * The scheduler property
     */
    private Boolean scheduler;
    /**
     * The statelessFunctions property
     */
    private Boolean statelessFunctions;
    /**
     * Instantiates a new {@link DeliveryReleaseConfiguration} and sets the default values.
     */
    public DeliveryReleaseConfiguration() {
        this.setMaxCatchUp(1);
        this.setMaxConcurrentExecutions(1);
    }
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link DeliveryReleaseConfiguration}
     */
    @jakarta.annotation.Nonnull
    public static DeliveryReleaseConfiguration createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new DeliveryReleaseConfiguration();
    }
    /**
     * Gets the allowedTimeZones property value. The allowedTimeZones property
     * @return a {@link java.util.List<String>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<String> getAllowedTimeZones() {
        return this.allowedTimeZones;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(6);
        deserializerMap.put("allowedTimeZones", (n) -> { this.setAllowedTimeZones(n.getCollectionOfPrimitiveValues(String.class)); });
        deserializerMap.put("jobs", (n) -> { this.setJobs(n.getBooleanValue()); });
        deserializerMap.put("maxCatchUp", (n) -> { this.setMaxCatchUp(n.getIntegerValue()); });
        deserializerMap.put("maxConcurrentExecutions", (n) -> { this.setMaxConcurrentExecutions(n.getIntegerValue()); });
        deserializerMap.put("scheduler", (n) -> { this.setScheduler(n.getBooleanValue()); });
        deserializerMap.put("statelessFunctions", (n) -> { this.setStatelessFunctions(n.getBooleanValue()); });
        return deserializerMap;
    }
    /**
     * Gets the jobs property value. The jobs property
     * @return a {@link Boolean}
     */
    @jakarta.annotation.Nullable
    public Boolean getJobs() {
        return this.jobs;
    }
    /**
     * Gets the maxCatchUp property value. The maxCatchUp property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getMaxCatchUp() {
        return this.maxCatchUp;
    }
    /**
     * Gets the maxConcurrentExecutions property value. The maxConcurrentExecutions property
     * @return a {@link Integer}
     */
    @jakarta.annotation.Nullable
    public Integer getMaxConcurrentExecutions() {
        return this.maxConcurrentExecutions;
    }
    /**
     * Gets the scheduler property value. The scheduler property
     * @return a {@link Boolean}
     */
    @jakarta.annotation.Nullable
    public Boolean getScheduler() {
        return this.scheduler;
    }
    /**
     * Gets the statelessFunctions property value. The statelessFunctions property
     * @return a {@link Boolean}
     */
    @jakarta.annotation.Nullable
    public Boolean getStatelessFunctions() {
        return this.statelessFunctions;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeCollectionOfPrimitiveValues("allowedTimeZones", this.getAllowedTimeZones());
        writer.writeBooleanValue("jobs", this.getJobs());
        writer.writeIntegerValue("maxCatchUp", this.getMaxCatchUp());
        writer.writeIntegerValue("maxConcurrentExecutions", this.getMaxConcurrentExecutions());
        writer.writeBooleanValue("scheduler", this.getScheduler());
        writer.writeBooleanValue("statelessFunctions", this.getStatelessFunctions());
    }
    /**
     * Sets the allowedTimeZones property value. The allowedTimeZones property
     * @param value Value to set for the allowedTimeZones property.
     */
    public void setAllowedTimeZones(@jakarta.annotation.Nullable final java.util.List<String> value) {
        this.allowedTimeZones = value;
    }
    /**
     * Sets the jobs property value. The jobs property
     * @param value Value to set for the jobs property.
     */
    public void setJobs(@jakarta.annotation.Nullable final Boolean value) {
        this.jobs = value;
    }
    /**
     * Sets the maxCatchUp property value. The maxCatchUp property
     * @param value Value to set for the maxCatchUp property.
     */
    public void setMaxCatchUp(@jakarta.annotation.Nullable final Integer value) {
        this.maxCatchUp = value;
    }
    /**
     * Sets the maxConcurrentExecutions property value. The maxConcurrentExecutions property
     * @param value Value to set for the maxConcurrentExecutions property.
     */
    public void setMaxConcurrentExecutions(@jakarta.annotation.Nullable final Integer value) {
        this.maxConcurrentExecutions = value;
    }
    /**
     * Sets the scheduler property value. The scheduler property
     * @param value Value to set for the scheduler property.
     */
    public void setScheduler(@jakarta.annotation.Nullable final Boolean value) {
        this.scheduler = value;
    }
    /**
     * Sets the statelessFunctions property value. The statelessFunctions property
     * @param value Value to set for the statelessFunctions property.
     */
    public void setStatelessFunctions(@jakarta.annotation.Nullable final Boolean value) {
        this.statelessFunctions = value;
    }
}
