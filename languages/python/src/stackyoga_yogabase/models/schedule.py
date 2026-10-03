from __future__ import annotations
import datetime
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union
from uuid import UUID

@dataclass
class Schedule(Parsable):
    # The functionId property
    function_id: Optional[str] = None
    # The functionVersion property
    function_version: Optional[str] = None
    # Lasts until the schedule is deleted: a schedule deleted and created again has a new one.
    instance_id: Optional[UUID] = None
    # How far the scheduler has got: when the schedule was created, then the last occurrence it ran or skipped past. Null for a one-shot.
    last_occurrence: Optional[datetime.datetime] = None
    # The instant a one-shot runs; null for a recurring schedule.
    one_shot_at_utc: Optional[datetime.datetime] = None
    # The projectId property
    project_id: Optional[UUID] = None
    # The id the schedule was written under. A write holds an id to the pattern of the definition's scheduleId member; a schedule written before the pattern was enforced may carry another.
    schedule_id: Optional[str] = None
    # The owner: the sub of the user who created the schedule.
    subject: Optional[UUID] = None
    # The schedule's version as decimal text, because it is a 64-bit number a JSON number cannot carry exactly. It changes whenever the schedule does and is not a counter: read it from the ETag, which carries it with the instance.
    version: Optional[str] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> Schedule:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: Schedule
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return Schedule()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        fields: dict[str, Callable[[Any], None]] = {
            "functionId": lambda n : setattr(self, 'function_id', n.get_str_value()),
            "functionVersion": lambda n : setattr(self, 'function_version', n.get_str_value()),
            "instanceId": lambda n : setattr(self, 'instance_id', n.get_uuid_value()),
            "lastOccurrence": lambda n : setattr(self, 'last_occurrence', n.get_datetime_value()),
            "oneShotAtUtc": lambda n : setattr(self, 'one_shot_at_utc', n.get_datetime_value()),
            "projectId": lambda n : setattr(self, 'project_id', n.get_uuid_value()),
            "scheduleId": lambda n : setattr(self, 'schedule_id', n.get_str_value()),
            "subject": lambda n : setattr(self, 'subject', n.get_uuid_value()),
            "version": lambda n : setattr(self, 'version', n.get_str_value()),
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
        writer.write_str_value("functionId", self.function_id)
        writer.write_str_value("functionVersion", self.function_version)
        writer.write_uuid_value("instanceId", self.instance_id)
        writer.write_datetime_value("lastOccurrence", self.last_occurrence)
        writer.write_datetime_value("oneShotAtUtc", self.one_shot_at_utc)
        writer.write_uuid_value("projectId", self.project_id)
        writer.write_str_value("scheduleId", self.schedule_id)
        writer.write_uuid_value("subject", self.subject)
        writer.write_str_value("version", self.version)
    

