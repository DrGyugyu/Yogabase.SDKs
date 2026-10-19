from enum import Enum

class DataFilter_op(str, Enum):
    Eq = "eq",
    Neq = "neq",
    Gt = "gt",
    Gte = "gte",
    Lt = "lt",
    Lte = "lte",
    In_ = "in",
    Like = "like",
    Ilike = "ilike",
    IsNull = "isNull",
    NotNull = "notNull",

