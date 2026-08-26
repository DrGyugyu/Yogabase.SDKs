from enum import Enum

class Capability_id(str, Enum):
    Auth = "auth",
    DataRest = "data-rest",
    Storage = "storage",
    FunctionsRest = "functions-rest",
    RealtimeSubscribe = "realtime-subscribe",

