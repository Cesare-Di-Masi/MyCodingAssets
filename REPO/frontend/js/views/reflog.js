const ReflogView = {
  async render(root) {
    root.innerHTML = `
      <h1>REFLOG / UNDO</h1>
      <div class="panel">
        <div class="text-dim mb-12" style="font-size:11px">Every position HEAD has held. Click RESTORE to jump back.</div>
        <div class="row mb-8" style="gap:6px;flex-wrap:wrap">
          <span class="text-dim" style="font-size:11px">Mode:</span>
          <label class="checkbox-row"><input type="radio" name="rmode" value="soft" /> SOFT</label>
          <label class="checkbox-row"><input type="radio" name="rmode" value="mixed" checked /> MIXED</label>
          <label class="checkbox-row"><input type="radio" name="rmode" value="hard" /> HARD</label>
          <label class="checkbox-row"><input type="radio" name="rmode" value="keep" /> KEEP</label>
        </div>
      </div>
      <div class="panel" id="reflog-list" style="padding:0"><div class="empty-state">Loading…</div></div>
    `;
    const panel = document.getElementById('reflog-list');
    try {
      const data = await Api.reflog();
      if (!data.entries.length) return panel.innerHTML = '<div class="empty-state">No reflog entries.</div>';
      panel.innerHTML = data.entries.map((e) => `
        <div class="commit-row" data-hash="${e.hash}" style="display:flex;gap:12px;align-items:center">
          <span class="commit-hash">${e.short}</span>
          <span class="pill" style="min-width:70px">${escapeHtml(e.ref)}</span>
          <span class="grow">${escapeHtml(e.action)}</span>
          <span class="text-dim" style="font-size:11px">${escapeHtml(e.author)}</span>
          <span class="text-dim" style="font-size:11px">${fmtDate(e.date)}</span>
          <button class="sm" data-restore="${e.hash}">RESTORE</button>
        </div>`).join('');
      panel.querySelectorAll('[data-restore]').forEach((btn) =>
        btn.addEventListener('click', async (ev) => {
          ev.stopPropagation();
          const hash = btn.getAttribute('data-restore');
          const mode = root.querySelector('input[name=rmode]:checked').value;
          const msg = mode === 'hard'
            ? `HARD reset to ${hash.slice(0,7)}?\n\nThis DISCARDS all uncommitted changes.`
            : `Reset to ${hash.slice(0,7)}?`;
          if (!confirm(msg)) return;
          if (mode === 'hard' && prompt(`Type ${hash.slice(0, 7)} to confirm the hard reset.`) !== hash.slice(0, 7)) return toast('Hard reset cancelled', 'warn');
          try {
            const result = await Api.reflogReset(hash, mode);
            toast(`Reset to ${hash.slice(0,7)}. Snapshot ${result.snapshotId}`, 'success', 5000);
            App.navigate('reflog'); App.refreshTopbar();
          } catch (e) { toast(e.message, 'error'); }
        }));
    } catch (e) { panel.innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`; }
  }
};