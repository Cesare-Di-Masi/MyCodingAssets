const express = require('express');
const path = require('path');
const fs = require('fs');
const crypto = require('crypto');
const { spawn } = require('child_process');
const { AsyncLocalStorage } = require('node:async_hooks');
const simpleGit = require('simple-git');

const config = require('../config.json');
const explorer = require('./filesystem/explorer');
const searchMod = require('./filesystem/search');
const analyzer = require('./filesystem/analyzer');
const depsMod = require('./filesystem/deps');
const ignoreMod = require('./filesystem/gitignore');
const { RepoWatcher } = require('./filesystem/watcher');
const statusMod = require('./git/status');
const historyMod = require('./git/history');
const diffMod = require('./git/diff');
const commitMod = require('./git/commit');
const branchesMod = require('./git/branches');
const stashMod = require('./git/stash');
const graphMod = require('./git/graph');
const blameMod = require('./git/blame');
const timelineMod = require('./git/timeline');
const reflogMod = require('./git/reflog');
const insightsMod = require('./git/insights');
const tagsMod = require('./git/tags');
const remotesMod = require('./git/remotes');
const bisectMod = require('./git/bisect');
const conflictMod = require('./git/conflict');
const snapshotsMod = require('./git/snapshots');
const mergeMod = require('./git/merge');
const cherryPickMod = require('./git/cherryPick');
const revertMod = require('./git/revert');
const resetMod = require('./git/reset');
const ownershipMod = require('./git/ownership');
const changelogMod = require('./git/changelog');
const githubMod = require('./github/api');
const ollamaMod = require('./ai/ollama');
const reposMod = require('./repos');
const githubAuth = require('./auth/github');
const authStore = require('./auth/store');
const indexDb = require('./index/db');
const indexer = require('./index/indexer');
const indexQuery = require('./index/query');

const APP_DIR = path.resolve(__dirname, '..');
const REPO_ROOT = path.resolve(APP_DIR, config.repoPath || '.');
const IGNORE_DIRS = config.ignoreDirs || ['node_modules', '.git'];
const PORT = process.env.PORT || config.port || 4747;
const GRAPH_LIMIT = config.graphCommitLimit || 400;
const GITHUB_TOKEN = config.githubToken || process.env.GITHUB_TOKEN || '';
const GITHUB_REPO = config.githubRepo || '';
const OLLAMA_URL = config.ollamaUrl || 'http://localhost:11434';
const OLLAMA_MODEL = config.ollamaModel || 'llama3.2';

if (!fs.existsSync(path.join(REPO_ROOT, '.git'))) {
  console.warn(`WARNING: ${REPO_ROOT} does not look like a Git repository.`);
}

const gitCache = new Map();
const repoContext = new AsyncLocalStorage();
function getGit(repoPath) {
  const abs = path.resolve(repoPath || REPO_ROOT);
  if (!gitCache.has(abs)) gitCache.set(abs, simpleGit(abs));
  return gitCache.get(abs);
}
function currentRepoRoot() { return repoContext.getStore()?.root || REPO_ROOT; }
function currentGit() { return repoContext.getStore()?.git || getGit(REPO_ROOT); }
const git = new Proxy({}, {
  get(target, property) {
    const instance = currentGit();
    const value = instance[property];
    return typeof value === 'function' ? value.bind(instance) : value;
  }
});
function registeredRepoPaths() {
  return new Set([REPO_ROOT, ...reposMod.list().map((repo) => path.resolve(repo.path))]);
}
function resolveRepoPath(requested) {
  const active = reposMod.active(REPO_ROOT);
  const raw = requested || (registeredRepoPaths().has(active) ? active : REPO_ROOT);
  if (typeof raw !== 'string' || raw.startsWith('\\\\') || raw.startsWith('//') || raw.split(/[\\/]/).includes('..')) throw new Error('Invalid repository path');
  const abs = path.resolve(raw);
  if (!registeredRepoPaths().has(abs)) throw new Error('Repository is not registered');
  return abs;
}
ignoreMod.ensureRccIndexIgnored(REPO_ROOT);
const app = express();
const RUNNER_ALLOWLIST = new Set(['npm', 'npx', 'pnpm', 'yarn', 'pytest', 'python', 'python3', 'cargo', 'go', 'make', 'docker', 'git']);
const runSessions = new Map();
const authConfig = { enabled: false, token: '', allowLocalhostWithoutToken: true, ...(config.auth || {}) };
config.auth = authConfig;

function isLoopbackRequest(req) {
  const host = req.hostname || '';
  const ip = req.ip || req.socket?.remoteAddress || '';
  return host === 'localhost' || host === '127.0.0.1' || host === '::1' || ip === '127.0.0.1' || ip === '::1' || ip === '::ffff:127.0.0.1';
}

function authMiddleware(req, res, next) {
  if (!authConfig.enabled) return next();
  if (authConfig.allowLocalhostWithoutToken && isLoopbackRequest(req)) return next();
  if (req.path === '/api/auth/status' || req.path === '/api/auth/verify') return next();
  if (req.path === '/api/watch' && req.query?.token === authConfig.token) return next();
  const header = req.get('authorization') || '';
  const token = header.startsWith('Bearer ') ? header.slice(7).trim() : '';
  if (token === authConfig.token) return next();
  return res.status(401).json({ error: 'Unauthorized' });
}

app.use(express.json({ limit: '5mb' }));
app.use(express.static(path.join(__dirname, '..', 'frontend')));
app.use('/api', authMiddleware);
app.use('/api', (req, res, next) => {
  try {
    const requested = req.query.repo || req.body?.repo;
    const root = resolveRepoPath(requested);
    repoContext.run({ root, git: getGit(root) }, next);
  } catch (error) {
    res.status(400).json({ error: error.message });
  }
});

