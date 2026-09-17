async function getStatus(git) {
  const s = await git.status();
  return {
    current: s.current,
    tracking: s.tracking,
    ahead: s.ahead,
    behind: s.behind,
    staged: s.staged,
    modified: s.modified.filter((f) => !s.staged.includes(f)),
    created: s.created,
    deleted: s.deleted,
    renamed: s.renamed,
    not_added: s.not_added,
    conflicted: s.conflicted,
    isClean: s.isClean(),
    totalChanges:
      s.staged.length + s.modified.length + s.not_added.length + s.deleted.length
  };
}
module.exports = { getStatus };