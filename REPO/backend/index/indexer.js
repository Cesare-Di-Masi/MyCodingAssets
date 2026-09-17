const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const dbMod = require('./db');

const TAG_RE = /\b(TODO|FIXME|HACK|BUG|NOTE|XXX)\b[:\s]?(.*)/;
const SECRET_HINTS = [
  { re: /-----BEGIN (RSA|EC|DSA|OPENSSH)? ?PRIVATE KEY-----/, label: 'Private key' },
  { re: /AKIA[0-9A-Z]{16}/, label: 'AWS access key' }, { re: /gh[pousr]_[A-Za-z0-9]{20,}/, label: 'GitHub token' },
  { re: /xox[baprs]-[A-Za-z0-9-]{10,}/, label: 'Slack token' }, { re: /(?:api|secret)[_-]?key\s*[:=]\s*["'][A-Za-z0-9_\-]{16,}["']/i, label: 'Generic API key' },
  { re: /(?:password|passwd|pwd)\s*[:=]\s*["'][^"']{6,}["']/i, label: 'Hardcoded password' }, { re: /eyJ[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}/, label: 'JWT token' }
];
const LANG = { '.js': 'JavaScript', '.mjs': 'JavaScript', '.cjs': 'JavaScript', '.ts': 'TypeScript', '.tsx': 'TypeScript', '.jsx': 'JavaScript', '.py': 'Python', '.go': 'Go', '.rs': 'Rust', '.java': 'Java', '.c': 'C', '.h': 'C', '.cpp': 'C++', '.cs': 'C#', '.css': 'CSS', '.html': 'HTML', '.md': 'Markdown', '.json': 'JSON', '.yml': 'YAML', '.yaml': 'YAML', '.sql': 'SQL', '.sh': 'Shell' };
const states = new Map();
const runningByRoot = new Map();
const lastRunByRoot = new Map();
function getState(repoRoot) {
  if (!states.has(repoRoot)) {
    let filesIndexed = 0; let lastIndexedAt = null;
    try { const database = dbMod.get(repoRoot); filesIndexed = database.prepare('SELECT count(*) count FROM files').get().count; lastIndexedAt = dbMod.getMeta(repoRoot, 'last_index_at'); } catch (e) {}
    states.set(repoRoot, { indexing: false, progress: filesIndexed ? 100 : 0, filesIndexed, lastIndexedAt });
  }
  return states.get(repoRoot);
}
function rel(root, full) { return path.relative(root, full).split(path.sep).join('/'); }
function isText(buf) { return !buf.subarray(0, 1000).includes(0); }
function extractSymbols(text, ext, file) {
  const out = []; const patterns = [/\b(?:function|class)\s+([A-Za-z_$][\w$]*)/g, /\b(?:const|let|var)\s+([A-Za-z_$][\w$]*)\s*=\s*(?:async\s*)?\(/g, /\b(?:def|class)\s+([A-Za-z_]\w*)/g, /^\s*import\s+(?:.+?\s+from\s+)?["']([^"']+)["']/gm];
  patterns.forEach((re, index) => { let m; while ((m = re.exec(text))) out.push({ path: file, line: text.slice(0, m.index).split('\n').length, kind: index === 0 || index === 2 ? 'declaration' : index === 3 ? 'import' : 'function', name: m[1] }); });
  return out;
}
function indexFile(repoRoot, filePath) {
  const database = dbMod.get(repoRoot); const full = path.resolve(repoRoot, filePath); const file = rel(repoRoot, full);
  if (!fs.existsSync(full)) { database.prepare('DELETE FROM files WHERE path = ?').run(file); database.prepare('DELETE FROM file_fts WHERE path = ?').run(file); database.prepare('DELETE FROM todos WHERE path = ?').run(file); database.prepare('DELETE FROM secrets WHERE path = ?').run(file); database.prepare('DELETE FROM symbols WHERE path = ?').run(file); return; }
  const st = fs.statSync(full); if (!st.isFile()) return;
  const buf = fs.readFileSync(full); const text = isText(buf) ? buf.toString('utf8') : null; const hash = crypto.createHash('sha1').update(buf).digest('hex'); const ext = path.extname(file).toLowerCase();
  const tx = () => dbMod.transaction(database, () => {
    database.prepare('INSERT INTO files(path,size,mtime,ext,language,hash_sha1,is_text,lines) VALUES(?,?,?,?,?,?,?,?) ON CONFLICT(path) DO UPDATE SET size=excluded.size,mtime=excluded.mtime,ext=excluded.ext,language=excluded.language,hash_sha1=excluded.hash_sha1,is_text=excluded.is_text,lines=excluded.lines').run(file, st.size, Math.floor(st.mtimeMs), ext, LANG[ext] || '', hash, text ? 1 : 0, text ? text.split('\n').length : 0);
    database.prepare('DELETE FROM todos WHERE path = ?').run(file); database.prepare('DELETE FROM secrets WHERE path = ?').run(file); database.prepare('DELETE FROM symbols WHERE path = ?').run(file); database.prepare('DELETE FROM file_fts WHERE path = ?').run(file);
    if (text) {
      const now = new Date().toISOString(); const todos = [], secrets = [];
      text.split('\n').forEach((line, i) => { const tag = line.match(TAG_RE); if (tag) todos.push([file, i + 1, tag[1], (tag[2] || '').trim().slice(0, 200), now]); SECRET_HINTS.forEach((hint) => { if (hint.re.test(line)) secrets.push([file, i + 1, hint.label, now]); }); });
      const todo = database.prepare('INSERT INTO todos(path,line,tag,text,indexed_at) VALUES(?,?,?,?,?)'); todos.forEach((r) => todo.run(...r)); const secret = database.prepare('INSERT INTO secrets(path,line,kind,indexed_at) VALUES(?,?,?,?)'); secrets.forEach((r) => secret.run(...r));
      const symbol = database.prepare('INSERT INTO symbols(path,line,kind,name) VALUES(?,?,?,?)'); extractSymbols(text, ext, file).forEach((r) => symbol.run(r.path, r.line, r.kind, r.name)); database.prepare('INSERT INTO file_fts(path,content) VALUES(?,?)').run(file, text);
    }
  }); tx();
}
function walk(repoRoot, ignoreDirs, onFile) {
  function visit(dir) { let entries; try { entries = fs.readdirSync(dir); } catch (e) { return; } for (const name of entries) { if (ignoreDirs.includes(name)) continue; const full = path.join(dir, name); let st; try { st = fs.statSync(full); } catch (e) { continue; } if (st.isDirectory()) { dbMod.get(repoRoot).prepare('INSERT OR REPLACE INTO dirs(path,mtime) VALUES(?,?)').run(rel(repoRoot, full), Math.floor(st.mtimeMs)); visit(full); } else onFile(full); } }
  visit(repoRoot);
}
async function indexCommits(repoRoot, git) {
  let raw = ''; try { raw = await git.raw(['log', '--all', '--date=iso-strict', '--pretty=format:__C__%x09%H%x09%an%x09%ae%x09%ad%x09%s%x09%b', '--numstat']); } catch (e) { return; }
  const database = dbMod.get(repoRoot); const commit = database.prepare('INSERT OR REPLACE INTO commits(hash,author,email,date,subject,body,kind) VALUES(?,?,?,?,?,?,?)'); const file = database.prepare('INSERT OR REPLACE INTO commit_files(commit_hash,path,additions,deletions) VALUES(?,?,?,?)'); let current = null;
  dbMod.transaction(database, () => { for (const line of raw.split('\n')) { if (line.startsWith('__C__\t')) { const p = line.split('\t'); current = p[1]; commit.run(p[1], p[2], p[3], p[4], p[5] || '', p.slice(6).join('\t'), 'commit'); continue; } const m = line.match(/^(\d+|-)?\t(\d+|-)?\t(.+)$/); if (m && current) file.run(current, m[3], m[1] === '-' ? 0 : Number(m[1]), m[2] === '-' ? 0 : Number(m[2])); } });
  try { const head = await git.revparse(['HEAD']); dbMod.setMeta(repoRoot, 'last_indexed_commit', head.trim()); } catch (e) {}
}
async function indexAll(repoRoot, ignoreDirs, git) {
  const existing = runningByRoot.get(repoRoot); if (existing) return existing;
  const previous = getState(repoRoot); states.set(repoRoot, { indexing: true, progress: 0, filesIndexed: 0, lastIndexedAt: previous.lastIndexedAt });
  const running = (async () => { const database = dbMod.get(repoRoot); database.exec('DELETE FROM files; DELETE FROM dirs; DELETE FROM todos; DELETE FROM secrets; DELETE FROM symbols; DELETE FROM file_fts; DELETE FROM commits; DELETE FROM commit_files;'); walk(repoRoot, ignoreDirs, (full) => { indexFile(repoRoot, full); const current = getState(repoRoot); current.filesIndexed++; current.progress = Math.min(95, current.filesIndexed % 1000 / 10); }); await indexCommits(repoRoot, git); dbMod.setMeta(repoRoot, 'last_index_at', new Date().toISOString()); states.set(repoRoot, { indexing: false, progress: 100, filesIndexed: database.prepare('SELECT count(*) count FROM files').get().count, lastIndexedAt: dbMod.getMeta(repoRoot, 'last_index_at') }); })().catch((e) => { getState(repoRoot).indexing = false; throw e; }).finally(() => { runningByRoot.delete(repoRoot); lastRunByRoot.set(repoRoot, Date.now()); });
  runningByRoot.set(repoRoot, running); return running;
}
async function indexIncremental(repoRoot, ignoreDirs, git, force = false) { if (!force && Date.now() - (lastRunByRoot.get(repoRoot) || 0) < 30000) return; if (!dbMod.isPopulated(repoRoot)) return indexAll(repoRoot, ignoreDirs, git); const current = getState(repoRoot); current.indexing = true; walk(repoRoot, ignoreDirs, (full) => { const file = rel(repoRoot, full); const old = dbMod.get(repoRoot).prepare('SELECT mtime,size FROM files WHERE path=?').get(file); const st = fs.statSync(full); if (!old || old.mtime !== Math.floor(st.mtimeMs) || old.size !== st.size) indexFile(repoRoot, file); }); await indexCommits(repoRoot, git); dbMod.setMeta(repoRoot, 'last_index_at', new Date().toISOString()); states.set(repoRoot, { ...getState(repoRoot), indexing: false, progress: 100, filesIndexed: dbMod.get(repoRoot).prepare('SELECT count(*) count FROM files').get().count, lastIndexedAt: dbMod.getMeta(repoRoot, 'last_index_at') }); lastRunByRoot.set(repoRoot, Date.now()); }
function getStatus(repoRoot) { return { ...getState(repoRoot) }; }
module.exports = { indexAll, indexIncremental, indexFile: (root, file) => indexFile(root, file), getStatus };