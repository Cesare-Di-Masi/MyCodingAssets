import React, { useEffect, useState } from 'react';
import io from 'socket.io-client';

// Dashboard.tsx
// Minimal, single-file React + TypeScript prototype for the Control Room homepage.
// Tailwind CSS assumed. Environment variables:
//   REACT_APP_API_BASE (e.g. http://localhost:5000/api/v1)
//   REACT_APP_API_KEY

const API_BASE = process.env.REACT_APP_API_BASE || '/api/v1';
const API_KEY = process.env.REACT_APP_API_KEY || '';

type Overview = {
  total_devices: number;
  active_devices: number;
  os_distribution: { name: string; count: number }[];
  top_services: { name: string; count: number }[];
  active_alerts: { info: number; warning: number; critical: number };
  subnet_count: number;
};

type Host = {
  ip: string;
  name?: string;
  os?: string;
  last_seen?: string;
  tags?: string[];
  risk_score?: number;
};

type JobSummary = { job_id: string; type: string; target: string; progress: number; status: string };

export default function Dashboard(): JSX.Element {
  const [overview, setOverview] = useState<Overview | null>(null);
  const [hosts, setHosts] = useState<Host[]>([]);
  const [jobs, setJobs] = useState<JobSummary[]>([]);
  const [logs, setLogs] = useState<string[]>([]);
  const [socketConnected, setSocketConnected] = useState(false);

  useEffect(() => {
    fetchOverview();
    fetchHosts();
    fetchJobs();
    const socket = io(API_BASE.replace('/api/v1',''), {
      auth: {
        token: API_KEY,
      },
      path: '/socket.io',
    });

    socket.on('connect', () => {
      setSocketConnected(true);
      pushLog(`Socket connected: ${socket.id}`);
    });

    socket.on('network-scan-complete', (payload: any) => {
      pushLog(`Scan complete: target=${payload.target} hosts=${payload.hosts?.length ?? 0}`);
      fetchOverview();
      fetchHosts();
    });

    socket.on('host-scan-complete', (payload: any) => {
      pushLog(`Host scan complete: ${payload.ip}`);
      fetchHosts();
    });

    socket.on('job-update', (payload: any) => {
      // replace job entry
      setJobs((prev) => {
        const idx = prev.findIndex((j) => j.job_id === payload.job_id);
        if (idx >= 0) {
          const copy = [...prev];
          copy[idx] = { ...copy[idx], ...payload };
          return copy;
        }
        return [payload, ...prev].slice(0, 20);
      });
    });

    socket.on('disconnect', () => {
      setSocketConnected(false);
      pushLog('Socket disconnected');
    });

    return () => {
      socket.disconnect();
    };
  }, []);

  function pushLog(line: string) {
    setLogs((l) => [new Date().toISOString() + ' ' + line, ...l].slice(0, 200));
  }

  async function fetchOverview() {
    try {
      const res = await fetch(`${API_BASE}/dashboard/overview`, { headers: { 'X-API-KEY': API_KEY } });
      if (!res.ok) throw new Error('overview fetch failed');
      const j = await res.json();
      setOverview(j as Overview);
    } catch (e) {
      pushLog('Error fetching overview: ' + String(e));
    }
  }

  async function fetchHosts() {
    try {
      const res = await fetch(`${API_BASE}/hosts?limit=12`, { headers: { 'X-API-KEY': API_KEY } });
      if (!res.ok) throw new Error('hosts fetch failed');
      const j = await res.json();
      setHosts(j as Host[]);
    } catch (e) {
      pushLog('Error fetching hosts: ' + String(e));
    }
  }

  async function fetchJobs() {
    try {
      const res = await fetch(`${API_BASE}/jobs?limit=10`, { headers: { 'X-API-KEY': API_KEY } });
      if (!res.ok) throw new Error('jobs fetch failed');
      const j = await res.json();
      setJobs(j as JobSummary[]);
    } catch (e) {
      pushLog('Error fetching jobs: ' + String(e));
    }
  }

  // Quick action: start a safe network scan (Example)
  async function startNetworkScan() {
    try {
      const res = await fetch(`${API_BASE}/scan/network`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-API-KEY': API_KEY },
        body: JSON.stringify({ target: '192.168.1.0/24', profile: 'safe' }),
      });
      if (!res.ok) throw new Error('scan start failed');
      const j = await res.json();
      pushLog(`Started network scan ${j.job_id || ''}`);
      fetchJobs();
    } catch (e) {
      pushLog('Error starting scan: ' + String(e));
    }
  }

  return (
    <div className="min-h-screen bg-slate-900 text-slate-100 font-sans">
      <header className="flex items-center justify-between px-6 py-4 border-b border-slate-800">
        <div className="flex items-center gap-4">
          <div className="text-2xl font-semibold">Control Room</div>
          <div className="text-sm text-slate-400">Network Dashboard</div>
        </div>
        <div className="flex items-center gap-4">
          <div className="text-sm">Socket: {socketConnected ? 'connected' : 'disconnected'}</div>
          <button
            onClick={startNetworkScan}
            className="bg-emerald-600 hover:bg-emerald-500 px-3 py-1 rounded text-sm font-medium"
          >
            Start Quick Scan
          </button>
        </div>
      </header>

      <main className="p-6 grid grid-cols-12 gap-6">
        {/* Left column: stats + jobs */}
        <section className="col-span-3 space-y-6">
          <div className="grid grid-cols-1 gap-4">
            <Card title="Devices">
              <div className="text-3xl font-bold">{overview ? overview.total_devices : '—'}</div>
              <div className="text-sm text-slate-400">Active: {overview ? overview.active_devices : '—'}</div>
            </Card>

            <Card title="OS Distribution">
              <div className="flex flex-col gap-2">
                {overview?.os_distribution?.map((o) => (
                  <div key={o.name} className="text-sm text-slate-300 flex justify-between">
                    <span>{o.name}</span>
                    <span>{o.count}</span>
                  </div>
                ))}
              </div>
            </Card>

            <Card title="Top Services">
              <ul className="text-sm space-y-1">
                {overview?.top_services?.map((s) => (
                  <li key={s.name} className="flex justify-between">
                    <span>{s.name}</span>
                    <span className="text-slate-300">{s.count}</span>
                  </li>
                ))}
              </ul>
            </Card>

            <Card title="Jobs (recent)">
              <div className="space-y-2 text-sm">
                {jobs.length === 0 && <div className="text-slate-400">No recent jobs</div>}
                {jobs.map((j) => (
                  <div key={j.job_id} className="flex justify-between">
                    <div>{j.type}</div>
                    <div className="text-slate-300">{j.progress}%</div>
                  </div>
                ))}
              </div>
            </Card>
          </div>
        </section>

        {/* Center: topology + hosts table */}
        <section className="col-span-6 space-y-6">
          <Card title="Network Map (interactive)">
            <div className="h-64 bg-gradient-to-br from-slate-800 to-slate-700 rounded p-4 overflow-hidden flex items-center justify-center text-slate-400">
              {/* Placeholder for topology graph (cytoscape/vis.js) */}
              <div>Topology graph placeholder — integra Cytoscape/vis.js qui</div>
            </div>
          </Card>

          <Card title="Hosts (recent)">
            <table className="w-full text-sm table-auto">
              <thead className="text-slate-400 text-left">
                <tr>
                  <th className="p-2">IP</th>
                  <th className="p-2">Name</th>
                  <th className="p-2">OS</th>
                  <th className="p-2">Last seen</th>
                  <th className="p-2">Actions</th>
                </tr>
              </thead>
              <tbody>
                {hosts.map((h) => (
                  <tr key={h.ip} className="border-t border-slate-800">
                    <td className="p-2">{h.ip}</td>
                    <td className="p-2">{h.name ?? '—'}</td>
                    <td className="p-2">{h.os ?? 'Unknown'}</td>
                    <td className="p-2">{h.last_seen ? new Date(h.last_seen).toLocaleString() : '—'}</td>
                    <td className="p-2">
                      <div className="flex gap-2">
                        <button className="px-2 py-1 bg-slate-700 rounded text-xs">Scan</button>
                        <button className="px-2 py-1 bg-slate-700 rounded text-xs">SSH</button>
                        <button className="px-2 py-1 bg-rose-600 rounded text-xs">Action</button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Card>
        </section>

        {/* Right column: alerts + metrics */}
        <section className="col-span-3 space-y-6">
          <Card title="Alerts">
            <div className="space-y-2 text-sm">
              <div className="flex justify-between text-amber-300">Info: {overview?.active_alerts.info ?? 0}</div>
              <div className="flex justify-between text-amber-500">Warnings: {overview?.active_alerts.warning ?? 0}</div>
              <div className="flex justify-between text-rose-500">Critical: {overview?.active_alerts.critical ?? 0}</div>
            </div>
          </Card>

          <Card title="Trends">
            <div className="h-36 flex items-center justify-center text-slate-400">Small charts placeholder</div>
          </Card>

          <Card title="Quick Actions">
            <div className="flex flex-col gap-2">
              <button onClick={startNetworkScan} className="w-full px-3 py-2 bg-sky-600 rounded">Start network scan</button>
              <button className="w-full px-3 py-2 bg-slate-700 rounded">Open Hosts page</button>
            </div>
          </Card>
        </section>

        {/* Bottom console: live logs */}
        <section className="col-span-12">
          <Card title="Live console / event feed">
            <div className="h-40 overflow-auto bg-black/50 rounded p-2 text-xs font-mono">
              {logs.map((l, idx) => (
                <div key={idx} className="text-slate-200 py-0.5">{l}</div>
              ))}
            </div>
          </Card>
        </section>
      </main>
    </div>
  );
}

function Card({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="bg-slate-800 rounded shadow-sm border border-slate-700 p-4">
      <div className="flex items-center justify-between mb-3">
        <div className="text-sm font-semibold">{title}</div>
        <div className="text-xs text-slate-400">{new Date().toLocaleString()}</div>
      </div>
      <div>{children}</div>
    </div>
  );
}
