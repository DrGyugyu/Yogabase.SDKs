// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';

/// auto generated
class DeliverySubmitRequest implements Parsable {
    ///  The previewDigest property
    String? previewDigest;
    ///  The previewId property
    UuidValue? previewId;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliverySubmitRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return DeliverySubmitRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['previewDigest'] = (node) => previewDigest = node.getStringValue();
        deserializerMap['previewId'] = (node) => previewId = node.getGuidValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('previewDigest', previewDigest);
        writer.writeUuidValue('previewId', previewId);
    }
}
