from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

@dataclass
class WorkflowStartRequest(Parsable):
    # The idempotencyKey property
    idempotency_key: Optional[str] = None
    # The inlineInputBase64 property
    inline_input_base64: Optional[str] = None
    # The payloadDigest property
    payload_digest: Optional[str] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> WorkflowStartRequest:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: WorkflowStartRequest
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return WorkflowStartRequest()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        fields: dict[str, Callable[[Any], None]] = {
            "idempotencyKey": lambda n : setattr(self, 'idempotency_key', n.get_str_value()),
            "inlineInputBase64": lambda n : setattr(self, 'inline_input_base64', n.get_str_value()),
            "payloadDigest": lambda n : setattr(self, 'payload_digest', n.get_str_value()),
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
        writer.write_str_value("idempotencyKey", self.idempotency_key)
        writer.write_str_value("inlineInputBase64", self.inline_input_base64)
        writer.write_str_value("payloadDigest", self.payload_digest)
    

