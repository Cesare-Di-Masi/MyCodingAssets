const dbMod = require('./db');

function db(root) { return dbMod.get(root); }
function searchFilename(root, query, { limit = 200 } = {}) {
  return db(root).prepare("SELECT path, 'filename' AS type FROM files WHERE path LIKE ? ORDER BY path LIMIT ?").all(`%${query}%`, limit);
}
function searchContent(root, query, { limit = 200, caseSensitive = false, regex = false } = {}) {
  const database = db(root);
  if (regex) {
    let re; try { re = new RegExp(query, caseSensitive ? '' : 'i'); } catch (e) { return [{ error: `Invalid regex: ${e.message}` }]; }
    const rows = database.prepare('SELECT path, content FROM file_fts LIMIT ?').all(Math.max(limit * 4, 500));
    return rows.reduce((out, row) => {
      if (out.length >= limit || !re.test(row.content)) return out;
      const matches = row.content.split('\n').map((text, i) => re.test(text) ? { line: i + 1, text: text.trim().slice(0, 200) } : null).filter(Boolean).slice(0, 5);
      out.push({ path: row.path, type: 'regex', matches });
      return out;
    }, []);
  }
  const match = query.trim().split(/\s+/).map((part) => `"${part.replace(/"/g, '""')}"`).join(' ');
  const rows = database.prepare('SELECT path, content FROM file_fts WHERE file_fts MATCH ? LIMIT ?').all(match, limit);
  const needle = caseSensitive ? query : query.toLowerCase();
  return rows.map((row) => ({
    path: row.path, type: 'content',
    matches: row.content.split('\n').map((text, i) => (caseSensitive ? text : text.toLowerCase()).includes(needle) ? { line: i + 1, text: text.trim().slice(0, 200) } : null).filter(Boolean).slice(0, 3)
  }));
}
function findDuplicates(root) {
  return db(root).prepare(`SELECT hash_sha1 AS hash, size, group_concat(path) AS paths FROM files WHERE hash_sha1 IS NOT NULL GROUP BY hash_sha1 HAVING count(*) > 1 ORDER BY size * count(*) DESC LIMIT 50`).all().map((r) => ({ ...r, files: r.paths.split(',') }));
}
function getTodos(root, filter = {}) {
  const where = filter.path ? ' WHERE path = ?' : '';
  return db(root).prepare(`SELECT path, line, tag, text FROM todos${where} ORDER BY path, line`).all(...(filter.path ? [filter.path] : []));
}
function getSecrets(root, filter = {}) {
  const where = filter.path ? ' WHERE path = ?' : '';
  return db(root).prepare(`SELECT path, line, kind FROM secrets${where} ORDER BY path, line`).all(...(filter.path ? [filter.path] : []));
}
function getOwnership(root, topN = 15) {
  const database = db(root);
  const rows = database.prepare(`SELECT c.author name, c.email, cf.path, count(*) count FROM commit_files cf JOIN commits c ON c.hash = cf.commit_hash GROUP BY c.author, c.email, cf.path`).all();
  const authors = {}; const dirs = {};
  for (const row of rows) {
    authors[row.name] = row.email;
    const dir = row.path.split('/')[0] || '(root)';
    dirs[dir] ||= {};
    dirs[dir][row.name] = (dirs[dir][row.name] || 0) + row.count;
  }
  const result = Object.entries(dirs).map(([dir, counts]) => {
    const total = Object.values(counts).reduce((sum, n) => sum + n, 0) || 1;
    return { dir, total, authors: Object.entries(counts).sort((a, b) => b[1] - a[1]).slice(0, 5).map(([name, count]) => ({ name, count, pct: count / total * 100, email: authors[name] || '' })) };
  }).sort((a, b) => b.total - a.total).slice(0, topN);
  return { authors: Object.entries(authors).map(([name, email]) => ({ name, email })), rows: result };
}
function getFileTimeline(root, file, limit = 100) {
  return { commits: db(root).prepare('SELECT hash, substr(hash, 1, 7) short, author, date, subject FROM commits WHERE hash IN (SELECT commit_hash FROM commit_files WHERE path = ?) ORDER BY date DESC LIMIT ?').all(file, limit) };
}
function getLanguageStats(root) { return db(root).prepare('SELECT language, count(*) count, sum(size) size FROM files GROUP BY language ORDER BY count DESC').all(); }
function getStats(root) {
  const row = db(root).prepare('SELECT count(*) totalFiles, coalesce(sum(size), 0) totalSize FROM files').get();
  const totalDirs = db(root).prepare('SELECT count(*) totalDirs FROM dirs').get().totalDirs;
  const extCounts = Object.fromEntries(db(root).prepare('SELECT ext, count(*) count FROM files GROUP BY ext').all().map((r) => [r.ext, r.count]));
  const largestFiles = db(root).prepare('SELECT path, size FROM files ORDER BY size DESC LIMIT 12').all();
  return { ...row, totalDirs, extCounts, largestFiles };
}
function symbols(root, file) { return db(root).prepare('SELECT line, kind, name FROM symbols WHERE path = ? ORDER BY line').all(file); }
function searchSymbols(root, q, limit = 100) { return db(root).prepare('SELECT path, line, kind, name FROM symbols WHERE name LIKE ? ORDER BY name, path, line LIMIT ?').all(`%${q}%`, limit); }

module.exports = { searchFilename, searchContent, findDuplicates, getTodos, getSecrets, getOwnership, getFileTimeline, getLanguageStats, getStats, symbols, searchSymbols };