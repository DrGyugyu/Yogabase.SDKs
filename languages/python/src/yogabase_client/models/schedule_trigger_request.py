from __future__ import annotations
import datetime
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

@dataclass
class ScheduleTriggerRequest(Parsable):
    # When the invocation must finish: later than now, and at most 3,600 seconds from now.
    deadline: Optional[datetime.datetime] = None
    # The caller's key for this run: 1 to 256 characters, with no leading or trailing white space. The same key against the same version of the schedule is the same run.
    idempotency_key: Optional[str] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> ScheduleTriggerRequest:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: ScheduleTriggerRequest
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return ScheduleTriggerRequest()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        fields: dict[str, Callable[[Any], None]] = {
            "deadline": lambda n : setattr(self, 'deadline', n.get_datetime_value()),
            "idempotencyKey": lambda n : setattr(self, 'idempotency_key', n.get_str_value()),
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
        writer.write_datetime_value("deadline", self.deadline)
        writer.write_str_value("idempotencyKey", self.idempotency_key)
    

