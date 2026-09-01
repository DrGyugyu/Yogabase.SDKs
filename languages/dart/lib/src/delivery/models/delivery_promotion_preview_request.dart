// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';

/// auto generated
class DeliveryPromotionPreviewRequest implements Parsable {
    ///  The migrationEvidenceDigest property
    String? migrationEvidenceDigest;
    ///  The sourceEnvironmentId property
    UuidValue? sourceEnvironmentId;
    ///  The sourceReleaseId property
    UuidValue? sourceReleaseId;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryPromotionPreviewRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return DeliveryPromotionPreviewRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['migrationEvidenceDigest'] = (node) => migrationEvidenceDigest = node.getStringValue();
        deserializerMap['sourceEnvironmentId'] = (node) => sourceEnvironmentId = node.getGuidValue();
        deserializerMap['sourceReleaseId'] = (node) => sourceReleaseId = node.getGuidValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('migrationEvidenceDigest', migrationEvidenceDigest);
        writer.writeUuidValue('sourceEnvironmentId', sourceEnvironmentId);
        writer.writeUuidValue('sourceReleaseId', sourceReleaseId);
    }
}
