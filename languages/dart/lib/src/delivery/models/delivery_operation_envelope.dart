// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';
import './delivery_operation_envelope_resource_id.dart';
import './delivery_operation_envelope_resource_type.dart';

/// auto generated
class DeliveryOperationEnvelope implements Parsable {
    ///  The kind property
    String? kind;
    ///  The operationId property
    UuidValue? operationId;
    ///  The resourceId property
    DeliveryOperationEnvelopeResourceId? resourceId;
    ///  The resourceType property
    DeliveryOperationEnvelopeResourceType? resourceType;
    ///  The revision property
    int? revision;
    ///  The state property
    String? state;
    ///  The updatedAt property
    DateTime? updatedAt;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryOperationEnvelope createFromDiscriminatorValue(ParseNode parseNode) {
        return DeliveryOperationEnvelope();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['kind'] = (node) => kind = node.getStringValue();
        deserializerMap['operationId'] = (node) => operationId = node.getGuidValue();
        deserializerMap['resourceId'] = (node) => resourceId = node.getObjectValue<DeliveryOperationEnvelopeResourceId>(DeliveryOperationEnvelopeResourceId.createFromDiscriminatorValue);
        deserializerMap['resourceType'] = (node) => resourceType = node.getObjectValue<DeliveryOperationEnvelopeResourceType>(DeliveryOperationEnvelopeResourceType.createFromDiscriminatorValue);
        deserializerMap['revision'] = (node) => revision = node.getIntValue();
        deserializerMap['state'] = (node) => state = node.getStringValue();
        deserializerMap['updatedAt'] = (node) => updatedAt = node.getDateTimeValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('kind', kind);
        writer.writeUuidValue('operationId', operationId);
        writer.writeObjectValue<DeliveryOperationEnvelopeResourceId>('resourceId', resourceId);
        writer.writeObjectValue<DeliveryOperationEnvelopeResourceType>('resourceType', resourceType);
        writer.writeIntValue('revision', revision);
        writer.writeStringValue('state', state);
        writer.writeDateTimeValue('updatedAt', updatedAt);
    }
}
