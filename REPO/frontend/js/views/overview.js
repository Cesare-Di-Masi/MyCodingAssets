const OverviewView = {
  async render(root) {
    root.innerHTML = `<div class="empty-state">Loading overview…</div>`;
    let data, insights;
    try {
      [data, insights] = await Promise.all([
        Api.overview(),
        Api.insights(30).catch(() => null)
      ]);
    } catch (e) {
      root.innerHTML = `<div class="empty-state text-danger">Failed to load: ${escapeHtml(e.message)}</div>`;
      return;
    }

    const extCounts = data.extCounts || {};
    const topExts = Object.entries(extCounts).sort((a, b) => b[1] - a[1]).slice(0, 12);
    const totalLang = topExts.reduce((s, [, c]) => s + c, 0) || 1;

    // Sparkline for last 30 days
    const daily = insights ? Object.values(insights.byDay) : [];
    const spark = daily.length ? sparkline(daily, { width: 160, height: 24 }) : '';

    // Language legend
    const legend = topExts.slice(0, 8).map(([ext, count]) => {
      const pct = count / totalLang * 100;
      const color = langColorFor(ext);
      return { ext, count, pct, color };
    });
    const legendBar = legend.map((l) => `<span style="width:${l.pct.toFixed(2)}%;background:${l.color}"></span>`).join('');

    root.innerHTML = `
      <h1>${icon('overview', 20)} Overview</h1>
      <div class="text-dim mono mb-16" style="font-size:11px">${escapeHtml(data.repoRoot)}</div>

      <div class="panel">
        <div class="stat-grid">
          <div class="stat-box">
            <div class="label">Files</div>
            <div class="value">${data.totalFiles.toLocaleString()}</div>
            <div class="sub">${data.totalDirs.toLocaleString()} directories</div>
          </div>
          <div class="stat-box">
            <div class="label">Size on disk</div>
            <div class="value">${fmtBytes(data.totalSize)}</div>
          </div>
          <div class="stat-box">
            <div class="label">Commits</div>
            <div class="value">${data.commitCount.toLocaleString()}</div>
            ${spark}
          </div>
          <div class="stat-box">
            <div class="label">Current branch</div>
            <div class="value" style="font-size:15px">${escapeHtml(data.currentBranch || '--')}</div>
            <div class="sub">${data.branchCount} branches</div>
          </div>
        </div>
      </div>

      <div class="panel">
        <h2>Languages</h2>
        <div class="lang-legend">${legendBar}</div>
        <div class="lang-list">
          ${legend.map((l) => `
            <div class="lang-item">
              <span class="dot" style="background:${l.color}"></span>
              <span>.${escapeHtml(l.ext)}</span>
              <span class="count">${l.pct.toFixed(1)}%</span>
            </div>`).join('')}
        </div>
      </div>

      <div class="panel">
        <h2>Largest files</h2>
        <table>
          <thead><tr><th>File</th><th style="width:100px;text-align:right">Size</th></tr></thead>
          <tbody>
            ${data.largestFiles.map((f) => `
              <tr data-path="${escapeHtml(f.path)}">
                <td style="display:flex;align-items:center;gap:8px">
                  ${fileIcon(f.path)}<span>${escapeHtml(f.path)}</span>
                </td>
                <td style="text-align:right" class="text-dim mono">${fmtBytes(f.size)}</td>
              </tr>`).join('')}
          </tbody>
        </table>
      </div>

      ${data.contributors && data.contributors.length ? `
      <div class="panel">
        <h2>Contributors</h2>
        <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(200px,1fr));gap:12px">
          ${data.contributors.slice(0, 8).map((c) => `
            <div class="contributor-card">
              ${avatarFor(c.name, c.email, 'lg')}
              <div class="meta">
                <div class="name">${escapeHtml(c.name)}</div>
                <div class="commits">${c.commits} ${c.commits === 1 ? 'commit' : 'commits'}</div>
              </div>
            </div>`).join('')}
        </div>
      </div>` : ''}

      ${data.status ? `
      <div class="panel">
        <h2>Working tree</h2>
        ${data.status.isClean
          ? `<div class="text-success">Clean — no pending changes</div>`
          : `<div class="row wrap" style="gap:8px">
              <span class="pill A">staged ${data.status.staged.length}</span>
              <span class="pill M">modified ${data.status.modified.length}</span>
              <span class="pill U">untracked ${data.status.not_added.length}</span>
              <span class="pill D">deleted ${data.status.deleted.length}</span>
             </div>`}
      </div>` : ''}
    `;

    root.querySelectorAll('tr[data-path]').forEach((row) =>
      row.addEventListener('click', () => inspectFile(row.getAttribute('data-path'))));
  }
};