const fs = require('fs');
const { ensureStateDirectory, stateFile } = require('../state-dir');

const STORE = stateFile('auth.json');
function read() { try { return JSON.parse(fs.readFileSync(STORE, 'utf8')); } catch (e) { return {}; } }
function write(data) {
  ensureStateDirectory();
  fs.writeFileSync(STORE, JSON.stringify(data, null, 2), { mode: 0o600 });
  if (process.platform !== 'win32') {
    try { fs.chmodSync(STORE, 0o600); } catch (e) {}
  }
}
function getToken() { const github = read().github; return github?.token ? github : null; }
function setToken(data) { const current = read(); current.github = data; write(current); return data; }
function getAccessToken() { return read().accessToken || null; }
function setAccessToken(token) { const current = read(); current.accessToken = token; write(current); return token; }
function clear() {
  const current = read(); delete current.github;
  if (!Object.keys(current).length) { try { fs.unlinkSync(STORE); } catch (e) {} } else write(current);
}
module.exports = { getToken, setToken, getAccessToken, setAccessToken, clear, STORE };
