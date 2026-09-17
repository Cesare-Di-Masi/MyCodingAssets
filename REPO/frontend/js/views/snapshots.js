const SnapshotsView = {
  async render(root) {
    root.innerHTML = `
      <h1>SNAPSHOTS</h1>
      <div class="panel">
        <div class="row" style="gap:8px">
          <input type="text" id="snap-label" placeholder="e.g. before risky rebase" />
          <button id="snap-create" class="primary">SNAPSHOT NOW</button>
        </div>
        <div class="text-dim" style="margin-top:8px;font-size:11px">Snapshots capture your working tree + HEAD. Restore anytime.</div>
      </div>
      <div class="panel" id="snap-list"><div class="empty-state">Loading…</div></div>`;
    const panel = document.getElementById('snap-list');

    async function load() {
      const data = await Api.snapshots();
      if (!data.snapshots.length) { panel.innerHTML = '<div class="empty-state">No snapshots.</div>'; return; }
      panel.innerHTML = data.snapshots.map((s) => `
        <div class="commit-row" style="display:flex;gap:12px;align-items:center">
          <span class="pill A">${escapeHtml(s.id)}</span>
          <span class="grow">${escapeHtml(s.label)}</span>
          <span class="text-dim" style="font-size:11px">HEAD ${escapeHtml((s.head || '').slice(0, 7))}</span>
          <span class="text-dim" style="font-size:11px">${fmtDate(s.createdAt)}</span>
          <button class="sm" data-restore="${s.id}">RESTORE</button>
          <button class="sm danger" data-del="${s.id}">DELETE</button>
        </div>`).join('');
      panel.querySelectorAll('[data-restore]').forEach((b) => b.addEventListener('click', async () => {
        if (!confirm('Restore snapshot? Current working tree will be affected.')) return;
        try { await Api.snapshotRestore(b.getAttribute('data-restore')); toast('Restored'); App.refreshTopbar(); } catch (e) { toast(e.message, 'error'); }
      }));
      panel.querySelectorAll('[data-del]').forEach((b) => b.addEventListener('click', async () => {
        if (!confirm('Delete snapshot?')) return;
        try { await Api.snapshotDelete(b.getAttribute('data-del')); toast('Deleted'); load(); } catch (e) { toast(e.message, 'error'); }
      }));
    }
    document.getElementById('snap-create').addEventListener('click', async () => {
      const label = document.getElementById('snap-label').value.trim() || 'snapshot';
      try { await Api.snapshotCreate(label); toast('Snapshot created'); document.getElementById('snap-label').value = ''; load(); }
      catch (e) { toast(e.message, 'error'); }
    });
    load();
  }
};