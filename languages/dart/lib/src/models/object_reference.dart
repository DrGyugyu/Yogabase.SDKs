// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// auto generated
class ObjectReference implements Parsable {
    ///  The uri property
    String? uri;
    ///  The version property
    String? version;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static ObjectReference createFromDiscriminatorValue(ParseNode parseNode) {
        return ObjectReference();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['uri'] = (node) => uri = node.getStringValue();
        deserializerMap['version'] = (node) => version = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('uri', uri);
        writer.writeStringValue('version', version);
    }
}
