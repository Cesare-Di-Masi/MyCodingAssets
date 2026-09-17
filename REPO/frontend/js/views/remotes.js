const RemotesView = {
  async render(root) {
    root.innerHTML = `
      <h1>REMOTES</h1>
      <div class="panel">
        <div class="row" style="gap:8px;flex-wrap:wrap">
          <input type="text" id="rm-name" placeholder="name" style="max-width:120px" />
          <input type="text" id="rm-url" placeholder="https://github.com/..." />
          <button id="rm-add" class="primary">ADD</button>
        </div>
      </div>
      <div class="panel" id="remotes-list"><div class="empty-state">Loading…</div></div>`;
    const panel = document.getElementById('remotes-list');

    async function load() {
      const data = await Api.remotes();
      if (!data.remotes.length) { panel.innerHTML = '<div class="empty-state">No remotes.</div>'; return; }
      panel.innerHTML = data.remotes.map((r) => `
        <div class="commit-row" style="display:flex;gap:12px;align-items:center">
          <span class="pill A">${escapeHtml(r.name)}</span>
          <span class="grow mono" style="font-size:11px">${escapeHtml(r.url)}</span>
          <span class="text-dim" style="font-size:11px">${escapeHtml(r.type || '')}</span>
          <button class="sm" data-fetch="${escapeHtml(r.name)}">FETCH</button>
          <button class="sm" data-pull="${escapeHtml(r.name)}">PULL</button>
          <button class="sm" data-push="${escapeHtml(r.name)}">PUSH</button>
          <button class="sm danger" data-del="${escapeHtml(r.name)}">REMOVE</button>
        </div>`).join('');
      panel.querySelectorAll('[data-fetch]').forEach((b) => b.addEventListener('click', async () => {
        toast('Fetching…'); try { await Api.fetchRemote(b.getAttribute('data-fetch')); toast('Fetched'); App.refreshTopbar(); } catch (e) { toast(e.message, 'error'); }
      }));
      panel.querySelectorAll('[data-pull]').forEach((b) => b.addEventListener('click', async () => {
        toast('Pulling…'); try { await Api.pullRemote(b.getAttribute('data-pull')); toast('Pulled'); App.refreshTopbar(); } catch (e) { toast(e.message, 'error'); }
      }));
      panel.querySelectorAll('[data-push]').forEach((b) => b.addEventListener('click', async () => {
        toast('Pushing…'); try { await Api.pushRemote(b.getAttribute('data-push')); toast('Pushed'); App.refreshTopbar(); } catch (e) { toast(e.message, 'error'); }
      }));
      panel.querySelectorAll('[data-del]').forEach((b) => b.addEventListener('click', async () => {
        if (!confirm('Remove remote?')) return;
        try { await Api.deleteRemote(b.getAttribute('data-del')); toast('Removed'); load(); } catch (e) { toast(e.message, 'error'); }
      }));
    }

    document.getElementById('rm-add').addEventListener('click', async () => {
      const name = document.getElementById('rm-name').value.trim();
      const url = document.getElementById('rm-url').value.trim();
      if (!name || !url) return;
      try { await Api.addRemote(name, url); toast('Remote added'); load(); } catch (e) { toast(e.message, 'error'); }
    });
    load();
  }
};