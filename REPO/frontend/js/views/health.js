const HealthView = {
  async render(root) {
    root.innerHTML = `
      <h1>REPOSITORY HEALTH</h1>
      <div class="panel" id="secrets-panel"><div class="empty-state">Scanning for secrets…</div></div>
      <div class="panel" id="todo-panel"><div class="empty-state">Scanning TODOs…</div></div>
      <div class="panel" id="dup-panel"><div class="empty-state">Checking duplicates…</div></div>
    `;

    try {
      const health = await Api.health();
      const secretsPanel = document.getElementById('secrets-panel');
      if (health.secrets.length) {
        secretsPanel.innerHTML = `
          <h2 style="color:var(--danger)">Security Scan — ${health.secrets.length} potential issue${health.secrets.length === 1 ? '' : 's'}</h2>
          ${health.secrets.map((s) =>
            `<div class="commit-row" data-path="${escapeHtml(s.path)}" style="display:flex;gap:10px;align-items:center">
              <span class="pill D">!</span>
              <span class="grow">${escapeHtml(s.path)}:${s.line}</span>
              <span class="text-dim">${escapeHtml(s.kind)}</span>
            </div>`).join('')}
        `;
      } else {
        secretsPanel.innerHTML = `<h2>Security Scan</h2><div class="text-accent">✓ No obvious secrets detected.</div>`;
      }

      const todoPanel = document.getElementById('todo-panel');
      const byTag = {};
      health.todos.forEach((t) => (byTag[t.tag] = (byTag[t.tag] || 0) + 1));
      todoPanel.innerHTML = `
        <h2>TODO / FIXME / HACK / BUG</h2>
        <div class="row wrap mb-12" style="gap:6px">
          ${Object.entries(byTag).map(([tag, count]) => `<span class="pill M">${tag}: ${count}</span>`).join(' ')}
        </div>
        ${health.todos.slice(0, 200).map((t) => `
          <div class="commit-row" data-path="${escapeHtml(t.path)}" style="display:flex;gap:10px;align-items:center">
            <span class="pill M">${t.tag}</span>
            <span class="text-dim" style="font-size:11px">${escapeHtml(t.path)}:${t.line}</span>
            <span class="grow" style="overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${escapeHtml(t.text)}</span>
          </div>`).join('')}
        ${health.todos.length > 200 ? `<div class="text-dim mb-8">…and ${health.todos.length - 200} more.</div>` : ''}
      `;

      document.querySelectorAll('[data-path]').forEach((el) =>
        el.addEventListener('click', () => inspectFile(el.getAttribute('data-path'))));
    } catch (e) {
      document.getElementById('secrets-panel').innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`;
    }

    try {
      const dup = await Api.duplicates();
      const dupPanel = document.getElementById('dup-panel');
      if (!dup.groups.length) {
        dupPanel.innerHTML = `<h2>Duplicate Files</h2><div class="text-dim">None found.</div>`;
      } else {
        const totalWasted = dup.groups.reduce((s, g) => s + g.size * (g.files.length - 1), 0);
        dupPanel.innerHTML = `
          <h2>Duplicate Files — ${dup.groups.length} group${dup.groups.length === 1 ? '' : 's'} · ~${fmtBytes(totalWasted)} reclaimable</h2>
          ${dup.groups.map((g) => `
            <div class="mb-12">
              <div class="text-dim mb-8">${fmtBytes(g.size)} each · ${g.files.length} copies</div>
              ${g.files.map((f) => `<div style="font-size:11px">${escapeHtml(f)}</div>`).join('')}
            </div>`).join('')}
        `;
      }
    } catch (e) {
      document.getElementById('dup-panel').innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`;
    }
  }
};