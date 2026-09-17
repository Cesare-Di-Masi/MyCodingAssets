const fs = require('fs');
const path = require('path');

async function list(git) {
  const status = await git.status();
  return { files: status.conflicted || [] };
}
async function read(repoRoot, file) {
  const abs = path.join(repoRoot, file);
  const text = fs.readFileSync(abs, 'utf8');
  const re = /<<<<<<< [^\n]*\n([\s\S]*?)\n?=======\n([\s\S]*?)\n?>>>>>>> [^\n]*/g;
  const blocks = [];
  let m;
  while ((m = re.exec(text)) !== null) {
    blocks.push({ ours: m[1], theirs: m[2], start: m.index, end: re.lastIndex });
  }
  return { raw: text, blocks };
}
async function resolve(repoRoot, file, resolution) {
  const abs = path.join(repoRoot, file);
  let text = fs.readFileSync(abs, 'utf8');
  for (const block of resolution) {
    const re = /<<<<<<< [^\n]*\n([\s\S]*?)\n?=======\n([\s\S]*?)\n?>>>>>>> [^\n]*/;
    const m = text.match(re);
    if (!m) break;
    let replacement = '';
    if (block.choice === 'ours') replacement = m[1];
    else if (block.choice === 'theirs') replacement = m[2];
    else if (block.choice === 'both') replacement = m[1] + '\n' + m[2];
    else if (block.choice === 'custom') replacement = block.custom || '';
    text = text.replace(re, replacement);
  }
  fs.writeFileSync(abs, text);
}
async function markResolved(git, file) { await git.add(file); }
module.exports = { list, read, resolve, markResolved };