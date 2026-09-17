async function reset(git, ref, options = {}) {
  const mode = ['soft', 'mixed', 'hard', 'keep'].includes(options.mode) ? options.mode : 'mixed';
  return git.raw(['reset', `--${mode}`, ref]);
}
module.exports = { reset };