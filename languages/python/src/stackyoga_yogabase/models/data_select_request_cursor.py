from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import ComposedTypeWrapper, Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .data_cursor import DataCursor
    from .data_select_request_cursor_member1 import DataSelectRequest_cursorMember1

@dataclass
class DataSelectRequest_cursor(ComposedTypeWrapper, Parsable):
    """
    Composed type wrapper for classes DataCursor, DataSelectRequest_cursorMember1
    """
    # Composed type representation for type DataCursor
    data_cursor: Optional[DataCursor] = None
    # Composed type representation for type DataSelectRequest_cursorMember1
    data_select_request_cursor_member1: Optional[DataSelectRequest_cursorMember1] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> DataSelectRequest_cursor:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: DataSelectRequest_cursor
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        try:
            child_node = parse_node.get_child_node("")
            mapping_value = child_node.get_str_value() if child_node else None
        except AttributeError:
            mapping_value = None
        result = DataSelectRequest_cursor()
        if mapping_value and mapping_value.casefold() == "DataCursor".casefold():
            from .data_cursor import DataCursor

            result.data_cursor = DataCursor()
        return result
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        from .data_cursor import DataCursor
        from .data_select_request_cursor_member1 import DataSelectRequest_cursorMember1

        if self.data_cursor:
            return self.data_cursor.get_field_deserializers()
        if self.data_select_request_cursor_member1:
            return self.data_select_request_cursor_member1.get_field_deserializers()
        return {}
    
    def serialize(self,writer: SerializationWriter) -> None:
        """
        Serializes information the current object
        param writer: Serialization writer to use to serialize this model
        Returns: None
        """
        if writer is None:
            raise TypeError("writer cannot be null.")
        if self.data_cursor:
            writer.write_object_value(None, self.data_cursor)
        elif self.data_select_request_cursor_member1:
            writer.write_object_value(None, self.data_select_request_cursor_member1)
    

