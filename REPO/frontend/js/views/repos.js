const ReposView = {
  async render(root) {
    root.innerHTML = `
      <h1>MULTI-REPO</h1>
      <div class="panel">
        <div class="row" style="gap:8px">
          <input type="text" id="repo-path" placeholder="C:\\path\\to\\another\\repo" />
          <button id="repo-add" class="primary">ADD</button>
        </div>
      </div>
      <div class="panel" id="repos-list"><div class="empty-state">Loading…</div></div>`;
    const panel = document.getElementById('repos-list');
    async function load() {
      const data = await Api.repos();
      if (!data.repos.length) { panel.innerHTML = '<div class="empty-state">No additional repos.</div>'; return; }
      const statuses = await Promise.all(data.repos.map((repo) => Api.indexStatus(repo.path).catch(() => ({}))));
      panel.innerHTML = data.repos.map((r, index) => `
        <div class="panel repo-card" data-repo="${escapeHtml(r.path)}" tabindex="0" role="button" style="margin-bottom:10px">
          <div class="row" style="justify-content:space-between;gap:12px"><strong>${escapeHtml(r.name)}</strong><span class="pill ${r.clean === false ? 'M' : r.clean === true ? 'A' : ''}">${r.clean === false ? 'DIRTY' : r.clean === true ? 'CLEAN' : '--'}</span></div>
          <div class="text-dim mono" style="font-size:11px;margin-top:5px;overflow:hidden;text-overflow:ellipsis">${escapeHtml(r.path)}</div>
          <div class="row" style="gap:14px;margin-top:8px;font-size:11px"><span>${escapeHtml(r.branch || 'detached')}</span><span>${r.commitCount === 1 ? '1 commit' : `${r.commitCount || 0} commits`}</span><span>${statuses[index].lastIndexedAt ? fmtDate(statuses[index].lastIndexedAt) : 'not indexed'}</span><button class="sm danger" data-del="${escapeHtml(r.path)}" style="margin-left:auto">REMOVE</button></div>        </div>`).join('');
      panel.querySelectorAll('[data-repo]').forEach((card) => card.addEventListener('click', async (event) => {
        if (event.target.closest('[data-del]')) return;
        try { await Api.activateRepo(card.getAttribute('data-repo')); State.activeRepo = card.getAttribute('data-repo') === data.current ? null : card.getAttribute('data-repo'); toast('Repository activated'); App.navigate('overview'); } catch (e) { toast(e.message, 'error'); }
      }));
      panel.querySelectorAll('[data-del]').forEach((b) => b.addEventListener('click', async () => {
        await Api.deleteRepo(b.getAttribute('data-del')); toast('Removed'); load();
      }));
    }
    document.getElementById('repo-add').addEventListener('click', async () => {
      const p = document.getElementById('repo-path').value.trim();
      if (!p) return;
      try { await Api.addRepo(p); toast('Repo added'); document.getElementById('repo-path').value = ''; load(); }
      catch (e) { toast(e.message, 'error'); }
    });
    load();
  }
};