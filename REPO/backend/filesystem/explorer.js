const fs = require('fs');
const path = require('path');

function shouldIgnore(name, ignoreDirs) { return ignoreDirs.includes(name); }

function statEntry(fullPath) {
  const st = fs.statSync(fullPath);
  return { size: st.size, mtime: st.mtimeMs, isDir: st.isDirectory() };
}

function isPathInside(repoRoot, candidate) {
  const rootReal = fs.realpathSync(repoRoot);
  let resolved;
  try {
    resolved = fs.realpathSync(candidate);
  } catch (e) {
    const parent = fs.realpathSync(path.dirname(candidate));
    resolved = path.join(parent, path.basename(candidate));
  }
  const rel = path.relative(rootReal, resolved);
  return !!rel && !rel.startsWith('..') && !path.isAbsolute(rel);
}

function listDir(repoRoot, relPath, ignoreDirs) {
  const abs = path.resolve(repoRoot, relPath || '.');
  if (!isPathInside(repoRoot, abs)) throw new Error('Invalid path');
  const names = fs.readdirSync(abs);
  const entries = names
    .filter((n) => !shouldIgnore(n, ignoreDirs))
    .map((n) => {
      const full = path.join(abs, n);
      let st;
      try { st = statEntry(full); } catch (e) { return null; }
      if (!isPathInside(repoRoot, full)) return null;
      return {
        name: n,
        path: path.relative(repoRoot, full).split(path.sep).join('/'),
        isDir: st.isDir,
        size: st.isDir ? null : st.size,
        mtime: st.mtime
      };
    })
    .filter(Boolean);

  entries.sort((a, b) => {
    if (a.isDir !== b.isDir) return a.isDir ? -1 : 1;
    return a.name.localeCompare(b.name);
  });
  return entries;
}

function walkRepo(repoRoot, ignoreDirs, cap = 200000) {
  const stats = {
    totalFiles: 0, totalDirs: 0, totalSize: 0,
    languages: {}, largestFiles: [], extCounts: {}
  };
  const extOf = (name) => (path.extname(name).replace('.', '').toLowerCase() || 'no-ext');

  function walk(dir) {
    if (stats.totalFiles > cap) return;
    let entries;
    try { entries = fs.readdirSync(dir); } catch (e) { return; }
    for (const name of entries) {
      if (shouldIgnore(name, ignoreDirs)) continue;
      const full = path.join(dir, name);
      if (!isPathInside(repoRoot, full)) continue;
      let st;
      try { st = fs.statSync(full); } catch (e) { continue; }
      if (st.isDirectory()) { stats.totalDirs++; walk(full); }
      else {
        stats.totalFiles++;
        stats.totalSize += st.size;
        const ext = extOf(name);
        stats.extCounts[ext] = (stats.extCounts[ext] || 0) + 1;
        stats.largestFiles.push({
          path: path.relative(repoRoot, full).split(path.sep).join('/'),
          size: st.size
        });
      }
    }
  }
  walk(repoRoot);
  stats.largestFiles.sort((a, b) => b.size - a.size);
  stats.largestFiles = stats.largestFiles.slice(0, 30);
  return stats;
}

function readFile(repoRoot, relPath, maxBytes = 500000) {
  const abs = path.resolve(repoRoot, relPath);
  if (!isPathInside(repoRoot, abs)) throw new Error('Invalid path');
  const st = fs.statSync(abs);
  if (st.isDirectory()) throw new Error('Is a directory');
  const buf = fs.readFileSync(abs);
  const truncated = buf.length > maxBytes;
  const text = buf.slice(0, maxBytes).toString('utf8');
  return { content: text, truncated, size: st.size };
}

module.exports = { listDir, walkRepo, readFile, isPathInside, shouldIgnore };