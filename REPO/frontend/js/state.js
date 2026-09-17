const State = { currentView: 'overview', inspectorPath: null, activeRepo: null };

/* Icon helper: icon('files', 14) => '<svg style="width:14px;height:14px"><use href="#i-files"/></svg>' */
function icon(name, size = 14) {
  return `<svg style="width:${size}px;height:${size}px"><use href="#i-${name}"/></svg>`;
}

function setStatusbar(text) {
  const el = document.getElementById('statusbar-left');
  if (el) el.textContent = text;
}

function fmtBytes(n) {
  if (n === null || n === undefined) return '--';
  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  let i = 0, v = n;
  while (v >= 1024 && i < units.length - 1) { v /= 1024; i++; }
  return `${v.toFixed(v < 10 && i > 0 ? 1 : 0)} ${units[i]}`;
}

function fmtDate(d) {
  try {
    const dt = new Date(d);
    const diff = Date.now() - dt.getTime();
    if (diff < 60000) return 'just now';
    if (diff < 3600000) return `${Math.floor(diff / 60000)}m ago`;
    if (diff < 86400000) return `${Math.floor(diff / 3600000)}h ago`;
    if (diff < 604800000) return `${Math.floor(diff / 86400000)}d ago`;
    return dt.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: dt.getFullYear() !== new Date().getFullYear() ? 'numeric' : undefined });
  } catch (e) { return d; }
}

