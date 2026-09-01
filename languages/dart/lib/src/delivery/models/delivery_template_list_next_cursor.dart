// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './delivery_template_list_next_cursor_member1.dart';

/// auto generated
/// Composed type wrapper for classes [DeliveryTemplateListNextCursorMember1], [String?]
class DeliveryTemplateListNextCursor implements Parsable {
    ///  Composed type representation for type [DeliveryTemplateListNextCursorMember1]
    DeliveryTemplateListNextCursorMember1? deliveryTemplateListNextCursorMember1;
    ///  Composed type representation for type [String?]
    String? string_;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryTemplateListNextCursor createFromDiscriminatorValue(ParseNode parseNode) {
        var result = DeliveryTemplateListNextCursor();
        if(parseNode.getStringValue() is String) {
            result.string_ = parseNode.getStringValue();
        }
        return result;
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        if(deliveryTemplateListNextCursorMember1 != null) {
            return deliveryTemplateListNextCursorMember1!.getFieldDeserializers();
        }
        return <String, void Function(ParseNode)>{};
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        if(deliveryTemplateListNextCursorMember1 != null) {
            writer.writeObjectValue<DeliveryTemplateListNextCursorMember1>(null, deliveryTemplateListNextCursorMember1);
        }
        else if(string_ != null) {
            writer.writeStringValue(null, string_);
        }
    }
}
