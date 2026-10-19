from enum import Enum

class ConcurrencyPolicy(str, Enum):
    Allow = "Allow",
    Forbid = "Forbid",
    Replace = "Replace",

