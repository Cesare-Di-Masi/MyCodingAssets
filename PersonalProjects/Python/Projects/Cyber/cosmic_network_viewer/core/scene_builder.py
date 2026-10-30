import math

def layout_celestial_bodies(root, center=(400, 400), radius_step=100):
    positions = {}
    def layout(body, center, level=0):
        children = body.children
        angle_step = 360 / max(1, len(children))
        positions[body.id] = center
        for i, child in enumerate(children):
            angle = math.radians(i * angle_step)
            radius = (level + 1) * radius_step
            x = center[0] + radius * math.cos(angle)
            y = center[1] + radius * math.sin(angle)
            layout(child, (x, y), level + 1)
    layout(root, center)
    return positions
