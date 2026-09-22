// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './capability_id.dart';

/// auto generated
class Capability implements Parsable {
    ///  The available property
    bool? available;
    ///  realtime-presence appears only when requested; it is available exactly when realtime-subscribe is and the runtime's realtime projection enables presence.
    CapabilityId? id;
    ///  The unavailableReason property
    String? unavailableReason;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static Capability createFromDiscriminatorValue(ParseNode parseNode) {
        return Capability();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['available'] = (node) => available = node.getBoolValue();
        deserializerMap['id'] = (node) => id = node.getEnumValue<CapabilityId>((stringValue) => CapabilityId.values.where((enumVal) => enumVal.value == stringValue).firstOrNull);
        deserializerMap['unavailableReason'] = (node) => unavailableReason = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeBoolValue('available', value:available);
        writer.writeEnumValue<CapabilityId>('id', id, (e) => e?.value);
        writer.writeStringValue('unavailableReason', unavailableReason);
    }
}
