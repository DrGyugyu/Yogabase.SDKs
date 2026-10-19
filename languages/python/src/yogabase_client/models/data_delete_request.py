from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .data_filter import DataFilter

@dataclass
class DataDeleteRequest(Parsable):
    # The filters property
    filters: Optional[list[DataFilter]] = None
    # Column names, or ["*"] for every column the caller may read.
    returning: Optional[list[str]] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> DataDeleteRequest:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: DataDeleteRequest
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return DataDeleteRequest()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        from .data_filter import DataFilter

        from .data_filter import DataFilter

        fields: dict[str, Callable[[Any], None]] = {
            "filters": lambda n : setattr(self, 'filters', n.get_collection_of_object_values(DataFilter)),
            "returning": lambda n : setattr(self, 'returning', n.get_collection_of_primitive_values(str)),
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
        writer.write_collection_of_object_values("filters", self.filters)
        writer.write_collection_of_primitive_values("returning", self.returning)
    

