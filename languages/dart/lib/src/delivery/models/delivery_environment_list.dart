// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './delivery_environment_list_next_cursor.dart';
import './delivery_environment_response.dart';

/// auto generated
class DeliveryEnvironmentList implements Parsable {
    ///  The items property
    Iterable<DeliveryEnvironmentResponse>? items;
    ///  The nextCursor property
    DeliveryEnvironmentListNextCursor? nextCursor;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryEnvironmentList createFromDiscriminatorValue(ParseNode parseNode) {
        return DeliveryEnvironmentList();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['items'] = (node) => items = node.getCollectionOfObjectValues<DeliveryEnvironmentResponse>(DeliveryEnvironmentResponse.createFromDiscriminatorValue);
        deserializerMap['nextCursor'] = (node) => nextCursor = node.getObjectValue<DeliveryEnvironmentListNextCursor>(DeliveryEnvironmentListNextCursor.createFromDiscriminatorValue);
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeCollectionOfObjectValues<DeliveryEnvironmentResponse>('items', items);
        writer.writeObjectValue<DeliveryEnvironmentListNextCursor>('nextCursor', nextCursor);
    }
}
