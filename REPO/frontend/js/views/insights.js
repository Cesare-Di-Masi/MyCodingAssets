const InsightsView = {
  async render(root) {
    root.innerHTML = `<h1>INSIGHTS</h1>
      <div class="panel" id="kind-panel"><div class="empty-state">Analyzing…</div></div>
      <div class="panel" id="heatmap-panel"></div>
      <div class="panel" id="author-panel"></div>`;
    let data;
    try { data = await Api.insights(365); }
    catch (e) { document.getElementById('kind-panel').innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`; return; }
    const KINDS = { feat:'#38f0b0', fix:'#ff5f4a', refactor:'#57c6ff', perf:'#f5c34a', docs:'#b085ff', test:'#7ce0ff', chore:'#8fa39a', style:'#c9d6d3', revert:'#ff9e5e', other:'#4a5a56' };

    document.getElementById('kind-panel').innerHTML = `
      <h2>Commit Classification · ${data.total} commits</h2>
      ${data.byKind.map((k) => `
        <div class="mb-8"><div class="row">
          <span style="width:80px;color:${KINDS[k.kind] || KINDS.other}">${escapeHtml(k.kind)}</span>
          <span class="grow"><div class="bar"><span style="width:${k.pct.toFixed(1)}%;background:${KINDS[k.kind] || KINDS.other}"></span></div></span>
          <span class="text-dim" style="width:80px;text-align:right;font-size:11px">${k.count} · ${k.pct.toFixed(1)}%</span>
        </div></div>`).join('')}`;

    const days = Object.entries(data.byDay);
    const max = Math.max(1, ...days.map(([, n]) => n));
    const weeks = [];
    for (let i = 0; i < days.length; i += 7) weeks.push(days.slice(i, i + 7));
    const colorFor = (n) => {
      if (n === 0) return '#10171a';
      const t = Math.min(1, n / max);
      return `rgb(${Math.round(31+(56-31)*t)},${Math.round(107+(240-107)*t)},${Math.round(82+(176-82)*t)})`;
    };
    const cell = 12, gap = 3;
    const svgW = weeks.length * (cell + gap) + 40;
    const svgH = 7 * (cell + gap) + 20;
    document.getElementById('heatmap-panel').innerHTML = `
      <h2>Activity Heatmap</h2>
      <svg width="${svgW}" height="${svgH}" style="display:block">
        ${weeks.map((w, wi) => w.map(([day, n], di) => {
          const x = 40 + wi * (cell + gap), y = 10 + di * (cell + gap);
          return `<rect x="${x}" y="${y}" width="${cell}" height="${cell}" rx="2" fill="${colorFor(n)}"><title>${day}: ${n}</title></rect>`;
        }).join('')).join('')}
        <text x="0" y="22" fill="#5f7570" font-size="9" font-family="monospace">Mon</text>
        <text x="0" y="70" fill="#5f7570" font-size="9" font-family="monospace">Wed</text>
        <text x="0" y="118" fill="#5f7570" font-size="9" font-family="monospace">Fri</text>
      </svg>`;

    document.getElementById('author-panel').innerHTML = `
      <h2>Top Contributors</h2>
      ${data.byAuthor.slice(0, 10).map((a) => `
        <div class="row mb-8" style="gap:10px">
          <span class="avatar">${escapeHtml(a.name[0] || '?')}</span>
          <span class="grow">${escapeHtml(a.name)}</span>
          <span class="text-accent" style="font-size:11px">${a.count}</span>
        </div>`).join('')}`;
  }
};