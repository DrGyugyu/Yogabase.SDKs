// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// auto generated
class RecoveryCodesResponse implements Parsable {
    ///  The recoveryCodes property
    Iterable<String>? recoveryCodes;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static RecoveryCodesResponse createFromDiscriminatorValue(ParseNode parseNode) {
        return RecoveryCodesResponse();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['recoveryCodes'] = (node) => recoveryCodes = node.getCollectionOfPrimitiveValues<String>();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeCollectionOfPrimitiveValues<String?>('recoveryCodes', recoveryCodes);
    }
}
