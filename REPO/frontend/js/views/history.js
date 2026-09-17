const HistoryView = {
  async render(root) {
    root.innerHTML = `
      <h1>${icon('history', 20)} History</h1>
      <div class="panel" style="padding:10px">
        <div class="row" style="gap:8px"><input type="text" id="hist-search" placeholder="Filter by message…" /><button class="sm" id="cherry-pick-selected" disabled>CHERRY-PICK SELECTED</button></div>
      </div>
      <div class="panel" id="history-list" style="padding:8px">
        <div class="empty-state">Loading history…</div>
      </div>
    `;
    const listEl = document.getElementById('history-list');
    const searchEl = document.getElementById('hist-search');
    const avatarMap = {};  // vuoto per ora, oppure popolato da un'API

    async function load(q) {
      listEl.innerHTML = `<div class="empty-state">Loading…</div>`;
      try {
        const data = await Api.history(200, null, q);
        if (!data.commits.length) return listEl.innerHTML = '<div class="empty-state">No commits found.</div>';
        listEl.innerHTML = `<div class="commit-row" style="font-size:11px"><span style="width:20px"></span><span>Select commits to cherry-pick</span></div>` + data.commits.map((c) => `
          <div class="commit-row" data-hash="${c.fullHash}">
            <input type="checkbox" class="cherry-check hist-select" value="${c.fullHash}" aria-label="Select commit ${c.hash} for cherry-pick" />
            ${avatarFor(c.author, c.email, '', avatarMap[c.fullHash] || avatarMap[c.hash] || null)}
            <div class="commit-main">
              <div class="commit-title">
                <span class="commit-hash">${c.hash}</span>
                <span class="commit-msg">${escapeHtml(c.subject)}</span>
                ${c.refs ? `<span class="ref-badge">${escapeHtml(c.refs.split(',')[0].trim())}</span>` : ''}
              </div>
              <div class="commit-meta">${escapeHtml(c.author)} · ${fmtDate(c.date)}</div>
            </div>
            <div class="commit-actions">
              <button class="commit-action" data-revert="${c.fullHash}" title="Revert commit">Revert</button>
            </div>
          </div>`).join('');
        const cherryButton = document.getElementById('cherry-pick-selected');
        const selected = () => [...listEl.querySelectorAll('.hist-select:checked')].map((input) => input.value);
        listEl.querySelectorAll('.hist-select').forEach((input) => input.addEventListener('click', (e) => { e.stopPropagation(); const count = selected().length; cherryButton.disabled = !count; cherryButton.textContent = count ? `CHERRY-PICK SELECTED (${count})` : 'CHERRY-PICK SELECTED'; }));
        cherryButton.addEventListener('click', async () => {
          const hashes = selected(); if (!hashes.length || !confirm(`Cherry-pick ${hashes.length} commit(s)?`)) return;
          try { const result = await Api.cherryPick(hashes); toast(`Cherry-picked ${hashes.length} commit(s). Snapshot ${result.snapshotId}`, 'success', 5000); load(searchEl.value.trim()); } catch (err) { toast(`${err.message}${err.snapshotId ? ` Snapshot ${err.snapshotId}` : ''}`, 'error', 6000); if (err.conflict) App.navigate('conflicts'); }
        });
        listEl.querySelectorAll('[data-revert]').forEach((btn) => btn.addEventListener('click', async (e) => {
          e.stopPropagation(); const hash = btn.getAttribute('data-revert'); if (!confirm(`Revert ${hash.slice(0, 7)}?`)) return;
          try { const result = await Api.revert(hash, false); toast(`Reverted. Snapshot ${result.snapshotId}`, 'success', 5000); load(searchEl.value.trim()); } catch (err) { toast(`${err.message}${err.snapshotId ? ` Snapshot ${err.snapshotId}` : ''}`, 'error'); }
        }));
        listEl.querySelectorAll('[data-hash]').forEach((el) =>
  el.addEventListener('click', () => Tabs.open('commit', { hash: el.getAttribute('data-hash') })));
      } catch (e) {
        listEl.innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`;
      }
    }

    let timer;
    searchEl.addEventListener('input', () => {
      clearTimeout(timer);
      timer = setTimeout(() => load(searchEl.value.trim()), 300);
    });

    load('');
  }
};