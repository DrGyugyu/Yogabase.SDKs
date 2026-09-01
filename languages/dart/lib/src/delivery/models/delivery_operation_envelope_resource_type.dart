// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './delivery_operation_envelope_resource_type_member1.dart';

/// auto generated
/// Composed type wrapper for classes [DeliveryOperationEnvelopeResourceTypeMember1], [String?]
class DeliveryOperationEnvelopeResourceType implements Parsable {
    ///  Composed type representation for type [DeliveryOperationEnvelopeResourceTypeMember1]
    DeliveryOperationEnvelopeResourceTypeMember1? deliveryOperationEnvelopeResourceTypeMember1;
    ///  Composed type representation for type [String?]
    String? string_;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryOperationEnvelopeResourceType createFromDiscriminatorValue(ParseNode parseNode) {
        var result = DeliveryOperationEnvelopeResourceType();
        if(parseNode.getStringValue() is String) {
            result.string_ = parseNode.getStringValue();
        }
        return result;
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        if(deliveryOperationEnvelopeResourceTypeMember1 != null) {
            return deliveryOperationEnvelopeResourceTypeMember1!.getFieldDeserializers();
        }
        return <String, void Function(ParseNode)>{};
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        if(deliveryOperationEnvelopeResourceTypeMember1 != null) {
            writer.writeObjectValue<DeliveryOperationEnvelopeResourceTypeMember1>(null, deliveryOperationEnvelopeResourceTypeMember1);
        }
        else if(string_ != null) {
            writer.writeStringValue(null, string_);
        }
    }
}
