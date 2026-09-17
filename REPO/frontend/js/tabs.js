const Tabs = (() => {
  const tabs = [];   // { id, kind, path, label, hash }
  let activeId = null;

  const tabsEl = () => document.getElementById('inspector-tabs');
  const contentEl = () => document.getElementById('inspector-content');

  function open(kind, opts = {}) {
    const id = kind + ':' + (opts.path || opts.hash || 'unknown');
    if (!tabs.find((t) => t.id === id)) {
      tabs.push({ id, kind, ...opts, label: opts.label || (opts.path ? opts.path.split('/').pop() : (opts.hash ? opts.hash.slice(0, 7) : kind)) });
      if (tabs.length > 12) tabs.shift(); // cap
    }
    activeId = id;
    if (kind === 'file' && opts.path) {
      const recent = JSON.parse(localStorage.getItem('rcc-recent-files') || '[]').filter((p) => p !== opts.path);
      recent.unshift(opts.path); localStorage.setItem('rcc-recent-files', JSON.stringify(recent.slice(0, 20)));
    }
    render();
    renderActive();
  }

  function close(id) {
    const idx = tabs.findIndex((t) => t.id === id);
    if (idx === -1) return;
    tabs.splice(idx, 1);
    const focusId = activeId === id ? (tabs[idx - 1]?.id || tabs[idx]?.id || null) : activeId;
    if (activeId === id) activeId = tabs[idx - 1]?.id || tabs[idx]?.id || null;
    render();
    renderActive();
    requestAnimationFrame(() => focusId && tabsEl().querySelector(`[data-id="${CSS.escape(focusId)}"]`)?.focus());
  }

  function render() {
    const el = tabsEl();
    if (!el) return;
    el.setAttribute('role', 'tablist');
    el.innerHTML = tabs.map((t) => `
      <div class="inspector-tab ${t.id === activeId ? 'active' : ''}" data-id="${escapeHtml(t.id)}" role="tab" tabindex="0" aria-selected="${t.id === activeId}" aria-controls="inspector-content">
        ${t.kind === 'file' ? fileIcon(t.path, 11) : `<svg style="width:11px;height:11px"><use href="#i-git-commit"/></svg>`}
        <span class="tab-label">${escapeHtml(t.label)}</span>
        <button class="tab-close" aria-label="Close ${escapeHtml(t.label)}" data-close="${escapeHtml(t.id)}"><svg><use href="#i-x"/></svg></button>
      </div>`).join('');

    el.querySelectorAll('.inspector-tab').forEach((node) => {
      const id = node.getAttribute('data-id');
      node.addEventListener('click', (e) => {
        if (e.target.closest('[data-close]')) { close(id); return; }
        activeId = id; render(); renderActive();
      });
      node.addEventListener('keydown', (e) => {
        const tabNodes = [...el.querySelectorAll('[role="tab"]')]; const index = tabNodes.indexOf(node);
        if (e.key === 'ArrowRight' || e.key === 'ArrowDown') { e.preventDefault(); tabNodes[(index + 1) % tabNodes.length].focus(); }
        if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') { e.preventDefault(); tabNodes[(index - 1 + tabNodes.length) % tabNodes.length].focus(); }
        if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); activeId = id; render(); renderActive(); }
      });
    });
  }

  function renderActive() {
    const container = contentEl();
    if (!container) return;
    const tab = tabs.find((t) => t.id === activeId);
    if (!tab) {
      container.innerHTML = `<div class="empty-state">
        <svg><use href="#i-file"/></svg>
        <div>Select a file, commit,<br>or TODO to inspect.</div>
      </div>`;
      return;
    }
    if (tab.kind === 'file') renderFile(tab, container);
    else if (tab.kind === 'commit') renderCommitTab(tab, container);
    else if (tab.kind === 'diff') renderDiffTab(tab, container);
    else if (tab.kind === 'html') container.innerHTML = tab.html || '';
  }

  async function renderFile(tab, container) {
    container.innerHTML = `<div class="empty-state">Loading…</div>`;
    try {
      const [data, blameInfo] = await Promise.all([
        Api.readFile(tab.path),
        Api.blame(tab.path).catch(() => ({ lines: [] }))
      ]);
      const lang = Highlight.langFor(tab.path);
      const ext = tab.path.split('.').pop().toLowerCase();
      const isImage = ['png','jpg','jpeg','gif','svg','webp','bmp','ico'].includes(ext);
      const isMarkdown = ['md','markdown'].includes(ext);

      const crumbs = tab.path.split('/');
      const breadcrumbHtml = `<div class="breadcrumbs">
        ${crumbs.map((c, i) => {
          const isLast = i === crumbs.length - 1;
          const rel = crumbs.slice(0, i + 1).join('/');
          return `${i > 0 ? '<span class="sep">›</span>' : ''}
            <span class="crumb ${isLast ? 'current' : ''}" ${!isLast ? `data-crumb="${escapeHtml(rel)}"` : ''}>${escapeHtml(c)}</span>`;
        }).join('')}
      </div>`;

      if (isImage) {
        container.innerHTML = `
          ${breadcrumbHtml}
          <div class="code-toolbar">
            <span class="lang">Image</span>
            <span class="path">${escapeHtml(tab.path)}</span>
            <span class="text-dim" style="font-size:11px">${fmtBytes(data.size)}</span>
          </div>
          <div class="file-content" style="text-align:center;padding:20px">
            <img src="/api/file?path=${encodeURIComponent(tab.path)}&raw=1" style="max-width:100%;border-radius:6px" alt="">
          </div>`;
        return;
      }

      const html = renderCodeLines(data.content, lang, tab.path);
      container.innerHTML = `
        ${breadcrumbHtml}
        <div class="code-toolbar">
          <span class="lang">${lang || ext || 'text'}</span>
          <span class="path">${escapeHtml(tab.path)}</span>
          <span class="text-dim" style="font-size:11px">${fmtBytes(data.size)}</span>
          <span class="btn-row">
            <button class="icon-btn sm" id="code-search" title="Find (Ctrl+F)"><svg><use href="#i-search"/></svg></button>
            <button class="icon-btn sm" id="code-copy" title="Copy path" aria-label="Copy path"><svg><use href="#i-copy"/></svg></button>
            <button class="icon-btn sm" id="code-bookmark" title="Bookmark file" aria-label="Bookmark file"><svg><use href="#i-tags"/></svg></button>
            <button class="icon-btn sm" id="code-blame" title="Toggle blame"><svg><use href="#i-history"/></svg></button>
            <button class="icon-btn sm" id="code-timeline" title="Timeline"><svg><use href="#i-reflog"/></svg></button>
            <button class="icon-btn sm" id="code-edit" title="Edit file"><svg><use href="#i-code"/></svg></button>
            <button class="icon-btn sm" id="code-raw" title="Open raw"><svg><use href="#i-external"/></svg></button>
          </span>
        </div>
        ${isMarkdown
          ? `<div id="md-preview" class="file-content" style="max-height:340px;display:none"></div>` : ''}
        ${html}
        <div id="code-search-bar" class="hidden" style="margin-top:8px;display:flex;gap:8px">
          <input type="text" id="code-search-input" placeholder="Find in file…" />
          <span id="code-search-count" class="text-dim" style="align-self:center;font-size:11px"></span>
        </div>
        <div id="blame-view" style="margin-top:12px;display:none">
          <div style="font-weight:600;font-size:12px;margin-bottom:8px">Blame</div>
          <div class="file-content" style="max-height:260px;font-size:11px">${blameInfo.lines.map((l) =>
            `<div style="display:flex;gap:8px"><span style="color:var(--accent);font-family:var(--font-mono);flex-shrink:0;width:60px">${l.short}</span><span style="color:var(--text-secondary);width:100px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${escapeHtml(l.author || '')}</span><span style="color:var(--text-primary)">${escapeHtml(l.text || '')}</span></div>`
          ).join('')}</div>
        </div>`;

      // Breadcrumb click → open containing directory
      container.querySelectorAll('[data-crumb]').forEach((c) =>
        c.addEventListener('click', () => App.navigate('files')));

      // Actions
      container.querySelector('#code-copy')?.addEventListener('click', () => {
        navigator.clipboard.writeText(tab.path).then(() => toast('Path copied'));
      });
      container.querySelector('#code-bookmark')?.addEventListener('click', () => {
        const saved = JSON.parse(localStorage.getItem('rcc-bookmarks') || '[]');
        const next = saved.includes(tab.path) ? saved.filter((p) => p !== tab.path) : [tab.path, ...saved];
        localStorage.setItem('rcc-bookmarks', JSON.stringify(next)); toast(next.includes(tab.path) ? 'Bookmarked file' : 'Bookmark removed');
      });
      container.querySelector('#code-edit')?.addEventListener('click', () => {
        const codeBlock = container.querySelector('.code-view');
        if (!codeBlock) return;
        const textarea = document.createElement('textarea');
        textarea.className = 'code-editor';
        textarea.value = data.content;
        textarea.setAttribute('spellcheck', 'false');
        codeBlock.replaceWith(textarea);
        const btnRow = container.querySelector('.btn-row');
        if (btnRow) {
          const saveBtn = document.createElement('button');
          saveBtn.type = 'button';
          saveBtn.className = 'sm primary';
          saveBtn.textContent = 'Save';
          saveBtn.addEventListener('click', async () => {
            try {
              await Api.writeFile(tab.path, textarea.value);
              toast('Saved file');
              Tabs.rerender();
            } catch (e) { toast(e.message, 'error'); }
          });
          const cancelBtn = document.createElement('button');
          cancelBtn.type = 'button';
          cancelBtn.className = 'sm';
          cancelBtn.textContent = 'Cancel';
          cancelBtn.addEventListener('click', () => Tabs.rerender());
          btnRow.appendChild(saveBtn);
          btnRow.appendChild(cancelBtn);
        }
      });
      container.querySelector('#code-raw')?.addEventListener('click', () => {
        const pattern = localStorage.getItem('rcc-editor-uri') || 'vscode://file/{path}';
        window.open(pattern.replace('{path}', encodeURI(tab.path)), '_blank');
      });
      container.querySelector('#code-blame')?.addEventListener('click', () => {
        const b = container.querySelector('#blame-view');
        b.style.display = b.style.display === 'none' ? 'block' : 'none';
      });
      container.querySelector('#code-timeline')?.addEventListener('click', () => openTimelineInTab(tab.path));

      // In-file search
      const searchBar = container.querySelector('#code-search-bar');
      const searchInput = container.querySelector('#code-search-input');
      const searchCount = container.querySelector('#code-search-count');
      container.querySelector('#code-search')?.addEventListener('click', () => {
        searchBar.classList.toggle('hidden');
        if (!searchBar.classList.contains('hidden')) searchInput.focus();
      });
      if (searchInput) {
        searchInput.addEventListener('input', () => {
          const q = searchInput.value;
          const codeEl = container.querySelector('.code-view');
          if (!codeEl) return;
          // Reset
          codeEl.querySelectorAll('.lc').forEach((c) => {
            c.innerHTML = c.dataset.raw ? c.dataset.raw : c.innerHTML;
          });
          if (!q) { searchCount.textContent = ''; return; }
          const re = new RegExp(q.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'gi');
          let total = 0;
          codeEl.querySelectorAll('.lc').forEach((c) => {
            if (!c.dataset.raw) c.dataset.raw = c.innerHTML;
            const raw = c.dataset.raw;
            if (re.test(raw)) {
              total += (raw.match(re) || []).length;
              c.innerHTML = raw.replace(re, (m) => `<mark>${m}</mark>`);
            }
            re.lastIndex = 0;
          });
          searchCount.textContent = total ? `${total} match${total === 1 ? '' : 'es'}` : 'no matches';
        });
      }

      // Markdown preview toggle
      if (isMarkdown) {
        const preview = container.querySelector('#md-preview');
        const toggle = document.createElement('button');
        toggle.className = 'icon-btn sm';
        toggle.title = 'Preview markdown';
        toggle.innerHTML = '<svg><use href="#i-code"/></svg>';
        toggle.style.marginLeft = '4px';
        container.querySelector('.code-toolbar .btn-row').prepend(toggle);
        preview.innerHTML = simpleMarkdown(data.content);
        toggle.addEventListener('click', () => {
          const codeEl = container.querySelector('.code-view');
          const showing = preview.style.display === 'block';
          preview.style.display = showing ? 'none' : 'block';
          if (codeEl) codeEl.style.display = showing ? '' : 'none';
        });
      }
    } catch (e) {
      container.innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`;
    }
  }

  function renderCodeLines(text, lang, path) {
    const lines = text.split('\n');
    const highlighted = Highlight.highlight(text, lang).split('\n');
    // If Prism returned fewer lines (unlikely), fall back to escaping
    const linesHtml = (highlighted.length === lines.length ? highlighted : lines.map(escapeHtml));
    return `<div class="code-view">
      <table><tbody>
        ${linesHtml.map((l, i) => `<tr><td class="ln">${i + 1}</td><td class="lc">${l || ' '}</td></tr>`).join('')}
      </tbody></table>
    </div>`;
  }

  /* Minimal markdown renderer — headings, bold, italics, code, links, lists */
  function simpleMarkdown(md) {
    let html = escapeHtml(md);
    html = html.replace(/```([\s\S]*?)```/g, (_, code) => `<pre style="background:var(--bg-raised);padding:12px;border-radius:6px;overflow:auto;font-size:11.5px">${code.trim()}</pre>`);
    html = html.replace(/^### (.*$)/gm, '<h3 style="margin:16px 0 8px;font-size:14px">$1</h3>');
    html = html.replace(/^## (.*$)/gm, '<h2 style="margin:20px 0 10px;font-size:15px">$1</h2>');
    html = html.replace(/^# (.*$)/gm, '<h1 style="margin:20px 0 10px;font-size:18px">$1</h1>');
    html = html.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
    html = html.replace(/\*([^*]+)\*/g, '<em>$1</em>');
    html = html.replace(/`([^`]+)`/g, '<code style="background:var(--bg-raised);padding:1px 5px;border-radius:3px;font-size:11.5px">$1</code>');
    html = html.replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2" target="_blank">$1</a>');
    html = html.replace(/^\- (.*$)/gm, '<div style="padding-left:14px">· $1</div>');
    html = html.replace(/\n\n/g, '<br><br>');
    return html;
  }

  async function openTimelineInTab(path) {
    const t = await Api.timeline(path);
    open('html', {
      label: `Timeline: ${path.split('/').pop()}`,
      html: `<div style="font-weight:600;font-size:13px;margin-bottom:12px">Timeline · ${escapeHtml(path)}</div>
        ${t.commits.map((c) => `<div class="commit-row" data-hash="${c.hash}" style="padding:8px 10px">
          <span class="commit-hash">${c.short}</span>
          <span style="font-size:12px;color:var(--text-primary)">${escapeHtml(c.subject)}</span>
          <div class="commit-meta">${escapeHtml(c.author)} · ${fmtDate(c.date)}</div>
        </div>`).join('')}`
    });
  }

  function renderCommitTab(tab, container) {
    container.innerHTML = `<div class="empty-state">Loading commit…</div>`;
    Promise.all([Api.commit(tab.hash), Api.commitDiff(tab.hash)]).then(([commit, diff]) => {
      container.innerHTML = `
        <div class="breadcrumbs">
          <span class="crumb" data-nav="history">History</span>
          <span class="sep">›</span>
          <span class="crumb current">${escapeHtml(tab.hash.slice(0, 7))}</span>
        </div>
        <div style="font-size:13px;font-weight:600;margin-bottom:6px">${escapeHtml(commit.message)}</div>
        <div class="text-dim" style="font-size:11px;margin-bottom:12px">${escapeHtml(commit.author)} · ${fmtDate(commit.date)}</div>
        <div style="font-weight:600;font-size:12px;margin-bottom:8px">Files changed</div>
        <div style="margin-bottom:12px">${commit.files.map((f) =>
          `<div class="row" style="font-size:11px;padding:3px 0"><span class="grow">${fileIcon(f.file, 11)} ${escapeHtml(f.file || '')}</span>${diffStatText(f.additions || 0, f.deletions || 0)}</div>`
        ).join('')}</div>
        <div style="font-weight:600;font-size:12px;margin-bottom:8px">Diff</div>
        ${renderDiff(diff.diff)}
      `;
      container.querySelectorAll('[data-nav]').forEach((b) =>
        b.addEventListener('click', () => App.navigate(b.getAttribute('data-nav'))));
    }).catch((e) => {
      container.innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`;
    });
  }

  function renderDiffTab(tab, container) {
    container.innerHTML = `<div class="empty-state">Loading diff…</div>`;
    Api.workingDiff(tab.path, tab.staged).then((d) => {
      const split = document.getElementById('insp-split-btn')?.classList.contains('active');
      container.innerHTML = `
        <div class="breadcrumbs">
          <span class="crumb" data-nav="changes">Changes</span>
          <span class="sep">›</span>
          <span class="crumb current">${escapeHtml(tab.path.split('/').pop())}</span>
        </div>
        ${split ? renderSplitDiff(d.diff) : renderDiff(d.diff)}`;
      container.querySelectorAll('[data-nav]').forEach((b) =>
        b.addEventListener('click', () => App.navigate(b.getAttribute('data-nav'))));
    }).catch((e) => {
      container.innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`;
    });
  }

  return { open, close, render, rerender: renderActive };
})();

/* Public helper used everywhere (replaces the old inspectFile) */
async function inspectFile(relPath) { Tabs.open('file', { path: relPath }); }