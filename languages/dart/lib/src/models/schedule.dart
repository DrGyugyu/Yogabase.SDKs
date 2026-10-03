// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';

/// auto generated
class Schedule implements Parsable {
    ///  The functionId property
    String? functionId;
    ///  The functionVersion property
    String? functionVersion;
    ///  Lasts until the schedule is deleted: a schedule deleted and created again has a new one.
    UuidValue? instanceId;
    ///  How far the scheduler has got: when the schedule was created, then the last occurrence it ran or skipped past. Null for a one-shot.
    DateTime? lastOccurrence;
    ///  The instant a one-shot runs; null for a recurring schedule.
    DateTime? oneShotAtUtc;
    ///  The projectId property
    UuidValue? projectId;
    ///  The scheduleId property
    String? scheduleId;
    ///  The owner: the sub of the user who created the schedule.
    UuidValue? subject;
    ///  The schedule's version as decimal text, because it is a 64-bit number a JSON number cannot carry exactly. It changes whenever the schedule does and is not a counter: read it from the ETag, which carries it with the instance.
    String? version;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static Schedule createFromDiscriminatorValue(ParseNode parseNode) {
        return Schedule();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['functionId'] = (node) => functionId = node.getStringValue();
        deserializerMap['functionVersion'] = (node) => functionVersion = node.getStringValue();
        deserializerMap['instanceId'] = (node) => instanceId = node.getGuidValue();
        deserializerMap['lastOccurrence'] = (node) => lastOccurrence = node.getDateTimeValue();
        deserializerMap['oneShotAtUtc'] = (node) => oneShotAtUtc = node.getDateTimeValue();
        deserializerMap['projectId'] = (node) => projectId = node.getGuidValue();
        deserializerMap['scheduleId'] = (node) => scheduleId = node.getStringValue();
        deserializerMap['subject'] = (node) => subject = node.getGuidValue();
        deserializerMap['version'] = (node) => version = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('functionId', functionId);
        writer.writeStringValue('functionVersion', functionVersion);
        writer.writeUuidValue('instanceId', instanceId);
        writer.writeDateTimeValue('lastOccurrence', lastOccurrence);
        writer.writeDateTimeValue('oneShotAtUtc', oneShotAtUtc);
        writer.writeUuidValue('projectId', projectId);
        writer.writeStringValue('scheduleId', scheduleId);
        writer.writeUuidValue('subject', subject);
        writer.writeStringValue('version', version);
    }
}
