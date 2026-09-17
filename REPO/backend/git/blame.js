async function getBlame(git, file) {
  try {
    const raw = await git.raw(['blame', '--line-porcelain', '--', file]);
    const lines = [];
    let current = {};
    const out = [];
    for (const line of raw.split('\n')) {
      if (/^[0-9a-f]{40}\s/.test(line)) {
        if (current.hash) out.push(current);
        const parts = line.split(' ');
        current = { hash: parts[0], short: parts[0].slice(0, 7), lineNo: Number(parts[2]) };
      } else if (line.startsWith('author ')) {
        current.author = line.slice(7);
      } else if (line.startsWith('author-time ')) {
        current.time = Number(line.slice(12)) * 1000;
      } else if (line.startsWith('\t')) {
        current.text = line.slice(1);
        out.push(current);
        current = {};
      }
    }
    if (current.hash) out.push(current);
    return { lines: out };
  } catch (e) {
    return { lines: [], error: e.message };
  }
}
module.exports = { getBlame };