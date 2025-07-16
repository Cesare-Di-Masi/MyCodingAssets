
from xml.etree import ElementTree as ET
from core.model import Node, VLAN, Network, Service

def parse_nmap_xml(path: str) -> Network:
    tree = ET.parse(path)
    root = tree.getroot()
    net = Network("Scanned Network")

    vlan = VLAN(1, "default", "#88c0d0")

    for host in root.findall("host"):
        ip = host.find("address[@addrtype='ipv4']").attrib['addr']
        mac_elem = host.find("address[@addrtype='mac']")
        mac = mac_elem.attrib['addr'] if mac_elem is not None else "unknown"
        hostname = ip
        node_type = "host"

        services = []
        for port in host.findall("ports/port"):
            proto = port.attrib['protocol']
            pnum = int(port.attrib['portid'])
            state = port.find("state").attrib['state']
            service = port.find("service")
            sname = service.attrib['name'] if service is not None else "unknown"
            services.append(Service(port=pnum, protocol=proto, name=sname, status=state))

        node = Node(id=ip, name=hostname, ip=ip, mac=mac, type=node_type, services=services)
        vlan.nodes.append(node)

    net.vlans.append(vlan)
    return net
