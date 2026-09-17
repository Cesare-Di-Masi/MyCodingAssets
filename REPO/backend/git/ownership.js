async function getOwnership(git, topN = 12) {
  const raw = await git.raw([
    'log', '--all', '--name-only',
    '--pretty=format:__C__%x09%an%x09%ae',
    '--date=short'
  ]);
  const authorEmail = {};
  const fileAuthor = {};
  let currentAuthor = null;
  for (const line of raw.split('\n')) {
    if (line.startsWith('__C__\t')) {
      const parts = line.slice(5).split('\t');
      currentAuthor = parts[0];
      authorEmail[currentAuthor] = parts[1] || '';
      continue;
    }
    if (!line.trim() || !currentAuthor) continue;
    if (!fileAuthor[line]) fileAuthor[line] = {};
    fileAuthor[line][currentAuthor] = (fileAuthor[line][currentAuthor] || 0) + 1;
  }

  const dirMap = {};
  for (const [file, aMap] of Object.entries(fileAuthor)) {
    const top = file.split('/')[0] || '(root)';
    if (!dirMap[top]) dirMap[top] = {};
    for (const [a, count] of Object.entries(aMap)) {
      dirMap[top][a] = (dirMap[top][a] || 0) + count;
    }
  }

  const rows = Object.entries(dirMap).map(([dir, counts]) => {
    const total = Object.values(counts).reduce((s, n) => s + n, 0) || 1;
    const top = Object.entries(counts).sort((a, b) => b[1] - a[1]).slice(0, 5)
      .map(([name, count]) => ({
        name, count, pct: count / total * 100, email: authorEmail[name] || ''
      }));
    return { dir, total, authors: top };
  }).sort((a, b) => b.total - a.total).slice(0, topN);

  const authors = Object.keys(authorEmail).map((name) => ({ name, email: authorEmail[name] }));
  return { authors, rows };
}
module.exports = { getOwnership };