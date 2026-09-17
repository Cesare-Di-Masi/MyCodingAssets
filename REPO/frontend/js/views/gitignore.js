const GitignoreView = {
  async render(root) {
    root.innerHTML = `
      <h1>.GITIGNORE</h1>
      <div class="panel" id="gi-panel"><div class="empty-state">Analyzing…</div></div>
    `;
    const panel = document.getElementById('gi-panel');
    try {
      const data = await Api.gitignore();
      const entries = Object.entries(data.counts || {}).sort((a, b) => b[1] - a[1]);

      panel.innerHTML = `
        ${data.exists ? '' : '<div class="text-warn mb-8">⚠ No .gitignore found.</div>'}
        <h2>Ignored Files by Rule</h2>
        <table>
          <thead><tr><th>Rule</th><th style="text-align:right;width:100px">Files</th></tr></thead>
          <tbody>
            ${entries.length ? entries.map(([rule, count]) => `
              <tr>
                <td class="mono" style="font-size:11px">${escapeHtml(rule)}</td>
                <td style="text-align:right" class="text-accent">${count}</td>
              </tr>`).join('') : '<tr><td class="text-dim">No matching directories with content.</td></tr>'}
          </tbody>
        </table>
        <h2 style="margin-top:20px">Potential Problems</h2>
        <div class="row wrap" style="gap:6px">
          ${data.suspects.map((s) =>
            `<span class="pill M" data-add="${escapeHtml(s.pattern)}" style="cursor:pointer">
              ⚠ ${escapeHtml(s.pattern)} not ignored
            </span>`).join('') || '<span class="text-accent">✓ No obvious gaps.</span>'}
        </div>
        <div class="mb-8" style="margin-top:20px"><strong>Suggested additions</strong></div>
        <div class="row wrap" style="gap:6px">
          ${['.env', 'secrets/', '*.pem', '*.key', 'id_rsa'].map((r) =>
            `<button class="sm" data-suggest="${escapeHtml(r)}">+ ${escapeHtml(r)}</button>`).join('')}
        </div>
      `;

      panel.querySelectorAll('[data-suggest], [data-add]').forEach((el) =>
        el.addEventListener('click', async () => {
          const rule = el.getAttribute('data-suggest') || el.getAttribute('data-add');
          if (!rule) return;
          try {
            await Api.addGitignore([rule]);
            toast(`Added "${rule}" to .gitignore`);
            App.navigate('gitignore');
          } catch (e) { toast(e.message, 'error'); }
        }));
    } catch (e) {
      panel.innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`;
    }
  }
};