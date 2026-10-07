from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

@dataclass
class ExternalIdentityStartRequest(Parsable):
    # Base64url of the SHA-256 of the client's verifier, without padding.
    code_challenge: Optional[str] = None
    # The redirectUri property
    redirect_uri: Optional[str] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> ExternalIdentityStartRequest:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: ExternalIdentityStartRequest
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return ExternalIdentityStartRequest()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        fields: dict[str, Callable[[Any], None]] = {
            "codeChallenge": lambda n : setattr(self, 'code_challenge', n.get_str_value()),
            "redirectUri": lambda n : setattr(self, 'redirect_uri', n.get_str_value()),
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
        writer.write_str_value("codeChallenge", self.code_challenge)
        writer.write_str_value("redirectUri", self.redirect_uri)
    

