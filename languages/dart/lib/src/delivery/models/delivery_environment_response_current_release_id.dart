// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './delivery_environment_response_current_release_id_member1.dart';
import './uuid.dart';

/// auto generated
/// Composed type wrapper for classes [DeliveryEnvironmentResponseCurrentReleaseIdMember1], [Uuid]
class DeliveryEnvironmentResponseCurrentReleaseId implements Parsable {
    ///  Composed type representation for type [DeliveryEnvironmentResponseCurrentReleaseIdMember1]
    DeliveryEnvironmentResponseCurrentReleaseIdMember1? deliveryEnvironmentResponseCurrentReleaseIdMember1;
    ///  Composed type representation for type [Uuid]
    Uuid? uuid;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryEnvironmentResponseCurrentReleaseId createFromDiscriminatorValue(ParseNode parseNode) {
        var result = DeliveryEnvironmentResponseCurrentReleaseId();
        return result;
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        if(deliveryEnvironmentResponseCurrentReleaseIdMember1 != null) {
            return deliveryEnvironmentResponseCurrentReleaseIdMember1!.getFieldDeserializers();
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
        if(deliveryEnvironmentResponseCurrentReleaseIdMember1 != null) {
            writer.writeObjectValue<DeliveryEnvironmentResponseCurrentReleaseIdMember1>(null, deliveryEnvironmentResponseCurrentReleaseIdMember1);
        }
        else if(uuid != null) {
            writer.writeObjectValue<Uuid>(null, uuid);
        }
    }
}