function handle(fn) {
  return (req, res) => {
    Promise.resolve(fn(req, res)).catch((err) => {
      console.error('[ERR]', req.method, req.url, err.message);
      res.status(500).json({ error: err.message || String(err) });
    });
  };
}

// ---------------- Overview ----------------
app.get('/api/overview', handle(async (req, res) => {
  const repoRoot = currentRepoRoot();
  let stats;
  try { stats = indexDb.isPopulated(repoRoot) ? indexQuery.getStats(repoRoot) : explorer.walkRepo(repoRoot, IGNORE_DIRS); }
  catch (e) { stats = explorer.walkRepo(repoRoot, IGNORE_DIRS); }
  let branchInfo = { current: null, branches: [] };
  let commitCount = 0, status = null, contributors = [];
  try {
    branchInfo = await branchesMod.listBranches(git);
    const rawCommitCount = await git.raw(['rev-list', '--count', 'HEAD']);
    commitCount = Number(rawCommitCount.trim()) || 0;
    status = await statusMod.getStatus(git);
    contributors = await historyMod.getContributors(git, 20);
  } catch (e) {}
  res.json({
    repoRoot,
    totalFiles: stats.totalFiles, totalDirs: stats.totalDirs, totalSize: stats.totalSize,
    extCounts: stats.extCounts, largestFiles: stats.largestFiles.slice(0, 12),
    commitCount, currentBranch: branchInfo.current, branchCount: branchInfo.branches.length,
    status, contributors
  });
}));

// ---------------- Files / Search ----------------
function resolveInsideRepo(relPath) {
  if (typeof relPath !== 'string' || !relPath.trim()) throw new Error('path required');
  const abs = path.resolve(currentRepoRoot(), relPath);
  if (!explorer.isPathInside(currentRepoRoot(), abs)) throw new Error('Invalid path');
  return abs;
}

function fileMimeType(filePath) {
  const ext = path.extname(filePath).toLowerCase();
  const map = {
    '.js': 'application/javascript; charset=utf-8', '.mjs': 'application/javascript; charset=utf-8', '.ts': 'application/typescript; charset=utf-8',
    '.css': 'text/css; charset=utf-8', '.html': 'text/html; charset=utf-8', '.json': 'application/json; charset=utf-8', '.md': 'text/markdown; charset=utf-8',
    '.txt': 'text/plain; charset=utf-8', '.svg': 'image/svg+xml', '.png': 'image/png', '.jpg': 'image/jpeg', '.jpeg': 'image/jpeg', '.gif': 'image/gif', '.webp': 'image/webp',
    '.ico': 'image/x-icon', '.xml': 'application/xml; charset=utf-8', '.yaml': 'application/yaml; charset=utf-8', '.yml': 'application/yaml; charset=utf-8'
  };
  return map[ext] || 'application/octet-stream';
}

app.get('/api/files', handle(async (req, res) => {
  res.json({ path: req.query.path || '.', entries: explorer.listDir(currentRepoRoot(), req.query.path || '.', IGNORE_DIRS) });
}));
const MIME_TYPES = {
  '.png': 'image/png', '.jpg': 'image/jpeg', '.jpeg': 'image/jpeg',
  '.gif': 'image/gif', '.svg': 'image/svg+xml', '.webp': 'image/webp',
  '.bmp': 'image/bmp', '.ico': 'image/x-icon',
  '.pdf': 'application/pdf',
  '.woff': 'font/woff', '.woff2': 'font/woff2', '.ttf': 'font/ttf',
  '.mp4': 'video/mp4', '.webm': 'video/webm',
  '.mp3': 'audio/mpeg', '.wav': 'audio/wav'
};

