from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .game_server_port_protocol import GameServerPort_protocol

@dataclass
class GameServerPort(Parsable):
    # Port to connect to on host.
    external_port: Optional[int] = None
    # Node address the member observed. Never composed by the Control Plane.
    host: Optional[str] = None
    # Port the workload listens on inside its Pod.
    internal_port: Optional[int] = None
    # The name property
    name: Optional[str] = None
    # The protocol property
    protocol: Optional[GameServerPort_protocol] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> GameServerPort:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: GameServerPort
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return GameServerPort()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        from .game_server_port_protocol import GameServerPort_protocol

        from .game_server_port_protocol import GameServerPort_protocol

        fields: dict[str, Callable[[Any], None]] = {
            "externalPort": lambda n : setattr(self, 'external_port', n.get_int_value()),
            "host": lambda n : setattr(self, 'host', n.get_str_value()),
            "internalPort": lambda n : setattr(self, 'internal_port', n.get_int_value()),
            "name": lambda n : setattr(self, 'name', n.get_str_value()),
            "protocol": lambda n : setattr(self, 'protocol', n.get_enum_value(GameServerPort_protocol)),
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
        writer.write_int_value("externalPort", self.external_port)
        writer.write_str_value("host", self.host)
        writer.write_int_value("internalPort", self.internal_port)
        writer.write_str_value("name", self.name)
        writer.write_enum_value("protocol", self.protocol)
    

