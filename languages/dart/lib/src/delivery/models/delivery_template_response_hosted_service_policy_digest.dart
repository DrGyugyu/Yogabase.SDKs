// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './delivery_template_response_hosted_service_policy_digest_member1.dart';
import './sha256_digest.dart';

/// auto generated
/// Composed type wrapper for classes [DeliveryTemplateResponseHostedServicePolicyDigestMember1], [Sha256Digest]
class DeliveryTemplateResponseHostedServicePolicyDigest implements Parsable {
    ///  Composed type representation for type [DeliveryTemplateResponseHostedServicePolicyDigestMember1]
    DeliveryTemplateResponseHostedServicePolicyDigestMember1? deliveryTemplateResponseHostedServicePolicyDigestMember1;
    ///  Composed type representation for type [Sha256Digest]
    Sha256Digest? sha256Digest;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryTemplateResponseHostedServicePolicyDigest createFromDiscriminatorValue(ParseNode parseNode) {
        var result = DeliveryTemplateResponseHostedServicePolicyDigest();
        return result;
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        if(deliveryTemplateResponseHostedServicePolicyDigestMember1 != null) {
            return deliveryTemplateResponseHostedServicePolicyDigestMember1!.getFieldDeserializers();
        }
        else if(sha256Digest != null) {
            return sha256Digest!.getFieldDeserializers();
        }
        return <String, void Function(ParseNode)>{};
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        if(deliveryTemplateResponseHostedServicePolicyDigestMember1 != null) {
            writer.writeObjectValue<DeliveryTemplateResponseHostedServicePolicyDigestMember1>(null, deliveryTemplateResponseHostedServicePolicyDigestMember1);
        }
        else if(sha256Digest != null) {
            writer.writeObjectValue<Sha256Digest>(null, sha256Digest);
        }
    }
}
