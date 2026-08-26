// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './invocation_request_object_reference.dart';

/// auto generated
class InvocationRequest implements Parsable {
    ///  The correlationId property
    String? correlationId;
    ///  The deadline property
    DateTime? deadline;
    ///  The idempotencyKey property
    String? idempotencyKey;
    ///  The inlinePayloadBase64 property
    String? inlinePayloadBase64;
    ///  The mode property
    int? mode;
    ///  The objectReference property
    InvocationRequestObjectReference? objectReference;
    ///  The payloadDigest property
    String? payloadDigest;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static InvocationRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return InvocationRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['correlationId'] = (node) => correlationId = node.getStringValue();
        deserializerMap['deadline'] = (node) => deadline = node.getDateTimeValue();
        deserializerMap['idempotencyKey'] = (node) => idempotencyKey = node.getStringValue();
        deserializerMap['inlinePayloadBase64'] = (node) => inlinePayloadBase64 = node.getStringValue();
        deserializerMap['mode'] = (node) => mode = node.getIntValue();
        deserializerMap['objectReference'] = (node) => objectReference = node.getObjectValue<InvocationRequestObjectReference>(InvocationRequestObjectReference.createFromDiscriminatorValue);
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
        writer.writeIntValue('mode', mode);
        writer.writeObjectValue<InvocationRequestObjectReference>('objectReference', objectReference);
        writer.writeStringValue('payloadDigest', payloadDigest);
    }
}
