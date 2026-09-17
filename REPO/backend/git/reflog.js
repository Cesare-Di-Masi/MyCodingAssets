async function getReflog(git, limit = 100) {
  try {
    const raw = await git.raw([
      'reflog', `--max-count=${limit}`,
      '--pretty=format:%H%x09%gd%x09%gs%x09%an%x09%ad',
      '--date=iso-strict'
    ]);
    if (!raw.trim()) return { entries: [] };
    return {
      entries: raw.split('\n').filter(Boolean).map((line) => {
        const [hash, ref, action, author, date] = line.split('\t');
        return { hash, short: hash.slice(0, 7), ref, action, author, date };
      })
    };
  } catch (e) { return { entries: [], error: e.message }; }
}
async function resetTo(git, hash, mode = 'hard') {
  if (!['soft', 'mixed', 'hard', 'keep'].includes(mode)) mode = 'mixed';
  await git.raw(['reset', `--${mode}`, hash]);
}
module.exports = { getReflog, resetTo };