function escapeHtml(s) {
  return String(s)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function toast(message, kind = 'success', ttl = 2800) {
  const container = document.getElementById('toast-container');
  if (!container) return;
  const el = document.createElement('div');
  el.className = `toast ${kind}`;
  el.innerHTML = `<span>${escapeHtml(message)}</span>`;
  container.appendChild(el);
  setTimeout(() => {
    el.style.transition = 'opacity 200ms, transform 200ms';
    el.style.opacity = '0';
    el.style.transform = 'translateY(8px) scale(0.96)';
    setTimeout(() => el.remove(), 220);
  }, ttl);
}

async function inspectFile(relPath) {
  const inspector = document.getElementById('inspector-content');
  inspector.innerHTML = `<div class="empty-state">${icon('refresh', 24)}<div>Loading…</div></div>`;
  try {
    const data = await Api.readFile(relPath);
    const fname = relPath.split('/').pop();
    inspector.innerHTML = `
      <div class="mb-16">
        <div style="font-size:13px;font-weight:600;color:var(--text-primary);margin-bottom:4px;word-break:break-word">${escapeHtml(fname)}</div>
        <div class="text-dim mono" style="font-size:11px;word-break:break-all">${escapeHtml(relPath)}</div>
      </div>
      <div class="stat-box mb-12">
        <div class="label">Size</div>
        <div class="value" style="font-size:16px">${fmtBytes(data.size)}</div>
      </div>
      <div class="row mb-12" style="gap:6px;flex-wrap:wrap">
        <button class="sm" id="insp-blame">${icon('history')} Blame</button>
        <button class="sm" id="insp-timeline">${icon('reflog')} Timeline</button>
      </div>
      <div id="insp-extra"></div>
      <div class="file-content" style="max-height:340px;margin-top:12px">${escapeHtml(data.content)}${data.truncated ? '\n\n… truncated …' : ''}</div>
    `;

    document.getElementById('insp-blame').addEventListener('click', () => showBlame(relPath));
    document.getElementById('insp-timeline').addEventListener('click', () => showTimeline(relPath));

    async function showBlame(p) {
      const extra = document.getElementById('insp-extra');
      extra.innerHTML = '<div class="text-dim mb-8" style="font-size:11px">Loading blame…</div>';
      const b = await Api.blame(p);
      extra.innerHTML = `
        <div class="mb-8" style="font-weight:600;font-size:12px">Blame</div>
        <div class="file-content" style="max-height:240px;font-size:11px">${b.lines.map((l) =>
          `<div style="display:flex;gap:8px"><span style="color:var(--accent);font-family:var(--font-mono);flex-shrink:0">${l.short}</span><span style="color:var(--text-secondary)">${escapeHtml(l.text || '')}</span></div>`
        ).join('')}</div>`;
    }

    async function showTimeline(p) {
      const extra = document.getElementById('insp-extra');
      extra.innerHTML = '<div class="text-dim mb-8" style="font-size:11px">Loading timeline…</div>';
      const t = await Api.timeline(p);
      if (!t.commits.length) return extra.innerHTML = '<div class="text-dim" style="font-size:12px">No history.</div>';
      extra.innerHTML = `
        <div class="mb-8" style="font-weight:600;font-size:12px">Timeline</div>
        ${t.commits.slice(0, 20).map((c) => `
          <div class="commit-row" data-tl="${c.hash}" style="padding:6px 8px">
            <span class="commit-hash">${c.short}</span>
            <span style="font-size:11.5px;color:var(--text-primary);overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${escapeHtml(c.subject)}</span>
            <div class="commit-meta">${escapeHtml(c.author)} · ${fmtDate(c.date)}</div>
          </div>`).join('')}`;
    }
  } catch (e) {
    inspector.innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`;
  }
}

/* ====================================================================
   Visual helpers: avatars, file icons, language colors, diff bars
   ==================================================================== */

/* GitHub avatar fallback: visible initials with optional GitHub image layered
   on top once it loads. No external DiceBear service is used.
   --------------------------------------------------------------- */
function avatarFor(name, email, size = '', overrideUrl = null) {
  const cls = 'avatar' + (size ? ' ' + size : '');
  const safeName = escapeHtml(name || '?');
  const initial = escapeHtml(((name || '?').trim().charAt(0) || '?').toUpperCase());

  // 1) URL esplicito passato dal chiamante (es. da avatarMap)
  let imgUrl = overrideUrl || null;

  // 2) Fallback: noreply email → avatar GitHub reale
  if (!imgUrl) {
    const noreply = (email || '').match(/^(?:\d+\+)?([^@]+)@users\.noreply\.github\.com$/i);
    if (noreply) imgUrl = `https://github.com/${encodeURIComponent(noreply[1])}.png?size=128`;
  }

  // 3) Colore stabile per l'iniziale (deterministico dal nome)
  let h = 0;
  const seed = String(name || email || '?');
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) | 0;
  const bg = `hsl(${Math.abs(h) % 360}, 55%, 45%)`;

  // L'iniziale è sempre presente sotto; l'img la copre solo se carica davvero.
  const imgLayer = imgUrl
    ? `<img src="${escapeHtml(imgUrl)}" alt="" loading="lazy" onerror="this.remove()">`
    : '';

  return `<span class="${cls}" style="background:${bg}" title="${safeName}"><span class="avatar-initial" aria-hidden="true">${initial}</span>${imgLayer}</span>`;
}

/* Canonical GitHub language colors */
const LANG_COLORS = {
  JavaScript: '#f1e05a', TypeScript: '#3178c6', Python: '#3572A5',
  Ruby: '#701516', Go: '#00ADD8', Rust: '#dea584', Java: '#b07219',
  'C#': '#178600', 'C++': '#f34b7d', C: '#555555', PHP: '#4F5D95',
  Swift: '#F05138', Kotlin: '#A97BFF', Scala: '#c22d40', Lua: '#000080',
  HTML: '#e34c26', CSS: '#563d7c', SCSS: '#c6538c', Vue: '#41b883',
  Svelte: '#ff3e00', Shell: '#89e051', PowerShell: '#012456',
  Dockerfile: '#384d54', Makefile: '#427819', Markdown: '#083fa1',
  JSON: '#cbcb41', YAML: '#cb171e', TOML: '#9c4221', XML: '#0060ac',
  SQL: '#e38c00', R: '#198ce7', Dart: '#00B4AB', Elixir: '#6e4a7e',
  Haskell: '#5e5086', Clojure: '#db5855', Erlang: '#B83998'
};

