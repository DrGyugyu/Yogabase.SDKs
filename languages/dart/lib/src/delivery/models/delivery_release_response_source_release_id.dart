// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './delivery_release_response_source_release_id_member1.dart';
import './uuid.dart';

/// auto generated
/// Composed type wrapper for classes [DeliveryReleaseResponseSourceReleaseIdMember1], [Uuid]
class DeliveryReleaseResponseSourceReleaseId implements Parsable {
    ///  Composed type representation for type [DeliveryReleaseResponseSourceReleaseIdMember1]
    DeliveryReleaseResponseSourceReleaseIdMember1? deliveryReleaseResponseSourceReleaseIdMember1;
    ///  Composed type representation for type [Uuid]
    Uuid? uuid;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryReleaseResponseSourceReleaseId createFromDiscriminatorValue(ParseNode parseNode) {
        var result = DeliveryReleaseResponseSourceReleaseId();
        return result;
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        if(deliveryReleaseResponseSourceReleaseIdMember1 != null) {
            return deliveryReleaseResponseSourceReleaseIdMember1!.getFieldDeserializers();
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
        if(deliveryReleaseResponseSourceReleaseIdMember1 != null) {
            writer.writeObjectValue<DeliveryReleaseResponseSourceReleaseIdMember1>(null, deliveryReleaseResponseSourceReleaseIdMember1);
        }
        else if(uuid != null) {
            writer.writeObjectValue<Uuid>(null, uuid);
        }
    }
}
