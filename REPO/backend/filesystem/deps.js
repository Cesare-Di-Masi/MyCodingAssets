const fs = require('fs');
const path = require('path');

function detect(repoRoot, ignoreDirs) {
  const manifests = [];

  function walk(dir, depth = 0) {
    if (depth > 4) return;
    let entries;
    try { entries = fs.readdirSync(dir); } catch (e) { return; }
    for (const name of entries) {
      if (ignoreDirs.includes(name)) continue;
      const full = path.join(dir, name);
      let st;
      try { st = fs.statSync(full); } catch (e) { continue; }
      if (st.isDirectory()) { walk(full, depth + 1); continue; }
      if (['package.json', 'requirements.txt', 'pyproject.toml', 'go.mod',
           'Cargo.toml', 'Gemfile', 'composer.json', 'pom.xml', 'build.gradle'].includes(name)) {
        manifests.push({ file: path.relative(repoRoot, full).split(path.sep).join('/'), full, kind: name });
      }
    }
  }
  walk(repoRoot);

  const results = manifests.map((m) => {
    let deps = [];
    try {
      if (m.kind === 'package.json') {
        const pkg = JSON.parse(fs.readFileSync(m.full, 'utf8'));
        deps = Object.entries({
          ...(pkg.dependencies || {}),
          ...(pkg.devDependencies || {})
        }).map(([name, version]) => ({ name, version, dev: !!pkg.devDependencies?.[name] }));
      } else if (m.kind === 'requirements.txt') {
        deps = fs.readFileSync(m.full, 'utf8').split('\n')
          .filter((l) => l.trim() && !l.startsWith('#'))
          .map((l) => {
            const m2 = l.match(/^([^=<>!~]+)[=<>!~]+(.+)$/);
            return m2 ? { name: m2[1].trim(), version: m2[2].trim() } : { name: l.trim(), version: '*' };
          });
      } else if (m.kind === 'go.mod') {
        const text = fs.readFileSync(m.full, 'utf8');
        deps = text.split('\n')
          .filter((l) => /^\s+\S+\s+v/.test(l))
          .map((l) => {
            const m2 = l.trim().split(/\s+/);
            return { name: m2[0], version: m2[1] };
          });
      } else {
        const text = fs.readFileSync(m.full, 'utf8').slice(0, 5000);
        deps = [{ name: '(manifest)', version: text.split('\n').length + ' lines' }];
      }
    } catch (e) { /* ignore parse errors */ }
    return { file: m.file, kind: m.kind, deps };
  });

  return { manifests: results };
}

module.exports = { detect };