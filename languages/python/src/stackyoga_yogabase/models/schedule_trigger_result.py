from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union
from uuid import UUID

@dataclass
class ScheduleTriggerResult(Parsable):
    # The invocation's id. It derives from the schedule's id, instance and version and the idempotency key, so a repeat has the same one.
    invocation_id: Optional[UUID] = None
    # 0 when this request enqueued the invocation, 1 when it found the one the same key had already made.
    outcome: Optional[int] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> ScheduleTriggerResult:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: ScheduleTriggerResult
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return ScheduleTriggerResult()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        fields: dict[str, Callable[[Any], None]] = {
            "invocationId": lambda n : setattr(self, 'invocation_id', n.get_uuid_value()),
            "outcome": lambda n : setattr(self, 'outcome', n.get_int_value()),
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
        writer.write_uuid_value("invocationId", self.invocation_id)
        writer.write_int_value("outcome", self.outcome)
    

