const { classify } = require('./insights');

async function generate(git, fromRef, toRef, version) {
  const range = fromRef && toRef ? `${fromRef}..${toRef}` : (fromRef ? `${fromRef}..HEAD` : 'HEAD');
  const raw = await git.raw([
    'log', range,
    '--pretty=format:%H%x09%an%x09%ad%x09%s%x09%b%x1e',
    '--date=short'
  ]);
  const entries = raw.split('\x1e').filter(Boolean).map((chunk) => {
    const [meta, ...bodyParts] = chunk.split('\n');
    const [hash, author, date, subject] = meta.split('\t');
    return { hash, short: hash.slice(0, 7), author, date, subject, body: bodyParts.join('\n').trim(), kind: classify(subject) };
  }).filter((e) => e.hash);

  const groups = { feat: [], fix: [], refactor: [], perf: [], docs: [], test: [], chore: [], style: [], revert: [], other: [] };
  for (const e of entries) groups[e.kind].push(e);

  const LABELS = {
    feat: 'Added', fix: 'Fixed', refactor: 'Changed', perf: 'Performance',
    docs: 'Documentation', test: 'Tests', chore: 'Chores', style: 'Styling',
    revert: 'Reverted', other: 'Other'
  };
  let out = '## ' + (version || 'Unreleased') + ' — ' + new Date().toISOString().slice(0, 10) + '\n\n';
  for (const [kind, items] of Object.entries(groups)) {
    if (!items.length) continue;
    out += '### ' + LABELS[kind] + '\n\n';
    for (const e of items) {
      const scope = (e.subject.match(/^\w+\(([^)]+)\)/) || [])[1];
      const clean = e.subject.replace(/^\w+(\([^)]+\))?!?:\s*/, '');
      out += '- ' + (scope ? '**' + scope + ':** ' : '') + clean + ' (' + e.short + ')\n';
    }
    out += '\n';
  }
  return { markdown: out, entries, groups };
}
module.exports = { generate };