const CLASSIFIERS = [
  { kind: 'feat',     re: /^(feat|feature|add|implement|introduce)\b/i },
  { kind: 'fix',      re: /^(fix|bugfix|hotfix|repair|correct|resolve)\b/i },
  { kind: 'refactor', re: /^(refactor|clean|rework|simplify|extract)\b/i },
  { kind: 'perf',     re: /^(perf|optimize|speed|faster)\b/i },
  { kind: 'docs',     re: /^(docs?|document|readme|comment)\b/i },
  { kind: 'test',     re: /^(test|spec|coverage)\b/i },
  { kind: 'chore',    re: /^(chore|build|ci|deps|bump|release|version)\b/i },
  { kind: 'style',    re: /^(style|format|lint|prettier)\b/i },
  { kind: 'revert',   re: /^revert\b/i }
];
function classify(subject) {
  const clean = subject.replace(/^\w+(\([^)]+\))?!?:\s*/, '').trim();
  for (const c of CLASSIFIERS) if (c.re.test(subject) || c.re.test(clean)) return c.kind;
  return 'other';
}
async function getInsights(git, days = 365) {
  const raw = await git.raw([
    'log', '--all', `--since=${days}.days`,
    '--pretty=format:%H%x09%an%x09%ad%x09%s', '--date=short'
  ]);
  const byKind = {}, byDay = {}, byAuthor = {};
  for (const line of raw.split('\n').filter(Boolean)) {
    const [hash, author, date, subject] = line.split('\t');
    const kind = classify(subject);
    byKind[kind] = (byKind[kind] || 0) + 1;
    byDay[date] = (byDay[date] || 0) + 1;
    byAuthor[author] = (byAuthor[author] || 0) + 1;
  }
  const total = Object.values(byKind).reduce((s, n) => s + n, 0) || 1;
  const filled = {};
  const today = new Date();
  for (let i = days - 1; i >= 0; i--) {
    const d = new Date(today); d.setDate(d.getDate() - i);
    const key = d.toISOString().slice(0, 10);
    filled[key] = byDay[key] || 0;
  }
  return {
    total,
    byKind: Object.entries(byKind).map(([kind, count]) => ({ kind, count, pct: count / total * 100 })).sort((a, b) => b.count - a.count),
    byDay: filled,
    byAuthor: Object.entries(byAuthor).map(([name, count]) => ({ name, count })).sort((a, b) => b.count - a.count)
  };
}
async function pickaxe(git, needle, file, limit = 100) {
  const args = ['log', `--max-count=${limit}`, '-S', needle, '--pretty=format:%H%x09%an%x09%ad%x09%s', '--date=iso-strict'];
  if (file) args.push('--', file);
  const raw = await git.raw(args);
  if (!raw.trim()) return { commits: [] };
  return {
    commits: raw.split('\n').filter(Boolean).map((line) => {
      const [hash, author, date, subject] = line.split('\t');
      return { hash, short: hash.slice(0, 7), author, date, subject };
    })
  };
}
module.exports = { getInsights, classify, pickaxe };