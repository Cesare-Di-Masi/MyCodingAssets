const ConflictsView = {
  async render(root) {
    root.innerHTML = `
      <h1>MERGE CONFLICTS</h1>
      <div class="panel" id="conflicts-list"><div class="empty-state">Checking…</div></div>
      <div id="conflicts-editor"></div>`;
    const list = document.getElementById('conflicts-list');
    const editor = document.getElementById('conflicts-editor');

    async function load() {
      const data = await Api.conflicts();
      if (!data.files.length) {
        list.innerHTML = `<div class="empty-state text-accent">✓ No conflicts.</div>`;
        editor.innerHTML = '';
        return;
      }
      list.innerHTML = `<h2>${data.files.length} file(s) in conflict</h2>` +
        data.files.map((f) => `<div class="commit-row" data-file="${escapeHtml(f)}" style="display:flex;gap:10px;align-items:center">
          <span class="pill D">!</span><span class="grow mono" style="font-size:12px">${escapeHtml(f)}</span>
          <button class="sm" data-open="${escapeHtml(f)}">RESOLVE</button></div>`).join('');
      list.querySelectorAll('[data-open]').forEach((b) =>
        b.addEventListener('click', () => openConflict(b.getAttribute('data-open'))));
    }

    async function openConflict(file) {
      editor.innerHTML = '<div class="panel"><div class="empty-state">Loading conflict…</div></div>';
      try {
        const data = await Api.conflictRead(file);
        if (!data.blocks.length) {
          editor.innerHTML = `<div class="panel"><div class="text-dim">No conflict markers in ${escapeHtml(file)} — file may already be resolved.</div>
            <button class="primary" id="mark-resolved" style="margin-top:12px">MARK RESOLVED</button></div>`;
          document.getElementById('mark-resolved').addEventListener('click', async () => {
            try { await Api.conflictResolve(file, []); toast('Marked resolved'); load(); } catch (e) { toast(e.message, 'error'); }
          });
          return;
        }
        editor.innerHTML = `
          <div class="panel">
            <h2>${escapeHtml(file)} · ${data.blocks.length} conflict block(s)</h2>
            <div id="blocks"></div>
            <button class="primary" id="apply-all" style="margin-top:12px">APPLY & MARK RESOLVED</button>
          </div>`;
        const blocksEl = document.getElementById('blocks');
        const choices = data.blocks.map(() => 'ours');
        data.blocks.forEach((b, i) => {
          const div = document.createElement('div');
          div.className = 'mb-16';
          div.innerHTML = `
            <div class="text-dim mb-8" style="font-size:11px">Block ${i + 1}</div>
            <div style="display:grid;grid-template-columns:1fr 1fr;gap:10px">
              <div>
                <div class="pill A mb-8">OURS</div>
                <pre class="file-content" style="max-height:180px">${escapeHtml(b.ours)}</pre>
                <label class="checkbox-row" style="margin-top:6px"><input type="radio" name="c${i}" value="ours" checked /> keep ours</label>
              </div>
              <div>
                <div class="pill R mb-8">THEIRS</div>
                <pre class="file-content" style="max-height:180px">${escapeHtml(b.theirs)}</pre>
                <label class="checkbox-row" style="margin-top:6px"><input type="radio" name="c${i}" value="theirs" /> keep theirs</label>
              </div>
            </div>
            <label class="checkbox-row" style="margin-top:6px"><input type="radio" name="c${i}" value="both" /> keep both</label>`;
          blocksEl.appendChild(div);
        });
        document.getElementById('apply-all').addEventListener('click', async () => {
          const resolution = choices.map((_, i) => ({ choice: root.querySelector(`input[name=c${i}]:checked`).value }));
          try {
            await Api.conflictResolve(file, resolution);
            toast('Resolved & staged');
            load(); App.refreshTopbar();
          } catch (e) { toast(e.message, 'error'); }
        });
      } catch (e) { editor.innerHTML = `<div class="panel"><div class="empty-state text-danger">${escapeHtml(e.message)}</div></div>`; }
    }
    load();
  }
};