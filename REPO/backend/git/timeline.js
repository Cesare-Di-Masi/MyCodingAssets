async function getFileTimeline(git, file, limit = 100) {
  try {
    const raw = await git.raw([
      'log', '--follow', `--max-count=${limit}`,
      '--pretty=format:%H%x09%an%x09%ad%x09%s',
      '--date=iso-strict', '--', file
    ]);
    if (!raw.trim()) return { commits: [] };
    return {
      commits: raw.split('\n').filter(Boolean).map((line) => {
        const [hash, author, date, subject] = line.split('\t');
        return { hash, short: hash.slice(0, 7), author, date, subject };
      })
    };
  } catch (e) {
    return { commits: [], error: e.message };
  }
}
module.exports = { getFileTimeline };