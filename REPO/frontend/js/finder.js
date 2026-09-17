const Finder = (() => {
  const overlay = () => document.getElementById('finder-overlay');
  const input = () => document.getElementById('finder-input');
  const results = () => document.getElementById('finder-results');
  let allFiles = null;
  let filtered = [];
  let activeIdx = 0;
  let previousFocus = null;

  function fuzzyScore(query, text) {
    if (!query) return 1;
    const q = query.toLowerCase();
    const t = text.toLowerCase();
    // Fast path: substring
    const idx = t.indexOf(q);
    if (idx !== -1) {
      return 1000 - idx + (t.length === q.length ? 500 : 0);
    }
    // Fuzzy: chars must appear in order
    let qi = 0, score = 0, lastPos = -1;
    for (let i = 0; i < t.length && qi < q.length; i++) {
      if (t[i] === q[qi]) {
        score += 20 - (i - lastPos);
        lastPos = i;
        qi++;
      }
    }
    return qi === q.length ? score : -1;
  }

  function highlightMatch(text, query) {
    if (!query) return escapeHtml(text);
    const idx = text.toLowerCase().indexOf(query.toLowerCase());
    if (idx === -1) return escapeHtml(text);
    return escapeHtml(text.slice(0, idx)) +
      `<span class="fmatch">${escapeHtml(text.slice(idx, idx + query.length))}</span>` +
      escapeHtml(text.slice(idx + query.length));
  }

  async function loadAllFiles() {
    if (allFiles) return allFiles;
    const list = [];
    async function walk(relPath) {
      const data = await Api.listFiles(relPath);
      for (const e of data.entries) {
        if (e.isDir) await walk(e.path);
        else list.push(e.path);
      }
    }
    await walk('.');
    allFiles = list;
    return list;
  }

  async function refresh() {
    const q = input().value.trim();
    const files = await loadAllFiles();
    filtered = files
      .map((f) => ({ path: f, score: fuzzyScore(q, f) }))
      .filter((r) => r.score > 0)
      .sort((a, b) => b.score - a.score)
      .slice(0, 200);
    activeIdx = 0;
    render();
  }

  function render() {
    const q = input().value.trim();
    if (!filtered.length) {
      results().innerHTML = `<div class="empty-state">No files match.</div>`;
      return;
    }
    results().innerHTML = filtered.map((r, i) => {
      const parts = r.path.split('/');
      const name = parts.pop();
      const dir = parts.join('/');
      return `<div class="finder-item ${i === activeIdx ? 'active' : ''}" data-idx="${i}">
        ${fileIcon(r.path, 12)}
        <span class="fpath">
          ${dir ? `<span class="fdir">${escapeHtml(dir)}/</span>` : ''}<span class="fname">${highlightMatch(name, q)}</span>
        </span>
        <span class="fmeta">${escapeHtml(r.path.split('.').pop().toLowerCase())}</span>
      </div>`;
    }).join('');

    results().querySelectorAll('.finder-item').forEach((el) => {
      const i = Number(el.getAttribute('data-idx'));
      el.addEventListener('click', () => open_(i));
      el.addEventListener('mouseenter', () => { activeIdx = i; updateActive(); });
    });
    updateActive();
    scrollIntoView();
  }

  function updateActive() {
    results().querySelectorAll('.finder-item').forEach((el, i) =>
      el.classList.toggle('active', i === activeIdx));
  }

  function scrollIntoView() {
    const el = results().querySelectorAll('.finder-item')[activeIdx];
    if (el) el.scrollIntoView({ block: 'nearest' });
  }

  function open_(idx) {
    const r = filtered[idx];
    if (!r) return;
    close();
    inspectFile(r.path);
  }

  function open() {
    previousFocus = document.activeElement;
    overlay().classList.remove('hidden');
    input().value = '';
    filtered = [];
    input().focus();
    refresh();
  }
  function close() { overlay().classList.add('hidden'); if (previousFocus?.focus) previousFocus.focus(); previousFocus = null; }
  function isOpen() { return !overlay().classList.contains('hidden'); }

  document.addEventListener('DOMContentLoaded', () => {
    overlay().addEventListener('click', (e) => { if (e.target === overlay()) close(); });
    input().addEventListener('input', refresh);
    input().addEventListener('keydown', (e) => {
      if (e.key === 'Escape') close();
      else if (e.key === 'ArrowDown') { activeIdx = Math.min(activeIdx + 1, filtered.length - 1); updateActive(); scrollIntoView(); e.preventDefault(); }
      else if (e.key === 'ArrowUp') { activeIdx = Math.max(activeIdx - 1, 0); updateActive(); scrollIntoView(); e.preventDefault(); }
      else if (e.key === 'Enter') { open_(activeIdx); e.preventDefault(); }
      else if (e.key === 'Tab') { e.preventDefault(); input().focus(); }
    });
  });

  return { open, close, isOpen };
})();