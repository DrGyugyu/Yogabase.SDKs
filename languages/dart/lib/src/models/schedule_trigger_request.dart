// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// auto generated
class ScheduleTriggerRequest implements Parsable {
    ///  When the invocation must finish: later than now, and at most 3,600 seconds from now.
    DateTime? deadline;
    ///  The caller's key for this run: 1 to 256 characters, with no leading or trailing white space. The same key against the same version of the schedule is the same run.
    String? idempotencyKey;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static ScheduleTriggerRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return ScheduleTriggerRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['deadline'] = (node) => deadline = node.getDateTimeValue();
        deserializerMap['idempotencyKey'] = (node) => idempotencyKey = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeDateTimeValue('deadline', deadline);
        writer.writeStringValue('idempotencyKey', idempotencyKey);
    }
}
