
from PyQt5.QtWidgets import QWidget
from PyQt5.QtGui import QPainter, QColor, QPen
from PyQt5.QtCore import Qt
import math

class NetworkRenderer(QWidget):
    def __init__(self, network):
        super().__init__()
        self.network = network
        self.setMinimumSize(1200, 900)

    def paintEvent(self, event):
        painter = QPainter(self)
        painter.fillRect(self.rect(), QColor("#0f1c2e"))

        cx, cy = self.width() // 2, self.height() // 2
        vlans = self.network.vlans
        step = 250
        for i, vlan in enumerate(vlans):
            radius = (i + 1) * step
            color = QColor(vlan.color)
            painter.setPen(QPen(color, 2, Qt.SolidLine))
            painter.drawEllipse(cx - radius, cy - radius, radius * 2, radius * 2)
            for j, node in enumerate(vlan.nodes):
                angle = (2 * math.pi * j) / len(vlan.nodes)
                nx = cx + math.cos(angle) * radius
                ny = cy + math.sin(angle) * radius
                painter.setBrush(QColor("#3d5a80") if node.type == "host" else QColor("#ee6c4d"))
                painter.drawEllipse(int(nx) - 8, int(ny) - 8, 16, 16)
