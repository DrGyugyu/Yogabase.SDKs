from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.api_error import APIError
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union
from uuid import UUID

@dataclass
class InvocationResult(APIError, Parsable):
    # The correlationId property
    correlation_id: Optional[str] = None
    # The failureCode property
    failure_code: Optional[str] = None
    # The inlineResultBase64 property
    inline_result_base64: Optional[str] = None
    # The invocationId property
    invocation_id: Optional[UUID] = None
    # The outcome property
    outcome: Optional[int] = None
    # The resultReference property
    result_reference: Optional[str] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> InvocationResult:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: InvocationResult
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return InvocationResult()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        fields: dict[str, Callable[[Any], None]] = {
            "correlationId": lambda n : setattr(self, 'correlation_id', n.get_str_value()),
            "failureCode": lambda n : setattr(self, 'failure_code', n.get_str_value()),
            "inlineResultBase64": lambda n : setattr(self, 'inline_result_base64', n.get_str_value()),
            "invocationId": lambda n : setattr(self, 'invocation_id', n.get_uuid_value()),
            "outcome": lambda n : setattr(self, 'outcome', n.get_int_value()),
            "resultReference": lambda n : setattr(self, 'result_reference', n.get_str_value()),
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
        writer.write_str_value("failureCode", self.failure_code)
        writer.write_str_value("inlineResultBase64", self.inline_result_base64)
        writer.write_uuid_value("invocationId", self.invocation_id)
        writer.write_int_value("outcome", self.outcome)
        writer.write_str_value("resultReference", self.result_reference)
    
    @property
    def primary_message(self) -> Optional[str]:
        """
        The primary error message.
        """
        return super().message

