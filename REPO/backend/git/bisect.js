let bisectState = { active: false, good: null, bad: null, log: [] };
async function start(git) {
  await git.raw(['bisect', 'reset']).catch(() => {});
  await git.raw(['bisect', 'start']);
  bisectState = { active: true, good: null, bad: null, log: [] };
}
async function mark(git, kind) {
  if (!bisectState.active) throw new Error('Bisect not running');
  if (kind !== 'good' && kind !== 'bad' && kind !== 'skip') throw new Error('Invalid mark');
  try { await git.raw(['bisect', kind]); } catch (e) {}
  bisectState[kind] = bisectState[kind] || new Date().toISOString();
  bisectState.log.push({ kind, at: new Date().toISOString() });
  let current = null;
  try { current = (await git.raw(['rev-parse', 'HEAD'])).trim(); } catch (e) {}
  let complete = false, culprit = null;
  try {
    const log = await git.raw(['bisect', 'log']);
    const m = log.match(/first bad commit: \[([0-9a-f]+)\]/);
    if (m) { complete = true; culprit = m[1]; bisectState.active = false; }
  } catch (e) {}
  return { current, complete, culprit };
}
async function reset(git) {
  try { await git.raw(['bisect', 'reset']); } catch (e) {}
  bisectState = { active: false, good: null, bad: null, log: [] };
}
function state() { return bisectState; }
module.exports = { start, mark, reset, state };