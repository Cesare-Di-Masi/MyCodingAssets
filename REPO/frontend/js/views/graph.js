const GraphView = {
  async render(root) {
    root.innerHTML = `
      <h1>GIT GRAPH</h1>
      <div class="panel" id="graph-panel">
        <div class="empty-state">Loading graph…</div>
      </div>
    `;
    try {
      const data = await Api.graph();
      const panel = document.getElementById('graph-panel');
      if (!data.commits.length) {
        panel.innerHTML = '<div class="empty-state">No commits.</div>';
        return;
      }
      // Mostra repo e numero commit per diagnosi rapida
      const repoLabel = (typeof State !== 'undefined' && State.activeRepo)
        ? State.activeRepo.split(/[\\/]/).pop()
        : 'default';
      const info = document.createElement('div');
      info.className = 'text-dim';
      info.style.cssText = 'font-size:11px;margin-bottom:8px;font-family:var(--font-mono)';
      info.textContent = `${repoLabel} · ${data.commits.length} commit${data.commits.length === 1 ? '' : 's'}`;
      panel.innerHTML = '';
      panel.appendChild(info);
      const graphHost = document.createElement('div');
      panel.appendChild(graphHost);
      panel.style.padding = '14px 6px';
      renderGitGraph(graphHost, data.commits);
    } catch (e) {
      document.getElementById('graph-panel').innerHTML =
        `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`;
    }
  }
};