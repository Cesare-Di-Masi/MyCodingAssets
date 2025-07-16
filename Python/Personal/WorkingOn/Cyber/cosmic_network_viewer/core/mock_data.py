
from .model import Network, VLAN, Node, Service
import random

def generate_test_network():
    net = Network("Aeroporto")
    for vid in range(1, 9):
        vlan = VLAN(vid, f"VLAN-{vid}", f"hsl({vid * 45}, 70%, 60%)")
        for i in range(random.randint(5, 20)):
            node_type = random.choice(["host", "server", "switch"])
            node = Node(
                id=f"{vid}-{i}",
                name=f"{node_type.upper()}-{vid}-{i}",
                ip=f"192.168.{vid}.{i+10}",
                mac=f"00:11:22:33:{vid:02x}:{i:02x}",
                type=node_type,
                services=[
                    Service(port=80, protocol="TCP", name="HTTP", status="open")
                ] if node_type != "switch" else []
            )
            vlan.nodes.append(node)
        net.vlans.append(vlan)
    return net
