// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './delivery_operation_envelope.dart';
import './delivery_operation_list_next_cursor.dart';

/// auto generated
class DeliveryOperationList implements Parsable {
    ///  The items property
    Iterable<DeliveryOperationEnvelope>? items;
    ///  The nextCursor property
    DeliveryOperationListNextCursor? nextCursor;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryOperationList createFromDiscriminatorValue(ParseNode parseNode) {
        return DeliveryOperationList();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['items'] = (node) => items = node.getCollectionOfObjectValues<DeliveryOperationEnvelope>(DeliveryOperationEnvelope.createFromDiscriminatorValue);
        deserializerMap['nextCursor'] = (node) => nextCursor = node.getObjectValue<DeliveryOperationListNextCursor>(DeliveryOperationListNextCursor.createFromDiscriminatorValue);
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeCollectionOfObjectValues<DeliveryOperationEnvelope>('items', items);
        writer.writeObjectValue<DeliveryOperationListNextCursor>('nextCursor', nextCursor);
    }
}
