from PyQt5.QtWidgets import QGraphicsScene, QGraphicsView, QGraphicsEllipseItem, QGraphicsTextItem
from PyQt5.QtGui import QBrush, QColor, QPen
from PyQt5.QtCore import Qt, QRectF
from core.scene_builder import layout_celestial_bodies

class NetworkRenderer(QGraphicsView):
    def __init__(self, network_root):
        super().__init__()
        self.scene = QGraphicsScene()
        self.setScene(self.scene)
        self.network_root = network_root
        self.draw_network()

    def draw_network(self):
        pos = layout_celestial_bodies(self.network_root)
        colors = {
            'star': QColor(255, 255, 0),
            'planet': QColor(0, 128, 255),
            'moon': QColor(192, 192, 192),
            'blackhole': QColor(0, 0, 0),
            'nebula': QColor(128, 0, 128),
            'satellite': QColor(0, 255, 255),
        }
        for body_id, (x, y) in pos.items():
            body = self.find_body(self.network_root, body_id)
            color = colors.get(body.body_type, Qt.white)
            item = QGraphicsEllipseItem(QRectF(x-10, y-10, 20, 20))
            item.setBrush(QBrush(color))
            item.setPen(QPen(Qt.black))
            self.scene.addItem(item)
            text = QGraphicsTextItem(body.label)
            text.setPos(x + 12, y - 10)
            self.scene.addItem(text)

    def find_body(self, root, target_id):
        if root.id == target_id:
            return root
        for child in root.children:
            found = self.find_body(child, target_id)
            if found:
                return found
        return None
