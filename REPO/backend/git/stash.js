async function list(git) {
  const raw = await git.raw(['stash', 'list', '--pretty=format:%gd|%s|%an|%ad']);
  if (!raw.trim()) return [];
  return raw.split('\n').filter(Boolean).map((line) => {
    const [ref, subject, author, date] = line.split('|');
    return { ref, subject, author, date };
  });
}
async function push(git, message, includeUntracked) {
  const args = ['stash', 'push'];
  if (includeUntracked) args.push('-u');
  if (message) args.push('-m', message);
  await git.raw(args);
}
async function pop(git, ref) {
  await git.raw(['stash', 'pop', ...(ref ? [ref] : [])]);
}
async function apply(git, ref) {
  await git.raw(['stash', 'apply', ...(ref ? [ref] : [])]);
}
async function drop(git, ref) {
  await git.raw(['stash', 'drop', ...(ref ? [ref] : [])]);
}
async function show(git, ref) {
  try {
    return await git.raw(['stash', 'show', '-p', ref || 'stash@{0}']);
  } catch (e) {
    return '';
  }
}
module.exports = { list, push, pop, apply, drop, show };