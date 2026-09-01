// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';

/// auto generated
class DeliveryPreviewResponse implements Parsable {
    ///  The action property
    String? action;
    ///  The approvalRequired property
    bool? approvalRequired;
    ///  The expectedRevision property
    int? expectedRevision;
    ///  The expiresAt property
    DateTime? expiresAt;
    ///  The previewDigest property
    String? previewDigest;
    ///  The previewId property
    UuidValue? previewId;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryPreviewResponse createFromDiscriminatorValue(ParseNode parseNode) {
        return DeliveryPreviewResponse();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['action'] = (node) => action = node.getStringValue();
        deserializerMap['approvalRequired'] = (node) => approvalRequired = node.getBoolValue();
        deserializerMap['expectedRevision'] = (node) => expectedRevision = node.getIntValue();
        deserializerMap['expiresAt'] = (node) => expiresAt = node.getDateTimeValue();
        deserializerMap['previewDigest'] = (node) => previewDigest = node.getStringValue();
        deserializerMap['previewId'] = (node) => previewId = node.getGuidValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('action', action);
        writer.writeBoolValue('approvalRequired', value:approvalRequired);
        writer.writeIntValue('expectedRevision', expectedRevision);
        writer.writeDateTimeValue('expiresAt', expiresAt);
        writer.writeStringValue('previewDigest', previewDigest);
        writer.writeUuidValue('previewId', previewId);
    }
}
