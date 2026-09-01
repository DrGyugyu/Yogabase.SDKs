// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';

/// auto generated
class DeliveryRollbackPreviewRequest implements Parsable {
    ///  The migrationEvidenceDigest property
    String? migrationEvidenceDigest;
    ///  The releaseId property
    UuidValue? releaseId;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryRollbackPreviewRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return DeliveryRollbackPreviewRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['migrationEvidenceDigest'] = (node) => migrationEvidenceDigest = node.getStringValue();
        deserializerMap['releaseId'] = (node) => releaseId = node.getGuidValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('migrationEvidenceDigest', migrationEvidenceDigest);
        writer.writeUuidValue('releaseId', releaseId);
    }
}
