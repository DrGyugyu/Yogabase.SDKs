// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './game_server_start_request_custom_data.dart';

/// auto generated
class GameServerStartRequest implements Parsable {
    ///  Bounded string map recorded on the allocation and carried to the instance. At most 4096 bytes as canonical JSON. It reaches customer code, so it must never carry a credential or another player's identity.
    GameServerStartRequestCustomData? customData;
    ///  Canonical hosted-service id to allocate an instance of.
    String? service;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static GameServerStartRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return GameServerStartRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['customData'] = (node) => customData = node.getObjectValue<GameServerStartRequestCustomData>(GameServerStartRequestCustomData.createFromDiscriminatorValue);
        deserializerMap['service'] = (node) => service = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeObjectValue<GameServerStartRequestCustomData>('customData', customData);
        writer.writeStringValue('service', service);
    }
}
