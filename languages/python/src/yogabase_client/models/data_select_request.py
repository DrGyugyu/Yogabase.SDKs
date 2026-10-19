from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .data_filter import DataFilter
    from .data_order import DataOrder

@dataclass
class DataSelectRequest(Parsable):
    # The limit property
    limit: Optional[int] = 100
    # The offset property
    offset: Optional[int] = 0
    # Column names, or ["*"] for every column the caller may read.
    columns: Optional[list[str]] = None
    # The filters property
    filters: Optional[list[DataFilter]] = None
    # The order property
    order: Optional[list[DataOrder]] = None
    
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
        from .data_order import DataOrder

        from .data_filter import DataFilter
        from .data_order import DataOrder

        fields: dict[str, Callable[[Any], None]] = {
            "columns": lambda n : setattr(self, 'columns', n.get_collection_of_primitive_values(str)),
            "filters": lambda n : setattr(self, 'filters', n.get_collection_of_object_values(DataFilter)),
            "limit": lambda n : setattr(self, 'limit', n.get_int_value()),
            "offset": lambda n : setattr(self, 'offset', n.get_int_value()),
            "order": lambda n : setattr(self, 'order', n.get_collection_of_object_values(DataOrder)),
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
        writer.write_collection_of_object_values("filters", self.filters)
        writer.write_int_value("limit", self.limit)
        writer.write_int_value("offset", self.offset)
        writer.write_collection_of_object_values("order", self.order)
    

