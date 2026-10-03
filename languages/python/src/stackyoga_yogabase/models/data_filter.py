from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .data_filter_op import DataFilter_op

@dataclass
class DataFilter(Parsable):
    # The column property
    column: Optional[str] = None
    # The op property
    op: Optional[DataFilter_op] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> DataFilter:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: DataFilter
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return DataFilter()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        from .data_filter_op import DataFilter_op

        from .data_filter_op import DataFilter_op

        fields: dict[str, Callable[[Any], None]] = {
            "column": lambda n : setattr(self, 'column', n.get_str_value()),
            "op": lambda n : setattr(self, 'op', n.get_enum_value(DataFilter_op)),
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
        writer.write_str_value("column", self.column)
        writer.write_enum_value("op", self.op)
    

