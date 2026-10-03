from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .data_row import DataRow

@dataclass
class DataUpsertRequest(Parsable):
    # The ignoreDuplicates property
    ignore_duplicates: Optional[bool] = False
    # The onConflict property
    on_conflict: Optional[list[str]] = None
    # Column names, or ["*"] for every column the caller may read.
    returning: Optional[list[str]] = None
    # The rows property
    rows: Optional[list[DataRow]] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> DataUpsertRequest:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: DataUpsertRequest
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return DataUpsertRequest()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        from .data_row import DataRow

        from .data_row import DataRow

        fields: dict[str, Callable[[Any], None]] = {
            "ignoreDuplicates": lambda n : setattr(self, 'ignore_duplicates', n.get_bool_value()),
            "onConflict": lambda n : setattr(self, 'on_conflict', n.get_collection_of_primitive_values(str)),
            "returning": lambda n : setattr(self, 'returning', n.get_collection_of_primitive_values(str)),
            "rows": lambda n : setattr(self, 'rows', n.get_collection_of_object_values(DataRow)),
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
        writer.write_bool_value("ignoreDuplicates", self.ignore_duplicates)
        writer.write_collection_of_primitive_values("onConflict", self.on_conflict)
        writer.write_collection_of_primitive_values("returning", self.returning)
        writer.write_collection_of_object_values("rows", self.rows)
    

