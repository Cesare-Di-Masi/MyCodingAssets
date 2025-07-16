
import math

def layout_vlans(network, center_x, center_y, radius_step=300):
    vlans = network.vlans
    angle_step = 2 * math.pi / len(vlans)
    positions = []
    for i, vlan in enumerate(vlans):
        angle = i * angle_step
        x = center_x + math.cos(angle) * radius_step
        y = center_y + math.sin(angle) * radius_step
        positions.append((vlan, x, y))
    return positions
