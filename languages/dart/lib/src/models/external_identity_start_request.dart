// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// auto generated
class ExternalIdentityStartRequest implements Parsable {
    ///  Base64url of the SHA-256 of the client's verifier, without padding.
    String? codeChallenge;
    ///  The redirectUri property
    String? redirectUri;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static ExternalIdentityStartRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return ExternalIdentityStartRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['codeChallenge'] = (node) => codeChallenge = node.getStringValue();
        deserializerMap['redirectUri'] = (node) => redirectUri = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('codeChallenge', codeChallenge);
        writer.writeStringValue('redirectUri', redirectUri);
    }
}
