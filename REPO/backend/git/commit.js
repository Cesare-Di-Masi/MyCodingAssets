async function stageFiles(git, files) {
  if (!files || !files.length) return;
  await git.add(files);
}
async function unstageFiles(git, files) {
  if (!files || !files.length) return;
  await git.reset(['HEAD', '--', ...files]);
}
async function commitFiles(git, message, amend = false) {
  if (!message || !message.trim()) throw new Error('Commit message required');
  const opts = amend ? { '--amend': null } : {};
  return git.commit(message, undefined, opts);
}
async function stageAll(git) { await git.add('.'); }
module.exports = { stageFiles, unstageFiles, commitFiles, stageAll };