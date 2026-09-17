const http = require('http');

function request(url, payload) {
  return new Promise((resolve, reject) => {
    const u = new URL(url);
    const body = JSON.stringify(payload);
    const req = http.request({
      hostname: u.hostname,
      port: u.port || 80,
      path: u.pathname,
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(body) }
    }, (res) => {
      let data = '';
      res.on('data', (c) => data += c);
      res.on('end', () => {
        try { resolve(JSON.parse(data)); } catch (e) { reject(e); }
      });
    });
    req.on('error', reject);
    req.write(body);
    req.end();
  });
}

async function isAvailable(baseUrl) {
  return new Promise((resolve) => {
    const u = new URL(baseUrl || 'http://localhost:11434');
    const r = http.get({ hostname: u.hostname, port: u.port || 11434, path: '/api/tags', timeout: 1500 }, (res) => {
      let d = ''; res.on('data', (c) => d += c);
      res.on('end', () => {
        try { resolve({ ok: res.statusCode === 200, models: (JSON.parse(d).models || []).map((m) => m.name) }); }
        catch (e) { resolve({ ok: false, models: [] }); }
      });
    });
    r.on('error', () => resolve({ ok: false, models: [] }));
    r.on('timeout', () => { r.destroy(); resolve({ ok: false, models: [] }); });
  });
}

async function ask(baseUrl, model, prompt, context) {
  const system = 'You are a repository analysis assistant. Answer concisely using the provided context. Do not invent file paths or commits that are not in the context.';
  const payload = {
    model: model || 'llama3.2',
    messages: [
      { role: 'system', content: system },
      { role: 'user', content: context ? ('Context:\n' + context + '\n\nQuestion: ' + prompt) : prompt }
    ],
    stream: false
  };
  const res = await request((baseUrl || 'http://localhost:11434') + '/api/chat', payload);
  return (res.message && res.message.content) || res.response || '(no response)';
}
module.exports = { isAvailable, ask };