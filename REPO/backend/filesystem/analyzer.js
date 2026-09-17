const fs = require('fs');
const path = require('path');

const TAG_RE = /\b(TODO|FIXME|HACK|BUG|NOTE|XXX)\b[:\s]?(.*)/;
const SECRET_HINTS = [
  { re: /-----BEGIN (RSA|EC|DSA|OPENSSH)? ?PRIVATE KEY-----/, label: 'Private key' },
  { re: /AKIA[0-9A-Z]{16}/, label: 'AWS access key' },
  { re: /gh[pousr]_[A-Za-z0-9]{20,}/, label: 'GitHub token' },
  { re: /xox[baprs]-[A-Za-z0-9-]{10,}/, label: 'Slack token' },
  { re: /(?:api|secret)[_-]?key\s*[:=]\s*["'][A-Za-z0-9_\-]{16,}["']/i, label: 'Generic API key' },
  { re: /(?:password|passwd|pwd)\s*[:=]\s*["'][^"']{6,}["']/i, label: 'Hardcoded password' },
  { re: /eyJ[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}/, label: 'JWT token' }
];

function scanRepoForTodosAndSecrets(repoRoot, ignoreDirs, maxFileSize = 1024 * 1024) {
  const todos = [];
  const secrets = [];

  function walk(dir) {
    let entries;
    try { entries = fs.readdirSync(dir); } catch (e) { return; }
    for (const name of entries) {
      if (ignoreDirs.includes(name)) continue;
      const full = path.join(dir, name);
      let st;
      try { st = fs.statSync(full); } catch (e) { continue; }
      if (st.isDirectory()) { walk(full); continue; }
      if (st.size > maxFileSize) continue;

      let text;
      try {
        const buf = fs.readFileSync(full);
        if (buf.slice(0, 500).includes(0)) continue;
        text = buf.toString('utf8');
      } catch (e) { continue; }

      const relPath = path.relative(repoRoot, full).split(path.sep).join('/');
      const lines = text.split('\n');
      lines.forEach((line, idx) => {
        const m = line.match(TAG_RE);
        if (m) todos.push({ path: relPath, line: idx + 1, tag: m[1], text: (m[2] || '').trim().slice(0, 200) });
        for (const hint of SECRET_HINTS) {
          if (hint.re.test(line)) secrets.push({ path: relPath, line: idx + 1, kind: hint.label });
        }
      });
    }
  }
  walk(repoRoot);
  return { todos, secrets };
}

function findDuplicates(largestFilesLike, repoRoot, ignoreDirs) {
  const crypto = require('crypto');
  const bySize = {};

  function walk(dir) {
    let entries;
    try { entries = fs.readdirSync(dir); } catch (e) { return; }
    for (const name of entries) {
      if (ignoreDirs.includes(name)) continue;
      const full = path.join(dir, name);
      let st;
      try { st = fs.statSync(full); } catch (e) { continue; }
      if (st.isDirectory()) { walk(full); continue; }
      if (st.size === 0) continue;
      (bySize[st.size] = bySize[st.size] || []).push(full);
    }
  }
  walk(repoRoot);

  const groups = [];
  for (const size of Object.keys(bySize)) {
    const files = bySize[size];
    if (files.length < 2) continue;
    const byHash = {};
    for (const f of files) {
      try {
        const buf = fs.readFileSync(f);
        const hash = crypto.createHash('sha1').update(buf).digest('hex');
        (byHash[hash] = byHash[hash] || []).push(path.relative(repoRoot, f).split(path.sep).join('/'));
      } catch (e) { continue; }
    }
    for (const hash of Object.keys(byHash)) {
      if (byHash[hash].length > 1) {
        groups.push({ hash, size: Number(size), files: byHash[hash] });
      }
    }
  }
  groups.sort((a, b) => (b.size * b.files.length) - (a.size * a.files.length));
  return groups.slice(0, 50);
}

module.exports = { scanRepoForTodosAndSecrets, findDuplicates };