app.get('/api/file', handle(async (req, res) => {
  if (!req.query.path) return res.status(400).json({ error: 'path required' });
  const repoRoot = currentRepoRoot();
  const abs = path.resolve(repoRoot, req.query.path);
  if (!explorer.isPathInside(repoRoot, abs)) return res.status(400).json({ error: 'Invalid path' });

  // Raw binary mode for images, fonts, media — the frontend asks for ?raw=1
  if (req.query.raw === '1') {
    let buf;
    try { buf = fs.readFileSync(abs); }
    catch (e) { return res.status(404).json({ error: 'File not found' }); }
    const ext = path.extname(abs).toLowerCase();
    res.setHeader('Content-Type', MIME_TYPES[ext] || 'application/octet-stream');
    res.setHeader('Cache-Control', 'private, max-age=60');
    return res.send(buf);
  }

  // Text mode (existing behavior)
  const result = explorer.readFile(repoRoot, req.query.path, config.maxFileReadBytes || 500000);
  res.json({ path: req.query.path, ...result });
}));
app.put('/api/file', handle(async (req, res) => {
  const relPath = typeof req.body?.path === 'string' ? req.body.path.trim() : '';
  if (!relPath) return res.status(400).json({ error: 'path required' });
  const abs = resolveInsideRepo(relPath);
  const raw = typeof req.body.content === 'string' ? req.body.content : '';
  const content = raw.replace(/^\uFEFF/, '');
  const maxWriteBytes = Number(config.maxFileWriteBytes || 5 * 1024 * 1024);
  const buf = Buffer.from(content, 'utf8');
  if (buf.length > maxWriteBytes) return res.status(413).json({ error: `File exceeds ${maxWriteBytes} bytes limit.` });
  if (buf.subarray(0, 512).includes(0)) return res.status(400).json({ error: 'Binary files are not supported for editing.' });
  if (!fs.existsSync(abs)) return res.status(404).json({ error: 'File not found' });
  fs.writeFileSync(abs, content, 'utf8');
  indexer.indexFile(currentRepoRoot(), relPath);
  const st = fs.statSync(abs);
  res.json({ ok: true, size: st.size, mtime: st.mtimeMs });
}));
app.post('/api/file/rename', handle(async (req, res) => {
  const from = typeof req.body?.from === 'string' ? req.body.from.trim() : '';
  const to = typeof req.body?.to === 'string' ? req.body.to.trim() : '';
  if (!from || !to) return res.status(400).json({ error: 'from and to required' });
  const fromAbs = resolveInsideRepo(from);
  const toAbs = path.resolve(currentRepoRoot(), to);
  if (!explorer.isPathInside(currentRepoRoot(), toAbs)) return res.status(400).json({ error: 'Invalid path' });
  if (fs.existsSync(toAbs)) return res.status(409).json({ error: 'Target already exists' });
  fs.renameSync(fromAbs, toAbs);
  indexer.indexFile(currentRepoRoot(), from);
  indexer.indexFile(currentRepoRoot(), to);
  res.json({ ok: true });
}));
app.delete('/api/file', handle(async (req, res) => {
  const relPath = typeof req.body?.path === 'string' ? req.body.path.trim() : '';
  if (!relPath) return res.status(400).json({ error: 'path required' });
  const abs = resolveInsideRepo(relPath);
  const repoRoot = currentRepoRoot();
  const trashRoot = path.join(repoRoot, '.rcc-trash');
  fs.mkdirSync(trashRoot, { recursive: true });
  const ts = new Date().toISOString().replace(/[:.]/g, '-');
  const base = path.basename(abs);
  const target = path.join(trashRoot, `${ts}-${base}`);
  let finalTarget = target;
  let i = 1;
  while (fs.existsSync(finalTarget)) { finalTarget = path.join(trashRoot, `${ts}-${path.basename(base, path.extname(base))}-${i}${path.extname(base)}`); i++; }
  // Move to .rcc-trash rather than deleting permanently so a file can be recovered.
  fs.renameSync(abs, finalTarget);
  indexer.indexFile(repoRoot, relPath);
  res.json({ ok: true, movedTo: path.relative(repoRoot, finalTarget).split(path.sep).join('/') });
}));
app.get('/api/search', handle(async (req, res) => {
  const q = req.query.q || '';
  const mode = ['content', 'regex', 'filename'].includes(req.query.mode) ? req.query.mode : 'filename';
  if (!q.trim()) return res.json({ results: [] });
  let results;
  try {
    if (indexDb.isPopulated(currentRepoRoot())) results = mode === 'filename'
      ? indexQuery.searchFilename(currentRepoRoot(), q, { limit: config.maxSearchResults || 200 })
      : indexQuery.searchContent(currentRepoRoot(), q, { limit: config.maxSearchResults || 200, regex: mode === 'regex' });
    else results = searchMod.search(currentRepoRoot(), IGNORE_DIRS, q, mode, config.maxSearchResults || 200);
  } catch (e) { results = searchMod.search(currentRepoRoot(), IGNORE_DIRS, q, mode, config.maxSearchResults || 200); }
  res.json({ results, mode });
}));

// ---------------- History / Graph / Blame / Timeline ----------------
app.get('/api/history', handle(async (req, res) => {
  res.json({ commits: await historyMod.getLog(git, Number(req.query.limit) || 100, req.query.branch, req.query.q) });
}));
app.get('/api/commit/:hash', handle(async (req, res) => res.json(await historyMod.getCommit(git, req.params.hash))));
app.get('/api/diff/:hash', handle(async (req, res) => {
  res.json({ diff: await diffMod.getDiff(git, { hash: req.params.hash, file: req.query.file }) });
}));
app.get('/api/graph', handle(async (req, res) => res.json(await graphMod.getGraph(git, GRAPH_LIMIT))));
app.get('/api/blame', handle(async (req, res) => {
  if (!req.query.path) return res.status(400).json({ error: 'path required' });
  res.json(await blameMod.getBlame(git, req.query.path));
}));
app.get('/api/timeline', handle(async (req, res) => {
  if (!req.query.path) return res.status(400).json({ error: 'path required' });
  try { res.json(indexDb.isPopulated(currentRepoRoot()) ? indexQuery.getFileTimeline(currentRepoRoot(), req.query.path) : await timelineMod.getFileTimeline(git, req.query.path)); }
  catch (e) { res.json(await timelineMod.getFileTimeline(git, req.query.path)); }
}));

// ---------------- Changes / Commit ----------------
app.get('/api/status', handle(async (req, res) => res.json(await statusMod.getStatus(git))));
app.get('/api/diff', handle(async (req, res) => {
  res.json({ diff: await diffMod.getDiff(git, { file: req.query.file, staged: req.query.staged === 'true' }) });
}));
app.post('/api/stage', handle(async (req, res) => { await commitMod.stageFiles(git, req.body.files); res.json({ ok: true }); }));
app.post('/api/unstage', handle(async (req, res) => { await commitMod.unstageFiles(git, req.body.files); res.json({ ok: true }); }));
app.post('/api/commit', handle(async (req, res) => {
  const result = await commitMod.commitFiles(git, req.body.message, req.body.amend);
  indexer.indexIncremental(currentRepoRoot(), IGNORE_DIRS, git, true).catch(() => {});
  res.json({ ok: true, result });
}));

