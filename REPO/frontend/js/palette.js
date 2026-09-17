const Palette = (() => {
  const overlay = () => document.getElementById('palette-overlay');
  const input = () => document.getElementById('palette-input');
  const results = () => document.getElementById('palette-results');
  let activeIdx = 0;
  let filtered = [];
  let previousFocus = null;

  const commands = [
    { label: 'Overview', key: 'G O', icon: '◈', action: () => App.navigate('overview') },
    { label: 'Files', key: 'G F', icon: '▸', action: () => App.navigate('files') },
    { label: 'Search', key: 'G S', icon: '⌕', action: () => App.navigate('search') },
    { label: 'History', key: 'G H', icon: '⟳', action: () => App.navigate('history') },
    { label: 'Graph', key: 'G G', icon: '◆', action: () => App.navigate('graph') },
    { label: 'Changes', key: 'G C', icon: '±', action: () => App.navigate('changes') },
    { label: 'Branches', key: 'G B', icon: '⑂', action: () => App.navigate('branches') },
    { label: 'Stashes', key: 'G T', icon: '⊟', action: () => App.navigate('stash') },
    { label: 'Repository Health', key: 'G R', icon: '✚', action: () => App.navigate('health') },
    { label: '.gitignore', key: 'G I', icon: '⌗', action: () => App.navigate('gitignore') },
    { label: 'Dependencies', key: 'G D', icon: '⬡', action: () => App.navigate('deps') },
    { label: 'Terminal', key: 'G X', icon: '>_', action: () => App.navigate('terminal') },
    { label: 'Refresh Topbar', icon: '↻', action: () => App.refreshTopbar() },
    { label: 'Insights', key: 'G N', icon: '📊', action: () => App.navigate('insights') },
{ label: 'Reflog / Undo', key: 'G U', icon: '↺', action: () => App.navigate('reflog') },
{ label: 'Snapshots', icon: '◲', action: () => App.navigate('snapshots') },
{ label: 'Tags', icon: '⌂', action: () => App.navigate('tags') },
{ label: 'Remotes', icon: '☁', action: () => App.navigate('remotes') },
{ label: 'Bisect', icon: '⚲', action: () => App.navigate('bisect') },
{ label: 'Conflicts', icon: '⚔', action: () => App.navigate('conflicts') },
{ label: 'Ownership', icon: '👥', action: () => App.navigate('ownership') },
{ label: 'Changelog', icon: '📜', action: () => App.navigate('changelog') },
{ label: 'GitHub', icon: '🐙', action: () => App.navigate('github') },
{ label: 'AI Assistant', icon: '🤖', action: () => App.navigate('ai') },
{ label: 'Multi-Repo', icon: '⊞', action: () => App.navigate('repos') },
{ label: 'Create Snapshot', icon: '◲', action: async () => {
    const label = prompt('Snapshot label?') || 'quick';
    await Api.snapshotCreate(label); toast('Snapshot created');
  }},
{ label: 'Fetch origin', icon: '⤓', action: async () => { await Api.fetchRemote('origin'); toast('Fetched'); App.refreshTopbar(); }},
    { label: 'Stage All Changes', icon: '⊕', action: async () => {
        const s = await Api.status();
        const all = [...s.modified, ...s.not_added, ...s.deleted];
        if (!all.length) return toast('Nothing to stage', 'warn');
        await Api.stage(all);
        toast(`Staged ${all.length} files`);
        App.navigate('changes');
      }},
    { label: 'Create Stash', icon: '⊞', action: async () => {
        const msg = prompt('Stash message?') || 'WIP';
        await Api.stashPush(msg, true);
        toast('Stash created');
      }}
  ];

  function render() {
    const q = input().value.toLowerCase();
    filtered = commands.filter((c) => c.label.toLowerCase().includes(q));
    if (!q) {
      const recent = JSON.parse(localStorage.getItem('rcc-recent-files') || '[]').slice(0, 8).map((path) => ({ label: `Recent: ${path}`, icon: '›', action: () => Tabs.open('file', { path }) }));
      filtered = [...recent, ...filtered];
    }
    if (q && !filtered.length) {
      results().innerHTML = '<div class="empty-state">No matching commands.</div>';
      return;
    }
    activeIdx = 0;
    results().innerHTML = filtered.map((c, i) =>
      `<div class="palette-item ${i === 0 ? 'active' : ''}" data-idx="${i}">
        <span class="icon">${c.icon || '›'}</span>
        <span>${escapeHtml(c.label)}</span>
        ${c.key ? `<span class="key-hint">${escapeHtml(c.key)}</span>` : ''}
      </div>`
    ).join('');

    results().querySelectorAll('.palette-item').forEach((el) => {
      el.addEventListener('click', () => run(Number(el.getAttribute('data-idx'))));
      el.addEventListener('mouseenter', () => {
        activeIdx = Number(el.getAttribute('data-idx'));
        highlight();
      });
    });
  }

  function highlight() {
    results().querySelectorAll('.palette-item').forEach((el, i) =>
      el.classList.toggle('active', i === activeIdx));
  }

  async function run(idx) {
    const cmd = filtered[idx];
    if (!cmd) return;
    close();
    try { await cmd.action(); } catch (e) { toast(e.message, 'error'); }
  }

  function open() {
    previousFocus = document.activeElement;
    overlay().classList.remove('hidden');
    input().value = '';
    render();
    input().focus();
  }
  function close() { overlay().classList.add('hidden'); if (previousFocus?.focus) previousFocus.focus(); previousFocus = null; }

  document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('palette-btn').addEventListener('click', open);
    overlay().addEventListener('click', (e) => { if (e.target === overlay()) close(); });
    input().addEventListener('input', render);
    input().addEventListener('keydown', (e) => {
      if (e.key === 'Escape') close();
      else if (e.key === 'ArrowDown') { activeIdx = Math.min(activeIdx + 1, filtered.length - 1); highlight(); e.preventDefault(); }
      else if (e.key === 'ArrowUp') { activeIdx = Math.max(activeIdx - 1, 0); highlight(); e.preventDefault(); }
      else if (e.key === 'Enter') { run(activeIdx); e.preventDefault(); }
      else if (e.key === 'Tab') { e.preventDefault(); input().focus(); }
    });
  });

  return { open, close };
})();