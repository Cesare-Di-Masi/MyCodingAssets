const ChangesView = {
  async render(root) {
    root.innerHTML = `
      <h1>CHANGES</h1>
      <div class="panel" id="changes-panel"><div class="empty-state">Loading status…</div></div>
      <div class="panel">
        <h2>Commit</h2>
        <textarea id="commit-msg" rows="3" placeholder="Commit message"></textarea>
        <div class="row mb-8" style="margin-top:10px;gap:8px;flex-wrap:wrap">
          <button id="commit-btn" class="primary">COMMIT</button>
          <button id="stage-all-btn">STAGE ALL</button>
          <button id="stash-btn">STASH</button>
        </div>
        <div id="commit-result" class="text-dim" style="margin-top:8px;font-size:12px"></div>
      </div>
    `;

    await refresh();

    document.getElementById('commit-btn').addEventListener('click', async () => {
      const msg = document.getElementById('commit-msg').value;
      const resultEl = document.getElementById('commit-result');
      try {
        const r = await Api.doCommit(msg);
        resultEl.innerHTML = `<span class="text-accent">✓ Committed</span> — ${escapeHtml(msg)}`;
        toast('Commit created');
        document.getElementById('commit-msg').value = '';
        await refresh();
        App.refreshTopbar();
      } catch (e) {
        resultEl.innerHTML = `<span class="text-danger">${escapeHtml(e.message)}</span>`;
        toast(e.message, 'error');
      }
    });

    document.getElementById('stage-all-btn').addEventListener('click', async () => {
      const s = await Api.status();
      const all = [...new Set([...s.modified, ...s.not_added, ...s.deleted])];
      if (!all.length) return toast('Nothing to stage', 'warn');
      await Api.stage(all);
      toast(`Staged ${all.length} files`);
      await refresh();
    });

    document.getElementById('stash-btn').addEventListener('click', async () => {
      const msg = prompt('Stash message?') || 'WIP';
      await Api.stashPush(msg, true);
      toast('Stash created');
      await refresh();
    });

    async function refresh() {
      const panel = document.getElementById('changes-panel');
      let status;
      try { status = await Api.status(); }
      catch (e) {
        panel.innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`;
        return;
      }

      panel.innerHTML = `
        ${section('STAGED', status.staged, 'A', true)}
        ${section('MODIFIED', status.modified, 'M', false)}
        ${section('UNTRACKED', status.not_added, 'U', false)}
        ${section('DELETED', status.deleted, 'D', false)}
        ${status.isClean ? '<div class="empty-state">Working tree clean.</div>' : ''}
      `;

      panel.querySelectorAll('[data-stage]').forEach((btn) =>
        btn.addEventListener('click', async (e) => {
          e.stopPropagation();
          await Api.stage([btn.getAttribute('data-stage')]);
          await refresh();
        }));
      panel.querySelectorAll('[data-unstage]').forEach((btn) =>
        btn.addEventListener('click', async (e) => {
          e.stopPropagation();
          await Api.unstage([btn.getAttribute('data-unstage')]);
          await refresh();
        }));
      panel.querySelectorAll('[data-viewdiff]').forEach((el) =>
  el.addEventListener('click', (e) => {
    if (e.target.closest('button')) return; // don't open if clicking Stage/Unstage
    const file = el.getAttribute('data-viewdiff');
    const staged = el.getAttribute('data-staged') === 'true';
    Tabs.open('diff', { path: file, staged, label: file.split('/').pop() });
  }));
    }

    function section(title, files, tag, staged) {
  if (!files || !files.length) return '';
  return `
    <div class="mb-8" style="font-weight:600;font-size:12px">${title} <span class="text-dim">(${files.length})</span></div>
    ${files.map((f) => `
      <div class="row mb-8" data-viewdiff="${escapeHtml(f)}" data-staged="${staged}" style="cursor:pointer;padding:6px 8px;border-radius:6px;transition:background 120ms">
        ${fileIcon(f, 13)}
        <span class="grow" style="overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${escapeHtml(f)}</span>
        <span class="pill ${tag}">${tag}</span>
        ${staged
          ? `<button class="sm" data-unstage="${escapeHtml(f)}">Unstage</button>`
          : `<button class="sm" data-stage="${escapeHtml(f)}">Stage</button>`}
      </div>`).join('')}
  `;
}
  }
};