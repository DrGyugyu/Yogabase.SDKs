from enum import Enum

class ActorInvocationResult_status(str, Enum):
    InProgress = "InProgress",
    Completed = "Completed",
    Failed = "Failed",