// ---------------- Branches ----------------
app.get('/api/branches', handle(async (req, res) => res.json(await branchesMod.listBranches(git))));
app.post('/api/checkout', handle(async (req, res) => { await branchesMod.checkout(git, req.body.branch); res.json({ ok: true }); }));
app.post('/api/create-branch', handle(async (req, res) => { await branchesMod.createBranch(git, req.body.name, req.body.from); res.json({ ok: true }); }));
app.post('/api/delete-branch', handle(async (req, res) => { await branchesMod.deleteBranch(git, req.body.name, req.body.force); res.json({ ok: true }); }));
app.get('/api/compare', handle(async (req, res) => res.json(await branchesMod.compare(git, req.query.a, req.query.b))));
app.get('/api/merge-preview', handle(async (req, res) => res.json(await branchesMod.mergePreview(git, req.query.from, req.query.into))));
async function snapshotBefore(req, label) { return snapshotsMod.create(git, currentRepoRoot(), `auto before ${label}`); }
app.post('/api/merge', handle(async (req, res) => { const snapshot = await snapshotBefore(req, 'merge'); try { const output = await mergeMod.merge(git, req.body.branch, req.body); res.json({ ok: true, output, snapshotId: snapshot.id }); } catch (e) { res.status(409).json({ error: e.message, snapshotId: snapshot.id, conflict: true }); } }));
app.post('/api/merge/abort', handle(async (req, res) => { const snapshot = await snapshotBefore(req, 'merge abort'); await mergeMod.mergeAbort(git); res.json({ ok: true, snapshotId: snapshot.id }); }));
app.post('/api/merge/continue', handle(async (req, res) => { const snapshot = await snapshotBefore(req, 'merge continue'); await mergeMod.mergeContinue(git, req.body.message); res.json({ ok: true, snapshotId: snapshot.id }); }));
app.post('/api/cherry-pick', handle(async (req, res) => { const snapshot = await snapshotBefore(req, 'cherry-pick'); try { const output = await cherryPickMod.cherryPick(git, req.body.hashes); res.json({ ok: true, output, snapshotId: snapshot.id }); } catch (e) { res.status(409).json({ error: e.message, snapshotId: snapshot.id, conflict: true }); } }));
app.post('/api/cherry-pick/abort', handle(async (req, res) => { const snapshot = await snapshotBefore(req, 'cherry-pick abort'); await cherryPickMod.cherryPickAbort(git); res.json({ ok: true, snapshotId: snapshot.id }); }));
app.post('/api/cherry-pick/continue', handle(async (req, res) => { const snapshot = await snapshotBefore(req, 'cherry-pick continue'); await cherryPickMod.cherryPickContinue(git); res.json({ ok: true, snapshotId: snapshot.id }); }));
app.post('/api/revert', handle(async (req, res) => { const snapshot = await snapshotBefore(req, 'revert'); try { const output = await revertMod.revert(git, req.body.hash, req.body); res.json({ ok: true, output, snapshotId: snapshot.id }); } catch (e) { res.status(409).json({ error: e.message, snapshotId: snapshot.id, conflict: true }); } }));
app.post('/api/revert/abort', handle(async (req, res) => { const snapshot = await snapshotBefore(req, 'revert abort'); await revertMod.revertAbort(git); res.json({ ok: true, snapshotId: snapshot.id }); }));
app.post('/api/revert/continue', handle(async (req, res) => { const snapshot = await snapshotBefore(req, 'revert continue'); await revertMod.revertContinue(git); res.json({ ok: true, snapshotId: snapshot.id }); }));
app.post('/api/reset', handle(async (req, res) => { if (!req.body.ref) return res.status(400).json({ error: 'ref required' }); const snapshot = await snapshotBefore(req, 'reset'); await resetMod.reset(git, req.body.ref, req.body); res.json({ ok: true, snapshotId: snapshot.id }); }));
app.get('/api/state/operation', handle(async (req, res) => {
  const fs = require('fs'); const gitDir = path.join(currentRepoRoot(), '.git');
  const candidates = [['merge', 'MERGE_HEAD'], ['cherry-pick', 'CHERRY_PICK_HEAD'], ['revert', 'REVERT_HEAD'], ['rebase', 'rebase-merge'], ['rebase', 'rebase-apply']];
  const found = candidates.find(([, marker]) => fs.existsSync(path.join(gitDir, marker)));
  res.json({ operation: found ? found[0] : null, conflicts: found ? (await statusMod.getStatus(git)).conflicted || [] : [] });
}));

// ---------------- Stash ----------------
app.get('/api/stash', handle(async (req, res) => res.json({ stashes: await stashMod.list(git) })));
app.post('/api/stash', handle(async (req, res) => { await stashMod.push(git, req.body.message, req.body.includeUntracked); res.json({ ok: true }); }));
app.post('/api/stash/pop', handle(async (req, res) => { await stashMod.pop(git, req.body.ref); res.json({ ok: true }); }));
app.post('/api/stash/apply', handle(async (req, res) => { await stashMod.apply(git, req.body.ref); res.json({ ok: true }); }));
app.post('/api/stash/drop', handle(async (req, res) => { await stashMod.drop(git, req.body.ref); res.json({ ok: true }); }));
app.get('/api/stash/show', handle(async (req, res) => res.json({ diff: await stashMod.show(git, req.query.ref) })));

// ---------------- Health / Duplicates / Gitignore / Deps ----------------
app.get('/api/health', handle(async (req, res) => {
  try {
    if (indexDb.isPopulated(currentRepoRoot())) { const todos = indexQuery.getTodos(currentRepoRoot()); const secrets = indexQuery.getSecrets(currentRepoRoot()); return res.json({ todoCount: todos.length, todos: todos.slice(0, 400), secrets }); }
  } catch (e) {}
  const scan = analyzer.scanRepoForTodosAndSecrets(currentRepoRoot(), IGNORE_DIRS);
  res.json({ todoCount: scan.todos.length, todos: scan.todos.slice(0, 400), secrets: scan.secrets });
}));
app.get('/api/duplicates', handle(async (req, res) => {
  try { if (indexDb.isPopulated(currentRepoRoot())) return res.json({ groups: indexQuery.findDuplicates(currentRepoRoot()) }); } catch (e) {}
  res.json({ groups: analyzer.findDuplicates(null, currentRepoRoot(), IGNORE_DIRS) });
}));
app.get('/api/gitignore', handle(async (req, res) => res.json(ignoreMod.analyze(currentRepoRoot(), IGNORE_DIRS))));
app.post('/api/gitignore', handle(async (req, res) => { ignoreMod.append(currentRepoRoot(), req.body.rules || []); res.json({ ok: true }); }));
app.get('/api/deps', handle(async (req, res) => res.json(depsMod.detect(currentRepoRoot(), IGNORE_DIRS))));

