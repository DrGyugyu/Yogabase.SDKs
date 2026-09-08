from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .game_server_start_request_custom_data import GameServerStartRequest_customData

@dataclass
class GameServerStartRequest(Parsable):
    # Bounded string map recorded on the allocation and carried to the instance. At most 4096 bytes as canonical JSON. It reaches customer code, so it must never carry a credential or another player's identity.
    custom_data: Optional[GameServerStartRequest_customData] = None
    # Canonical hosted-service id to allocate an instance of.
    service: Optional[str] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> GameServerStartRequest:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: GameServerStartRequest
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return GameServerStartRequest()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        from .game_server_start_request_custom_data import GameServerStartRequest_customData

        from .game_server_start_request_custom_data import GameServerStartRequest_customData

        fields: dict[str, Callable[[Any], None]] = {
            "customData": lambda n : setattr(self, 'custom_data', n.get_object_value(GameServerStartRequest_customData)),
            "service": lambda n : setattr(self, 'service', n.get_str_value()),
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
        writer.write_object_value("customData", self.custom_data)
        writer.write_str_value("service", self.service)
    

