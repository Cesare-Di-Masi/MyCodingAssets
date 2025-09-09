"""
Enhanced Flask backend implementing safe, simulated "nyanBOX" features.

Important: THIS IMPLEMENTATION DOES NOT PROVIDE OR ENABLE ANY REAL ATTACK/DOMINANCE/JAMMING
FUNCTIONALITY (deauth, jamming, beacon flooding, captive portals, etc.). Those
are harmful and illegal to use in many jurisdictions.

Instead this server exposes:
 - simulated WiFi/BLE/Signal tools that return realistic-looking mock data
 - an XP / leveling system persisted to a JSON file
 - SSE (Server-Sent Events) streaming for real-time logs
 - modules management and safe "run" endpoints that only simulate actions
 - simple API key protection for control endpoints

Install requirements:
 pip install flask psutil flask-cors

Run:
 PYTHONUNBUFFERED=1 python nyanbox_flask_enhanced.py

"""
from flask import Flask, jsonify, render_template, request, Response, abort
from flask_cors import CORS
import psutil, random, time, datetime, platform, socket, json, os, threading

app = Flask(__name__, static_folder='static', template_folder='templates')
CORS(app)

# --- Configuration / persistence ---
DATA_FILE = 'nyan_state.json'
API_KEY = os.environ.get('NYAN_API_KEY', 'devkey')  # change in production
LOCK = threading.Lock()

DEFAULT_STATE = {
    'xp': {},
    'levels': {},
    'modules': [
        {"name": "Memory Cortex", "status": "active"},
        {"name": "Mood Engine", "status": "active"},
        {"name": "Sanity Relay", "status": "idle"},
        {"name": "VoiceBox", "status": "active"},
        {"name": "Pulse Matrix", "status": "active"},
        {"name": "Net Synapse", "status": "active"}
    ],
    'logs': []
}

if not os.path.exists(DATA_FILE):
    with open(DATA_FILE, 'w') as f:
        json.dump(DEFAULT_STATE, f, indent=2)


def load_state():
    with LOCK:
        with open(DATA_FILE, 'r') as f:
            return json.load(f)


def save_state(state):
    with LOCK:
        with open(DATA_FILE, 'w') as f:
            json.dump(state, f, indent=2)

# --- Utilities ---

def require_api_key():
    key = request.headers.get('X-API-KEY') or request.args.get('api_key')
    if key != API_KEY:
        abort(403, 'API key missing or invalid')


def add_log(msg):
    state = load_state()
    datetime.datetime.now(datetime.timezone.utc).isoformat()
    state['logs'].insert(0, {'time': now, 'msg': msg})
    state['logs'] = state['logs'][:500]
    save_state(state)

# --- SSE (Server-Sent Events) for real-time logs ---
clients = []


def event_stream():
    last_index = 0
    while True:
        state = load_state()
        logs = state.get('logs', [])
        if len(logs) > last_index:
            for entry in logs[:len(logs)-last_index]:
                yield f"data: {json.dumps(entry)}\n\n"
            last_index = len(logs)
        time.sleep(1)


@app.route('/stream')
def stream():
    return Response(event_stream(), mimetype='text/event-stream')

# --- Basic endpoints ---
@app.route('/')
def index():
    return render_template('index.html')

@app.route('/api/vitals')
def api_vitals():
    cpu = psutil.cpu_percent(interval=0.2)
    ram = psutil.virtual_memory().percent
    uptime = int(time.time() - psutil.boot_time())
    disk = psutil.disk_usage('/').percent
    net = psutil.net_io_counters()

    data = {
        'cpu': cpu,
        'ram': ram,
        'uptime': uptime,
        'disk': disk,
        'bandwidth_up': net.bytes_sent,
        'bandwidth_down': net.bytes_recv,
        'pps_in': random.randint(100, 2000),
        'pps_out': random.randint(100, 2000),
        'latency': random.randint(5, 120),
        'packetloss': random.randint(0, 5),
        'hostname': platform.node(),
        'os': platform.system() + ' ' + platform.release(),
    }
    add_log(f"Vitals updated: CPU {cpu}%, RAM {ram}%")
    return jsonify(data)

@app.route('/api/modules')
def api_modules():
    state = load_state()
    return jsonify(state.get('modules', []))

# --- Simulated safe wireless tools ---

@app.route('/api/nyan/wifi_scan')
def api_wifi_scan():
    """Return a simulated list of nearby WiFi APs. Safe: no active probing, purely synthetic."""
    aps = []
    ssids = ['Home', 'CoffeeShop', 'CorpNet', 'GUEST', 'iot-bridge']
    for i in range(random.randint(4, 12)):
        aps.append({
            'ssid': random.choice(ssids) + (f"-{random.randint(1,99)}" if random.random()>.6 else ''),
            'bssid': ':'.join('%02x' % random.randint(0, 255) for _ in range(6)),
            'signal': random.randint(-90, -30),
            'channel': random.choice([1,6,11,36,40,44,48]),
            'enc': random.choice(['WPA2','WPA3','Open'])
        })
    add_log(f"Simulated WiFi scan: {len(aps)} APs")
    return jsonify({'aps': aps})