// ---------------- Reflog ----------------
app.get('/api/reflog', handle(async (req, res) => res.json(await reflogMod.getReflog(git, Number(req.query.limit) || 100))));
app.post('/api/reflog/reset', handle(async (req, res) => { const snapshot = await snapshotBefore(req, 'reflog reset'); await reflogMod.resetTo(git, req.body.hash, req.body.mode); res.json({ ok: true, snapshotId: snapshot.id }); }));

// ---------------- Insights / Pickaxe ----------------
app.get('/api/insights', handle(async (req, res) => res.json(await insightsMod.getInsights(git, Number(req.query.days) || 365))));
app.get('/api/pickaxe', handle(async (req, res) => {
  if (!req.query.q) return res.status(400).json({ error: 'q required' });
  res.json(await insightsMod.pickaxe(git, req.query.q, req.query.file, 100));
}));

// ---------------- Tags ----------------
app.get('/api/tags', handle(async (req, res) => res.json(await tagsMod.list(git))));
app.post('/api/tags', handle(async (req, res) => { await tagsMod.create(git, req.body.name, req.body.message, req.body.ref); res.json({ ok: true }); }));
app.post('/api/tags/delete', handle(async (req, res) => { await tagsMod.remove(git, req.body.name); res.json({ ok: true }); }));
app.post('/api/tags/push', handle(async (req, res) => { await tagsMod.push(git, req.body.name, req.body.remote); res.json({ ok: true }); }));
app.post('/api/tags/delete-remote', handle(async (req, res) => { await tagsMod.removeRemote(git, req.body.name, req.body.remote); res.json({ ok: true }); }));

// ---------------- Remotes ----------------
app.get('/api/remotes', handle(async (req, res) => res.json(await remotesMod.list(git))));
app.post('/api/remotes', handle(async (req, res) => { await remotesMod.add(git, req.body.name, req.body.url); res.json({ ok: true }); }));
app.post('/api/remotes/delete', handle(async (req, res) => { await remotesMod.remove(git, req.body.name); res.json({ ok: true }); }));
app.post('/api/remotes/fetch', handle(async (req, res) => { await remotesMod.fetch(git, req.body.name); res.json({ ok: true }); }));
app.post('/api/remotes/pull', handle(async (req, res) => { await remotesMod.pull(git, req.body.name); res.json({ ok: true }); }));
app.post('/api/remotes/push', handle(async (req, res) => { await remotesMod.push(git, req.body.name, req.body.branch); res.json({ ok: true }); }));

// ---------------- Bisect ----------------
app.get('/api/bisect', handle(async (req, res) => res.json(bisectMod.state())));
app.post('/api/bisect/start', handle(async (req, res) => { await bisectMod.start(git); res.json(bisectMod.state()); }));
app.post('/api/bisect/mark', handle(async (req, res) => res.json(await bisectMod.mark(git, req.body.kind))));
app.post('/api/bisect/reset', handle(async (req, res) => { await bisectMod.reset(git); res.json({ ok: true }); }));

// ---------------- Conflicts ----------------
app.get('/api/conflicts', handle(async (req, res) => res.json(await conflictMod.list(git))));
app.get('/api/conflicts/read', handle(async (req, res) => {
  if (!req.query.file) return res.status(400).json({ error: 'file required' });
  res.json(await conflictMod.read(currentRepoRoot(), req.query.file));
}));
app.post('/api/conflicts/resolve', handle(async (req, res) => {
  await conflictMod.resolve(currentRepoRoot(), req.body.file, req.body.resolution);
  await conflictMod.markResolved(git, req.body.file);
  res.json({ ok: true });
}));

// ---------------- Snapshots ----------------
app.get('/api/snapshots', handle(async (req, res) => res.json({ snapshots: snapshotsMod.list(currentRepoRoot()) })));
app.post('/api/snapshots', handle(async (req, res) => res.json(await snapshotsMod.create(git, currentRepoRoot(), req.body.label))));
app.post('/api/snapshots/restore', handle(async (req, res) => { await snapshotsMod.restore(git, currentRepoRoot(), req.body.id); res.json({ ok: true }); }));
app.post('/api/snapshots/delete', handle(async (req, res) => { await snapshotsMod.remove(git, currentRepoRoot(), req.body.id); res.json({ ok: true }); }));

// ---------------- Ownership ----------------
app.get('/api/ownership', handle(async (req, res) => {
  try { if (indexDb.isPopulated(currentRepoRoot())) return res.json(indexQuery.getOwnership(currentRepoRoot(), 15)); } catch (e) {}
  res.json(await ownershipMod.getOwnership(git, 15));
}));

// ---------------- SQLite index ----------------
app.get('/api/index/status', handle(async (req, res) => res.json({ ...indexer.getStatus(currentRepoRoot()), dbSize: indexDb.size(currentRepoRoot()) })));
app.post('/api/index/rebuild', handle(async (req, res) => { res.status(202).json({ accepted: true }); indexer.indexAll(currentRepoRoot(), IGNORE_DIRS, git).catch((e) => console.error('[index]', e.message)); }));
app.get('/api/index/symbols', handle(async (req, res) => {
  if (!req.query.path) return res.status(400).json({ error: 'path required' });
  res.json({ symbols: indexQuery.symbols(currentRepoRoot(), req.query.path) });
}));
app.get('/api/index/symbols/search', handle(async (req, res) => res.json({ symbols: indexQuery.searchSymbols(currentRepoRoot(), req.query.q || '') })));

