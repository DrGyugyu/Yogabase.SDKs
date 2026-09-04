// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';
import './delivery_template_response_hosted_service_policy_digest.dart';

/// auto generated
class DeliveryTemplateResponse implements Parsable {
    ///  The hostedServicePolicyDigest property
    DeliveryTemplateResponseHostedServicePolicyDigest? hostedServicePolicyDigest;
    ///  The id property
    UuidValue? id;
    ///  The name property
    String? name;
    ///  The revision property
    int? revision;
    ///  The state property
    String? state;
    ///  The targetId property
    UuidValue? targetId;
    ///  The templateDigest property
    String? templateDigest;
    ///  The version property
    int? version;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryTemplateResponse createFromDiscriminatorValue(ParseNode parseNode) {
        return DeliveryTemplateResponse();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['hostedServicePolicyDigest'] = (node) => hostedServicePolicyDigest = node.getObjectValue<DeliveryTemplateResponseHostedServicePolicyDigest>(DeliveryTemplateResponseHostedServicePolicyDigest.createFromDiscriminatorValue);
        deserializerMap['id'] = (node) => id = node.getGuidValue();
        deserializerMap['name'] = (node) => name = node.getStringValue();
        deserializerMap['revision'] = (node) => revision = node.getIntValue();
        deserializerMap['state'] = (node) => state = node.getStringValue();
        deserializerMap['targetId'] = (node) => targetId = node.getGuidValue();
        deserializerMap['templateDigest'] = (node) => templateDigest = node.getStringValue();
        deserializerMap['version'] = (node) => version = node.getIntValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeObjectValue<DeliveryTemplateResponseHostedServicePolicyDigest>('hostedServicePolicyDigest', hostedServicePolicyDigest);
        writer.writeUuidValue('id', id);
        writer.writeStringValue('name', name);
        writer.writeIntValue('revision', revision);
        writer.writeStringValue('state', state);
        writer.writeUuidValue('targetId', targetId);
        writer.writeStringValue('templateDigest', templateDigest);
        writer.writeIntValue('version', version);
    }
}
