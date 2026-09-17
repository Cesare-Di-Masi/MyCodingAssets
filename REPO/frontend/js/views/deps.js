const DepsView = {
  async render(root) {
    root.innerHTML = `
      <h1>DEPENDENCIES</h1>
      <div class="panel" id="deps-panel"><div class="empty-state">Detecting manifests…</div></div>
    `;
    const panel = document.getElementById('deps-panel');
    try {
      const data = await Api.deps();
      if (!data.manifests.length) {
        panel.innerHTML = '<div class="empty-state">No manifests detected.</div>';
        return;
      }
      panel.innerHTML = data.manifests.map((m) => `
        <div class="mb-16">
          <div class="row mb-8" style="gap:10px">
            <span class="pill A">${escapeHtml(m.kind)}</span>
            <span class="text-accent">${escapeHtml(m.file)}</span>
            <span class="text-dim" style="font-size:11px">${m.deps.length} entries</span>
          </div>
          <table>
            <tbody>
              ${m.deps.slice(0, 200).map((d) => `
                <tr>
                  <td>${escapeHtml(d.name)}</td>
                  <td class="text-dim" style="font-size:11px">${escapeHtml(String(d.version || ''))}</td>
                  <td style="text-align:right">${d.dev ? '<span class="pill">dev</span>' : ''}</td>
                </tr>`).join('')}
            </tbody>
          </table>
          ${m.deps.length > 200 ? `<div class="text-dim mb-8">…and ${m.deps.length - 200} more.</div>` : ''}
        </div>
      `).join('');
    } catch (e) {
      panel.innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`;
    }
  }
};