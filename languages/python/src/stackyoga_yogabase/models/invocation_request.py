from __future__ import annotations
import datetime
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .invocation_request_object_reference import InvocationRequest_objectReference

@dataclass
class InvocationRequest(Parsable):
    # The correlationId property
    correlation_id: Optional[str] = None
    # The deadline property
    deadline: Optional[datetime.datetime] = None
    # The idempotencyKey property
    idempotency_key: Optional[str] = None
    # The inlinePayloadBase64 property
    inline_payload_base64: Optional[str] = None
    # The mode property
    mode: Optional[int] = None
    # The objectReference property
    object_reference: Optional[InvocationRequest_objectReference] = None
    # The payloadDigest property
    payload_digest: Optional[str] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> InvocationRequest:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: InvocationRequest
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return InvocationRequest()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        from .invocation_request_object_reference import InvocationRequest_objectReference

        from .invocation_request_object_reference import InvocationRequest_objectReference

        fields: dict[str, Callable[[Any], None]] = {
            "correlationId": lambda n : setattr(self, 'correlation_id', n.get_str_value()),
            "deadline": lambda n : setattr(self, 'deadline', n.get_datetime_value()),
            "idempotencyKey": lambda n : setattr(self, 'idempotency_key', n.get_str_value()),
            "inlinePayloadBase64": lambda n : setattr(self, 'inline_payload_base64', n.get_str_value()),
            "mode": lambda n : setattr(self, 'mode', n.get_int_value()),
            "objectReference": lambda n : setattr(self, 'object_reference', n.get_object_value(InvocationRequest_objectReference)),
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
        writer.write_str_value("correlationId", self.correlation_id)
        writer.write_datetime_value("deadline", self.deadline)
        writer.write_str_value("idempotencyKey", self.idempotency_key)
        writer.write_str_value("inlinePayloadBase64", self.inline_payload_base64)
        writer.write_int_value("mode", self.mode)
        writer.write_object_value("objectReference", self.object_reference)
        writer.write_str_value("payloadDigest", self.payload_digest)
    