// ---------------- Changelog ----------------
app.get('/api/changelog', handle(async (req, res) => {
  res.json(await changelogMod.generate(git, req.query.from, req.query.to, req.query.version));
}));

// ---------------- GitHub ----------------
app.get('/api/github', handle(async (req, res) => {
  const saved = authStore.getToken();
  const token = saved?.token || GITHUB_TOKEN;
  if (!token) return res.json({ configured: false, reason: 'No token. Sign in with GitHub or set "githubToken" in config.json or GITHUB_TOKEN env.' });
  try {
    const info = await githubMod.getRepoInfo(git, token, GITHUB_REPO);
    res.json({ configured: true, ...info });
  } catch (e) {
    res.json({ configured: true, error: e.message });
  }
}));

app.get('/api/github/commit-avatars', handle(async (req, res) => {
  const saved = authStore.getToken();
  const token = saved?.token || GITHUB_TOKEN;
  const map = await githubMod.getCommitAvatars(git, token, GITHUB_REPO);
  res.json({ avatars: map });
}));

// ---------------- GitHub authentication ----------------
app.post('/api/auth/github/start', handle(async (req, res) => res.json(await githubAuth.start(config))));
app.post('/api/auth/github/poll', handle(async (req, res) => {
  if (!req.body.device_code) return res.status(400).json({ error: 'device_code required' });
  res.json(await githubAuth.poll(config, req.body.device_code));
}));
app.post('/api/auth/github/manual', handle(async (req, res) => res.json(await githubAuth.manual(req.body.token))));
app.get('/api/auth/github/me', handle(async (req, res) => {
  const saved = authStore.getToken();
  res.json(saved ? { authenticated: true, login: saved.login, name: saved.name, avatarUrl: saved.avatarUrl, scopes: saved.scopes || [] } : { authenticated: false });
}));
app.get('/api/auth/status', handle(async (req, res) => {
  const requires = !!authConfig.enabled && !(authConfig.allowLocalhostWithoutToken && isLoopbackRequest(req));
  res.json({ requiresAuth: requires });
}));
app.post('/api/auth/verify', handle(async (req, res) => {
  const token = typeof req.body?.token === 'string' ? req.body.token : '';
  if (!authConfig.enabled) return res.json({ ok: true });
  if (token === authConfig.token) return res.json({ ok: true });
  return res.status(401).json({ error: 'Unauthorized' });
}));
app.post('/api/auth/github/logout', handle(async (req, res) => { authStore.clear(); res.json({ ok: true }); }));
app.post('/api/settings', handle(async (req, res) => {
  const allowed = ['githubClientId', 'editorUri', 'ollamaUrl', 'ollamaModel'];
  const next = { ...config };
  allowed.forEach((key) => { if (typeof req.body[key] === 'string') next[key] = req.body[key]; });
  fs.writeFileSync(path.join(__dirname, '..', 'config.json'), JSON.stringify(next, null, 2) + '\n');
  res.json({ ok: true });
}));

// ---------------- AI / Ollama ----------------
app.get('/api/ai/status', handle(async (req, res) => res.json(await ollamaMod.isAvailable(OLLAMA_URL))));
app.post('/api/ai/ask', handle(async (req, res) => {
  if (!req.body.prompt) return res.status(400).json({ error: 'prompt required' });
  try {
    const answer = await ollamaMod.ask(OLLAMA_URL, OLLAMA_MODEL, req.body.prompt, req.body.context);
    res.json({ answer, model: OLLAMA_MODEL });
  } catch (e) {
    res.status(500).json({ error: `Ollama error: ${e.message}. Is it running at ${OLLAMA_URL}?` });
  }
}));

// ---------------- Multi-repo ----------------
app.get('/api/repos', handle(async (req, res) => {
  const list = reposMod.list();
  const summaries = await Promise.all(list.map((r) => reposMod.summarize(r.path)));
  res.json({ repos: summaries, current: reposMod.active(REPO_ROOT) });
}));
app.get('/api/repos/current', handle(async (req, res) => res.json({ repo: reposMod.active(REPO_ROOT), defaultRepo: REPO_ROOT })));
app.post('/api/repos/activate', handle(async (req, res) => { const repo = resolveRepoPath(req.body.repo); reposMod.activate(repo); res.json({ ok: true, repo }); }));
app.post('/api/repos', handle(async (req, res) => {
  const candidate = req.body.path;
  if (typeof candidate !== 'string' || candidate.startsWith('\\\\') || candidate.startsWith('//') || candidate.split(/[\\/]/).includes('..')) return res.status(400).json({ error: 'Invalid repository path' });
  const repo = path.resolve(candidate);
  if (!fs.existsSync(path.join(repo, '.git'))) return res.status(400).json({ error: 'Path is not a Git repository' });
  reposMod.add(repo); res.json({ ok: true, repo });
}));
app.post('/api/repos/delete', handle(async (req, res) => { reposMod.remove(req.body.path); res.json({ ok: true }); }));
app.delete('/api/repos', handle(async (req, res) => { if (!req.body.path) return res.status(400).json({ error: 'path required' }); reposMod.remove(req.body.path); res.json({ ok: true }); }));

