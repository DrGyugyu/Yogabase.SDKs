// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// auto generated
class DeliveryReleaseConfiguration implements Parsable {
    ///  The allowedTimeZones property
    Iterable<String>? allowedTimeZones;
    ///  The jobs property
    bool? jobs;
    ///  The longest a durable job's function may run, in seconds. Valid only with jobs. Omitting it, or sending 30, is the default.
    int? jobsMaxRunSeconds;
    ///  The maxCatchUp property
    int? maxCatchUp;
    ///  The maxConcurrentExecutions property
    int? maxConcurrentExecutions;
    ///  The scheduler property
    bool? scheduler;
    ///  The statelessFunctions property
    bool? statelessFunctions;
    /// Instantiates a new [DeliveryReleaseConfiguration] and sets the default values.
    DeliveryReleaseConfiguration() :  
        jobsMaxRunSeconds = 30,
        maxCatchUp = 1,
        maxConcurrentExecutions = 1;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryReleaseConfiguration createFromDiscriminatorValue(ParseNode parseNode) {
        return DeliveryReleaseConfiguration();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['allowedTimeZones'] = (node) => allowedTimeZones = node.getCollectionOfPrimitiveValues<String>();
        deserializerMap['jobs'] = (node) => jobs = node.getBoolValue();
        deserializerMap['jobsMaxRunSeconds'] = (node) => jobsMaxRunSeconds = node.getIntValue();
        deserializerMap['maxCatchUp'] = (node) => maxCatchUp = node.getIntValue();
        deserializerMap['maxConcurrentExecutions'] = (node) => maxConcurrentExecutions = node.getIntValue();
        deserializerMap['scheduler'] = (node) => scheduler = node.getBoolValue();
        deserializerMap['statelessFunctions'] = (node) => statelessFunctions = node.getBoolValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeCollectionOfPrimitiveValues<String?>('allowedTimeZones', allowedTimeZones);
        writer.writeBoolValue('jobs', value:jobs);
        writer.writeIntValue('jobsMaxRunSeconds', jobsMaxRunSeconds);
        writer.writeIntValue('maxCatchUp', maxCatchUp);
        writer.writeIntValue('maxConcurrentExecutions', maxConcurrentExecutions);
        writer.writeBoolValue('scheduler', value:scheduler);
        writer.writeBoolValue('statelessFunctions', value:statelessFunctions);
    }
}
