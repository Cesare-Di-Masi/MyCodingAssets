# Quick start (dev, local)

1) Copia .env.example -> .env and edit if needed:
   cp .env.example .env

2) Build & start:
   docker-compose up --build

3) Backend UI:
   Open http://localhost:5000

4) Test endpoints (replace API key if changed):
   curl -H "X-API-KEY: supersecret_lab" http://localhost:5000/api/dashboard/overview
   curl -X POST -H "Content-Type: application/json" -H "X-API-KEY: supersecret_lab" -d '{"target":"192.168.1.0/24"}' http://localhost:5000/api/scan/network

Notes:
- The worker executes nmap inside the same container image (nmap is installed in the image).
- In some environments, nmap may need NET_RAW capability to perform certain scan types. For lab testing, the default scans (ping, top ports) should work.
- For production: use Kubernetes pod security policies, seccomp, and run tools in separate restricted runners.
