async function listBranches(git) {
  const summary = await git.branch(['-a', '-v', '--no-abbrev']);
  const current = summary.current;
  const branches = Object.values(summary.branches).map((b) => ({
    name: b.name,
    current: b.current,
    commit: b.commit,
    label: b.label,
    remote: b.name.startsWith('remotes/')
  }));
  return { current, branches };
}

async function checkout(git, branchName) { await git.checkout(branchName); }

async function createBranch(git, name, from) {
  if (from) await git.checkoutBranch(name, from);
  else await git.checkoutLocalBranch(name);
}

async function deleteBranch(git, name, force) {
  if (force) await git.branch(['-D', name]);
  else await git.branch(['-d', name]);
}

async function compare(git, branchA, branchB) {
  const range = `${branchA}...${branchB}`;
  const log = await git.log({ from: range });
  const diffSummary = await git.diffSummary([range]);
  const numstat = await git.raw(['diff', '--numstat', range]);
  const files = numstat.split('\n').filter(Boolean).map((l) => {
    const [add, del, file] = l.split('\t');
    return {
      file,
      additions: add === '-' ? 0 : Number(add),
      deletions: del === '-' ? 0 : Number(del)
    };
  });
  return {
    commits: log.all.map((c) => ({
      hash: c.hash.slice(0, 7),
      fullHash: c.hash,
      message: c.message,
      author: c.author_name,
      date: c.date
    })),
    filesChanged: diffSummary.files.length,
    insertions: diffSummary.insertions,
    deletions: diffSummary.deletions,
    files
  };
}

async function mergePreview(git, from, into) {
  try {
    const base = await git.raw(['merge-base', from, into]);
    const diff = await git.raw(['diff', '--name-only', `${base.trim()}`, from]);
    const files = diff.split('\n').filter(Boolean);
    return { base: base.trim(), files, possibleConflicts: files.length };
  } catch (e) {
    return { base: null, files: [], possibleConflicts: 0 };
  }
}

module.exports = { listBranches, checkout, createBranch, deleteBranch, compare, mergePreview };