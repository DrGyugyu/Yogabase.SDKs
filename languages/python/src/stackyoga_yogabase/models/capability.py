from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .capability_id import Capability_id

@dataclass
class Capability(Parsable):
    # The available property
    available: Optional[bool] = None
    # realtime-presence appears only when requested; it is available exactly when realtime-subscribe is and the runtime's realtime projection enables presence. data-rest appears only when requested; it is available exactly when the runtime enables the Data API, which requires auth.
    id: Optional[Capability_id] = None
    # The unavailableReason property
    unavailable_reason: Optional[str] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> Capability:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: Capability
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return Capability()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        from .capability_id import Capability_id

        from .capability_id import Capability_id

        fields: dict[str, Callable[[Any], None]] = {
            "available": lambda n : setattr(self, 'available', n.get_bool_value()),
            "id": lambda n : setattr(self, 'id', n.get_enum_value(Capability_id)),
            "unavailableReason": lambda n : setattr(self, 'unavailable_reason', n.get_str_value()),
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
        writer.write_bool_value("available", self.available)
        writer.write_enum_value("id", self.id)
        writer.write_str_value("unavailableReason", self.unavailable_reason)
    

