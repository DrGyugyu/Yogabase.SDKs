// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// auto generated
class ActorInvocationRequest implements Parsable {
    ///  The correlationId property
    String? correlationId;
    ///  Must be within 30 seconds of admission.
    DateTime? deadline;
    ///  The idempotencyKey property
    String? idempotencyKey;
    ///  The inlinePayloadBase64 property
    String? inlinePayloadBase64;
    ///  The payloadDigest property
    String? payloadDigest;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static ActorInvocationRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return ActorInvocationRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['correlationId'] = (node) => correlationId = node.getStringValue();
        deserializerMap['deadline'] = (node) => deadline = node.getDateTimeValue();
        deserializerMap['idempotencyKey'] = (node) => idempotencyKey = node.getStringValue();
        deserializerMap['inlinePayloadBase64'] = (node) => inlinePayloadBase64 = node.getStringValue();
        deserializerMap['payloadDigest'] = (node) => payloadDigest = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('correlationId', correlationId);
        writer.writeDateTimeValue('deadline', deadline);
        writer.writeStringValue('idempotencyKey', idempotencyKey);
        writer.writeStringValue('inlinePayloadBase64', inlinePayloadBase64);
        writer.writeStringValue('payloadDigest', payloadDigest);
    }
}
