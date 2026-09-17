async function cherryPick(git, hashes) { return git.raw(['cherry-pick', ...(hashes || [])]); }
async function cherryPickAbort(git) { return git.raw(['cherry-pick', '--abort']); }
async function cherryPickContinue(git) {
	return git.env({ ...process.env, GIT_EDITOR: 'true', GIT_SEQUENCE_EDITOR: 'true' }).raw(['cherry-pick', '--continue']);
}
module.exports = { cherryPick, cherryPickAbort, cherryPickContinue };