// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';

/// auto generated
class EmailConfirmationRequest implements Parsable {
    ///  The token property
    String? token;
    ///  The userId property
    UuidValue? userId;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static EmailConfirmationRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return EmailConfirmationRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['token'] = (node) => token = node.getStringValue();
        deserializerMap['userId'] = (node) => userId = node.getGuidValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('token', token);
        writer.writeUuidValue('userId', userId);
    }
}
