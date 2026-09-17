const fs = require('fs');
const path = require('path');
const simpleGit = require('simple-git');

const STORE = path.join(__dirname, '..', 'data', 'repos.json');
const ACTIVE_STORE = path.join(__dirname, '..', 'data', 'active.json');

function load() {
  if (!fs.existsSync(STORE)) return [];
  try { return JSON.parse(fs.readFileSync(STORE, 'utf8')); } catch (e) { return []; }
}
function save(list) {
  fs.mkdirSync(path.dirname(STORE), { recursive: true });
  fs.writeFileSync(STORE, JSON.stringify(list, null, 2));
}
function loadActive() {
  try { return JSON.parse(fs.readFileSync(ACTIVE_STORE, 'utf8')).path || null; } catch (e) { return null; }
}
function saveActive(repoPath) {
  fs.mkdirSync(path.dirname(ACTIVE_STORE), { recursive: true });
  fs.writeFileSync(ACTIVE_STORE, JSON.stringify({ path: path.resolve(repoPath) }, null, 2));
}
function list() { return load(); }
function add(repoPath) {
  const abs = path.resolve(repoPath);
  const all = load();
  if (!all.find((r) => r.path === abs)) {
    all.push({ path: abs, name: path.basename(abs), addedAt: new Date().toISOString() });
    save(all);
  }
  return all;
}
function remove(repoPath) {
  const abs = path.resolve(repoPath);
  save(load().filter((r) => r.path !== abs));
  if (loadActive() === abs) { try { fs.unlinkSync(ACTIVE_STORE); } catch (e) {} }
}

// Legge il repo attivo, ma solo se è ancora un repo Git valido.
// Se il path salvato è orfano (cartella spostata, cancellata, o repo non più
// registrato), scarta active.json e ricade sul default. Evita il classico
// "Repository is not registered" su tutte le API dopo un cambio di repoPath.
function active(defaultPath) {
  const saved = loadActive();
  if (saved && fs.existsSync(path.join(saved, '.git'))) return saved;
  if (saved) { try { fs.unlinkSync(ACTIVE_STORE); } catch (e) {} }
  return path.resolve(defaultPath);
}

function activate(repoPath) { const abs = path.resolve(repoPath); saveActive(abs); return abs; }

async function summarize(repoPath) {
  const abs = path.resolve(repoPath);
  const git = simpleGit(abs);
  let branch = null, commitCount = 0, clean = null, error = null;
  try {
    const b = await git.branch(['--show-current']);
    branch = b.current;
    const rawCommitCount = await git.raw(['rev-list', '--count', 'HEAD']);
    commitCount = Number(rawCommitCount.trim()) || 0;
    const s = await git.status();
    clean = s.isClean();
  } catch (e) { error = e.message; }
  return {
    path: abs,
    name: path.basename(abs),
    branch,
    commitCount,
    clean,
    exists: fs.existsSync(abs),
    error,
  };
}

module.exports = { list, add, remove, summarize, active, activate };