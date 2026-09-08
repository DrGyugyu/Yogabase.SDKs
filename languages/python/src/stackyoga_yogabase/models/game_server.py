from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .game_server_port import GameServerPort

@dataclass
class GameServer(Parsable):
    # Empty when no member has observed an address for this instance; a client must not connect to such an instance.
    network_ports: Optional[list[GameServerPort]] = None
    # The serverId property
    server_id: Optional[str] = None
    # Lifecycle state, verbatim. Read it as text: a state Yogabase adds later must stay readable rather than fail a client.
    state: Optional[str] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> GameServer:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: GameServer
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return GameServer()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        from .game_server_port import GameServerPort

        from .game_server_port import GameServerPort

        fields: dict[str, Callable[[Any], None]] = {
            "networkPorts": lambda n : setattr(self, 'network_ports', n.get_collection_of_object_values(GameServerPort)),
            "serverId": lambda n : setattr(self, 'server_id', n.get_str_value()),
            "state": lambda n : setattr(self, 'state', n.get_str_value()),
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
        writer.write_collection_of_object_values("networkPorts", self.network_ports)
        writer.write_str_value("serverId", self.server_id)
        writer.write_str_value("state", self.state)
    

