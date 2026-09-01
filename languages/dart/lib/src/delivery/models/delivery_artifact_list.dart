// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './delivery_artifact_list_next_cursor.dart';
import './delivery_artifact_response.dart';

/// auto generated
class DeliveryArtifactList implements Parsable {
    ///  The items property
    Iterable<DeliveryArtifactResponse>? items;
    ///  The nextCursor property
    DeliveryArtifactListNextCursor? nextCursor;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryArtifactList createFromDiscriminatorValue(ParseNode parseNode) {
        return DeliveryArtifactList();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['items'] = (node) => items = node.getCollectionOfObjectValues<DeliveryArtifactResponse>(DeliveryArtifactResponse.createFromDiscriminatorValue);
        deserializerMap['nextCursor'] = (node) => nextCursor = node.getObjectValue<DeliveryArtifactListNextCursor>(DeliveryArtifactListNextCursor.createFromDiscriminatorValue);
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeCollectionOfObjectValues<DeliveryArtifactResponse>('items', items);
        writer.writeObjectValue<DeliveryArtifactListNextCursor>('nextCursor', nextCursor);
    }
}
