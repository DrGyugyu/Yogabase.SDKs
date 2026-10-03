from enum import Enum

class MisfirePolicy(str, Enum):
    RunImmediately = "RunImmediately",
    SkipToNextOccurrence = "SkipToNextOccurrence",
    RunAllMissed = "RunAllMissed",

