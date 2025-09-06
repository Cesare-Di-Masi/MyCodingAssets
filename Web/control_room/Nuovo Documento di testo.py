from flask import Flask, request, jsonify, send_from_directory
import subprocess
import shlex
import threading
import time
import os
import psutil

app = Flask(__name__)
FRONTEND_DIR = os.path.dirname(os.path.abspath(__file__))

live_scan_data = {
    'hosts': [],
    'vulnerabilities': [],
    'bandwidth_up': 0,
    'bandwidth_down': 0,
    'protocols': 'TCP:0% | UDP:0% | ICMP:0%',
    'pps_in': 0,
    'pps_out': 0,
    'latency_ms': 0,
    'packet_loss': 0,
    'cpu': 0,
    'ram': 0,
    'disk': 0,
    'uptime': 0,
    'scan_history': [],
    'logs': []
}

def nmap_scan(target='192.168.1.0/24'):
    try:
        cmd = f"nmap -O -sV {shlex.quote(target)}"
        result = subprocess.run(shlex.split(cmd), capture_output=True, text=True)
        return parse_nmap(result.stdout, target)
    except Exception as e:
        print(f"Errore Nmap: {e}")
        return live_scan_data

def parse_nmap(output, target):
    hosts = []
    vulnerabilities = []
    lines = output.split('\n')
    current_host = None
    for line in lines:
        if line.startswith('Nmap scan report for '):
            ip = line.split(' ')[-1]
            current_host = {'ip': ip, 'os': '', 'ports': [], 'services': []}
            hosts.append(current_host)
        elif 'OS details:' in line and current_host:
            current_host['os'] = line.replace('OS details:', '').strip()
        elif '/tcp' in line and current_host:
            parts = line.split()
            if len(parts) >= 3:
                port = int(parts[0].split('/')[0])
                service = parts[2]
                current_host['ports'].append(port)
                current_host['services'].append(service)
    timestamp = time.strftime('%Y-%m-%d %H:%M:%S')
    live_scan_data['scan_history'].append({'timestamp': timestamp, 'target': target, 'hosts': len(hosts), 'vulnerabilities': len(vulnerabilities)})
    live_scan_data['logs'].append(f"{timestamp} - Scan completato: {len(hosts)} host rilevati")
    return {
        'hosts': hosts,
        'vulnerabilities': vulnerabilities,
        'bandwidth_up': 50 + int(50*time.time()%50),
        'bandwidth_down': 100 + int(100*time.time()%100),
        'protocols': 'TCP:70% | UDP:20% | ICMP:10%',
        'pps_in': int(200*time.time()%1000),
        'pps_out': int(200*time.time()%1000),
        'latency_ms': int(50*time.time()%100),
        'packet_loss': int(10*time.time()%5),
        'cpu': int(psutil.cpu_percent()),
        'ram': int(psutil.virtual_memory().percent),
        'disk': int(psutil.disk_usage('/').percent),
        'uptime': int(time.time() - psutil.boot_time()),
        'scan_history': live_scan_data['scan_history'],
        'logs': live_scan_data['logs']
    }

def live_scan_loop():
    while True:
        live_scan_data.update(nmap_scan())
        time.sleep(30)

@app.route('/api/scan', methods=['POST'])
def scan():
    return jsonify(live_scan_data)

@app.route('/')
def serve_frontend():
    return send_from_directory(FRONTEND_DIR, 'index.html')

@app.route('/<path:path>')
def serve_static(path):
    return send_from_directory(FRONTEND_DIR, path)

if __name__ == '__main__':
    t = threading.Thread(target=live_scan_loop, daemon=True)
    t.start()
    app.run(host='0.0.0.0', port=5000)