@app.route('/api/nyan/channel_analyzer')
def api_channel_analyzer():
    """Simulated channel utilization metrics for planning/education."""
    channels = {ch: {'util_percent': random.randint(0, 90), 'ap_count': random.randint(0,8)} for ch in [1,6,11,36,40,44,48]}
    add_log('Channel analyzer executed')
    return jsonify(channels)

@app.route('/api/nyan/ble_scan')
def api_ble_scan():
    devs = []
    names = ['Tile','MiBand','AirPods','HeartSensor','Pwnagotchi']
    for i in range(random.randint(2,8)):
        devs.append({
            'name': random.choice(names),
            'addr': ':'.join('%02x' % random.randint(0, 255) for _ in range(6)),
            'rssi': random.randint(-100, -20),
            'adv_payload': {'manufacturer': random.randint(0, 65535)}
        })
    add_log(f"Simulated BLE scan: {len(devs)} devices")
    return jsonify({'devices': devs})

# --- Operations that WOULD BE OFFENSIVE/ILLEGAL: provide safe refusal + simulation endpoints ---
OFFENSIVE_TOOLS = ['deauth', 'jam', 'beacon_spam', 'evil_portal']

@app.route('/api/nyan/action', methods=['POST'])
def api_nyan_action():
    """Generic endpoint to run nyan actions. If action is destructive, refuse and log the attempt.
    Otherwise perform a safe simulation and award XP.
    """
    require_api_key()
    body = request.get_json() or {}
    action = (body.get('action') or '').lower()
    actor = body.get('actor', 'anonymous')

    if not action:
        abort(400, 'action required')

    if any(x in action for x in OFFENSIVE_TOOLS):
        add_log(f"Refused offensive action '{action}' requested by {actor}")
        return jsonify({'status': 'refused', 'reason': 'Destructive or illegal action requested. Only simulations are allowed.'}), 403

    # safe simulations
    result = {'status': 'ok', 'action': action}
    xp_gain = 0
    if action == 'wifi_scan':
        result['result'] = api_wifi_scan().get_json()
        xp_gain = 5
    elif action == 'ble_scan':
        result['result'] = api_ble_scan().get_json()
        xp_gain = 5
    elif action == 'channel_analyze':
        result['result'] = api_channel_analyzer().get_json()
        xp_gain = 3
    else:
        result['result'] = {'note': 'Unknown action simulated'}
        xp_gain = 1

    # award xp
    state = load_state()
    state.setdefault('xp', {})
    state['xp'].setdefault(actor, 0)
    state['xp'][actor] += xp_gain
    # simple level calc
    lvl = int(state['xp'][actor] ** 0.5)
    state.setdefault('levels', {})
    state['levels'][actor] = lvl
    save_state(state)

    add_log(f"Simulated action '{action}' by {actor} (+{xp_gain} XP)")
    return jsonify(result)

@app.route('/api/nyan/xp/<actor>')
def api_get_xp(actor):
    state = load_state()
    xp = state.get('xp', {}).get(actor, 0)
    lvl = state.get('levels', {}).get(actor, 0)
    return jsonify({'actor': actor, 'xp': xp, 'level': lvl})

@app.route('/api/nyan/logs')
def api_get_logs():
    state = load_state()
    return jsonify(state.get('logs', []))

# --- Admin: toggle module state ---
@app.route('/api/admin/module_toggle', methods=['POST'])
def api_module_toggle():
    require_api_key()
    body = request.get_json() or {}
    name = body.get('name')
    if not name:
        abort(400, 'module name required')
    state = load_state()
    for m in state.get('modules', []):
        if m['name'].lower() == name.lower():
            m['status'] = 'active' if m.get('status') != 'active' else 'idle'
            save_state(state)
            add_log(f"Module '{m['name']}' toggled to {m['status']}")
            return jsonify(m)
    abort(404, 'module not found')

# --- Safe helper endpoints for frontend development ---
@app.route('/api/processes')
def api_processes():
    proc_list = []
    for p in psutil.process_iter(attrs=['pid','name','cpu_percent','memory_percent']):
        try:
            proc_list.append(p.info)
        except (psutil.NoSuchProcess, psutil.AccessDenied):
            continue
    top = sorted(proc_list, key=lambda x: x.get('cpu_percent', 0), reverse=True)[:10]
    return jsonify(top)

@app.route('/api/protocols')
def api_protocols():
    conns = psutil.net_connections(kind='inet')
    tcp = len([c for c in conns if c.type == socket.SOCK_STREAM])
    udp = len([c for c in conns if c.type == socket.SOCK_DGRAM])
    icmp = random.randint(0, 10)
    other = max(0, len(conns) - tcp - udp)
    data = {'TCP': tcp, 'UDP': udp, 'ICMP': icmp, 'Other': other}
    return jsonify(data)

# --- Run background vitals logger to create data for SSE ---
def background_vitals_loop():
    while True:
        try:
            api_vitals()  # will add a log entry
        except Exception:
            pass
        time.sleep(10)

if __name__ == '__main__':
    t = threading.Thread(target=background_vitals_loop, daemon=True)
    t.start()
    app.run(host='0.0.0.0', port=5000, debug=True)
