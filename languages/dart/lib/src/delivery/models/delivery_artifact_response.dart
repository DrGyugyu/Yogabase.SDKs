// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';

/// auto generated
class DeliveryArtifactResponse implements Parsable {
    ///  The artifactVersion property
    int? artifactVersion;
    ///  The bundleDigest property
    String? bundleDigest;
    ///  The id property
    UuidValue? id;
    ///  The keyId property
    String? keyId;
    ///  The manifestDigest property
    String? manifestDigest;
    ///  The revision property
    int? revision;
    ///  The sbomDigest property
    String? sbomDigest;
    ///  The scanResultDigest property
    String? scanResultDigest;
    ///  The signerId property
    String? signerId;
    ///  The status property
    String? status;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryArtifactResponse createFromDiscriminatorValue(ParseNode parseNode) {
        return DeliveryArtifactResponse();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['artifactVersion'] = (node) => artifactVersion = node.getIntValue();
        deserializerMap['bundleDigest'] = (node) => bundleDigest = node.getStringValue();
        deserializerMap['id'] = (node) => id = node.getGuidValue();
        deserializerMap['keyId'] = (node) => keyId = node.getStringValue();
        deserializerMap['manifestDigest'] = (node) => manifestDigest = node.getStringValue();
        deserializerMap['revision'] = (node) => revision = node.getIntValue();
        deserializerMap['sbomDigest'] = (node) => sbomDigest = node.getStringValue();
        deserializerMap['scanResultDigest'] = (node) => scanResultDigest = node.getStringValue();
        deserializerMap['signerId'] = (node) => signerId = node.getStringValue();
        deserializerMap['status'] = (node) => status = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeIntValue('artifactVersion', artifactVersion);
        writer.writeStringValue('bundleDigest', bundleDigest);
        writer.writeUuidValue('id', id);
        writer.writeStringValue('keyId', keyId);
        writer.writeStringValue('manifestDigest', manifestDigest);
        writer.writeIntValue('revision', revision);
        writer.writeStringValue('sbomDigest', sbomDigest);
        writer.writeStringValue('scanResultDigest', scanResultDigest);
        writer.writeStringValue('signerId', signerId);
        writer.writeStringValue('status', status);
    }
}
