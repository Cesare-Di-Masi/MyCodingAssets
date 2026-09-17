async function list(git) {
  try {
    const raw = await git.raw(['tag', '-l', '--format=%(refname:short)|%(objectname:short)|%(creatordate:iso-strict)|%(contents:subject)|%(objecttype)']);
    if (!raw.trim()) return { tags: [] };
    return {
      tags: raw.split('\n').filter(Boolean).map((l) => {
        const [name, hash, date, subject, type] = l.split('|');
        return { name, hash, date, subject, type };
      }).reverse()
    };
  } catch (e) { return { tags: [], error: e.message }; }
}
async function create(git, name, message, ref) {
  if (!name) throw new Error('Tag name required');
  if (message) await git.raw(['tag', '-a', name, '-m', message, ...(ref ? [ref] : [])]);
  else await git.raw(['tag', name, ...(ref ? [ref] : [])]);
}
async function remove(git, name) { await git.raw(['tag', '-d', name]); }
async function push(git, name, remote) { await git.raw(['push', remote || 'origin', `refs/tags/${name}`]); }
async function removeRemote(git, name, remote) { await git.raw(['push', remote || 'origin', `:refs/tags/${name}`]); }
module.exports = { list, create, remove, push, removeRemote };