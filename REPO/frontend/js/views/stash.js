const StashView = {
  async render(root) {
    root.innerHTML = `
      <h1>STASHES</h1>
      <div class="panel">
        <div class="row" style="gap:8px">
          <input type="text" id="stash-msg" placeholder="Stash message (optional)" />
          <button id="stash-push-btn" class="primary">STASH</button>
        </div>
      </div>
      <div class="panel" id="stash-list"><div class="empty-state">Loading…</div></div>
    `;

    async function load() {
      const panel = document.getElementById('stash-list');
      try {
        const { stashes } = await Api.stashList();
        if (!stashes.length) {
          panel.innerHTML = '<div class="empty-state">No stashes.</div>';
          return;
        }
        panel.innerHTML = stashes.map((s) => `
          <div class="commit-row" style="display:flex;align-items:center;gap:10px">
            <span class="pill R">${escapeHtml(s.ref)}</span>
            <span class="grow">${escapeHtml(s.subject || '(no message)')}</span>
            <span class="text-dim" style="font-size:11px">${escapeHtml(s.author || '')}</span>
            <button class="sm" data-show="${escapeHtml(s.ref)}">DIFF</button>
            <button class="sm" data-apply="${escapeHtml(s.ref)}">APPLY</button>
            <button class="sm" data-pop="${escapeHtml(s.ref)}">POP</button>
            <button class="sm danger" data-drop="${escapeHtml(s.ref)}">DROP</button>
          </div>`).join('');

        panel.querySelectorAll('[data-show]').forEach((b) => b.addEventListener('click', async () => {
          const ref = b.getAttribute('data-show');
          const inspector = document.getElementById('inspector-content');
          inspector.innerHTML = '<div class="empty-state">Loading stash diff…</div>';
          const d = await Api.stashShow(ref);
          inspector.innerHTML = `<div class="mb-8"><strong>${escapeHtml(ref)}</strong></div>${renderDiff(d.diff)}`;
        }));
        panel.querySelectorAll('[data-apply]').forEach((b) => b.addEventListener('click', async () => {
          try { await Api.stashApply(b.getAttribute('data-apply')); toast('Stash applied'); load(); }
          catch (e) { toast(e.message, 'error'); }
        }));
        panel.querySelectorAll('[data-pop]').forEach((b) => b.addEventListener('click', async () => {
          try { await Api.stashPop(b.getAttribute('data-pop')); toast('Stash popped'); load(); App.refreshTopbar(); }
          catch (e) { toast(e.message, 'error'); }
        }));
        panel.querySelectorAll('[data-drop]').forEach((b) => b.addEventListener('click', async () => {
          if (!confirm('Drop stash?')) return;
          try { await Api.stashDrop(b.getAttribute('data-drop')); toast('Stash dropped'); load(); }
          catch (e) { toast(e.message, 'error'); }
        }));
      } catch (e) {
        panel.innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`;
      }
    }

    document.getElementById('stash-push-btn').addEventListener('click', async () => {
      const msg = document.getElementById('stash-msg').value;
      try {
        await Api.stashPush(msg, true);
        toast('Stash created');
        document.getElementById('stash-msg').value = '';
        load();
      } catch (e) { toast(e.message, 'error'); }
    });

    load();
  }
};