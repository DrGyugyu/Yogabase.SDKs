from __future__ import annotations
import datetime
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union
from uuid import UUID

if TYPE_CHECKING:
    from .capability import Capability

@dataclass
class ClientConfiguration(Parsable):
    # The capabilities property
    capabilities: Optional[list[Capability]] = None
    # The environmentId property
    environment_id: Optional[UUID] = None
    # The gatewayEndpoint property
    gateway_endpoint: Optional[str] = None
    # The issuedAt property
    issued_at: Optional[datetime.datetime] = None
    # The oidcAuthority property
    oidc_authority: Optional[str] = None
    # The oidcDiscoveryEndpoint property
    oidc_discovery_endpoint: Optional[str] = None
    # The oidcPublicClientId property
    oidc_public_client_id: Optional[str] = None
    # The oidcRedirectUris property
    oidc_redirect_uris: Optional[list[str]] = None
    # The oidcScopes property
    oidc_scopes: Optional[list[str]] = None
    # The projectId property
    project_id: Optional[UUID] = None
    # The realtimePath property
    realtime_path: Optional[str] = None
    # The schema property
    schema: Optional[str] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> ClientConfiguration:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: ClientConfiguration
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return ClientConfiguration()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        from .capability import Capability

        from .capability import Capability

        fields: dict[str, Callable[[Any], None]] = {
            "capabilities": lambda n : setattr(self, 'capabilities', n.get_collection_of_object_values(Capability)),
            "environmentId": lambda n : setattr(self, 'environment_id', n.get_uuid_value()),
            "gatewayEndpoint": lambda n : setattr(self, 'gateway_endpoint', n.get_str_value()),
            "issuedAt": lambda n : setattr(self, 'issued_at', n.get_datetime_value()),
            "oidcAuthority": lambda n : setattr(self, 'oidc_authority', n.get_str_value()),
            "oidcDiscoveryEndpoint": lambda n : setattr(self, 'oidc_discovery_endpoint', n.get_str_value()),
            "oidcPublicClientId": lambda n : setattr(self, 'oidc_public_client_id', n.get_str_value()),
            "oidcRedirectUris": lambda n : setattr(self, 'oidc_redirect_uris', n.get_collection_of_primitive_values(str)),
            "oidcScopes": lambda n : setattr(self, 'oidc_scopes', n.get_collection_of_primitive_values(str)),
            "projectId": lambda n : setattr(self, 'project_id', n.get_uuid_value()),
            "realtimePath": lambda n : setattr(self, 'realtime_path', n.get_str_value()),
            "schema": lambda n : setattr(self, 'schema', n.get_str_value()),
        }
        return fields
    
    def serialize(self,writer: SerializationWriter) -> None:
        """
        Serializes information the current object
        param writer: Serialization writer to use to serialize this model
        Returns: None
        """
        if writer is None:
            raise TypeError("writer cannot be null.")
        writer.write_collection_of_object_values("capabilities", self.capabilities)
        writer.write_uuid_value("environmentId", self.environment_id)
        writer.write_str_value("gatewayEndpoint", self.gateway_endpoint)
        writer.write_datetime_value("issuedAt", self.issued_at)
        writer.write_str_value("oidcAuthority", self.oidc_authority)
        writer.write_str_value("oidcDiscoveryEndpoint", self.oidc_discovery_endpoint)
        writer.write_str_value("oidcPublicClientId", self.oidc_public_client_id)
        writer.write_collection_of_primitive_values("oidcRedirectUris", self.oidc_redirect_uris)
        writer.write_collection_of_primitive_values("oidcScopes", self.oidc_scopes)
        writer.write_uuid_value("projectId", self.project_id)
        writer.write_str_value("realtimePath", self.realtime_path)
        writer.write_str_value("schema", self.schema)
    

