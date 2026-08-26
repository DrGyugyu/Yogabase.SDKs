// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// auto generated
class ObjectWriteResult implements Parsable {
    ///  The alreadyExisted property
    bool? alreadyExisted;
    ///  The digest property
    String? digest;
    ///  The encryption property
    String? encryption;
    ///  The key property
    String? key;
    ///  The sizeBytes property
    int? sizeBytes;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static ObjectWriteResult createFromDiscriminatorValue(ParseNode parseNode) {
        return ObjectWriteResult();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['alreadyExisted'] = (node) => alreadyExisted = node.getBoolValue();
        deserializerMap['digest'] = (node) => digest = node.getStringValue();
        deserializerMap['encryption'] = (node) => encryption = node.getStringValue();
        deserializerMap['key'] = (node) => key = node.getStringValue();
        deserializerMap['sizeBytes'] = (node) => sizeBytes = node.getIntValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeBoolValue('alreadyExisted', value:alreadyExisted);
        writer.writeStringValue('digest', digest);
        writer.writeStringValue('encryption', encryption);
        writer.writeStringValue('key', key);
        writer.writeIntValue('sizeBytes', sizeBytes);
    }
}
