// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// auto generated
class WorkflowStartRequest implements Parsable {
    ///  The idempotencyKey property
    String? idempotencyKey;
    ///  The inlineInputBase64 property
    String? inlineInputBase64;
    ///  The payloadDigest property
    String? payloadDigest;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static WorkflowStartRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return WorkflowStartRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['idempotencyKey'] = (node) => idempotencyKey = node.getStringValue();
        deserializerMap['inlineInputBase64'] = (node) => inlineInputBase64 = node.getStringValue();
        deserializerMap['payloadDigest'] = (node) => payloadDigest = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('idempotencyKey', idempotencyKey);
        writer.writeStringValue('inlineInputBase64', inlineInputBase64);
        writer.writeStringValue('payloadDigest', payloadDigest);
    }
}
