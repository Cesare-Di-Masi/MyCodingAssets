#!/bin/bash
echo "[+] Scansione in corso..."
sudo nmap -sS -O -sV -T4 -oX data/scan.xml $1
echo "[+] Scan completato. Output salvato in data/scan.xml"
