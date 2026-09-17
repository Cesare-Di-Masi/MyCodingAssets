const fs = require('fs');
const path = require('path');

const STORE = path.join(__dirname, '..', 'data', 'auth.json');
function read() { try { return JSON.parse(fs.readFileSync(STORE, 'utf8')); } catch (e) { return {}; } }
function write(data) {
  fs.mkdirSync(path.dirname(STORE), { recursive: true });
  fs.writeFileSync(STORE, JSON.stringify(data, null, 2), { mode: 0o600 });
  try { fs.chmodSync(STORE, 0o600); } catch (e) { /* Windows permissions are user-scoped by default. */ }
}
function getToken() { const github = read().github; return github?.token ? github : null; }
function setToken(data) { const current = read(); current.github = data; write(current); return data; }
function clear() {
  const current = read(); delete current.github;
  if (!Object.keys(current).length) { try { fs.unlinkSync(STORE); } catch (e) {} } else write(current);
}
module.exports = { getToken, setToken, clear, STORE };