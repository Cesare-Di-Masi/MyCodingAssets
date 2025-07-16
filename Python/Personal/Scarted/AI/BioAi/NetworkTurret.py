import threading
import time
import scapy.all as scapy
import os
import subprocess
import sqlite3
from datetime import datetime
from collections import defaultdict
import re

# === CONFIG ===
INTERFACE = "wlan0mon"
DB_PATH = "defender_logs.db"
HONEYPOT_VLAN_ID = 99
SEVERITY_THRESHOLD = {"Low": 1, "Medium": 2, "High": 3, "Critical": 4}
MAC_PENALTY = defaultdict(int)
MAC_TIMERS = defaultdict(float)
LOCK = threading.Lock()

# === DB INIT ===
def init_db():
    with sqlite3.connect(DB_PATH) as conn:
        c = conn.cursor()
        c.execute('''CREATE TABLE IF NOT EXISTS logs (
                        timestamp TEXT,
                        mac TEXT,
                        ip TEXT,
                        reason TEXT,
                        severity TEXT,
                        fingerprint TEXT
                    )''')
        c.execute('''CREATE TABLE IF NOT EXISTS quarantine (
                        mac TEXT PRIMARY KEY,
                        entered_at TEXT,
                        reason TEXT,
                        duration INTEGER
                    )''')
        conn.commit()

# === LOGGING ===
def log_event(mac, ip, reason, severity, fingerprint):
    with LOCK:
        timestamp = datetime.utcnow().isoformat()
        with sqlite3.connect(DB_PATH) as conn:
            c = conn.cursor()
            c.execute("INSERT INTO logs VALUES (?, ?, ?, ?, ?, ?)",
                      (timestamp, mac, ip, reason, severity, fingerprint))
            conn.commit()
        print(f"[{severity}] {timestamp} | {mac} | {ip} | {reason}")

# === QUARANTENA ===
def quarantine_mac(mac, reason, duration):
    with LOCK:
        timestamp = datetime.utcnow().isoformat()
        with sqlite3.connect(DB_PATH) as conn:
            c = conn.cursor()
            c.execute("INSERT OR REPLACE INTO quarantine VALUES (?, ?, ?, ?)",
                      (mac, timestamp, reason, duration))
            cmd = f"iptables -A INPUT -m mac --mac-source {mac} -j DROP"
            subprocess.run(cmd, shell=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            print(f"[BLOCKED] {mac} quarantined for {duration}s due to {reason}")

# === FINGERPRINT ===
def extract_fingerprint(pkt):
    fields = {
        'len': len(pkt),
        'type': getattr(pkt, 'type', -1),
        'subtype': getattr(pkt, 'subtype', -1),
        'proto': getattr(pkt, 'proto', -1),
        'rssi': getattr(pkt, 'dBm_AntSignal', -999),
        'src': pkt.addr2,
        'dst': pkt.addr1,
        'summary': pkt.summary()
    }
    return str(fields)

# === ANALISI PACCHETTI ===
def detect_packet(pkt):
    if not pkt.haslayer(scapy.Dot11):
        return
    mac = pkt.addr2
    if mac is None:
        return

    fp = extract_fingerprint(pkt)
    reason = "Generic"
    severity = "Low"

    if pkt.type == 0 and pkt.subtype == 12:
        reason = "Deauth Flood Detected"
        severity = "High"
        MAC_PENALTY[mac] += 3
    elif pkt.type == 0 and pkt.subtype == 8:
        ssid = pkt.info.decode(errors="ignore") if hasattr(pkt, "info") else "Unknown"
        if ssid in ssid_registry and ssid_registry[ssid] != pkt.addr3:
            reason = f"Rogue Beacon Detected: {ssid}"
            severity = "Critical"
            MAC_PENALTY[mac] += 4
        ssid_registry[ssid] = pkt.addr3
    elif pkt.haslayer(scapy.ARP):
        reason = "ARP Packet"
        severity = "Medium"
        MAC_PENALTY[mac] += 1

    if MAC_PENALTY[mac] >= 5:
        if mac not in MAC_TIMERS or (time.time() - MAC_TIMERS[mac] > 600):
            quarantine_mac(mac, reason, 3600)
            MAC_TIMERS[mac] = time.time()

    log_event(mac, "0.0.0.0", reason, severity, fp)

# === THREAD: SNIFFER ===
def packet_sniffer():
    scapy.sniff(iface=INTERFACE, prn=detect_packet, store=0)

# === THREAD: CHURN MONITOR ===
CHURN_TRACKER = defaultdict(list)

def churn_monitor():
    while True:
        now = time.time()
        for mac, ts_list in list(CHURN_TRACKER.items()):
            CHURN_TRACKER[mac] = [t for t in ts_list if now - t < 60]
            if len(CHURN_TRACKER[mac]) > 3:
                quarantine_mac(mac, "MAC Churn > 3 in 60s", 1800)
                CHURN_TRACKER.pop(mac)
        time.sleep(15)

# === THREAD: HONEYPOT VLAN ENFORCER ===
def vlan_enforcer():
    while True:
        with sqlite3.connect(DB_PATH) as conn:
            c = conn.cursor()
            c.execute("SELECT mac, entered_at, duration FROM quarantine")
            for mac, entered_at, duration in c.fetchall():
                entered = datetime.fromisoformat(entered_at)
                if (datetime.utcnow() - entered).total_seconds() > duration:
                    cmd = f"iptables -D INPUT -m mac --mac-source {mac} -j DROP"
                    subprocess.run(cmd, shell=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                    c.execute("DELETE FROM quarantine WHERE mac = ?", (mac,))
                    print(f"[RELEASED] {mac} exited quarantine")
        time.sleep(60)

# === SSID REGISTRY ===
ssid_registry = {}

# === MAIN ===
def main():
    init_db()
    threads = [
        threading.Thread(target=packet_sniffer),
        threading.Thread(target=churn_monitor),
        threading.Thread(target=vlan_enforcer),
    ]
    for t in threads:
        t.daemon = True
        t.start()
    while True:
        time.sleep(1)

main()
