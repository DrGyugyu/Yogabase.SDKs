// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './game_server_port.dart';

/// auto generated
class GameServer implements Parsable {
    ///  Empty when no member has observed an address for this instance; a client must not connect to such an instance.
    Iterable<GameServerPort>? networkPorts;
    ///  The serverId property
    String? serverId;
    ///  Lifecycle state, verbatim. Read it as text: a state Yogabase adds later must stay readable rather than fail a client.
    String? state;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static GameServer createFromDiscriminatorValue(ParseNode parseNode) {
        return GameServer();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['networkPorts'] = (node) => networkPorts = node.getCollectionOfObjectValues<GameServerPort>(GameServerPort.createFromDiscriminatorValue);
        deserializerMap['serverId'] = (node) => serverId = node.getStringValue();
        deserializerMap['state'] = (node) => state = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeCollectionOfObjectValues<GameServerPort>('networkPorts', networkPorts);
        writer.writeStringValue('serverId', serverId);
        writer.writeStringValue('state', state);
    }
}
