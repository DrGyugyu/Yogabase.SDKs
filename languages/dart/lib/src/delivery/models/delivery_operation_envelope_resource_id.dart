// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './delivery_operation_envelope_resource_id_member1.dart';
import './uuid.dart';

/// auto generated
/// Composed type wrapper for classes [DeliveryOperationEnvelopeResourceIdMember1], [Uuid]
class DeliveryOperationEnvelopeResourceId implements Parsable {
    ///  Composed type representation for type [DeliveryOperationEnvelopeResourceIdMember1]
    DeliveryOperationEnvelopeResourceIdMember1? deliveryOperationEnvelopeResourceIdMember1;
    ///  Composed type representation for type [Uuid]
    Uuid? uuid;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryOperationEnvelopeResourceId createFromDiscriminatorValue(ParseNode parseNode) {
        var result = DeliveryOperationEnvelopeResourceId();
        return result;
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        if(deliveryOperationEnvelopeResourceIdMember1 != null) {
            return deliveryOperationEnvelopeResourceIdMember1!.getFieldDeserializers();
        }
        else if(uuid != null) {
            return uuid!.getFieldDeserializers();
        }
        return <String, void Function(ParseNode)>{};
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        if(deliveryOperationEnvelopeResourceIdMember1 != null) {
            writer.writeObjectValue<DeliveryOperationEnvelopeResourceIdMember1>(null, deliveryOperationEnvelopeResourceIdMember1);
        }
        else if(uuid != null) {
            writer.writeObjectValue<Uuid>(null, uuid);
        }
    }
}
