// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';
import './delivery_release_configuration.dart';

/// auto generated
class DeliveryReleasePreviewRequest implements Parsable {
    ///  The artifactAdmissionIds property
    Iterable<UuidValue>? artifactAdmissionIds;
    ///  The configuration property
    DeliveryReleaseConfiguration? configuration;
    ///  The migrationEvidenceDigest property
    String? migrationEvidenceDigest;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryReleasePreviewRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return DeliveryReleasePreviewRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['artifactAdmissionIds'] = (node) => artifactAdmissionIds = node.getCollectionOfPrimitiveValues<UuidValue>();
        deserializerMap['configuration'] = (node) => configuration = node.getObjectValue<DeliveryReleaseConfiguration>(DeliveryReleaseConfiguration.createFromDiscriminatorValue);
        deserializerMap['migrationEvidenceDigest'] = (node) => migrationEvidenceDigest = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeCollectionOfPrimitiveValues<UuidValue>('artifactAdmissionIds', artifactAdmissionIds);
        writer.writeObjectValue<DeliveryReleaseConfiguration>('configuration', configuration);
        writer.writeStringValue('migrationEvidenceDigest', migrationEvidenceDigest);
    }
}
