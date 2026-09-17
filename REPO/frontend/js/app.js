const VIEWS = {
  overview: OverviewView, files: FilesView, search: SearchView,
  history: HistoryView, graph: GraphView, insights: InsightsView,
  changes: ChangesView, branches: BranchesView, reflog: ReflogView,
  stash: StashView, snapshots: SnapshotsView, tags: TagsView,
  remotes: RemotesView, bisect: BisectView, conflicts: ConflictsView,
  health: HealthView, gitignore: GitignoreView, deps: DepsView,
  ownership: OwnershipView, changelog: ChangelogView, bookmarks: BookmarksView,
  github: GithubView, ai: AiView, repos: ReposView, runner: RunnerView, terminal: TerminalView
};

const App = {
  navigate(viewName) {
    State.currentView = viewName;
    localStorage.setItem('rcc-workspace', JSON.stringify({ view: viewName }));
    location.hash = '#/' + viewName;
    document.querySelectorAll('[data-nav] li').forEach((li) => {
      li.classList.toggle('active', li.getAttribute('data-view') === viewName);
      li.setAttribute('aria-selected', li.getAttribute('data-view') === viewName ? 'true' : 'false');
    });
    const root = document.getElementById('view-root');
    const view = VIEWS[viewName];
    if (!view) return root.innerHTML = `<div class="empty-state">Unknown view: ${escapeHtml(viewName)}</div>`;
    setStatusbar(`$ ${viewName}`);
    root.scrollTop = 0;
    view.render(root).then(() => ensureFormLabels(root)).catch((e) => {
      root.innerHTML = `<div class="empty-state text-danger">View error: ${escapeHtml(e.message)}</div>`;
    });
    requestAnimationFrame(() => { const heading = root.querySelector('h1'); if (heading) { heading.tabIndex = -1; heading.focus(); } });
  },
  async refreshTopbar() {
  try {
    const data = await Api.overview();
    document.getElementById('branch-name').textContent = data.currentBranch || '--';
    const n = Number(data.commitCount) || 0;
    document.getElementById('commit-count').textContent = `${n} ${n === 1 ? 'commit' : 'commits'}`;
    const chip = document.getElementById('clean-indicator');
    if (data.status) {
      const dirty = !data.status.isClean;
      chip.textContent = dirty ? `Dirty · ${data.status.totalChanges}` : 'Clean';
      chip.classList.toggle('dirty', dirty);

      // Sidebar badge on Changes
      const changesNav = document.querySelector('[data-view="changes"]');
      if (changesNav) {
        let badge = changesNav.querySelector('.nav-badge');
        if (dirty && data.status.totalChanges > 0) {
          if (!badge) {
            badge = document.createElement('span');
            badge.className = 'nav-badge';
            changesNav.appendChild(badge);
          }
          badge.textContent = data.status.totalChanges;
        } else if (badge) {
          badge.remove();
        }
      }
    } else chip.textContent = '--';
  } catch (e) {}
}
};

function ensureFormLabels(root) {
  root.querySelectorAll('input, textarea, select').forEach((field) => {
    if (field.closest('label') || field.id && root.querySelector(`label[for="${CSS.escape(field.id)}"]`)) return;
    const label = document.createElement('label');
    label.className = 'sr-only'; label.htmlFor = field.id || `rcc-field-${Math.random().toString(36).slice(2)}`; field.id = label.htmlFor;
    label.textContent = field.getAttribute('aria-label') || field.getAttribute('placeholder') || 'Input';
    field.parentNode.insertBefore(label, field);
  });
}

document.querySelectorAll('[data-nav] li').forEach((li) =>
  li.addEventListener('click', () => App.navigate(li.getAttribute('data-view'))));
document.querySelectorAll('[data-nav] li').forEach((li) => {
  li.setAttribute('role', 'tab'); li.tabIndex = 0; li.setAttribute('aria-selected', 'false');
  li.addEventListener('keydown', (e) => {
    const tabs = [...document.querySelectorAll('[data-nav] li')]; const i = tabs.indexOf(li);
    if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); App.navigate(li.getAttribute('data-view')); }
    if (e.key === 'ArrowDown' || e.key === 'ArrowRight') { e.preventDefault(); tabs[(i + 1) % tabs.length].focus(); }
    if (e.key === 'ArrowUp' || e.key === 'ArrowLeft') { e.preventDefault(); tabs[(i - 1 + tabs.length) % tabs.length].focus(); }
  });
});
document.querySelectorAll('[data-nav]').forEach((list, index) => { list.setAttribute('role', 'tablist'); list.setAttribute('aria-label', `Navigation group ${index + 1}`); });

document.addEventListener('keydown', (e) => {
  if ((e.ctrlKey || e.metaKey) && (e.key.toLowerCase() === 'k' || e.key.toLowerCase() === 'p')) {
    e.preventDefault(); Palette.open(); return;
  }
  if (e.key === 'Escape') Palette.close();
});

setInterval(() => {
  const el = document.getElementById('status-clock');
  if (el) el.textContent = new Date().toLocaleTimeString();
}, 1000);

