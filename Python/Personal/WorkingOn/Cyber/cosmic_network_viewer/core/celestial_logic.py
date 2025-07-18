class CelestialBody:
    def __init__(self, id, label, body_type):
        self.id = id
        self.label = label
        self.body_type = body_type  # 'star', 'planet', 'moon', etc.
        self.children = []
        self.parent = None

    def add_child(self, body):
        self.children.append(body)
        body.parent = self

class Star(CelestialBody):
    def __init__(self, id, label):
        super().__init__(id, label, 'star')

class Planet(CelestialBody):
    def __init__(self, id, label):
        super().__init__(id, label, 'planet')

class Moon(CelestialBody):
    def __init__(self, id, label):
        super().__init__(id, label, 'moon')

class BlackHole(CelestialBody):
    def __init__(self, id, label):
        super().__init__(id, label, 'blackhole')

class Nebula(CelestialBody):
    def __init__(self, id, label):
        super().__init__(id, label, 'nebula')

class Satellite(CelestialBody):
    def __init__(self, id, label):
        super().__init__(id, label, 'satellite')
