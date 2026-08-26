from enum import Enum

class DataValue_type(str, Enum):
    Null = "null",
    String = "string",
    Bool = "bool",
    Int64 = "int64",
    Decimal = "decimal",
    Double = "double",
    Guid = "guid",
    Timestamp = "timestamp",
    Json = "json",