// Deep linking
window.addEventListener('hashchange', () => {
  const v = location.hash.replace(/^#\//, '');
  if (v && VIEWS[v] && v !== State.currentView) App.navigate(v);
});

// Live watcher indicator
Watcher.on((data) => {
  const el = document.getElementById('watch-indicator');
  if (!el || !data.files?.length) return;
  el.textContent = `⟳ ${data.files.length} file(s) changed`;
  el.className = 'text-accent';
  setTimeout(() => { el.textContent = ''; el.className = 'text-dim'; }, 3000);
});

function showAuthLock() {
  const overlay = document.getElementById('auth-overlay');
  if (!overlay) return;
  overlay.classList.remove('hidden');
  const input = document.getElementById('auth-token-input');
  const form = document.getElementById('auth-token-form');
  const msg = document.getElementById('auth-token-message');
  if (!input || !form || !msg) return;

  const unlock = async () => {
    const token = input.value.trim();
    if (!token) {
      msg.textContent = 'Enter the bearer token to continue.';
      return;
    }
    try {
      await Api.verifyToken(token);
      localStorage.setItem('rcc-bearer-token', token);
      overlay.classList.add('hidden');
      location.reload();
    } catch (e) {
      msg.textContent = e.message || 'Token rejected.';
      input.focus();
    }
  };

  input.value = '';
  msg.textContent = 'This workspace requires a bearer token for LAN access.';
  form.onsubmit = (e) => { e.preventDefault(); unlock(); };
  input.focus();
}

async function initializeAuthGate() {
  try {
    const status = await Api.authStatus();
    if (!status.requiresAuth) return false;
    const persisted = localStorage.getItem('rcc-bearer-token');
    if (persisted) {
      try {
        await Api.verifyToken(persisted);
        return false;
      } catch (_) {
        localStorage.removeItem('rcc-bearer-token');
      }
    }
    showAuthLock();
    return true;
  } catch (e) {
    return false;
  }
}

async function bootApp() {
  const locked = await initializeAuthGate();
  if (locked) return;
  const savedWorkspace = JSON.parse(localStorage.getItem('rcc-workspace') || '{}');
  const initial = location.hash.replace(/^#\//, '') || savedWorkspace.view || 'overview';
  try {
    const data = await Api.currentRepo();
    State.activeRepo = data.repo === data.defaultRepo ? null : data.repo;
  } catch (_) {}
  App.navigate(VIEWS[initial] ? initial : 'overview');
  App.refreshTopbar();
  loadRepoSwitcher();
  setInterval(App.refreshTopbar, 15000);
}
bootApp();

async function loadRepoSwitcher() {
  const menu = document.getElementById('repo-switcher-menu');
  const button = document.getElementById('repo-switcher-btn');
  const name = document.getElementById('repo-switcher-name');
  if (!menu || !button || !name) return;
  try {
    const data = await Api.repos();
    name.textContent = (State.activeRepo || data.current).split(/[\\/]/).pop();
    menu.innerHTML = data.repos.map((repo) => `<button class="repo-switcher-item" role="menuitem" data-repo="${escapeHtml(repo.path)}"><strong>${escapeHtml(repo.name)}</strong><span class="repo-meta">${escapeHtml(repo.branch || 'detached')} · ${repo.clean ? 'clean' : 'pending changes'} · ${escapeHtml(repo.path)}</span></button>`).join('');
    if (!data.repos.some((repo) => repo.path === data.current)) menu.insertAdjacentHTML('afterbegin', `<button class="repo-switcher-item" role="menuitem" data-repo="${escapeHtml(data.current)}"><strong>${escapeHtml(data.current.split(/[\\/]/).pop())}</strong><span class="repo-meta">active default · ${escapeHtml(data.current)}</span></button>`);
    menu.querySelectorAll('[data-repo]').forEach((item) => item.addEventListener('click', async () => {
      const repo = item.getAttribute('data-repo');
      try { await Api.activateRepo(repo); State.activeRepo = repo === data.current ? null : repo; menu.classList.add('hidden'); button.setAttribute('aria-expanded', 'false'); App.navigate(State.currentView); App.refreshTopbar(); refreshIndexIndicator(); loadRepoSwitcher(); } catch (error) { toast(error.message, 'error'); }
    }));
  } catch (error) { name.textContent = 'Unavailable'; }
}
document.getElementById('repo-switcher-btn')?.addEventListener('click', () => {
  const menu = document.getElementById('repo-switcher-menu'); const button = document.getElementById('repo-switcher-btn'); const open = menu.classList.toggle('hidden'); button.setAttribute('aria-expanded', String(!open)); if (!open) menu.querySelector('[role="menuitem"]')?.focus();
});

async function refreshOperationBanner() {
  const content = document.getElementById('content'); if (!content) return;
  let banner = document.getElementById('operation-banner');
  try {
    const data = await Api.operationState();
    if (!data.operation) { if (banner) banner.remove(); return; }
    if (!banner) { banner = document.createElement('div'); banner.id = 'operation-banner'; banner.className = 'panel'; content.insertBefore(banner, content.firstChild); }
    banner.innerHTML = `<strong>${escapeHtml(data.operation)} in progress</strong><span class="text-dim">${data.conflicts?.length ? ' Resolve conflicts before continuing.' : ''}</span><button class="sm" data-op-action="continue">Continue</button><button class="sm danger" data-op-action="abort">Abort</button>${data.conflicts?.length ? '<button class="sm" data-op-view="conflicts">Conflicts</button>' : ''}`;
    const operationKey = data.operation.replace('-', '');
    banner.querySelector('[data-op-action=continue]').onclick = async () => { await Api[`${operationKey}Continue`](); refreshOperationBanner(); };
    banner.querySelector('[data-op-action=abort]').onclick = async () => { if (confirm(`Abort ${data.operation}?`)) { await Api[`${operationKey}Abort`](); refreshOperationBanner(); } };
    banner.querySelector('[data-op-view]')?.addEventListener('click', () => App.navigate('conflicts'));
  } catch (e) {}
}
refreshOperationBanner();
setInterval(refreshOperationBanner, 5000);

async function refreshIndexIndicator() {
  const el = document.getElementById('index-indicator');
  if (!el) return;
  try {
    const status = await Api.indexStatus();
    if (status.indexing) el.textContent = `Indexing ${status.filesIndexed || 0} files…`;
    else if (status.filesIndexed) el.textContent = `Indexed · ${status.filesIndexed.toLocaleString()} files · ${fmtDate(status.lastIndexedAt)}`;
    else el.textContent = 'Index pending';
  } catch (e) { el.textContent = ''; }
}
refreshIndexIndicator();
setInterval(refreshIndexIndicator, 5000);

/* ============ Recent activity ticker ============ */
(function startActivityTicker() {
  const el = document.getElementById('activity-ticker');
  if (!el) return;
  let lastHash = null;

  async function tick() {
    try {
      const data = await Api.history(1);
      const c = data.commits && data.commits[0];
      if (!c) return;
      if (c.fullHash === lastHash) return;
      const isNew = lastHash !== null;
      lastHash = c.fullHash;
      el.innerHTML = `
        <span class="pulse"></span>
        ${avatarFor(c.author, c.email, 'sm')}
        <span style="overflow:hidden;text-overflow:ellipsis;white-space:nowrap">
          <span class="text-accent mono" style="font-size:11px">${c.hash}</span>
          <span style="color:var(--text-secondary)"> ${escapeHtml(c.subject)}</span>
        </span>
      `;
      if (isNew) {
        el.style.transition = 'background 300ms';
        el.style.background = 'rgba(61,220,151,0.10)';
        setTimeout(() => { el.style.background = 'transparent'; }, 1400);
        App.refreshTopbar();
      }
    } catch (_) {}
  }

  // Insert the ticker into the statusbar
  const right = document.getElementById('statusbar-right');
  if (right && !document.getElementById('activity-ticker')) {
    const span = document.createElement('span');
    span.id = 'activity-ticker';
    right.insertBefore(span, right.firstChild);
  }

  tick();
  setInterval(tick, 20000);
})();

/* ============ Resizable panels ============ */
(function initResize() {
  const layout = document.getElementById('layout');
  const leftHandle = document.getElementById('resize-left');
  const rightHandle = document.getElementById('resize-right');
  const saved = JSON.parse(localStorage.getItem('rcc-panels') || '{}');
  let leftW = saved.left || 232;
  let rightW = saved.right || 340;

  function apply() {
    layout.style.gridTemplateColumns = `${leftW}px 3px minmax(0, 1fr) 3px ${rightW}px`;
  }
  apply();

  function makeDraggable(handle, side) {
    let startX, startSize, current;
    handle.addEventListener('mousedown', (e) => {
      e.preventDefault();
      startX = e.clientX;
      startSize = side === 'left' ? leftW : rightW;
      document.body.classList.add('resizing');
      handle.classList.add('dragging');

      function onMove(ev) {
        const delta = ev.clientX - startX;
        current = side === 'left'
          ? Math.max(160, Math.min(420, startSize + delta))
          : Math.max(220, Math.min(640, startSize - delta));
        if (side === 'left') leftW = current; else rightW = current;
        apply();
      }
      function onUp() {
        document.body.classList.remove('resizing');
        handle.classList.remove('dragging');
        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('mouseup', onUp);
        localStorage.setItem('rcc-panels', JSON.stringify({ left: leftW, right: rightW }));
      }
      document.addEventListener('mousemove', onMove);
      document.addEventListener('mouseup', onUp);
    });
  }
  makeDraggable(leftHandle, 'left');
  makeDraggable(rightHandle, 'right');
})();
/* Keyboard help hint in statusbar */
document.addEventListener('DOMContentLoaded', () => {
  const left = document.getElementById('statusbar-left');
  if (!left) return;
  const hint = document.createElement('span');
  hint.style.cssText = 'margin-left:12px;color:var(--text-quaternary);font-size:10.5px';
  hint.innerHTML = 'press <kbd style="background:var(--bg-raised);border:1px solid var(--border-subtle);border-radius:3px;padding:1px 5px;font-size:10px">?</kbd> for shortcuts';
  left.parentNode.appendChild(hint);
});