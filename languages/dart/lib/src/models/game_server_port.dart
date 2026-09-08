// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './game_server_port_protocol.dart';

/// auto generated
class GameServerPort implements Parsable {
    ///  Port to connect to on host.
    int? externalPort;
    ///  Node address the member observed. Never composed by the Control Plane.
    String? host;
    ///  Port the workload listens on inside its Pod.
    int? internalPort;
    ///  The name property
    String? name;
    ///  The protocol property
    GameServerPortProtocol? protocol;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static GameServerPort createFromDiscriminatorValue(ParseNode parseNode) {
        return GameServerPort();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['externalPort'] = (node) => externalPort = node.getIntValue();
        deserializerMap['host'] = (node) => host = node.getStringValue();
        deserializerMap['internalPort'] = (node) => internalPort = node.getIntValue();
        deserializerMap['name'] = (node) => name = node.getStringValue();
        deserializerMap['protocol'] = (node) => protocol = node.getEnumValue<GameServerPortProtocol>((stringValue) => GameServerPortProtocol.values.where((enumVal) => enumVal.value == stringValue).firstOrNull);
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeIntValue('externalPort', externalPort);
        writer.writeStringValue('host', host);
        writer.writeIntValue('internalPort', internalPort);
        writer.writeStringValue('name', name);
        writer.writeEnumValue<GameServerPortProtocol>('protocol', protocol, (e) => e?.value);
    }
}
