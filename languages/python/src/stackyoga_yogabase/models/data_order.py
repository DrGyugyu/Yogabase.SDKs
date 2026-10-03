from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

@dataclass
class DataOrder(Parsable):
    # The descending property
    descending: Optional[bool] = False
    # The column property
    column: Optional[str] = None
    # The nullsFirst property
    nulls_first: Optional[bool] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> DataOrder:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: DataOrder
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return DataOrder()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        fields: dict[str, Callable[[Any], None]] = {
            "column": lambda n : setattr(self, 'column', n.get_str_value()),
            "descending": lambda n : setattr(self, 'descending', n.get_bool_value()),
            "nullsFirst": lambda n : setattr(self, 'nulls_first', n.get_bool_value()),
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
        writer.write_bool_value("descending", self.descending)
        writer.write_bool_value("nullsFirst", self.nulls_first)
    

