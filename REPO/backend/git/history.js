async function getLog(git, limit = 100, branch, search) {
  const args = { maxCount: limit };
  if (branch) args.from = branch;
  if (search) args['--grep'] = search;
  const log = await git.log(args);
  return log.all.map((c) => ({
    hash: c.hash.slice(0, 7),
    fullHash: c.hash,
    message: c.message,
    subject: (c.message || '').split('\n')[0],
    body: (c.message || '').split('\n').slice(1).join('\n').trim(),
    author: c.author_name,
    email: c.author_email,
    date: c.date,
    refs: c.refs
  }));
}

async function getContributors(git, limit = 20) {
  try {
    const raw = await git.raw(['shortlog', '-sne', 'HEAD']);
    return raw
      .split('\n')
      .filter(Boolean)
      .slice(0, limit)
      .map((line) => {
        const m = line.trim().match(/^(\d+)\s+(.+?)\s+<(.+)>$/);
        if (!m) return null;
        return { commits: Number(m[1]), name: m[2], email: m[3] };
      })
      .filter(Boolean);
  } catch (e) {
    return [];
  }
}

async function getCommit(git, hash) {
  const show = await git.show([hash, '--stat', '--format=fuller']);
  const numstat = await git.raw(['show', '--numstat', '--format=', hash]);
  const files = numstat
    .split('\n')
    .filter(Boolean)
    .map((line) => {
      const [add, del, file] = line.split('\t');
      return {
        file: file || '',
        additions: add === '-' ? null : Number(add),
        deletions: del === '-' ? null : Number(del)
      };
    });
  const log = await git.log(['-1', hash]);
  const entry = log.latest;
  return {
    hash,
    message: entry ? entry.message : '',
    author: entry ? entry.author_name : '',
    email: entry ? entry.author_email : '',
    date: entry ? entry.date : '',
    files,
    raw: show
  };
}

module.exports = { getLog, getCommit, getContributors };