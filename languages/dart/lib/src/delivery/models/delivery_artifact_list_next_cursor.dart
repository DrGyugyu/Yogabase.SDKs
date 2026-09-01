// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './delivery_artifact_list_next_cursor_member1.dart';

/// auto generated
/// Composed type wrapper for classes [DeliveryArtifactListNextCursorMember1], [String?]
class DeliveryArtifactListNextCursor implements Parsable {
    ///  Composed type representation for type [DeliveryArtifactListNextCursorMember1]
    DeliveryArtifactListNextCursorMember1? deliveryArtifactListNextCursorMember1;
    ///  Composed type representation for type [String?]
    String? string_;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryArtifactListNextCursor createFromDiscriminatorValue(ParseNode parseNode) {
        var result = DeliveryArtifactListNextCursor();
        if(parseNode.getStringValue() is String) {
            result.string_ = parseNode.getStringValue();
        }
        return result;
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        if(deliveryArtifactListNextCursorMember1 != null) {
            return deliveryArtifactListNextCursorMember1!.getFieldDeserializers();
        }
        return <String, void Function(ParseNode)>{};
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        if(deliveryArtifactListNextCursorMember1 != null) {
            writer.writeObjectValue<DeliveryArtifactListNextCursorMember1>(null, deliveryArtifactListNextCursorMember1);
        }
        else if(string_ != null) {
            writer.writeStringValue(null, string_);
        }
    }
}
