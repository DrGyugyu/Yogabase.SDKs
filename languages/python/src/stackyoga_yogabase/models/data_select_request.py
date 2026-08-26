from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .data_filter import DataFilter
    from .data_ordering import DataOrdering
    from .data_select_request_cursor import DataSelectRequest_cursor

@dataclass
class DataSelectRequest(Parsable):
    # The columns property
    columns: Optional[list[str]] = None
    # The cursor property
    cursor: Optional[DataSelectRequest_cursor] = None
    # The filters property
    filters: Optional[list[DataFilter]] = None
    # The ordering property
    ordering: Optional[list[DataOrdering]] = None
    # The pageSize property
    page_size: Optional[int] = None
    # The queryDepth property
    query_depth: Optional[int] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> DataSelectRequest:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: DataSelectRequest
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return DataSelectRequest()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        from .data_filter import DataFilter
        from .data_ordering import DataOrdering
        from .data_select_request_cursor import DataSelectRequest_cursor

        from .data_filter import DataFilter
        from .data_ordering import DataOrdering
        from .data_select_request_cursor import DataSelectRequest_cursor

        fields: dict[str, Callable[[Any], None]] = {
            "columns": lambda n : setattr(self, 'columns', n.get_collection_of_primitive_values(str)),
            "cursor": lambda n : setattr(self, 'cursor', n.get_object_value(DataSelectRequest_cursor)),
            "filters": lambda n : setattr(self, 'filters', n.get_collection_of_object_values(DataFilter)),
            "ordering": lambda n : setattr(self, 'ordering', n.get_collection_of_object_values(DataOrdering)),
            "pageSize": lambda n : setattr(self, 'page_size', n.get_int_value()),
            "queryDepth": lambda n : setattr(self, 'query_depth', n.get_int_value()),
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
        writer.write_collection_of_primitive_values("columns", self.columns)
        writer.write_object_value("cursor", self.cursor)
        writer.write_collection_of_object_values("filters", self.filters)
        writer.write_collection_of_object_values("ordering", self.ordering)
        writer.write_int_value("pageSize", self.page_size)
        writer.write_int_value("queryDepth", self.query_depth)
    

