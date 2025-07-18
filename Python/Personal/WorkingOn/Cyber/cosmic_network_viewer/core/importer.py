import xml.etree.ElementTree as ET
from core.celestial_logic import Star, Planet, Moon, BlackHole, Nebula

def parse_nmap_xml(path):
    tree = ET.parse(path)
    root = tree.getroot()
    celestial_objects = []
    for i, host in enumerate(root.findall("host")):
        addr = host.find("address").attrib.get("addr")
        name = host.findtext("hostnames/hostname[@name]", default=addr)
        last = int(addr.split(".")[-1])
        if last == 1:
            obj = Star(addr, name)
        elif last in [2, 3]:
            obj = Planet(addr, name)
        elif last in [4, 5]:
            obj = Moon(addr, name)
        elif last == 254:
            obj = BlackHole(addr, name)
        else:
            obj = Nebula(addr, name)
        celestial_objects.append(obj)
    # basic attachment logic: star -> planets -> moons
    stars = [o for o in celestial_objects if o.body_type == "star"]
    planets = [o for o in celestial_objects if o.body_type == "planet"]
    moons = [o for o in celestial_objects if o.body_type == "moon"]
    blackholes = [o for o in celestial_objects if o.body_type == "blackhole"]
    nebulas = [o for o in celestial_objects if o.body_type == "nebula"]
    if stars:
        star = stars[0]
        for p in planets:
            star.add_child(p)
        for i, m in enumerate(moons):
            planets[i % len(planets)].add_child(m)
        for b in blackholes + nebulas:
            star.add_child(b)
        return star
    return None
