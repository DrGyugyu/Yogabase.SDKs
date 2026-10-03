// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// auto generated
class JobSubmitRequest implements Parsable {
    ///  The caller's key for this request: trimmed, with no control character. It belongs to the signed-in user, who may repeat it for the same request and for no other.
    String? idempotencyKey;
    ///  The id of a deployed stateless function; its highest released version runs the job.
    String? jobType;
    ///  How many times the job may be attempted. Absent or null is 5. The idempotency key binds it, with the job type and the payload.
    int? maxAttempts;
    ///  The function's request, at most 1,048,576 bytes before base64 encoding. Absent, null or empty is a job that carries none.
    String? payloadBase64;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static JobSubmitRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return JobSubmitRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['idempotencyKey'] = (node) => idempotencyKey = node.getStringValue();
        deserializerMap['jobType'] = (node) => jobType = node.getStringValue();
        deserializerMap['maxAttempts'] = (node) => maxAttempts = node.getIntValue();
        deserializerMap['payloadBase64'] = (node) => payloadBase64 = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('idempotencyKey', idempotencyKey);
        writer.writeStringValue('jobType', jobType);
        writer.writeIntValue('maxAttempts', maxAttempts);
        writer.writeStringValue('payloadBase64', payloadBase64);
    }
}
