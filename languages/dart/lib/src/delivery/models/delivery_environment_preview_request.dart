// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';

/// auto generated
class DeliveryEnvironmentPreviewRequest implements Parsable {
    ///  The name property
    String? name;
    ///  The templateId property
    UuidValue? templateId;
    ///  The templateVersion property
    int? templateVersion;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryEnvironmentPreviewRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return DeliveryEnvironmentPreviewRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['name'] = (node) => name = node.getStringValue();
        deserializerMap['templateId'] = (node) => templateId = node.getGuidValue();
        deserializerMap['templateVersion'] = (node) => templateVersion = node.getIntValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('name', name);
        writer.writeUuidValue('templateId', templateId);
        writer.writeIntValue('templateVersion', templateVersion);
    }
}