/* Map a file extension → language name (canonical) */
const EXT_TO_LANG = {
  js: 'JavaScript', mjs: 'JavaScript', cjs: 'JavaScript',
  jsx: 'JavaScript', ts: 'TypeScript', tsx: 'TypeScript',
  py: 'Python', rb: 'Ruby', go: 'Go', rs: 'Rust', java: 'Java',
  cs: 'C#', cpp: 'C++', cc: 'C++', cxx: 'C++', c: 'C', h: 'C', hpp: 'C++',
  php: 'PHP', swift: 'Swift', kt: 'Kotlin', kts: 'Kotlin',
  scala: 'Scala', lua: 'Lua', html: 'HTML', htm: 'HTML',
  css: 'CSS', scss: 'SCSS', sass: 'SCSS', less: 'CSS',
  vue: 'Vue', svelte: 'Svelte',
  sh: 'Shell', bash: 'Shell', zsh: 'Shell', fish: 'Shell',
  ps1: 'PowerShell', bat: 'PowerShell', cmd: 'PowerShell',
  dockerfile: 'Dockerfile', makefile: 'Makefile',
  md: 'Markdown', markdown: 'Markdown', txt: 'Markdown',
  json: 'JSON', yaml: 'YAML', yml: 'YAML', toml: 'TOML', xml: 'XML',
  sql: 'SQL', r: 'R', dart: 'Dart', ex: 'Elixir', exs: 'Elixir',
  hs: 'Haskell', clj: 'Clojure', erl: 'Erlang'
};

function langColorFor(extOrLang) {
  const key = String(extOrLang || '').replace(/^\./, '').toLowerCase();
  const lang = EXT_TO_LANG[key] || extOrLang;
  return LANG_COLORS[lang] || '#5f7570';
}

/* File-type icon, tinted by language */
function fileIcon(name, size = 14) {
  const ext = String(name || '').split('.').pop().toLowerCase();
  const color = langColorFor(ext);
  return `<svg class="file-icon" style="width:${size}px;height:${size}px;color:${color}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>`;
}

/* Inline diff stat bar (additions vs deletions, proportional) */
function diffStatBar(add, del, large = false) {
  const a = Math.max(0, Number(add) || 0);
  const d = Math.max(0, Number(del) || 0);
  const total = a + d;
  if (!total) return `<span class="diff-stat${large ? ' diff-stat-lg' : ''}"></span>`;
  const pct = (a / total) * 100;
  return `<span class="diff-stat${large ? ' diff-stat-lg' : ''}">
    <span class="add" style="width:${pct.toFixed(2)}%"></span>
    <span class="del" style="width:${(100 - pct).toFixed(2)}%"></span>
  </span>`;
}

/* Inline "+N -N" text pair */
function diffStatText(add, del) {
  const a = Number(add) || 0;
  const d = Number(del) || 0;
  return `<span class="stat-mini"><span class="add">+${a}</span><span class="del">−${d}</span></span>`;
}

/* Mini sparkline SVG from array of numbers */
function sparkline(values, opts = {}) {
  const w = opts.width || 120;
  const h = opts.height || 22;
  const pad = 2;
  if (!values || !values.length) return '';
  const max = Math.max(1, ...values);
  const step = (w - pad * 2) / Math.max(1, values.length - 1);
  const points = values.map((v, i) => {
    const x = pad + i * step;
    const y = h - pad - (v / max) * (h - pad * 2);
    return [x, y];
  });
  const linePath = points.map(([x, y], i) => (i === 0 ? `M${x},${y}` : `L${x},${y}`)).join(' ');
  const fillPath = linePath + ` L${points[points.length - 1][0]},${h} L${points[0][0]},${h} Z`;
  const uid = 'sg' + Math.random().toString(36).slice(2, 8);
  return `<svg class="sparkline" viewBox="0 0 ${w} ${h}" preserveAspectRatio="none">
    <defs><linearGradient id="${uid}" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%" stop-color="var(--accent)" stop-opacity="0.35"/>
      <stop offset="100%" stop-color="var(--accent)" stop-opacity="0"/>
    </linearGradient></defs>
    <path d="${fillPath}" fill="url(#${uid})"/>
    <path d="${linePath}"/>
  </svg>`;
}