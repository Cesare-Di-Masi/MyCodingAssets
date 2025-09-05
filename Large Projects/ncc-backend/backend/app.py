# backend/app.py
import os
import json
import uuid
from datetime import datetime
from flask import Flask, jsonify, request, send_from_directory, render_template
from flask_cors import CORS
from flask_socketio import SocketIO, emit
from celery import Celery
from dotenv import load_dotenv
import subprocess
import boto3
from minio import Minio
from .db import engine
from .models import Job
from sqlalchemy.orm import Session


load_dotenv()

API_KEY = os.environ.get("NCC_API_KEY","supersecret_lab")
SECRET_KEY = os.environ.get("SECRET_KEY","change_this_secret")

app = Flask(__name__, static_folder='static', template_folder='.')
app.config['SECRET_KEY'] = SECRET_KEY
CORS(app)
socketio = SocketIO(app, cors_allowed_origins="*", async_mode='eventlet')

# Celery config
CELERY_BROKER_URL = os.environ.get("CELERY_BROKER_URL","redis://redis:6379/0")
CELERY_RESULT_BACKEND = os.environ.get("CELERY_RESULT_BACKEND","redis://redis:6379/1")

celery = Celery(app.import_name, broker=CELERY_BROKER_URL, backend=CELERY_RESULT_BACKEND)
celery.conf.update(result_expires=3600)

minio_client = Minio(
    os.getenv("MINIO_ENDPOINT","minio:9000"),
    access_key=os.getenv("MINIO_ACCESS","minioadmin"),
    secret_key=os.getenv("MINIO_SECRET","minioadmin"),
    secure=False
)

BUCKET = "artifacts"
if not minio_client.bucket_exists(BUCKET):
    minio_client.make_bucket(BUCKET)

@celery.task(bind=True)
def run_nmap_scan(self, job_id, target, profile):
    outpath = f"/tmp/{job_id}.xml"
    cmd = ["/bin/bash","/srv/backend/scripts/run_nmap.sh", target, profile, outpath]
    try:
        proc = subprocess.run(cmd, capture_output=True, text=True, timeout=600)
        minio_client.fput_object(BUCKET, f"{job_id}.xml", outpath)

        with Session(engine) as db:
            job = db.query(Job).filter_by(job_id=job_id).first()
            if job:
                job.status = "done"
                job.result_summary = proc.stdout[:500]
                db.commit()

        return {"ok": True, "artifact": f"{BUCKET}/{job_id}.xml"}
    except Exception as e:
        with Session(engine) as db:
            job = db.query(Job).filter_by(job_id=job_id).first()
            if job:
                job.status = "failed"
                job.result_summary = str(e)
                db.commit()
        return {"ok": False, "error": str(e)}

# Helper: simple API key check (dev). Replace with JWT/Keycloak for prod.
def require_api_key(req):
    key = req.headers.get('X-API-KEY') or req.headers.get('Authorization')
    if not key:
        return False
    if key.startswith('Bearer '):
        key = key.split(' ',1)[1]
    return key == API_KEY

# Serve UI
@app.route('/')
def index():
    return send_from_directory('.', 'index.html')

# Dashboard overview (mock or lightweight)
@app.route('/api/dashboard/overview', methods=['GET'])
def dashboard_overview():
    if not require_api_key(request):
        return jsonify({'error':'unauthorized'}), 403
    data = {
        "total_devices": 5,
        "active_devices": 3,
        "os_distribution":[{"name":"Linux","count":3},{"name":"Windows","count":2}],
        "top_services":[{"name":"ssh","count":3},{"name":"http","count":2}],
        "top_vulns_count": 1,
        "subnet_count": 2,
        "topology": {"nodes": [], "edges": []}
    }
    return jsonify(data)

# Hosts list (mock)
@app.route('/api/hosts', methods=['GET'])
def hosts():
    if not require_api_key(request):
        return jsonify({'error':'unauthorized'}), 403
    hosts = [
        {"ip":"192.168.1.10","name":"srv-1","os":"Linux","last_seen": datetime.utcnow().isoformat()},
        {"ip":"192.168.1.11","name":"srv-2","os":"Linux","last_seen": datetime.utcnow().isoformat()},
        {"ip":"192.168.1.20","name":"win-1","os":"Windows","last_seen": datetime.utcnow().isoformat()},
    ]
    limit = int(request.args.get('limit', len(hosts)))
    return jsonify(hosts[:limit])

