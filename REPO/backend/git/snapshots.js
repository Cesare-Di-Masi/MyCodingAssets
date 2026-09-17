const fs = require('fs');
const path = require('path');

const SNAPSHOT_DIR = '.rcc-snapshots';

async function create(git, repoRoot, label) {
  const dir = path.join(repoRoot, SNAPSHOT_DIR);
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
  const id = Date.now().toString(36) + '-' + Math.random().toString(36).slice(2, 8);
  const head = (await git.revparse(['HEAD'])).trim();
  const stashRef = `snapshot-${id}`;
  try {
    await git.raw(['stash', 'push', '-u', '-m', stashRef]);
    const stashList = await git.raw(['stash', 'list']);
    const line = stashList.split('\n').find((l) => l.includes(stashRef));
    const index = line ? line.match(/stash@\{(\d+)\}/)[1] : null;
    const meta = { id, label: label || stashRef, createdAt: new Date().toISOString(), head, stashRef, stashIndex: index };
    fs.writeFileSync(path.join(dir, `${id}.json`), JSON.stringify(meta, null, 2));
    return meta;
  } catch (e) {
    const meta = { id, label: label || stashRef, createdAt: new Date().toISOString(), head, empty: true };
    fs.writeFileSync(path.join(dir, `${id}.json`), JSON.stringify(meta, null, 2));
    return meta;
  }
}
function list(repoRoot) {
  const dir = path.join(repoRoot, SNAPSHOT_DIR);
  if (!fs.existsSync(dir)) return [];
  return fs.readdirSync(dir)
    .filter((f) => f.endsWith('.json'))
    .map((f) => { try { return JSON.parse(fs.readFileSync(path.join(dir, f), 'utf8')); } catch (e) { return null; } })
    .filter(Boolean)
    .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
}
async function restore(git, repoRoot, id) {
  const dir = path.join(repoRoot, SNAPSHOT_DIR);
  const file = path.join(dir, `${id}.json`);
  if (!fs.existsSync(file)) throw new Error('Snapshot not found');
  const meta = JSON.parse(fs.readFileSync(file, 'utf8'));
  if (meta.empty) { await git.raw(['checkout', meta.head]); return meta; }
  const stashList = await git.raw(['stash', 'list']);
  const line = stashList.split('\n').find((l) => l.includes(meta.stashRef));
  if (!line) throw new Error('Snapshot stash was removed manually');
  const idx = line.match(/stash@\{(\d+)\}/)[1];
  await git.raw(['stash', 'apply', `stash@{${idx}}`]);
  return meta;
}
async function remove(git, repoRoot, id) {
  const dir = path.join(repoRoot, SNAPSHOT_DIR);
  const file = path.join(dir, `${id}.json`);
  if (!fs.existsSync(file)) return;
  const meta = JSON.parse(fs.readFileSync(file, 'utf8'));
  if (meta.stashRef) {
    const stashList = await git.raw(['stash', 'list']);
    const line = stashList.split('\n').find((l) => l.includes(meta.stashRef));
    if (line) {
      const idx = line.match(/stash@\{(\d+)\}/)[1];
      await git.raw(['stash', 'drop', `stash@\{${idx}\}`]);
    }
  }
  fs.unlinkSync(file);
}
module.exports = { create, list, restore, remove };