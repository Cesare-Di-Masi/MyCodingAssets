// Computes lanes and renders a commit graph on a canvas.
function renderGitGraph(container, commits) {
  const ROW_H = 26;
  const LANE_W = 16;
  const PAD_LEFT = 12;
  const PAD_TOP = 14;

  const hashToIdx = {};
  commits.forEach((c, i) => (hashToIdx[c.hash] = i));

  // Lane assignment
  const lanes = []; // lanes[i] = { head: hash, color }
  const commitLane = new Array(commits.length);
  const palette = ['#38f0b0', '#57c6ff', '#b085ff', '#f5c34a', '#ff5f4a', '#7ce0ff', '#ff9e5e'];

  function pickLane() {
    for (let i = 0; i < lanes.length; i++) if (lanes[i] === null) return i;
    return lanes.length;
  }

  for (let i = 0; i < commits.length; i++) {
    const c = commits[i];
    // If any lane expects this commit, keep it; else create a new lane
    let lane = lanes.indexOf(c.hash);
    if (lane === -1) {
      lane = pickLane();
      lanes[lane] = c.hash;
    }
    commitLane[i] = lane;

    // Free lanes only for first parent; add additional parents as new lanes
    for (let p = 0; p < c.parents.length; p++) {
      const parent = c.parents[p];
      if (p === 0) {
        lanes[lane] = parent;
      } else {
        const newLane = pickLane();
        lanes[newLane] = parent;
      }
    }
    // If no parents, free this lane
    if (c.parents.length === 0) lanes[lane] = null;
  }

  const numLanes = Math.max(2, lanes.length);
  const canvas = document.createElement('canvas');
  const graphWidth = PAD_LEFT + numLanes * LANE_W + 8;
  const dpr = window.devicePixelRatio || 1;
  const totalH = commits.length * ROW_H + PAD_TOP * 2;

  canvas.width = graphWidth * dpr;
  canvas.height = totalH * dpr;
  canvas.style.width = graphWidth + 'px';
  canvas.style.height = totalH + 'px';
  const ctx = canvas.getContext('2d');
  ctx.scale(dpr, dpr);

  const laneColor = (l) => palette[l % palette.length];
  const yFor = (i) => PAD_TOP + i * ROW_H + ROW_H / 2;
  const xFor = (l) => PAD_LEFT + l * LANE_W + LANE_W / 2;

  // Draw edges first
  for (let i = 0; i < commits.length; i++) {
    const c = commits[i];
    const x1 = xFor(commitLane[i]);
    const y1 = yFor(i);
    for (const p of c.parents) {
      const pIdx = hashToIdx[p];
      if (pIdx === undefined) continue;
      const x2 = xFor(commitLane[pIdx]);
      const y2 = yFor(pIdx);
      ctx.strokeStyle = laneColor(commitLane[pIdx]);
      ctx.globalAlpha = 0.55;
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.moveTo(x1, y1);
      if (x1 === x2) {
        ctx.lineTo(x2, y2);
      } else {
        ctx.bezierCurveTo(x1, (y1 + y2) / 2, x2, (y1 + y2) / 2, x2, y2);
      }
      ctx.stroke();
      ctx.globalAlpha = 1;
    }
  }

  // Draw commit nodes
  for (let i = 0; i < commits.length; i++) {
    const x = xFor(commitLane[i]);
    const y = yFor(i);
    const isMerge = commits[i].parents.length > 1;
    ctx.fillStyle = laneColor(commitLane[i]);
    ctx.beginPath();
    ctx.arc(x, y, isMerge ? 4 : 3.2, 0, Math.PI * 2);
    ctx.fill();
    if (isMerge) {
      ctx.strokeStyle = '#04080a';
      ctx.lineWidth = 1.2;
      ctx.stroke();
    }
    ctx.shadowColor = laneColor(commitLane[i]);
    ctx.shadowBlur = 6;
    ctx.beginPath();
    ctx.arc(x, y, 1.5, 0, Math.PI * 2);
    ctx.fill();
    ctx.shadowBlur = 0;
  }

  // Commit labels column
  const labels = document.createElement('div');
  labels.style.cssText = 'position:absolute;left:' + (graphWidth + 4) + 'px;top:0;right:0';

  const wrapper = document.createElement('div');
  wrapper.style.cssText = 'position:relative;min-height:' + totalH + 'px';
  wrapper.appendChild(canvas);
  wrapper.appendChild(labels);

  commits.forEach((c, i) => {
  const row = document.createElement('div');
  row.style.cssText = `position:absolute;top:${PAD_TOP + i * ROW_H}px;height:${ROW_H}px;display:flex;align-items:center;gap:8px;padding:0 8px 0 4px;cursor:pointer;font-size:12px;border-radius:4px`;
  row.innerHTML = `
    ${avatarFor(c.author, c.email, 'sm')}
    <span class="commit-hash" style="min-width:56px">${c.short}</span>
    <span class="commit-msg" style="flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${escapeHtml(c.subject)}</span>
    ${c.refs ? `<span class="ref-badge">${escapeHtml(c.refs.split(',')[0].trim())}</span>` : ''}
    <span class="text-dim" style="font-size:11px">${fmtDate(c.date)}</span>
  `;
  row.addEventListener('mouseenter', () => row.style.background = 'rgba(255,255,255,0.04)');
  row.addEventListener('mouseleave', () => row.style.background = 'transparent');
  row.addEventListener('click', () => showCommitInInspector(c.hash));
  labels.appendChild(row);
});

  container.innerHTML = '';
  container.appendChild(wrapper);
}

async function showCommitInInspector(hash) {
  const inspector = document.getElementById('inspector-content');
  inspector.innerHTML = '<div class="empty-state">Loading commit…</div>';
  try {
    const commit = await Api.commit(hash);
    const diff = await Api.commitDiff(hash);
    inspector.innerHTML = `
      <div class="mb-8"><span class="commit-hash">${hash.slice(0, 7)}</span></div>
      <div class="mb-8">${escapeHtml(commit.message)}</div>
      <div class="text-dim mb-12" style="font-size:11px">${escapeHtml(commit.author)} · ${fmtDate(commit.date)}</div>
      <div class="mb-8"><strong>FILES CHANGED</strong></div>
      <div class="mb-12">${commit.files.map((f) =>
        `<div class="row" style="font-size:11px"><span class="grow" style="overflow:hidden;text-overflow:ellipsis">${escapeHtml(f.file || '')}</span><span class="text-accent">+${f.additions ?? 0}</span><span class="text-danger">-${f.deletions ?? 0}</span></div>`
      ).join('')}</div>
      <div class="mb-8"><strong>DIFF</strong></div>
      ${renderDiff(diff.diff)}
    `;
  } catch (e) {
    inspector.innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`;
  }
}