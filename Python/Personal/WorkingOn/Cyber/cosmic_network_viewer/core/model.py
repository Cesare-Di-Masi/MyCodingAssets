
class Node:
    def __init__(self, id, name, ip, mac, type, services=None):
        self.id = id
        self.name = name
        self.ip = ip
        self.mac = mac
        self.type = type
        self.services = services or []

class Service:
    def __init__(self, port, protocol, name, status):
        self.port = port
        self.protocol = protocol
        self.name = name
        self.status = status

class VLAN:
    def __init__(self, id, name, color, nodes=None):
        self.id = id
        self.name = name
        self.color = color
        self.nodes = nodes or []

class Network:
    def __init__(self, name, vlans=None):
        self.name = name
        self.vlans = vlans or []
