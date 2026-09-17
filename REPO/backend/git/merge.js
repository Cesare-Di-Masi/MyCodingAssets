async function merge(git, branch, options = {}) {
  const args = ['merge'];
  if (options.ff === false) args.push('--no-ff'); else if (options.ff === true) args.push('--ff-only');
  if (options.noCommit) args.push('--no-commit'); if (options.squash) args.push('--squash');
  if (options.message) args.push('-m', options.message); args.push(branch); return git.raw(args);
}
async function mergeAbort(git) { return git.raw(['merge', '--abort']); }
async function mergeContinue(git, message) { const args = ['merge', '--continue']; if (message) args.push('-m', message); return git.raw(args); }
module.exports = { merge, mergeAbort, mergeContinue };