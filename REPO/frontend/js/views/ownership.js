const OwnershipView = {
  async render(root) {
    root.innerHTML = `<h1>${icon('ownership', 20)} Code Ownership</h1>
      <div class="panel" id="own-panel"><div class="empty-state">Analyzing…</div></div>`;
    const panel = document.getElementById('own-panel');
    try {
      const data = await Api.ownership();
      if (!data.rows.length) return panel.innerHTML = '<div class="empty-state">No data.</div>';
      panel.innerHTML = data.rows.map((r) => `
        <div class="mb-16">
          <div class="row mb-12" style="gap:10px">
            <span style="font-weight:600;color:var(--text-primary)">${escapeHtml(r.dir)}</span>
            <span class="text-dim" style="font-size:11px">${r.total} touches</span>
          </div>
          ${r.authors.map((a) => `
            <div class="row mb-8" style="gap:10px;padding-left:8px">
              ${avatarFor(a.name, a.email, 'sm')}
              <span style="min-width:140px;font-size:12px">${escapeHtml(a.name)}</span>
              <span class="grow"><div class="bar"><span style="width:${a.pct.toFixed(1)}%"></span></div></span>
              <span class="text-dim mono" style="width:52px;text-align:right;font-size:11px">${a.pct.toFixed(0)}%</span>
            </div>`).join('')}
        </div>`).join('');
    } catch (e) {
      panel.innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`;
    }
  }
};