// ---------------- Live file watcher (SSE) ----------------
const watcherSessions = new Map();
app.get('/api/watch', (req, res) => {
  // EventSource cannot send Authorization headers, so the token is accepted here as a query string.
  // This is visible in server logs and should only be used for a LAN-only local tool.
  if (authConfig.enabled && !(authConfig.allowLocalhostWithoutToken && isLoopbackRequest(req)) && req.query?.token !== authConfig.token) {
    return res.status(401).json({ error: 'Unauthorized' });
  }
  res.set({
    'Content-Type': 'text/event-stream',
    'Cache-Control': 'no-cache',
    'Connection': 'keep-alive',
    'X-Accel-Buffering': 'no'
  });
  res.flushHeaders();
  res.write('event: ready\ndata: {}\n\n');
  const repoRoot = currentRepoRoot();
  let session = watcherSessions.get(repoRoot);
  if (!session && watcherSessions.size >= 5) return res.status(429).end('Too many repository watchers');
  if (!session) {
    session = { watcher: new RepoWatcher(repoRoot, IGNORE_DIRS), clients: new Set() };
    session.watcher.start();
    session.watcher.on('change', (files) => {
      files.forEach((file) => { try { indexer.indexFile(repoRoot, file); } catch (e) {} });
      session.clients.forEach((client) => { try { client.write(`data: ${JSON.stringify({ files })}\n\n`); } catch (e) {} });
    });
    watcherSessions.set(repoRoot, session);
  }
  const watcher = session.watcher;
  session.clients.add(res);
  const ping = setInterval(() => res.write(': ping\n\n'), 25000);
  req.on('close', () => {
    clearInterval(ping);
    session.clients.delete(res);
    if (!session.clients.size) { watcher.watchers.forEach((w) => { try { w.close(); } catch (e) {} }); watcherSessions.delete(repoRoot); }
  });
});

