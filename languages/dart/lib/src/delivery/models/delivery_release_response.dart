// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';
import './delivery_release_response_source_release_id.dart';

/// auto generated
class DeliveryReleaseResponse implements Parsable {
    ///  The artifactSetDigest property
    String? artifactSetDigest;
    ///  The configurationDigest property
    String? configurationDigest;
    ///  The desiredRuntimeRevision property
    int? desiredRuntimeRevision;
    ///  The id property
    UuidValue? id;
    ///  The migrationEvidenceDigest property
    String? migrationEvidenceDigest;
    ///  The releaseType property
    String? releaseType;
    ///  The revision property
    int? revision;
    ///  The sourceReleaseId property
    DeliveryReleaseResponseSourceReleaseId? sourceReleaseId;
    ///  The status property
    String? status;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryReleaseResponse createFromDiscriminatorValue(ParseNode parseNode) {
        return DeliveryReleaseResponse();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['artifactSetDigest'] = (node) => artifactSetDigest = node.getStringValue();
        deserializerMap['configurationDigest'] = (node) => configurationDigest = node.getStringValue();
        deserializerMap['desiredRuntimeRevision'] = (node) => desiredRuntimeRevision = node.getIntValue();
        deserializerMap['id'] = (node) => id = node.getGuidValue();
        deserializerMap['migrationEvidenceDigest'] = (node) => migrationEvidenceDigest = node.getStringValue();
        deserializerMap['releaseType'] = (node) => releaseType = node.getStringValue();
        deserializerMap['revision'] = (node) => revision = node.getIntValue();
        deserializerMap['sourceReleaseId'] = (node) => sourceReleaseId = node.getObjectValue<DeliveryReleaseResponseSourceReleaseId>(DeliveryReleaseResponseSourceReleaseId.createFromDiscriminatorValue);
        deserializerMap['status'] = (node) => status = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('artifactSetDigest', artifactSetDigest);
        writer.writeStringValue('configurationDigest', configurationDigest);
        writer.writeIntValue('desiredRuntimeRevision', desiredRuntimeRevision);
        writer.writeUuidValue('id', id);
        writer.writeStringValue('migrationEvidenceDigest', migrationEvidenceDigest);
        writer.writeStringValue('releaseType', releaseType);
        writer.writeIntValue('revision', revision);
        writer.writeObjectValue<DeliveryReleaseResponseSourceReleaseId>('sourceReleaseId', sourceReleaseId);
        writer.writeStringValue('status', status);
    }
}
