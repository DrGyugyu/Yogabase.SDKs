from enum import Enum

class JobStatus(str, Enum):
    Queued = "Queued",
    Running = "Running",
    Succeeded = "Succeeded",
    Failed = "Failed",
    Cancelled = "Cancelled",
    DeadLettered = "DeadLettered",

