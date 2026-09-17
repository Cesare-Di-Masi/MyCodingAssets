const https = require('https');
const store = require('./store');

function request(method, endpoint, body) {
  return new Promise((resolve, reject) => {
    const payload = body ? new URLSearchParams(body).toString() : '';
    const req = https.request({ hostname: 'github.com', path: endpoint, method, headers: { Accept: 'application/json', 'Content-Type': 'application/x-www-form-urlencoded', 'Content-Length': Buffer.byteLength(payload), 'User-Agent': 'Repo-Command-Center' } }, (res) => {
      let text = ''; res.on('data', (chunk) => text += chunk); res.on('end', () => { try { resolve({ status: res.statusCode, data: JSON.parse(text) }); } catch (e) { reject(e); } });
    });
    req.on('error', reject); if (payload) req.write(payload); req.end();
  });
}
function clientId(config) {
  // Register a GitHub OAuth App or provide a public client ID in config.json.
  return config.githubClientId || '';
}
async function start(config) {
  const id = clientId(config); if (!id) throw new Error('GitHub device flow is not configured.');
  const result = await request('POST', '/login/device/code', { client_id: id, scope: 'repo workflow' });
  if (!result.data.device_code) throw new Error(result.data.error_description || 'Could not start GitHub device flow.');
  return result.data;
}
async function poll(config, deviceCode) {
  const id = clientId(config); if (!id) return { status: 'denied' };
  const result = await request('POST', '/login/oauth/access_token', { client_id: id, device_code: deviceCode, grant_type: 'urn:ietf:params:oauth:grant-type:device_code' });
  const data = result.data;
  if (data.error === 'authorization_pending') return { status: 'pending' };
  if (data.error === 'slow_down') return { status: 'pending', slowDown: true };
  if (data.error === 'expired_token') return { status: 'expired' };
  if (data.error === 'access_denied') return { status: 'denied' };
  if (!data.access_token) return { status: 'denied' };
  await saveToken(data.access_token, (data.scope || '').split(',').filter(Boolean));
  return { status: 'complete' };
}
async function getUser(token) {
  return new Promise((resolve, reject) => {
    const req = https.get({ hostname: 'api.github.com', path: '/user', headers: { Accept: 'application/vnd.github+json', Authorization: `Bearer ${token}`, 'User-Agent': 'Repo-Command-Center' } }, (res) => { let text = ''; res.on('data', (c) => text += c); res.on('end', () => { try { const user = JSON.parse(text); if (res.statusCode >= 400) return reject(new Error(user.message || 'GitHub token rejected')); resolve(user); } catch (e) { reject(e); } }); }); req.on('error', reject);
  });
}
async function saveToken(token, scopes = []) {
  const user = await getUser(token);
  store.setToken({ token, login: user.login, name: user.name || user.login, avatarUrl: user.avatar_url, scopes, savedAt: new Date().toISOString() });
}
async function manual(token) { if (!token) throw new Error('token required'); await saveToken(token.trim()); return { status: 'complete' }; }
module.exports = { start, poll, manual, clientId };