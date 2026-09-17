const fs = require('fs');
const path = require('path');

function readRules(repoRoot) {
  const gi = path.join(repoRoot, '.gitignore');
  if (!fs.existsSync(gi)) return { exists: false, rules: [], path: gi };
  const text = fs.readFileSync(gi, 'utf8');
  const rules = text.split('\n').map((l) => l.trim()).filter(Boolean);
  return { exists: true, rules, path: gi, text };
}

function analyze(repoRoot, ignoreDirs) {
  const info = readRules(repoRoot);
  const counts = {};
  for (const rule of info.rules) {
    const clean = rule.replace(/^!/, '').replace(/\/$/, '');
    if (!clean || clean.startsWith('#')) continue;
    const base = clean.split('/')[0];
    const full = path.join(repoRoot, base);
    if (fs.existsSync(full) && fs.statSync(full).isDirectory()) {
      let n = 0;
      (function walk(dir) {
        let entries;
        try { entries = fs.readdirSync(dir); } catch (e) { return; }
        for (const e of entries) {
          const f = path.join(dir, e);
          try {
            const st = fs.statSync(f);
            if (st.isDirectory()) walk(f);
            else { n++; if (n > 5000) return; }
          } catch (_) {}
        }
      })(full);
      counts[rule] = n;
    }
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
  const gi = path.join(repoRoot, '.gitignore');
  const existing = fs.existsSync(gi) ? fs.readFileSync(gi, 'utf8') : '';
  const toAdd = rules.filter((r) => r && !existing.includes(r));
  if (!toAdd.length) return;
  const prefix = existing.endsWith('\n') || !existing ? '' : '\n';
  fs.appendFileSync(gi, `${prefix}\n# Added by Repo Command Center\n${toAdd.join('\n')}\n`);
}

function ensureRccIndexIgnored(repoRoot) {
  append(repoRoot, ['.rcc-index/']);
}

module.exports = { analyze, append, readRules, ensureRccIndexIgnored };