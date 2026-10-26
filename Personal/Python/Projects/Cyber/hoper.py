#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
NETWORK PENETRATION TOOL - VERSIONE COMPLETA
Scopo educativo - Utilizzare solo su reti autorizzate
"""

import os
import sys
import subprocess
import threading
import multiprocessing
from concurrent.futures import ThreadPoolExecutor
import datetime
import time
import argparse
import re
import signal
import json
import socket
import struct
import platform
from pathlib import Path
import sqlite3
import base64
import hashlib
import binascii
import tempfile
import shutil
import requests
import zipfile
import tarfile
import gzip
import random
import string
import ipaddress
import netifaces
import psutil
import getpass
import secrets
import queue
import asyncio
import webbrowser
from flask import Flask, render_template, request, jsonify, send_from_directory
from flask_socketio import SocketIO
from flask_cors import CORS
from werkzeug.security import generate_password_hash, check_password_hash
from jinja2 import Environment, FileSystemLoader
from cryptography.fernet import Fernet
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
from cryptography.hazmat.backends import default_backend
from cryptography.hazmat.primitives import serialization
from cryptography.hazmat.primitives.asymmetric import rsa
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.kdf.scrypt import Scrypt
from cryptography.hazmat.primitives.ciphers.aead import AESGCM
from cryptography.hazmat.primitives import constant_time
from cryptography.hazmat.primitives.kdf.hkdf import HKDF

# Configurazione
WORK_DIR = "/tmp/pentest_complete"
DB_FILE = os.path.join(WORK_DIR, "pentest.db")
ENCRYPTION_KEY_FILE = os.path.join(WORK_DIR, "encryption.key")
TEMPLATES_DIR = os.path.join(WORK_DIR, "templates")
STATIC_DIR = os.path.join(WORK_DIR, "static")

# Inizializzazione directory
os.makedirs(WORK_DIR, exist_ok=True)
os.makedirs(TEMPLATES_DIR, exist_ok=True)
os.makedirs(STATIC_DIR, exist_ok=True)

# Controllo privilegi root
if os.geteuid() != 0:
    print("ERRORE: Eseguire come root (sudo) per alcune funzionalità")
    sys.exit(1)

# Inizializzazione Flask
app = Flask(__name__)
app.config['SECRET_KEY'] = secrets.token_hex(16)
app.config['SQLALCHEMY_DATABASE_URI'] = f'sqlite:///{DB_FILE}'
app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False
CORS(app)
socketio = SocketIO(app, cors_allowed_origins="*")

# Database
from flask_sqlalchemy import SQLAlchemy
db = SQLAlchemy(app)

# Modelli database
class ScanResult(db.Model):
    id = db.Column(db.Integer, primary_key=True)
    scan_type = db.Column(db.String(50))
    target = db.Column(db.String(255))
    result = db.Column(db.Text)
    timestamp = db.Column(db.DateTime, default=datetime.datetime.utcnow)
    encrypted = db.Column(db.Boolean, default=False)

class DecryptedKey(db.Model):
    id = db.Column(db.Integer, primary_key=True)
    scan_id = db.Column(db.Integer, db.ForeignKey('scan_result.id'))
    key = db.Column(db.String(255))
    iv = db.Column(db.String(255))
    timestamp = db.Column(db.DateTime, default=datetime.datetime.utcnow)

# Creazione tabelle
with app.app_context():
    db.create_all()

# Generazione chiave di crittografia
def generate_encryption_key():
    if not os.path.exists(ENCRYPTION_KEY_FILE):
        key = Fernet.generate_key()
        with open(ENCRYPTION_KEY_FILE, 'wb') as f:
            f.write(key)
    with open(ENCRYPTION_KEY_FILE, 'rb') as f:
        return f.read()

ENCRYPTION_KEY = generate_encryption_key()
cipher_suite = Fernet(ENCRYPTION_KEY)

# Funzioni di crittografia
def encrypt_data(data):
    return cipher_suite.encrypt(data.encode())

def decrypt_data(encrypted_data):
    return cipher_suite.decrypt(encrypted_data).decode()

# Installazione tool
def install_tools():
    print("Installazione tool necessari...")
    
    # Aggiornamento sistema
    subprocess.run(["apt", "update"], check=True)
    
    # Tool di base
    tools = [
        "nmap", "tshark", "nikto", "sqlmap", "xsser", 
        "hydra", "aircrack-ng", "dirb", "gobuster", "john",
        "hashcat", "metasploit-framework", "wireshark", "tcpdump",
        "netcat", "socat", "sslscan", "whatweb", "wpscan",
        "recon-ng", "theHarvester", "cewl", "crunch", "pyrit"
    ]
    
    for tool in tools:
        try:
            subprocess.run(["apt", "install", "-y", tool], check=True)
            print(f"Installato: {tool}")
        except subprocess.CalledProcessError:
            print(f"Errore nell'installazione di: {tool}")
    
    # Librerie Python
    python_libs = [
        "flask", "flask-socketio", "flask-cors", "flask-sqlalchemy",
        "cryptography", "requests", "colorama", "psutil", "netifaces",
        "aiohttp", "jinja2", "werkzeug", "click", "itsdangerous",
        "markupsafe", "pyparsing", "packaging", "appdirs",
        "setuptools", "wheel", "pip", "pyopenssl", "pycryptodome",
        "bcrypt", "pyjwt", "pymongo", "redis", "celery",
        "sqlalchemy", "alembic", "migrate", "flask-migrate",
        "flask-marshmallow", "flask-restful", "flask-httpauth",
        "flask-login", "flask-bcrypt", "pymysql", "psycopg2-binary"
    ]
    
    for lib in python_libs:
        try:
            subprocess.run([sys.executable, "-m", "pip", "install", lib], check=True)
            print(f"Installata libreria: {lib}")
        except subprocess.CalledProcessError:
            print(f"Errore nell'installazione di: {lib}")

# Template HTML
INDEX_TEMPLATE = """
<!DOCTYPE html>
<html>
<head>
    <title>Network Penetration Tool</title>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css" rel="stylesheet">
    <script src="https://cdn.socket.io/4.4.1/socket.io.min.js"></script>
    <style>
        body { background-color: #1a1a1a; color: #fff; }
        .sidebar { background-color: #2d2d2d; min-height: 100vh; }
        .main-content { background-color: #333; border-radius: 10px; }
        .btn-custom { background-color: #0d6efd; border: none; }
        .btn-custom:hover { background-color: #0b5ed7; }
        .result-card { background-color: #444; border-radius: 8px; margin-bottom: 15px; }
        .log-area { background-color: #222; border-radius: 5px; font-family: monospace; }
    </style>
</head>
<body>
    <div class="container-fluid">
        <div class="row">
            <div class="col-md-3 sidebar p-4">
                <h4 class="text-center mb-4">PENETRATION TOOL</h4>
                <div class="d-grid gap-2">
                    <button class="btn btn-custom btn-block" onclick="showSection('scan')">Scansione Rete</button>
                    <button class="btn btn-custom btn-block" onclick="showSection('sniff')">Sniffing</button>
                    <button class="btn btn-custom btn-block" onclick="showSection('vuln')">Vulnerabilità</button>
                    <button class="btn btn-custom btn-block" onclick="showSection('web')">Attacchi Web</button>
                    <button class="btn btn-custom btn-block" onclick="showSection('wifi')">Wireless</button>
                    <button class="btn btn-custom btn-block" onclick="showSection('decrypt')">Decrittazione</button>
                    <button class="btn btn-custom btn-block" onclick="showSection('results')">Risultati</button>
                </div>
                <hr class="my-4">
                <div class="text-center">
                    <small>Versione 1.0<br>Solo per uso educativo</small>
                </div>
            </div>
            <div class="col-md-9">
                <div class="main-content p-4">
                    <!-- Scansione Rete -->
                    <div id="scan-section" class="content-section">
                        <h3>Scansione Rete</h3>
                        <div class="row mb-3">
                            <div class="col-md-6">
                                <input type="text" id="scan-target" class="form-control" placeholder="Target (es. 192.168.1.0/24)">
                            </div>
                            <div class="col-md-3">
                                <input type="text" id="scan-ports" class="form-control" placeholder="Porte (opzionale)">
                            </div>
                            <div class="col-md-3">
                                <button class="btn btn-success w-100" onclick="startScan()">Avvia Scansione</button>
                            </div>
                        </div>
                        <div class="progress d-none" id="scan-progress">
                            <div class="progress-bar progress-bar-striped progress-bar-animated" role="progressbar"></div>
                        </div>
                        <div class="log-area mt-3 p-3" id="scan-log" style="height: 200px; overflow-y: auto;"></div>
                    </div>
                    
                    <!-- Sniffing -->
                    <div id="sniff-section" class="content-section d-none">
                        <h3>Sniffing Traffico</h3>
                        <div class="row mb-3">
                            <div class="col-md-6">
                                <select id="sniff-interface" class="form-select">
                                    <option value="">Seleziona interfaccia</option>
                                </select>
                            </div>
                            <div class="col-md-3">
                                <input type="number" id="sniff-duration" class="form-control" placeholder="Durata (secondi)" value="60">
                            </div>
                            <div class="col-md-3">
                                <button class="btn btn-success w-100" onclick="startSniffing()">Avvia Sniffing</button>
                            </div>
                        </div>
                        <div class="log-area mt-3 p-3" id="sniff-log" style="height: 200px; overflow-y: auto;"></div>
                    </div>
                    
                    <!-- Vulnerabilità -->
                    <div id="vuln-section" class="content-section d-none">
                        <h3>Analisi Vulnerabilità</h3>
                        <div class="row mb-3">
                            <div class="col-md-8">
                                <input type="text" id="vuln-target" class="form-control" placeholder="Target IP o URL">
                            </div>
                            <div class="col-md-4">
                                <button class="btn btn-success w-100" onclick="startVulnScan()">Avvia Analisi</button>
                            </div>
                        </div>
                        <div class="log-area mt-3 p-3" id="vuln-log" style="height: 200px; overflow-y: auto;"></div>
                    </div>
                    
                    <!-- Attacchi Web -->
                    <div id="web-section" class="content-section d-none">
                        <h3>Attacchi Web</h3>
                        <div class="row mb-3">
                            <div class="col-md-4">
                                <select id="web-attack-type" class="form-select">
                                    <option value="sql">SQL Injection</option>
                                    <option value="xss">XSS</option>
                                    <option value="brute">Brute Force</option>
                                    <option value="dir">Directory Scan</option>
                                </select>
                            </div>
                            <div class="col-md-6">
                                <input type="text" id="web-target" class="form-control" placeholder="Target URL">
                            </div>
                            <div class="col-md-2">
                                <button class="btn btn-success w-100" onclick="startWebAttack()">Avvia Attacco</button>
                            </div>
                        </div>
                        <div class="log-area mt-3 p-3" id="web-log" style="height: 200px; overflow-y: auto;"></div>
                    </div>
                    
                    <!-- Wireless -->
                    <div id="wifi-section" class="content-section d-none">
                        <h3>Attacchi Wireless</h3>
                        <div class="row mb-3">
                            <div class="col-md-4">
                                <select id="wifi-attack-type" class="form-select">
                                    <option value="monitor">Monitoraggio</option>
                                    <option value="wep">WEP Crack</option>
                                    <option value="wpa">WPA Crack</option>
                                    <option value="deauth">Deauth</option>
                                </select>
                            </div>
                            <div class="col-md-4">
                                <select id="wifi-interface" class="form-select">
                                    <option value="">Seleziona interfaccia</option>
                                </select>
                            </div>
                            <div class="col-md-4">
                                <button class="btn btn-success w-100" onclick="startWifiAttack()">Avvia Attacco</button>
                            </div>
                        </div>
                        <div class="log-area mt-3 p-3" id="wifi-log" style="height: 200px; overflow-y: auto;"></div>
                    </div>
                    
                    <!-- Decrittazione -->
                    <div id="decrypt-section" class="content-section d-none">
                        <h3>Decrittazione Dati</h3>
                        <div class="row mb-3">
                            <div class="col-md-8">
                                <select id="decrypt-scan" class="form-select">
                                    <option value="">Seleziona scansione</option>
                                </select>
                            </div>
                            <div class="col-md-4">
                                <button class="btn btn-success w-100" onclick="decryptData()">Decrittografia</button>
                            </div>
                        </div>
                        <div class="log-area mt-3 p-3" id="decrypt-log" style="height: 200px; overflow-y: auto;"></div>
                    </div>
                    
                    <!-- Risultati -->
                    <div id="results-section" class="content-section d-none">
                        <h3>Risultati Scansioni</h3>
                        <div class="row mb-3">
                            <div class="col-md-6">
                                <button class="btn btn-primary w-100" onclick="loadResults()">Carica Risultati</button>
                            </div>
                            <div class="col-md-6">
                                <button class="btn btn-danger w-100" onclick="clearResults()">Cancella Tutti</button>
                            </div>
                        </div>
                        <div id="results-container"></div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/js/bootstrap.bundle.min.js"></script>
    <script>
        const socket = io();
        
        function showSection(section) {
            document.querySelectorAll('.content-section').forEach(el => {
                el.classList.add('d-none');
            });
            document.getElementById(section + '-section').classList.remove('d-none');
            
            if (section === 'scan') {
                loadNetworkInterfaces();
            } else if (section === 'sniff') {
                loadNetworkInterfaces();
            } else if (section === 'wifi') {
                loadNetworkInterfaces();
            } else if (section === 'decrypt') {
                loadScanOptions();
            }
        }
        
        function loadNetworkInterfaces() {
            fetch('/api/interfaces')
                .then(response => response.json())
                .then(data => {
                    const selects = ['sniff-interface', 'wifi-interface'];
                    selects.forEach(selectId => {
                        const select = document.getElementById(selectId);
                        select.innerHTML = '<option value="">Seleziona interfaccia</option>';
                        data.interfaces.forEach(iface => {
                            select.innerHTML += `<option value="${iface}">${iface}</option>`;
                        });
                    });
                });
        }
        
        function loadScanOptions() {
            fetch('/api/scans')
                .then(response => response.json())
                .then(data => {
                    const select = document.getElementById('decrypt-scan');
                    select.innerHTML = '<option value="">Seleziona scansione</option>';
                    data.scans.forEach(scan => {
                        select.innerHTML += `<option value="${scan.id}">${scan.scan_type} - ${scan.target}</option>`;
                    });
                });
        }
        
        function startScan() {
            const target = document.getElementById('scan-target').value;
            const ports = document.getElementById('scan-ports').value;
            
            if (!target) {
                alert('Inserisci un target valido');
                return;
            }
            
            document.getElementById('scan-progress').classList.remove('d-none');
            socket.emit('start_scan', {target, ports});
        }
        
        function startSniffing() {
            const interface = document.getElementById('sniff-interface').value;
            const duration = document.getElementById('sniff-duration').value;
            
            if (!interface || !duration) {
                alert('Seleziona interfaccia e durata');
                return;
            }
            
            socket.emit('start_sniffing', {interface, duration});
        }
        
        function startVulnScan() {
            const target = document.getElementById('vuln-target').value;
            
            if (!target) {
                alert('Inserisci un target valido');
                return;
            }
            
            socket.emit('start_vuln_scan', {target});
        }
        
        function startWebAttack() {
            const attackType = document.getElementById('web-attack-type').value;
            const target = document.getElementById('web-target').value;
            
            if (!attackType || !target) {
                alert('Seleziona tipo di attacco e target');
                return;
            }
            
            socket.emit('start_web_attack', {attack_type: attackType, target});
        }
        
        function startWifiAttack() {
            const attackType = document.getElementById('wifi-attack-type').value;
            const interface = document.getElementById('wifi-interface').value;
            
            if (!attackType || !interface) {
                alert('Seleziona tipo di attacco e interfaccia');
                return;
            }
            
            socket.emit('start_wifi_attack', {attack_type: attackType, interface});
        }
        
        function decryptData() {
            const scanId = document.getElementById('decrypt-scan').value;
            
            if (!scanId) {
                alert('Seleziona una scansione');
                return;
            }
            
            fetch(`/api/decrypt/${scanId}`)
                .then(response => response.json())
                .then(data => {
                    const logArea = document.getElementById('decrypt-log');
                    logArea.innerHTML = data.decrypted_data;
                });
        }
        
        function loadResults() {
            fetch('/api/results')
                .then(response => response.json())
                .then(data => {
                    const container = document.getElementById('results-container');
                    container.innerHTML = '';
                    
                    data.results.forEach(result => {
                        const card = document.createElement('div');
                        card.className = 'result-card p-3';
                        card.innerHTML = `
                            <h5>${result.scan_type} - ${result.target}</h5>
                            <small>${result.timestamp}</small>
                            <div class="mt-2">
                                <button class="btn btn-sm btn-outline-info" onclick="viewResult(${result.id})">Visualizza</button>
                                ${result.encrypted ? '<button class="btn btn-sm btn-outline-warning" onclick="decryptResult(${result.id})">Decrittografa</button>' : ''}
                            </div>
                        `;
                        container.appendChild(card);
                    });
                });
        }
        
        function viewResult(id) {
            fetch(`/api/result/${id}`)
                .then(response => response.json())
                .then(data => {
                    alert(data.result);
                });
        }
        
        function decryptResult(id) {
            fetch(`/api/decrypt/${id}`)
                .then(response => response.json())
                .then(data => {
                    alert(data.decrypted_data);
                });
        }
        
        function clearResults() {
            if (confirm('Sei sicuro di voler cancellare tutti i risultati?')) {
                fetch('/api/results', {method: 'DELETE'})
                    .then(() => {
                        loadResults();
                    });
            }
        }
        
        // Socket event listeners
        socket.on('scan_log', data => {
            const logArea = document.getElementById('scan-log');
            logArea.innerHTML += data.log + '\\n';
            logArea.scrollTop = logArea.scrollHeight;
        });
        
        socket.on('sniffing_log', data => {
            const logArea = document.getElementById('sniff-log');
            logArea.innerHTML += data.log + '\\n';
            logArea.scrollTop = logArea.scrollHeight;
        });
        
        socket.on('vuln_log', data => {
            const logArea = document.getElementById('vuln-log');
            logArea.innerHTML += data.log + '\\n';
            logArea.scrollTop = logArea.scrollHeight;
        });
        
        socket.on('web_log', data => {
            const logArea = document.getElementById('web-log');
            logArea.innerHTML += data.log + '\\n';
            logArea.scrollTop = logArea.scrollHeight;
        });
        
        socket.on('wifi_log', data => {
            const logArea = document.getElementById('wifi-log');
            logArea.innerHTML += data.log + '\\n';
            logArea.scrollTop = logArea.scrollHeight;
        });
        
        socket.on('scan_complete', data => {
            document.getElementById('scan-progress').classList.add('d-none');
            alert('Scansione completata!');
            loadScanOptions();
        });
        
        socket.on('sniffing_complete', data => {
            alert('Sniffing completato!');
        });
        
        socket.on('vuln_complete', data => {
            alert('Analisi vulnerabilità completata!');
            loadScanOptions();
        });
        
        socket.on('web_complete', data => {
            alert('Attacco web completato!');
            loadScanOptions();
        });
        
        socket.on('wifi_complete', data => {
            alert('Attacco wireless completato!');
        });
    </script>
</body>
</html>
"""

# Scrivi template HTML
with open(os.path.join(TEMPLATES_DIR, "index.html"), "w") as f:
    f.write(INDEX_TEMPLATE)

# Funzioni per i tool di pentest
def get_network_interfaces():
    """Ottieni le interfacce di rete disponibili"""
    try:
        result = subprocess.run(["ip", "link", "show"], capture_output=True, text=True)
        interfaces = re.findall(r'\d+: (\w+):', result.stdout)
        return [iface for iface in interfaces if iface != "lo"]
    except Exception as e:
        print(f"Errore nel ottenere interfacie: {e}")
        return []

def run_command(cmd, timeout=None):
    """Esegue un comando e restituisce l'output"""
    try:
        result = subprocess.run(cmd, shell=True, capture_output=True, text=True, timeout=timeout)
        return result.returncode == 0, result.stdout, result.stderr
    except subprocess.TimeoutExpired:
        return False, "", "Timeout"
    except Exception as e:
        return False, "", str(e)

# Funzioni per le scansioni
def scan_network(target, ports=None):
    """Esegue scansione rete con nmap"""
    output_dir = os.path.join(WORK_DIR, f"scan_{datetime.datetime.now().strftime('%Y%m%d_%H%M%S')}")
    os.makedirs(output_dir, exist_ok=True)
    
    cmd = f"nmap -sV -O --script vuln"
    if ports:
        cmd += f" -p {ports}"
    cmd += f" {target} -oN {output_dir}/scan.txt"
    
    success, stdout, stderr = run_command(cmd, timeout=300)
    
    if success:
        with open(os.path.join(output_dir, "scan.txt"), "r") as f:
            result = f.read()
        
        scan_result = ScanResult(
            scan_type="Network Scan",
            target=target,
            result=encrypt_data(result),
            encrypted=True
        )
        db.session.add(scan_result)
        db.session.commit()
        
        return output_dir
    else:
        return None

def sniff_traffic(interface, duration):
    """Esegue sniffing del traffico"""
    output_file = os.path.join(WORK_DIR, f"sniff_{interface}_{datetime.datetime.now().strftime('%Y%m%d_%H%M%S')}.pcap")
    
    cmd = f"tshark -i {interface} -a duration:{duration} -w {output_file}"
    success, stdout, stderr = run_command(cmd, timeout=int(duration) + 10)
    
    if success:
        result = f"Capture salvata in: {output_file}"
        scan_result = ScanResult(
            scan_type="Traffic Sniffing",
            target=f"{interface} ({duration}s)",
            result=encrypt_data(result),
            encrypted=True
        )
        db.session.add(scan_result)
        db.session.commit()
        
        return output_file
    else:
        return None

def vuln_analysis(target):
    """Esegue analisi delle vulnerabilità"""
    output_dir = os.path.join(WORK_DIR, f"vuln_{datetime.datetime.now().strftime('%Y%m%d_%H%M%S')}")
    os.makedirs(output_dir, exist_ok=True)
    
    # Nmap scan
    cmd = f"nmap --script vuln -sV -O -oN {output_dir}/nmap.txt {target}"
    success, stdout, stderr = run_command(cmd, timeout=600)
    
    # Nikto scan
    if target.startswith(('http://', 'https://')):
        cmd = f"nikto -h {target} -output {output_dir}/nikto.txt"
        success2, stdout2, stderr2 = run_command(cmd, timeout=600)
    else:
        success2 = True
        stdout2 = ""
    
    if success and success2:
        with open(os.path.join(output_dir, "nmap.txt"), "r") as f:
            nmap_result = f.read()
        
        nikto_result = stdout2
        
        result = f"NMAP RESULT:\n{nmap_result}\n\nNIKTO RESULT:\n{nikto_result}"
        
        scan_result = ScanResult(
            scan_type="Vulnerability Scan",
            target=target,
            result=encrypt_data(result),
            encrypted=True
        )
        db.session.add(scan_result)
        db.session.commit()
        
        return output_dir
    else:
        return None

def web_attack(attack_type, target):
    """Esegue attacchi web"""
    output_dir = os.path.join(WORK_DIR, f"web_{attack_type}_{datetime.datetime.now().strftime('%Y%m%d_%H%M%S')}")
    os.makedirs(output_dir, exist_ok=True)
    
    if attack_type == "sql":
        cmd = f"sqlmap -u '{target}' --batch --level=5 --risk=3 --output-dir={output_dir}"
    elif attack_type == "xss":
        cmd = f"xsser -u '{target}' --auto --output={output_dir}/xss.txt"
    elif attack_type == "brute":
        cmd = f"hydra -L admin -P /usr/share/wordlists/rockyou.txt '{target}' http-post-form '/login:username=^USER^&password=^PASS^:F=incorrect' -o {output_dir}/hydra.txt"
    elif attack_type == "dir":
        cmd = f"dirb {target} /usr/share/wordlists/common.txt -o {output_dir}/dirb.txt"
    else:
        return None
    
    success, stdout, stderr = run_command(cmd, timeout=600)
    
    if success:
        with open(os.path.join(output_dir, "result.txt"), "w") as f:
            f.write(stdout)
        
        scan_result = ScanResult(
            scan_type=f"Web {attack_type.upper()}",
            target=target,
            result=encrypt_data(stdout),
            encrypted=True
        )
        db.session.add(scan_result)
        db.session.commit()
        
        return output_dir
    else:
        return None

def wifi_attack(attack_type, interface):
    """Esegue attacchi wireless"""
    output_dir = os.path.join(WORK_DIR, f"wifi_{attack_type}_{datetime.datetime.now().strftime('%Y%m%d_%H%M%S')}")
    os.makedirs(output_dir, exist_ok=True)
    
    if attack_type == "monitor":
        cmd = f"airodump-ng {interface}"
    elif attack_type == "wep":
        cmd = f"aircrack-ng -b {request.json.get('bssid')} -w /usr/share/wordlists/rockyou.txt {request.json.get('cap_file')}"
    elif attack_type == "wpa":
        cmd = f"aircrack-ng -w /usr/share/wordlists/rockyou.txt {request.json.get('cap_file')}"
    elif attack_type == "deauth":
        cmd = f"aireplay-ng -0 10 -a {request.json.get('bssid')} {interface}"
    else:
        return None
    
    success, stdout, stderr = run_command(cmd, timeout=600)
    
    if success:
        with open(os.path.join(output_dir, "result.txt"), "w") as f:
            f.write(stdout)
        
        scan_result = ScanResult(
            scan_type=f"WiFi {attack_type.upper()}",
            target=interface,
            result=encrypt_data(stdout),
            encrypted=True
        )
        db.session.add(scan_result)
        db.session.commit()
        
        return output_dir
    else:
        return None

# Route API
@app.route('/')
def index():
    return render_template('index.html')

@app.route('/api/interfaces')
def get_interfaces():
    interfaces = get_network_interfaces()
    return jsonify({"interfaces": interfaces})

@app.route('/api/scans')
def get_scans():
    scans = ScanResult.query.all()
    return jsonify({"scans": [{"id": s.id, "scan_type": s.scan_type, "target": s.target} for s in scans]})

@app.route('/api/results')
def get_results():
    results = ScanResult.query.all()
    return jsonify({"results": [{"id": r.id, "scan_type": r.scan_type, "target": r.target, "timestamp": r.timestamp, "encrypted": r.encrypted} for r in results]})

@app.route('/api/results', methods=['DELETE'])
def clear_results():
    ScanResult.query.delete()
    db.session.commit()
    return jsonify({"status": "success"})

@app.route('/api/result/<int:result_id>')
def get_result(result_id):
    result = ScanResult.query.get_or_404(result_id)
    return jsonify({"result": result.result})

@app.route('/api/decrypt/<int:scan_id>')
def decrypt_scan(scan_id):
    scan = ScanResult.query.get_or_404(scan_id)
    
    if scan.encrypted:
        try:
            decrypted = decrypt_data(scan.result)
            return jsonify({"decrypted_data": decrypted})
        except Exception as e:
            return jsonify({"error": str(e)}), 500
    else:
        return jsonify({"decrypted_data": scan.result})

# SocketIO events
@socketio.on('start_scan')
def handle_start_scan(data):
    target = data.get('target')
    ports = data.get('ports')
    
    def run_scan():
        socketio.emit('scan_log', {'log': f"Inizio scansione di {target}..."})
        
        output_dir = scan_network(target, ports)
        
        if output_dir:
            socketio.emit('scan_log', {'log': f"Scansione completata. Risultati in: {output_dir}"})
            socketio.emit('scan_complete')
        else:
            socketio.emit('scan_log', {'log': "Errore durante la scansione"})
    
    thread = threading.Thread(target=run_scan)
    thread.start()

@socketio.on('start_sniffing')
def handle_start_sniffing(data):
    interface = data.get('interface')
    duration = data.get('duration')
    
    def run_sniffing():
        socketio.emit('sniffing_log', {'log': f"Inizio sniffing su {interface} per {duration} secondi..."})
        
        output_file = sniff_traffic(interface, duration)
        
        if output_file:
            socketio.emit('sniffing_log', {'log': f"Sniffing completato. Capture in: {output_file}"})
            socketio.emit('sniffing_complete')
        else:
            socketio.emit('sniffing_log', {'log': "Errore durante lo sniffing"})
    
    thread = threading.Thread(target=run_sniffing)
    thread.start()

@socketio.on('start_vuln_scan')
def handle_start_vuln_scan(data):
    target = data.get('target')
    
    def run_vuln_scan():
        socketio.emit('vuln_log', {'log': f"Inizio analisi vulnerabilità di {target}..."})
        
        output_dir = vuln_analysis(target)
        
        if output_dir:
            socketio.emit('vuln_log', {'log': f"Analisi completata. Risultati in: {output_dir}"})
            socketio.emit('vuln_complete')
        else:
            socketio.emit('vuln_log', {'log': "Errore durante l'analisi"})
    
    thread = threading.Thread(target=run_vuln_scan)
    thread.start()

@socketio.on('start_web_attack')
def handle_start_web_attack(data):
    attack_type = data.get('attack_type')
    target = data.get('target')
    
    def run_web_attack():
        socketio.emit('web_log', {'log': f"Inizio attacco {attack_type} su {target}..."})
        
        output_dir = web_attack(attack_type, target)
        
        if output_dir:
            socketio.emit('web_log', {'log': f"Attacco completato. Risultati in: {output_dir}"})
            socketio.emit('web_complete')
        else:
            socketio.emit('web_log', {'log': "Errore durante l'attacco"})
    
    thread = threading.Thread(target=run_web_attack)
    thread.start()

@socketio.on('start_wifi_attack')
def handle_start_wifi_attack(data):
    attack_type = data.get('attack_type')
    interface = data.get('interface')
    
    def run_wifi_attack():
        socketio.emit('wifi_log', {'log': f"Inizio attacco {attack_type} su {interface}..."})
        
        output_dir = wifi_attack(attack_type, interface)
        
        if output_dir:
            socketio.emit('wifi_log', {'log': f"Attacco completato. Risultati in: {output_dir}"})
            socketio.emit('wifi_complete')
        else:
            socketio.emit('wifi_log', {'log': "Errore durante l'attacco"})
    
    thread = threading.Thread(target=run_wifi_attack)
    thread.start()

# Funzione principale
def main():
    # Installa tool necessari
    install_tools()
    
    # Avvia il server web
    print("Avvio del server web...")
    print("Apri il browser all'indirizzo http://localhost:5000")
    webbrowser.open('http://localhost:5000')
    
    # Avvia Flask
    app.run(host='0.0.0.0', port=5000, debug=True)

if __name__ == "__main__":
    main()