# Host detail
@app.route('/api/host/<ip>', methods=['GET'])
def host_detail(ip):
    if not require_api_key(request):
        return jsonify({'error':'unauthorized'}), 403
    # in prod, query DB
    return jsonify({"ip": ip, "name":"example", "os":"Linux", "last_seen": datetime.utcnow().isoformat(), "ports": []})

# Tools status (local + remote mocked)
@app.route('/api/tools/status', methods=['GET'])
def tools_status():
    if not require_api_key(request):
        return jsonify({'error':'unauthorized'}), 403
    # Local detection example (quick, non-blocking)
    try:
        out = subprocess.run(['nmap','--version'], capture_output=True, text=True, timeout=2)
        local = out.stdout.splitlines()[0] if out.stdout else "n/a"
    except Exception:
        local = "n/a"

    tools = [
        {"name":"nmap","local_version":local,"remote_version":"7.92","update_available": False},
        {"name":"wireshark","local_version":"n/a","remote_version":"4.0.0","update_available": False},
    ]
    return jsonify(tools)

# Lists (whitelist/blacklist) stored in-memory for demo (in prod DB)
LISTS = {"whitelist": [], "blacklist": []}
@app.route('/api/lists', methods=['GET','POST'])
def lists():
    if not require_api_key(request):
        return jsonify({'error':'unauthorized'}), 403
    if request.method == 'GET':
        return jsonify(LISTS)
    payload = request.json or {}
    typ = payload.get('type')
    val = payload.get('value')
    if typ not in ['whitelist','blacklist'] or not val:
        return jsonify({'error':'bad request'}), 400
    LISTS[typ].append({'id': str(uuid.uuid4()), 'value': val, 'added_by': 'api', 'time': datetime.utcnow().isoformat()})
    return jsonify({'status':'ok'})

# Jobs listing (simple)
JOBS = {}

@app.route('/api/jobs', methods=['GET'])
def jobs():
    if not require_api_key(request):
        return jsonify(list(JOBS.values()))
    return jsonify(list(JOBS.values()))

# Start network scan (enqueues a Celery job)
@app.route('/api/scan/network', methods=['POST'])
def scan_network():
    if not require_api_key(request):
        return jsonify({'error':'unauthorized'}), 403
    req = request.json or {}
    target = req.get('target','192.168.1.0/24')
    profile = req.get('profile','safe')
    job_id = str(uuid.uuid4())
    JOBS[job_id] = {'job_id':job_id,'type':'network-scan','target':target,'status':'queued'}
    # enqueue
    task = run_nmap_scan.delay(job_id, target, profile)
    JOBS[job_id]['celery_id'] = task.id
    socketio.emit('job-update', JOBS[job_id])
    return jsonify({'job_id': job_id})

# Host scan endpoint
@app.route('/api/host/scan', methods=['POST'])
def scan_host():
    if not require_api_key(request):
        return jsonify({'error':'unauthorized'}), 403
    req = request.json or {}
    ip = req.get('ip')
    if not ip:
        return jsonify({'error':'ip required'}), 400
    job_id = str(uuid.uuid4())
    JOBS[job_id] = {'job_id':job_id,'type':'host-scan','target':ip,'status':'queued'}
    task = run_nmap_scan.delay(job_id, ip, 'full')
    JOBS[job_id]['celery_id'] = task.id
    socketio.emit('job-update', JOBS[job_id])
    return jsonify({'job_id': job_id})

# Celery task to run nmap (calls script)
@celery.task(bind=True)
def run_nmap_scan(self, job_id, target, profile):
    try:
        JOBS[job_id]['status'] = 'running'
        socketio.emit('job-update', JOBS[job_id])
        # run helper script that executes nmap (inside container)
        # scripts/run_nmap.sh <target> <profile> <outpath>
        outpath = f"/tmp/{job_id}.xml"
        cmd = ["/bin/bash","/srv/backend/scripts/run_nmap.sh", target, profile, outpath]
        proc = subprocess.run(cmd, capture_output=True, text=True, timeout=600)
        # store artifact: for demo, we just return stdout length
        JOBS[job_id]['status'] = 'done'
        JOBS[job_id]['result'] = {'stdout': proc.stdout[:1000], 'rc': proc.returncode}
        socketio.emit('job-update', JOBS[job_id])
        return {'ok': True}
    except Exception as e:
        JOBS[job_id]['status'] = 'failed'
        JOBS[job_id]['error'] = str(e)
        socketio.emit('job-update', JOBS[job_id])
        return {'ok': False, 'error': str(e)}

# simple socket event
@socketio.on('connect')
def handle_connect():
    emit('status', {'msg': 'connected'})

if __name__ == '__main__':
    # run with eventlet
    socketio.run(app, host='0.0.0.0', port=5000)
