async function getDiff(git, { file, hash, staged }) {
  if (hash) {
    const args = ['show', hash, '--format='];
    if (file) args.push('--', file);
    return git.raw(args);
  }
  const args = [];
  if (staged) args.push('--staged');
  if (file) args.push('--', file);
  return git.diff(args);
}
module.exports = { getDiff };