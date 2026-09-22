from enum import Enum

class Capability_id(str, Enum):
    Auth = "auth",
    Storage = "storage",
    FunctionsRest = "functions-rest",
    RealtimeSubscribe = "realtime-subscribe",
    RealtimePresence = "realtime-presence",