// ---------------- Terminal ----------------
const ALLOWED_GIT_SUBCOMMANDS = [
  'status', 'log', 'diff', 'branch', 'show', 'blame', 'remote', 'stash',
  'add', 'reset', 'commit', 'checkout', 'fetch', 'rev-parse', 'describe',
  'tag', 'reflog', 'shortlog', 'config', 'ls-files', 'ls-tree', 'show-ref'
];
app.post('/api/terminal', handle(async (req, res) => {
  const cmd = (req.body.command || '').trim();
  if (!cmd.startsWith('git ')) return res.status(400).json({ error: 'Only "git ..." commands are allowed.' });
  const parts = cmd.slice(4).trim().match(/(?:[^\s"]+|"[^"]*")+/g) || [];
  if (!ALLOWED_GIT_SUBCOMMANDS.includes(parts[0])) return res.status(400).json({ error: `git ${parts[0]} not permitted.` });
  try { res.json({ output: await git.raw(parts) }); }
  catch (err) { res.json({ output: err.message || String(err), error: true }); }
}));

function detectCommands(repoRoot) {
  const commands = [];
  const packageJsonPath = path.join(repoRoot, 'package.json');
  if (fs.existsSync(packageJsonPath)) {
    try {
      const pkg = JSON.parse(fs.readFileSync(packageJsonPath, 'utf8'));
      const scripts = pkg.scripts || {};
      Object.entries(scripts).forEach(([name, script]) => {
        commands.push({ label: `npm run ${name}`, cmd: 'npm', args: ['run', name], cwd: repoRoot });
      });
    } catch (_) {}
  }
  if (fs.existsSync(path.join(repoRoot, 'pyproject.toml')) || fs.existsSync(path.join(repoRoot, 'pytest.ini')) || fs.existsSync(path.join(repoRoot, 'tests'))) {
    commands.push({ label: 'pytest', cmd: 'pytest', args: [], cwd: repoRoot });
  }
  if (fs.existsSync(path.join(repoRoot, 'Cargo.toml'))) {
    ['cargo test', 'cargo build', 'cargo clippy'].forEach((label) => {
      const [cmd, ...args] = label.split(' ');
      commands.push({ label, cmd, args, cwd: repoRoot });
    });
  }
  if (fs.existsSync(path.join(repoRoot, 'go.mod'))) {
    commands.push({ label: 'go test ./...', cmd: 'go', args: ['test', './...'], cwd: repoRoot });
    commands.push({ label: 'go build ./...', cmd: 'go', args: ['build', './...'], cwd: repoRoot });
  }
  if (fs.existsSync(path.join(repoRoot, 'Makefile'))) {
    const txt = fs.readFileSync(path.join(repoRoot, 'Makefile'), 'utf8');
    txt.split(/\r?\n/).forEach((line) => {
      const match = line.match(/^([A-Za-z0-9_.-]+):\s*$/);
      if (!match || match[1].startsWith('.')) return;
      const target = match[1];
      commands.push({ label: `make ${target}`, cmd: 'make', args: [target], cwd: repoRoot });
    });
  }
  if (fs.existsSync(path.join(repoRoot, 'docker-compose.yml')) || fs.existsSync(path.join(repoRoot, 'docker-compose.yaml'))) {
    commands.push({ label: 'docker compose up', cmd: 'docker', args: ['compose', 'up'], cwd: repoRoot });
    commands.push({ label: 'docker compose down', cmd: 'docker', args: ['compose', 'down'], cwd: repoRoot });
  }
  return { commands: commands.slice(0, 50) };
}
app.get('/api/run/detect', handle(async (req, res) => {
  res.json(detectCommands(currentRepoRoot()));
}));
app.post('/api/run', handle(async (req, res) => {
  const cmd = typeof req.body?.cmd === 'string' ? req.body.cmd.trim() : '';
  const args = Array.isArray(req.body?.args) ? req.body.args : [];
  const givenCwd = typeof req.body?.cwd === 'string' ? req.body.cwd : '.';
  if (!cmd || !RUNNER_ALLOWLIST.has(cmd)) return res.status(400).json({ error: 'Command not permitted by runner allowlist.' });
  const repoRoot = currentRepoRoot();
  const cwd = path.resolve(repoRoot, givenCwd || '.');
  if (!explorer.isPathInside(repoRoot, cwd)) return res.status(400).json({ error: 'Invalid working directory.' });
  const runId = crypto.randomUUID();
  const session = { runId, cmd, args, cwd, start: Date.now(), output: [], exitCode: null, signal: null, process: null, ended: false };
  const proc = spawn(cmd, args, { cwd, shell: false, detached: process.platform !== 'win32', stdio: ['ignore', 'pipe', 'pipe'] });
  session.process = proc;
  runSessions.set(runId, session);
  const trimOldRuns = () => { while (runSessions.size > 10) { const oldest = runSessions.keys().next().value; if (oldest) runSessions.delete(oldest); } };
  const collect = (chunk, stream) => {
    const text = chunk.toString('utf8');
    session.output.push({ stream, text, at: Date.now() });
    if (session.output.length > 2500) session.output = session.output.slice(-2500);
  };
  proc.stdout.on('data', (chunk) => collect(chunk, 'stdout'));
  proc.stderr.on('data', (chunk) => collect(chunk, 'stderr'));
  proc.on('exit', (code, signal) => {
    session.exitCode = code;
    session.signal = signal;
    session.ended = true;
    session.durationMs = Date.now() - session.start;
    trimOldRuns();
  });
  proc.on('error', (error) => {
    session.output.push({ stream: 'stderr', text: `${error.message}\n`, at: Date.now() });
    session.ended = true;
    session.exitCode = 1;
    session.durationMs = Date.now() - session.start;
  });
  res.json({ ok: true, runId });
}));
app.post('/api/run/stop', handle(async (req, res) => {
  const runId = req.body?.runId;
  const session = runSessions.get(runId);
  if (!session || !session.process) return res.status(404).json({ error: 'Run not found' });
  try {
    if (process.platform === 'win32') {
      spawn('taskkill', ['/PID', String(session.process.pid), '/T', '/F'], { stdio: 'ignore' });
    } else {
      process.kill(-session.process.pid, 'SIGTERM');
    }
  } catch (_) {}
  res.json({ ok: true });
}));
app.get('/api/run/stream/:runId', handle(async (req, res) => {
  const runId = req.params.runId;
  const session = runSessions.get(runId);
  if (!session) return res.status(404).json({ error: 'Run not found' });
  res.setHeader('Content-Type', 'text/event-stream');
  res.setHeader('Cache-Control', 'no-cache');
  res.setHeader('Connection', 'keep-alive');
  res.flushHeaders();
  res.write(`event: meta\ndata: ${JSON.stringify({ runId, cmd: session.cmd, args: session.args, cwd: session.cwd, startedAt: session.start })}\n\n`);
  session.output.forEach((entry) => {
    res.write(`data: ${JSON.stringify({ stream: entry.stream, text: entry.text })}\n\n`);
  });
  const onUpdate = () => {
    const latest = session.output.slice(-50);
    latest.forEach((entry) => {
      res.write(`data: ${JSON.stringify({ stream: entry.stream, text: entry.text })}\n\n`);
    });
    session.output = [];
    if (session.ended) {
      res.write(`event: exit\ndata: ${JSON.stringify({ exitCode: session.exitCode, signal: session.signal, durationMs: session.durationMs })}\n\n`);
      res.end();
    }
  };
  if (session.ended) { onUpdate(); return; }
  const interval = setInterval(() => {
    if (session.ended) { clearInterval(interval); onUpdate(); return; }
    if (session.output.length) { const latest = session.output.splice(0, session.output.length); latest.forEach((entry) => res.write(`data: ${JSON.stringify({ stream: entry.stream, text: entry.text })}\n\n`)); }
  }, 150);
  req.on('close', () => clearInterval(interval));
}));

app.listen(PORT, () => {
  if (authConfig.enabled && !authConfig.token) {
    authConfig.token = crypto.randomBytes(32).toString('hex');
    config.auth = authConfig;
    fs.writeFileSync(path.join(__dirname, '..', 'config.json'), JSON.stringify(config, null, 2) + '\n');
    console.log('');
    console.log('═══════════════════════════════════════════');
    console.log('  ACCESS TOKEN (save this; shown only once)');
    console.log(`  ${authConfig.token}`);
    console.log('═══════════════════════════════════════════');
    console.log('');
  }
  console.log('');
  console.log('  ██████╗ ███████╗██████╗  ██████╗');
  console.log('  ██╔══██╗██╔════╝██╔══██╗██╔═══██╗');
  console.log('  ██████╔╝█████╗  ██████╔╝██║   ██║');
  console.log('  ██╔══██╗██╔══╝  ██╔═══╝ ██║   ██║');
  console.log('  ██║  ██║███████╗██║     ╚██████╔╝');
  console.log('  ╚═╝  ╚═╝╚══════╝╚═╝      ╚═════╝');
  console.log('  REPO COMMAND CENTER v2 · full');
  console.log('  --------------------');
  console.log(`  Repo:   ${REPO_ROOT}`);
  console.log(`  Server: http://localhost:${PORT}`);
  console.log(`  Auth:   ${authConfig.enabled ? 'enabled' : 'disabled'}`);
  console.log(`  GitHub: ${GITHUB_TOKEN ? 'configured' : 'not configured'}`);
  console.log(`  Ollama: ${OLLAMA_URL}`);
  console.log('');
  setTimeout(() => indexer.indexIncremental(REPO_ROOT, IGNORE_DIRS, git, false).catch((e) => console.error('[index]', e.message)), 2000);
  setInterval(() => indexer.indexIncremental(REPO_ROOT, IGNORE_DIRS, git, false).catch((e) => console.error('[index]', e.message)), 5 * 60 * 1000);
});