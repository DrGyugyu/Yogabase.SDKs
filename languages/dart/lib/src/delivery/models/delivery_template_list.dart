// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './delivery_template_list_next_cursor.dart';
import './delivery_template_response.dart';

/// auto generated
class DeliveryTemplateList implements Parsable {
    ///  The items property
    Iterable<DeliveryTemplateResponse>? items;
    ///  The nextCursor property
    DeliveryTemplateListNextCursor? nextCursor;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryTemplateList createFromDiscriminatorValue(ParseNode parseNode) {
        return DeliveryTemplateList();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['items'] = (node) => items = node.getCollectionOfObjectValues<DeliveryTemplateResponse>(DeliveryTemplateResponse.createFromDiscriminatorValue);
        deserializerMap['nextCursor'] = (node) => nextCursor = node.getObjectValue<DeliveryTemplateListNextCursor>(DeliveryTemplateListNextCursor.createFromDiscriminatorValue);
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeCollectionOfObjectValues<DeliveryTemplateResponse>('items', items);
        writer.writeObjectValue<DeliveryTemplateListNextCursor>('nextCursor', nextCursor);
    }
}
