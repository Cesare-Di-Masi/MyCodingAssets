// Returns commits with parent lists; the frontend computes lanes.
async function getGraph(git, limit = 400) {
  const raw = await git.raw([
    'log', '--all', `--max-count=${limit}`,
    '--pretty=format:%H%x09%P%x09%an%x09%ad%x09%s%x09%D',
    '--date=iso-strict'
  ]);
  if (!raw.trim()) return { commits: [] };

  const commits = raw.split('\n').filter(Boolean).map((line) => {
    const [hash, parents, author, date, subject, refs] = line.split('\t');
    return {
      hash,
      short: hash.slice(0, 7),
      parents: parents ? parents.split(' ').filter(Boolean) : [],
      author,
      date,
      subject,
      refs: refs || ''
    };
  });

  return { commits };
}
module.exports = { getGraph };