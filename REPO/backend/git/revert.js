async function revert(git, hash, options = {}) { return git.raw(['revert', ...(options.noCommit ? ['--no-commit'] : []), hash]); }
async function revertAbort(git) { return git.raw(['revert', '--abort']); }
async function revertContinue(git) { return git.raw(['revert', '--continue']); }
module.exports = { revert, revertAbort, revertContinue };