from enum import Enum

class GameServerPort_protocol(str, Enum):
    Tcp = "tcp",
    Udp = "udp",

