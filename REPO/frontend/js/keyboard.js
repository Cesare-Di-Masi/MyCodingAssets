(function initKeyboard() {
  const GOTO = {
    o: 'overview', f: 'files', s: 'search', h: 'history', g: 'graph',
    n: 'insights', c: 'changes', b: 'branches', u: 'reflog', t: 'stash',
    x: 'terminal'
  };
  let gPending = false;
  let gTimer = null;
  let helpPreviousFocus = null;

  const SHORTCUTS = [
    { section: 'Navigation' },
    { keys: ['Ctrl', 'K'], label: 'Open command palette', action: () => Palette.open() },
    { keys: ['Ctrl', 'P'], label: 'Go to file (fuzzy finder)', action: () => Finder.open() },
    { keys: ['G', 'O'], label: 'Overview', action: () => App.navigate('overview') },
    { keys: ['G', 'F'], label: 'Files', action: () => App.navigate('files') },
    { keys: ['G', 'S'], label: 'Search', action: () => App.navigate('search') },
    { keys: ['G', 'H'], label: 'History', action: () => App.navigate('history') },
    { keys: ['G', 'G'], label: 'Graph', action: () => App.navigate('graph') },
    { keys: ['G', 'C'], label: 'Changes', action: () => App.navigate('changes') },
    { keys: ['G', 'B'], label: 'Branches', action: () => App.navigate('branches') },
    { keys: ['G', 'X'], label: 'Terminal', action: () => App.navigate('terminal') },
    { section: 'View' },
    { keys: ['Ctrl', 'Shift', 'L'], label: 'Cycle theme', action: () => Theme.cycle() },
    { keys: ['Ctrl', 'Shift', 'Z'], label: 'Toggle zen mode', action: toggleZen },
    { keys: ['Ctrl', 'Shift', 'S'], label: 'Toggle split diff', action: toggleSplit },
    { keys: ['Ctrl', 'Shift', 'O'], label: 'Open repository switcher', action: () => document.getElementById('repo-switcher-btn')?.click() },
    { keys: ['?'], label: 'Show this help', action: openHelp },
    { section: 'Files' },
    { keys: ['Ctrl', 'W'], label: 'Close active inspector tab', action: () => Tabs.close('') },
    { section: 'Global' },
    { keys: ['Esc'], label: 'Close overlay', action: closeAllOverlays }
  ];

  function toggleZen() {
    document.body.classList.toggle('zen');
    toast(document.body.classList.contains('zen') ? 'Zen mode on' : 'Zen mode off', 'info', 1200);
  }

  function toggleSplit() {
    const btn = document.getElementById('insp-split-btn');
    if (btn) btn.classList.toggle('active');
    Tabs.rerender();
  }

  function openHelp() {
    helpPreviousFocus = document.activeElement;
    const overlay = document.getElementById('help-overlay');
    const grid = document.getElementById('help-grid');
    grid.innerHTML = SHORTCUTS.map((s) => {
      if (s.section) return `<div class="help-section-title">${escapeHtml(s.section)}</div>`;
      return `<div class="help-row">
        <span>${escapeHtml(s.label)}</span>
        <span class="help-keys">${s.keys.map((k) => `<kbd>${escapeHtml(k)}</kbd>`).join('')}</span>
      </div>`;
    }).join('');
    overlay.classList.remove('hidden');
    document.getElementById('help-close').focus();
  }

  function closeHelp() {
    document.getElementById('help-overlay').classList.add('hidden');
    if (helpPreviousFocus?.focus) helpPreviousFocus.focus();
    helpPreviousFocus = null;
  }

  function closeAllOverlays() {
    Palette.close();
    Finder.close();
    closeHelp();
    closeSettings();
  }
  function openSettings() { const el = document.getElementById('settings-overlay'); if (!el) return; el.classList.remove('hidden'); document.getElementById('settings-theme').value = localStorage.getItem('rcc-theme') || 'system'; document.getElementById('settings-editor').value = localStorage.getItem('rcc-editor-uri') || 'vscode://file/{path}'; document.getElementById('settings-notifications').checked = localStorage.getItem('rcc-notifications') === 'true'; document.getElementById('settings-theme').focus(); }
  function closeSettings() { document.getElementById('settings-overlay')?.classList.add('hidden'); }

  document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('zen-btn').addEventListener('click', toggleZen);
    document.getElementById('help-close').addEventListener('click', closeHelp);
    document.getElementById('help-overlay').addEventListener('click', (e) => {
      if (e.target.id === 'help-overlay') closeHelp();
    });
    document.getElementById('settings-close').addEventListener('click', closeSettings);
    document.getElementById('settings-rebuild').addEventListener('click', async () => { await Api.indexRebuild(); toast('Index rebuild started', 'info'); });
    document.getElementById('settings-clear').addEventListener('click', () => { if (confirm('Clear local preferences?')) { localStorage.clear(); location.reload(); } });
    document.getElementById('settings-save').addEventListener('click', async () => { const theme = document.getElementById('settings-theme').value; const editor = document.getElementById('settings-editor').value; localStorage.setItem('rcc-theme', theme); localStorage.setItem('rcc-editor-uri', editor); localStorage.setItem('rcc-notifications', document.getElementById('settings-notifications').checked); Theme.apply(theme); await Api.saveSettings({ editorUri: editor, ollamaUrl: document.getElementById('settings-ollama-url').value, ollamaModel: document.getElementById('settings-ollama-model').value, githubClientId: document.getElementById('settings-github-client').value }); closeSettings(); toast('Settings saved'); });

    // Split diff toggle
    document.getElementById('insp-split-btn').addEventListener('click', toggleSplit);

    document.addEventListener('keydown', (e) => {
      const tag = (e.target.tagName || '').toLowerCase();
      const inInput = tag === 'input' || tag === 'textarea' || e.target.isContentEditable;

      // Ctrl shortcuts work everywhere
      if (e.ctrlKey || e.metaKey) {
        if (e.key.toLowerCase() === 'k' || e.key.toLowerCase() === 'p') {
          e.preventDefault(); Palette.open(); return;
        }
        if (e.shiftKey && e.key.toLowerCase() === 'l') {
          e.preventDefault(); Theme.cycle(); return;
        }
        if (e.shiftKey && e.key.toLowerCase() === 'z') {
          e.preventDefault(); toggleZen(); return;
        }
        if (e.shiftKey && e.key.toLowerCase() === 's') {
          e.preventDefault(); toggleSplit(); return;
        }
        if (e.shiftKey && e.key.toLowerCase() === 'o') { e.preventDefault(); document.getElementById('repo-switcher-btn')?.click(); return; }
        if (e.key === ',') { e.preventDefault(); openSettings(); return; }
        if (e.key.toLowerCase() === 'w' && !inInput) {
          // Close active tab
          const active = document.querySelector('.inspector-tab.active');
          if (active) {
            e.preventDefault();
            Tabs.close(active.getAttribute('data-id'));
          }
          return;
        }
        return;
      }

      if (inInput) {
        if (e.key === 'Escape') e.target.blur();
        return;
      }

      // Vim-style "g <key>" navigation
      if (gPending) {
        const view = GOTO[e.key.toLowerCase()];
        gPending = false;
        clearTimeout(gTimer);
        if (view) { e.preventDefault(); App.navigate(view); }
        return;
      }
      if (e.key.toLowerCase() === 'g') {
        gPending = true;
        clearTimeout(gTimer);
        gTimer = setTimeout(() => { gPending = false; }, 1200);
        return;
      }

      // Single-key shortcuts
      if (e.key === '?') { e.preventDefault(); openHelp(); }
      else if (e.key === 'Escape') { closeAllOverlays(); }
      else if (e.key === '/') { e.preventDefault(); Palette.open(); }
      else if (e.key.toLowerCase() === 't') {
        // Toggle theme with lowercase "t" outside inputs
        e.preventDefault(); Theme.cycle();
      }
    });
  });

  // Expose for the palette
  window.__keyboardHelp = openHelp;
})();