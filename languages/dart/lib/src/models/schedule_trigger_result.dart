// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';

/// auto generated
class ScheduleTriggerResult implements Parsable {
    ///  The invocation's id. It derives from the schedule's id, instance and version and the idempotency key, so a repeat has the same one.
    UuidValue? invocationId;
    ///  0 when this request enqueued the invocation, 1 when it found the one the same key had already made.
    int? outcome;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static ScheduleTriggerResult createFromDiscriminatorValue(ParseNode parseNode) {
        return ScheduleTriggerResult();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['invocationId'] = (node) => invocationId = node.getGuidValue();
        deserializerMap['outcome'] = (node) => outcome = node.getIntValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeUuidValue('invocationId', invocationId);
        writer.writeIntValue('outcome', outcome);
    }
}
