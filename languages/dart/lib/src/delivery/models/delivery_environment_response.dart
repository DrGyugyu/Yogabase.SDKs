// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';
import './delivery_environment_response_current_release_id.dart';

/// auto generated
class DeliveryEnvironmentResponse implements Parsable {
    ///  The currentReleaseId property
    DeliveryEnvironmentResponseCurrentReleaseId? currentReleaseId;
    ///  The id property
    UuidValue? id;
    ///  The isProduction property
    bool? isProduction;
    ///  The lifecycle property
    String? lifecycle;
    ///  The name property
    String? name;
    ///  The revision property
    int? revision;
    ///  The runtimeRevision property
    int? runtimeRevision;
    ///  The slug property
    String? slug;
    ///  The templateId property
    UuidValue? templateId;
    ///  The templateVersion property
    int? templateVersion;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryEnvironmentResponse createFromDiscriminatorValue(ParseNode parseNode) {
        return DeliveryEnvironmentResponse();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['currentReleaseId'] = (node) => currentReleaseId = node.getObjectValue<DeliveryEnvironmentResponseCurrentReleaseId>(DeliveryEnvironmentResponseCurrentReleaseId.createFromDiscriminatorValue);
        deserializerMap['id'] = (node) => id = node.getGuidValue();
        deserializerMap['isProduction'] = (node) => isProduction = node.getBoolValue();
        deserializerMap['lifecycle'] = (node) => lifecycle = node.getStringValue();
        deserializerMap['name'] = (node) => name = node.getStringValue();
        deserializerMap['revision'] = (node) => revision = node.getIntValue();
        deserializerMap['runtimeRevision'] = (node) => runtimeRevision = node.getIntValue();
        deserializerMap['slug'] = (node) => slug = node.getStringValue();
        deserializerMap['templateId'] = (node) => templateId = node.getGuidValue();
        deserializerMap['templateVersion'] = (node) => templateVersion = node.getIntValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeObjectValue<DeliveryEnvironmentResponseCurrentReleaseId>('currentReleaseId', currentReleaseId);
        writer.writeUuidValue('id', id);
        writer.writeBoolValue('isProduction', value:isProduction);
        writer.writeStringValue('lifecycle', lifecycle);
        writer.writeStringValue('name', name);
        writer.writeIntValue('revision', revision);
        writer.writeIntValue('runtimeRevision', runtimeRevision);
        writer.writeStringValue('slug', slug);
        writer.writeUuidValue('templateId', templateId);
        writer.writeIntValue('templateVersion', templateVersion);
    }
}
