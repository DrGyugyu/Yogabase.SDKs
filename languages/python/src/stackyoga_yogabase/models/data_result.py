from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .data_result_rows import DataResult_rows

@dataclass
class DataResult(Parsable):
    # The affectedRows property
    affected_rows: Optional[int] = None
    # The rows property
    rows: Optional[list[DataResult_rows]] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> DataResult:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: DataResult
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return DataResult()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        from .data_result_rows import DataResult_rows

        from .data_result_rows import DataResult_rows

        fields: dict[str, Callable[[Any], None]] = {
            "affectedRows": lambda n : setattr(self, 'affected_rows', n.get_int_value()),
            "rows": lambda n : setattr(self, 'rows', n.get_collection_of_object_values(DataResult_rows)),
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
        writer.write_int_value("affectedRows", self.affected_rows)
        writer.write_collection_of_object_values("rows", self.rows)
    

