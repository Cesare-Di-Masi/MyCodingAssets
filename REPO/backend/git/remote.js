async function list(git) {
  const raw = await git.raw(['remote', '-v']);
  const map = {};
  raw.split('\n').filter(Boolean).forEach((l) => {
    const parts = l.split(/\s+/);
    if (!map[parts[0]]) map[parts[0]] = { name: parts[0], url: parts[1], type: parts[2] };
  });
  return { remotes: Object.values(map) };
}
async function add(git, name, url) { await git.raw(['remote', 'add', name, url]); }
async function remove(git, name) { await git.raw(['remote', 'remove', name]); }
async function fetch(git, name) { await git.raw(['fetch', name || '--all']); }
async function pull(git, name) { await git.raw(['pull', name || 'origin']); }
async function push(git, name, branch) {
  const args = ['push', name || 'origin'];
  if (branch) args.push(branch);
  await git.raw(args);
}
async function tracking(git) {
  try { return { raw: await git.raw(['remote', 'show', 'origin']) }; }
  catch (e) { return { raw: '' }; }
}
module.exports = { list, add, remove, fetch, pull, push, tracking };