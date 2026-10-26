const fs = require('fs');
const path = require('path');

function readRules(repoRoot) {
  const gi = path.join(repoRoot, '.gitignore');
  if (!fs.existsSync(gi)) return { exists: false, rules: [], path: gi };
  const text = fs.readFileSync(gi, 'utf8');
  const rules = text.split('\n').map((l) => l.trim()).filter(Boolean);
  return { exists: true, rules, path: gi, text };
}

function countFiles(root, ignoreDirs, cap = 5000) {
  let count = 0;
  const pending = [root];
  const visited = new Set();
  while (pending.length && count < cap) {
    const dir = pending.pop();
    let realDir;
    try { realDir = fs.realpathSync(dir); } catch (e) { continue; }
    if (visited.has(realDir)) continue;
    visited.add(realDir);
    let entries;
    try { entries = fs.readdirSync(dir); } catch (e) { continue; }
    for (const name of entries) {
      if (ignoreDirs.includes(name)) continue;
      let st;
      try { st = fs.lstatSync(path.join(dir, name)); } catch (e) { continue; }
      if (st.isSymbolicLink()) continue;
      if (st.isDirectory()) pending.push(path.join(dir, name));
      else if (st.isFile()) count++;
      if (count >= cap) break;
    }
  }
  return count;
}

function analyze(repoRoot, ignoreDirs) {
  const info = readRules(repoRoot);
  const counts = {};
  for (const rule of info.rules) {
    const clean = rule.replace(/^!/, '').replace(/\/$/, '');
    if (!clean || clean.startsWith('#')) continue;
    const base = clean.split('/')[0];
    const full = path.join(repoRoot, base);
    try {
      const st = fs.lstatSync(full);
      if (st.isDirectory() && !st.isSymbolicLink()) counts[rule] = countFiles(full, ignoreDirs);
    } catch (e) {}
  }

  // Suspicious: sensitive-looking files not ignored
  const suspects = [];
  const names = ['secret', 'secrets', '.env', 'credentials', 'id_rsa', '.pem', 'apikey', 'api_key', 'token'];
  const gitignoreText = info.text || '';
  for (const s of names) {
    const escaped = s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    if (!new RegExp(escaped, 'i').test(gitignoreText)) {
      suspects.push({ pattern: s, ignored: false });
    }
  }

  return {
    exists: info.exists,
    rules: info.rules,
    counts,
    suspects,
    text: info.text || ''
  };
}

function append(repoRoot, rules) {
  if (!Array.isArray(rules)) throw new Error('rules must be an array');
  if (rules.length > 200) throw new Error('Too many ignore rules');
  const gi = path.join(repoRoot, '.gitignore');
  const existing = fs.existsSync(gi) ? fs.readFileSync(gi, 'utf8') : '';
  const existingRules = new Set(existing.split(/\r?\n/).map((rule) => rule.trim()).filter(Boolean));
  const candidates = rules.map((rule) => {
    if (typeof rule !== 'string') throw new Error('Each ignore rule must be a string');
    const clean = rule.trim();
    if (clean.length > 500 || /[\r\n\0]/.test(clean)) throw new Error('Invalid ignore rule');
    return clean;
  }).filter(Boolean);
  const toAdd = [...new Set(candidates)].filter((rule) => !existingRules.has(rule));
  if (!toAdd.length) return;
  const prefix = existing.endsWith('\n') || !existing ? '' : '\n';
  fs.appendFileSync(gi, `${prefix}\n# Added by Repo Command Center\n${toAdd.join('\n')}\n`);
}

function ensureRccIndexIgnored(repoRoot) {
  append(repoRoot, ['.rcc-index/']);
}

module.exports = { analyze, append, readRules, ensureRccIndexIgnored };
