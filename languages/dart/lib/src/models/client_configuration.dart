// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';
import './capability.dart';

/// auto generated
class ClientConfiguration implements Parsable {
    ///  The withdrawn data-rest capability may still appear on an installation that predates the Data API removal. Clients accept it and ignore it.
    Iterable<Capability>? capabilities;
    ///  The environmentId property
    UuidValue? environmentId;
    ///  The gatewayEndpoint property
    String? gatewayEndpoint;
    ///  The issuedAt property
    DateTime? issuedAt;
    ///  The oidcAuthority property
    String? oidcAuthority;
    ///  The oidcDiscoveryEndpoint property
    String? oidcDiscoveryEndpoint;
    ///  The oidcPublicClientId property
    String? oidcPublicClientId;
    ///  Exact OAuth redirect allowlist. Entries are public HTTPS URLs, loopback HTTP URLs, or lowercase reverse-domain private-use native schemes such as com.example.app:/oauth/callback. Userinfo, query strings, and fragments are forbidden.
    Iterable<String>? oidcRedirectUris;
    ///  The oidcScopes property
    Iterable<String>? oidcScopes;
    ///  The projectId property
    UuidValue? projectId;
    ///  Exact HTTPS origin of the separately deployed SignalR realtime gateway.
    String? realtimeEndpoint;
    ///  The realtimePath property
    String? realtimePath;
    ///  The schema property
    String? schema;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static ClientConfiguration createFromDiscriminatorValue(ParseNode parseNode) {
        return ClientConfiguration();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['capabilities'] = (node) => capabilities = node.getCollectionOfObjectValues<Capability>(Capability.createFromDiscriminatorValue);
        deserializerMap['environmentId'] = (node) => environmentId = node.getGuidValue();
        deserializerMap['gatewayEndpoint'] = (node) => gatewayEndpoint = node.getStringValue();
        deserializerMap['issuedAt'] = (node) => issuedAt = node.getDateTimeValue();
        deserializerMap['oidcAuthority'] = (node) => oidcAuthority = node.getStringValue();
        deserializerMap['oidcDiscoveryEndpoint'] = (node) => oidcDiscoveryEndpoint = node.getStringValue();
        deserializerMap['oidcPublicClientId'] = (node) => oidcPublicClientId = node.getStringValue();
        deserializerMap['oidcRedirectUris'] = (node) => oidcRedirectUris = node.getCollectionOfPrimitiveValues<String>();
        deserializerMap['oidcScopes'] = (node) => oidcScopes = node.getCollectionOfPrimitiveValues<String>();
        deserializerMap['projectId'] = (node) => projectId = node.getGuidValue();
        deserializerMap['realtimeEndpoint'] = (node) => realtimeEndpoint = node.getStringValue();
        deserializerMap['realtimePath'] = (node) => realtimePath = node.getStringValue();
        deserializerMap['schema'] = (node) => schema = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeCollectionOfObjectValues<Capability>('capabilities', capabilities);
        writer.writeUuidValue('environmentId', environmentId);
        writer.writeStringValue('gatewayEndpoint', gatewayEndpoint);
        writer.writeDateTimeValue('issuedAt', issuedAt);
        writer.writeStringValue('oidcAuthority', oidcAuthority);
        writer.writeStringValue('oidcDiscoveryEndpoint', oidcDiscoveryEndpoint);
        writer.writeStringValue('oidcPublicClientId', oidcPublicClientId);
        writer.writeCollectionOfPrimitiveValues<String?>('oidcRedirectUris', oidcRedirectUris);
        writer.writeCollectionOfPrimitiveValues<String?>('oidcScopes', oidcScopes);
        writer.writeUuidValue('projectId', projectId);
        writer.writeStringValue('realtimeEndpoint', realtimeEndpoint);
        writer.writeStringValue('realtimePath', realtimePath);
        writer.writeStringValue('schema', schema);
    }
}
