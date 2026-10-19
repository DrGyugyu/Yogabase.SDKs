from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

@dataclass
class ObjectWriteResult(Parsable):
    # The alreadyExisted property
    already_existed: Optional[bool] = None
    # The digest property
    digest: Optional[str] = None
    # The encryption property
    encryption: Optional[str] = None
    # The key property
    key: Optional[str] = None
    # The sizeBytes property
    size_bytes: Optional[int] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> ObjectWriteResult:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: ObjectWriteResult
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return ObjectWriteResult()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        fields: dict[str, Callable[[Any], None]] = {
            "alreadyExisted": lambda n : setattr(self, 'already_existed', n.get_bool_value()),
            "digest": lambda n : setattr(self, 'digest', n.get_str_value()),
            "encryption": lambda n : setattr(self, 'encryption', n.get_str_value()),
            "key": lambda n : setattr(self, 'key', n.get_str_value()),
            "sizeBytes": lambda n : setattr(self, 'size_bytes', n.get_int_value()),
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
        writer.write_bool_value("alreadyExisted", self.already_existed)
        writer.write_str_value("digest", self.digest)
        writer.write_str_value("encryption", self.encryption)
        writer.write_str_value("key", self.key)
        writer.write_int_value("sizeBytes", self.size_bytes)
    

