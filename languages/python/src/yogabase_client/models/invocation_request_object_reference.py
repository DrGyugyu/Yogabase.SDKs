from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import ComposedTypeWrapper, Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .invocation_request_object_reference_member1 import InvocationRequest_objectReferenceMember1
    from .object_reference import ObjectReference

@dataclass
class InvocationRequest_objectReference(ComposedTypeWrapper, Parsable):
    """
    Composed type wrapper for classes InvocationRequest_objectReferenceMember1, ObjectReference
    """
    # Composed type representation for type InvocationRequest_objectReferenceMember1
    invocation_request_object_reference_member1: Optional[InvocationRequest_objectReferenceMember1] = None
    # Composed type representation for type ObjectReference
    object_reference: Optional[ObjectReference] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> InvocationRequest_objectReference:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: InvocationRequest_objectReference
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        try:
            child_node = parse_node.get_child_node("")
            mapping_value = child_node.get_str_value() if child_node else None
        except AttributeError:
            mapping_value = None
        result = InvocationRequest_objectReference()
        if mapping_value and mapping_value.casefold() == "ObjectReference".casefold():
            from .object_reference import ObjectReference

            result.object_reference = ObjectReference()
        return result
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        from .invocation_request_object_reference_member1 import InvocationRequest_objectReferenceMember1
        from .object_reference import ObjectReference

        if self.invocation_request_object_reference_member1:
            return self.invocation_request_object_reference_member1.get_field_deserializers()
        if self.object_reference:
            return self.object_reference.get_field_deserializers()
        return {}
    
    def serialize(self,writer: SerializationWriter) -> None:
        """
        Serializes information the current object
        param writer: Serialization writer to use to serialize this model
        Returns: None
        """
        if writer is None:
            raise TypeError("writer cannot be null.")
        if self.invocation_request_object_reference_member1:
            writer.write_object_value(None, self.invocation_request_object_reference_member1)
        elif self.object_reference:
            writer.write_object_value(None, self.object_reference)
    

