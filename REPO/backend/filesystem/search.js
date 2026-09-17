const fs = require('fs');
const path = require('path');

function isProbablyText(buf) {
  const len = Math.min(buf.length, 1000);
  for (let i = 0; i < len; i++) if (buf[i] === 0) return false;
  return true;
}

function search(repoRoot, ignoreDirs, query, mode, maxResults) {
  const results = [];
  const q = query.toLowerCase();
  const maxFileSize = 2 * 1024 * 1024;
  let regex = null;
  if (mode === 'regex') {
    try { regex = new RegExp(query, 'i'); } catch (e) { return [{ error: 'Invalid regex: ' + e.message }]; }
  }

  function walk(dir) {
    if (results.length >= maxResults) return;
    let entries;
    try { entries = fs.readdirSync(dir); } catch (e) { return; }
    for (const name of entries) {
      if (results.length >= maxResults) return;
      if (ignoreDirs.includes(name)) continue;
      const full = path.join(dir, name);
      let st;
      try { st = fs.statSync(full); } catch (e) { continue; }
      const relPath = path.relative(repoRoot, full).split(path.sep).join('/');

      if (st.isDirectory()) { walk(full); continue; }

      if (mode === 'filename') {
        if (name.toLowerCase().includes(q)) results.push({ path: relPath, type: 'filename' });
        continue;
      }

      if (st.size > maxFileSize) continue;
      let buf;
      try { buf = fs.readFileSync(full); } catch (e) { continue; }
      if (!isProbablyText(buf)) continue;
      const text = buf.toString('utf8');

      if (mode === 'regex') {
        if (!regex.test(text)) continue;
        regex.lastIndex = 0;
        const lines = text.split('\n');
        const matches = [];
        for (let i = 0; i < lines.length && matches.length < 5; i++) {
          if (regex.test(lines[i])) {
            matches.push({ line: i + 1, text: lines[i].trim().slice(0, 200) });
            regex.lastIndex = 0;
          }
        }
        results.push({ path: relPath, type: 'regex', matches });
      } else {
        if (!text.toLowerCase().includes(q)) continue;
        const lines = text.split('\n');
        const matches = [];
        for (let i = 0; i < lines.length && matches.length < 3; i++) {
          if (lines[i].toLowerCase().includes(q)) {
            matches.push({ line: i + 1, text: lines[i].trim().slice(0, 200) });
          }
        }
        results.push({ path: relPath, type: 'content', matches });
      }
    }
  }
  walk(repoRoot);
  return results.slice(0, maxResults);
}
module.exports = { search };