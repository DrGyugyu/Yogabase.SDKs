from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

@dataclass
class JobSubmitRequest(Parsable):
    # The caller's key for this request: trimmed, with no control character. It belongs to the signed-in user, who may repeat it for the same request and for no other.
    idempotency_key: Optional[str] = None
    # The id of a deployed stateless function; its highest released version runs the job.
    job_type: Optional[str] = None
    # How many times the job may be attempted. Absent or null is 5. The idempotency key binds it, with the job type and the payload.
    max_attempts: Optional[int] = None
    # The function's request, at most 1,048,576 bytes before base64 encoding. Absent, null or empty is a job that carries none.
    payload_base64: Optional[str] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> JobSubmitRequest:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: JobSubmitRequest
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return JobSubmitRequest()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        fields: dict[str, Callable[[Any], None]] = {
            "idempotencyKey": lambda n : setattr(self, 'idempotency_key', n.get_str_value()),
            "jobType": lambda n : setattr(self, 'job_type', n.get_str_value()),
            "maxAttempts": lambda n : setattr(self, 'max_attempts', n.get_int_value()),
            "payloadBase64": lambda n : setattr(self, 'payload_base64', n.get_str_value()),
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
        writer.write_str_value("jobType", self.job_type)
        writer.write_int_value("maxAttempts", self.max_attempts)
        writer.write_str_value("payloadBase64", self.payload_base64)
    

