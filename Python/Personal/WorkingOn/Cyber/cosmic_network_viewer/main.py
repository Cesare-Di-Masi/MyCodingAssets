import sys
from PyQt5.QtWidgets import QApplication
from ui.renderer import NetworkRenderer
from core.importer import parse_nmap_xml

if __name__ == "__main__":
    app = QApplication(sys.argv)
    net = parse_nmap_xml("data/scan.xml")
    viewer = NetworkRenderer(net)
    viewer.setWindowTitle("Cosmic Network Viewer")
    viewer.show()
    sys.exit(app.exec_())
