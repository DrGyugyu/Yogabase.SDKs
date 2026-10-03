from __future__ import annotations
import datetime
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .concurrency_policy import ConcurrencyPolicy
    from .misfire_policy import MisfirePolicy

@dataclass
class ScheduleDefinitionRequest(Parsable):
    # What a schedule does when an occurrence is due while an earlier occurrence of the same schedule has not finished. An occurrence has not finished while its invocation is queued (waiting to start, or waiting for a retry) or running, and its deadline has not passed. Allow starts the new occurrence alongside. Forbid skips it. Replace ends an earlier occurrence that is still queued, with the failure code schedule_occurrence_replaced, and starts the new one; an occurrence that is already running cannot be stopped, because nothing can interrupt a Lambda that is executing, so while one is running Replace skips the new occurrence, as Forbid does. A skipped occurrence is consumed, not deferred: the schedule moves on to its next occurrence and the skipped one never runs. Forbid never starts an occurrence while an earlier one is in flight, and Replace never while one is running; when several are due at once (RunAllMissed after an outage) Forbid starts only the oldest and Replace only the newest. A manual run is not an occurrence: the policy neither holds it back nor is held back by it, and a one-shot has no earlier occurrence, so the policy does not apply to it. The decision is made in the transaction that enqueues the occurrence, so two schedulers cannot both decide. An unknown name is a bare 400. Required: a request that leaves it out is a 400 whose problem names the member.
    concurrency_policy: Optional[ConcurrencyPolicy] = None
    # Five fields, minute hour day-of-month month day-of-week, each a number, *, a list (1,15), a range (1-5), a step (*/5 or 8-18/2) or, for the month and the day of the week, a name (JAN, MON); 7 is Sunday. No seconds field and no @ macros. It is evaluated in timeZone. A one-shot still sends a valid expression, which does not decide when it runs.
    cron: Optional[str] = None
    # How long an occurrence's invocation has to finish, counted from when the scheduler starts it: the poll that finds the occurrence due, which is a little after its time.
    deadline_seconds: Optional[int] = None
    # The id of a deployed Stateless Lambda.
    function_id: Optional[str] = None
    # The exact released version of that function which every occurrence runs. It must be admitted for the project, and an update checks it again.
    function_version: Optional[str] = None
    # How many times an occurrence's invocation may be attempted.
    max_attempts: Optional[int] = None
    # The most missed occurrences RunAllMissed runs after an outage. The other policies ignore it.
    max_catch_up: Optional[int] = None
    # What the scheduler does about occurrences it missed while it was unavailable. RunImmediately runs once, now, however many were missed, and resumes the cadence. SkipToNextOccurrence abandons what it missed and resumes at the next occurrence. RunAllMissed runs each missed occurrence, oldest first, up to maxCatchUp. An unknown name is a bare 400. Required: a request that leaves it out is a 400 whose problem names the member.
    misfire_policy: Optional[MisfirePolicy] = None
    # Makes the schedule a one-shot: it runs once at this instant, which must be a UTC timestamp (Z or +00:00), and is then deleted. An instant already past runs at the scheduler's next poll. Absent or null is a recurring schedule. An update keeps a schedule's kind: it cannot turn a one-shot into a recurring schedule or the reverse (409).
    one_shot_at_utc: Optional[datetime.datetime] = None
    # The request every occurrence's invocation carries, at most 1,048,576 bytes before base64 encoding. Absent, null or empty is none.
    payload_base64: Optional[str] = None
    # The schedule's id, chosen by the caller and the same as the path's: 1 to 128 characters, a letter or a digit first and then letters, digits and . _ ~ : - only. The id is one segment of the schedule's URL path, so it is made of what a path segment carries without being escaped (a slash would end the segment, a space or a character outside ASCII would have to be escaped, and no id may be . or .., which URL clients resolve away), and the console addresses the schedule by the same id. An id that does not follow the rule is a 400 whose problem names the member and the rule in its detail.
    schedule_id: Optional[str] = None
    # The time zone the cron expression is evaluated in, by the id the gateway's system knows it by (UTC, Asia/Seoul). An unknown one is 400.
    time_zone: Optional[str] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> ScheduleDefinitionRequest:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: ScheduleDefinitionRequest
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return ScheduleDefinitionRequest()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        from .concurrency_policy import ConcurrencyPolicy
        from .misfire_policy import MisfirePolicy

        from .concurrency_policy import ConcurrencyPolicy
        from .misfire_policy import MisfirePolicy

        fields: dict[str, Callable[[Any], None]] = {
            "concurrencyPolicy": lambda n : setattr(self, 'concurrency_policy', n.get_enum_value(ConcurrencyPolicy)),
            "cron": lambda n : setattr(self, 'cron', n.get_str_value()),
            "deadlineSeconds": lambda n : setattr(self, 'deadline_seconds', n.get_int_value()),
            "functionId": lambda n : setattr(self, 'function_id', n.get_str_value()),
            "functionVersion": lambda n : setattr(self, 'function_version', n.get_str_value()),
            "maxAttempts": lambda n : setattr(self, 'max_attempts', n.get_int_value()),
            "maxCatchUp": lambda n : setattr(self, 'max_catch_up', n.get_int_value()),
            "misfirePolicy": lambda n : setattr(self, 'misfire_policy', n.get_enum_value(MisfirePolicy)),
            "oneShotAtUtc": lambda n : setattr(self, 'one_shot_at_utc', n.get_datetime_value()),
            "payloadBase64": lambda n : setattr(self, 'payload_base64', n.get_str_value()),
            "scheduleId": lambda n : setattr(self, 'schedule_id', n.get_str_value()),
            "timeZone": lambda n : setattr(self, 'time_zone', n.get_str_value()),
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
        writer.write_enum_value("concurrencyPolicy", self.concurrency_policy)
        writer.write_str_value("cron", self.cron)
        writer.write_int_value("deadlineSeconds", self.deadline_seconds)
        writer.write_str_value("functionId", self.function_id)
        writer.write_str_value("functionVersion", self.function_version)
        writer.write_int_value("maxAttempts", self.max_attempts)
        writer.write_int_value("maxCatchUp", self.max_catch_up)
        writer.write_enum_value("misfirePolicy", self.misfire_policy)
        writer.write_datetime_value("oneShotAtUtc", self.one_shot_at_utc)
        writer.write_str_value("payloadBase64", self.payload_base64)
        writer.write_str_value("scheduleId", self.schedule_id)
        writer.write_str_value("timeZone", self.time_zone)
    

