
# Cosmic Network Viewer

Una visualizzazione orbitale 2D in stile **Kurzgesagt** per reti complesse.

## 🔧 Requisiti

- Python 3.10+
- PyQt5
- Nmap (per scansioni attive)
- Scapy (per scansioni passive)

## ▶️ Esecuzione GUI (con dati simulati)

```bash
pip install PyQt5 scapy
python main.py
```

## ▶️ Esecuzione con scansione reale

```bash
sudo bash tools/scan_launcher.sh 192.168.1.0/24
python main.py
```

## 🔍 Descrizione

- Ogni VLAN è un'orbita
- Ogni nodo è un pianeta
- Servizi possono orbitare o comparire su hover

## 📂 Struttura

- `core/`: logica di rete e scansione
- `ui/`: rendering orbitale
- `tools/`: scansione da shell
- `data/`: XML Nmap o dump pcap

## 🛰️ Estensioni future

- Supporto live packet capture
- Interfaccia cliccabile su nodi
- SVG e animazioni orbitanti
