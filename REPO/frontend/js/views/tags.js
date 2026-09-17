const TagsView = {
  async render(root) {
    root.innerHTML = `
      <h1>TAGS</h1>
      <div class="panel">
        <div class="row" style="gap:8px;flex-wrap:wrap">
          <input type="text" id="tag-name" placeholder="v1.0.0" style="max-width:160px" />
          <input type="text" id="tag-msg" placeholder="Release message (optional)" />
          <button id="tag-create" class="primary">CREATE</button>
        </div>
      </div>
      <div class="panel" id="tags-list" style="padding:0"><div class="empty-state">Loading…</div></div>`;
    const panel = document.getElementById('tags-list');

    async function load() {
      const data = await Api.tags();
      if (!data.tags.length) { panel.innerHTML = '<div class="empty-state">No tags.</div>'; return; }
      panel.innerHTML = data.tags.map((t) => `
        <div class="commit-row" style="display:flex;gap:12px;align-items:center">
          <span class="pill A">${escapeHtml(t.name)}</span>
          <span class="commit-hash">${escapeHtml(t.hash || '')}</span>
          <span class="grow">${escapeHtml(t.subject || '')}</span>
          <span class="text-dim" style="font-size:11px">${fmtDate(t.date)}</span>
          <button class="sm" data-push="${escapeHtml(t.name)}">PUSH</button>
          <button class="sm danger" data-del="${escapeHtml(t.name)}">DELETE</button>
        </div>`).join('');
      panel.querySelectorAll('[data-push]').forEach((b) => b.addEventListener('click', async () => {
        try { await Api.pushTag(b.getAttribute('data-push')); toast('Tag pushed'); } catch (e) { toast(e.message, 'error'); }
      }));
      panel.querySelectorAll('[data-del]').forEach((b) => b.addEventListener('click', async () => {
        if (!confirm('Delete tag?')) return;
        try { await Api.deleteTag(b.getAttribute('data-del')); toast('Tag deleted'); load(); } catch (e) { toast(e.message, 'error'); }
      }));
    }

    document.getElementById('tag-create').addEventListener('click', async () => {
      const name = document.getElementById('tag-name').value.trim();
      const message = document.getElementById('tag-msg').value.trim();
      if (!name) return;
      try {
        await Api.createTag(name, message);
        toast(`Tag ${name} created`);
        document.getElementById('tag-name').value = '';
        document.getElementById('tag-msg').value = '';
        load();
      } catch (e) { toast(e.message, 'error'); }
    });
    load();
  }
};