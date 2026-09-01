// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './delivery_release_list_next_cursor_member1.dart';

/// auto generated
/// Composed type wrapper for classes [DeliveryReleaseListNextCursorMember1], [String?]
class DeliveryReleaseListNextCursor implements Parsable {
    ///  Composed type representation for type [DeliveryReleaseListNextCursorMember1]
    DeliveryReleaseListNextCursorMember1? deliveryReleaseListNextCursorMember1;
    ///  Composed type representation for type [String?]
    String? string_;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryReleaseListNextCursor createFromDiscriminatorValue(ParseNode parseNode) {
        var result = DeliveryReleaseListNextCursor();
        if(parseNode.getStringValue() is String) {
            result.string_ = parseNode.getStringValue();
        }
        return result;
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        if(deliveryReleaseListNextCursorMember1 != null) {
            return deliveryReleaseListNextCursorMember1!.getFieldDeserializers();
        }
        return <String, void Function(ParseNode)>{};
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        if(deliveryReleaseListNextCursorMember1 != null) {
            writer.writeObjectValue<DeliveryReleaseListNextCursorMember1>(null, deliveryReleaseListNextCursorMember1);
        }
        else if(string_ != null) {
            writer.writeStringValue(null, string_);
        }
    }
}
