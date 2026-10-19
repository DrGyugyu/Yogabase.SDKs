from __future__ import annotations
import datetime
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
from typing import Any, Optional, TYPE_CHECKING, Union
from uuid import UUID

if TYPE_CHECKING:
    from .job_status import JobStatus

@dataclass
class Job(Parsable):
    # How many attempts have started: 0 until a worker first claims the job, and never more than maxAttempts.
    attempt: Optional[int] = None
    # True once the job's owner has asked for it to stop, and so on a cancelled job.
    cancellation_requested: Optional[bool] = None
    # When the job became Succeeded, Cancelled or DeadLettered. Null before.
    completed_at: Optional[datetime.datetime] = None
    # The createdAt property
    created_at: Optional[datetime.datetime] = None
    # Why the last failed attempt failed, as a stable code. Null until an attempt fails, and again once the job succeeds or is cancelled.
    failure_code: Optional[str] = None
    # The jobId property
    job_id: Optional[UUID] = None
    # The jobType property
    job_type: Optional[str] = None
    # The maxAttempts property
    max_attempts: Optional[int] = None
    # How far the job reports it has got; 100 once it has succeeded.
    progress_percent: Optional[int] = None
    # A name for the stored result, not a capability: the bytes are read from the result route. Null while there is none.
    result_reference: Optional[str] = None
    # Queued and Running are in progress. Succeeded, Cancelled and DeadLettered are final: nothing about the job changes again. A failed attempt sends the job back to Queued for its next attempt, or on to DeadLettered once maxAttempts attempts have started. Failed names a failed attempt between those two, is never final, and is not a state the ledger records: a client that reads it treats the job as in progress.
    status: Optional[JobStatus] = None
    # The updatedAt property
    updated_at: Optional[datetime.datetime] = None
    
    @staticmethod
    def create_from_discriminator_value(parse_node: ParseNode) -> Job:
        """
        Creates a new instance of the appropriate class based on discriminator value
        param parse_node: The parse node to use to read the discriminator value and create the object
        Returns: Job
        """
        if parse_node is None:
            raise TypeError("parse_node cannot be null.")
        return Job()
    
    def get_field_deserializers(self,) -> dict[str, Callable[[ParseNode], None]]:
        """
        The deserialization information for the current model
        Returns: dict[str, Callable[[ParseNode], None]]
        """
        from .job_status import JobStatus

        from .job_status import JobStatus

        fields: dict[str, Callable[[Any], None]] = {
            "attempt": lambda n : setattr(self, 'attempt', n.get_int_value()),
            "cancellationRequested": lambda n : setattr(self, 'cancellation_requested', n.get_bool_value()),
            "completedAt": lambda n : setattr(self, 'completed_at', n.get_datetime_value()),
            "createdAt": lambda n : setattr(self, 'created_at', n.get_datetime_value()),
            "failureCode": lambda n : setattr(self, 'failure_code', n.get_str_value()),
            "jobId": lambda n : setattr(self, 'job_id', n.get_uuid_value()),
            "jobType": lambda n : setattr(self, 'job_type', n.get_str_value()),
            "maxAttempts": lambda n : setattr(self, 'max_attempts', n.get_int_value()),
            "progressPercent": lambda n : setattr(self, 'progress_percent', n.get_int_value()),
            "resultReference": lambda n : setattr(self, 'result_reference', n.get_str_value()),
            "status": lambda n : setattr(self, 'status', n.get_enum_value(JobStatus)),
            "updatedAt": lambda n : setattr(self, 'updated_at', n.get_datetime_value()),
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
        writer.write_int_value("attempt", self.attempt)
        writer.write_bool_value("cancellationRequested", self.cancellation_requested)
        writer.write_datetime_value("completedAt", self.completed_at)
        writer.write_datetime_value("createdAt", self.created_at)
        writer.write_str_value("failureCode", self.failure_code)
        writer.write_uuid_value("jobId", self.job_id)
        writer.write_str_value("jobType", self.job_type)
        writer.write_int_value("maxAttempts", self.max_attempts)
        writer.write_int_value("progressPercent", self.progress_percent)
        writer.write_str_value("resultReference", self.result_reference)
        writer.write_enum_value("status", self.status)
        writer.write_datetime_value("updatedAt", self.updated_at)
    

