const BranchesView = {
  async render(root) {
    root.innerHTML = `
      <h1>BRANCHES</h1>
      <div class="panel">
        <div class="row" style="gap:8px">
          <input type="text" id="new-branch" placeholder="new-branch-name" />
          <button id="create-branch-btn" class="primary">CREATE</button>
        </div>
      </div>
      <div class="panel" id="branches-panel"><div class="empty-state">Loading…</div></div>
    `;
    const panel = document.getElementById('branches-panel');

    document.getElementById('create-branch-btn').addEventListener('click', async () => {
      const name = document.getElementById('new-branch').value.trim();
      if (!name) return;
      try {
        await Api.createBranch(name);
        toast(`Created and switched to ${name}`);
        document.getElementById('new-branch').value = '';
        App.navigate('branches');
        App.refreshTopbar();
      } catch (e) { toast(e.message, 'error'); }
    });

    try {
      const data = await Api.branches();
      const local = data.branches.filter((b) => !b.remote);
      const remote = data.branches.filter((b) => b.remote);

      panel.innerHTML = `
        <div class="mb-12"><strong>LOCAL</strong> <span class="text-dim">(${local.length})</span></div>
        <table>
          <tbody>
            ${local.map((b) => `
              <tr data-branch="${escapeHtml(b.name)}">
                <td style="width:20px">${b.current ? '<span class="text-accent">●</span>' : '<span class="text-dim">○</span>'}</td>
                <td>${escapeHtml(b.name)}</td>
                <td class="text-dim" style="font-size:11px">${escapeHtml(b.label || '')}</td>
                <td style="width:180px;text-align:right">
                  ${b.current ? '<span class="text-dim" style="font-size:11px">current</span>' : `
                    <button class="sm" data-checkout="${escapeHtml(b.name)}">CHECKOUT</button>
                    <button class="sm" data-merge="${escapeHtml(b.name)}">MERGE</button>
                    <button class="sm danger" data-del="${escapeHtml(b.name)}">DELETE</button>`}
                </td>
              </tr>`).join('')}
          </tbody>
        </table>
        <div class="mb-12" style="margin-top:20px"><strong>REMOTE</strong> <span class="text-dim">(${remote.length})</span></div>
        <table>
          <tbody>
            ${remote.map((b) => `<tr><td>${escapeHtml(b.name)}</td><td class="text-dim">${escapeHtml(b.label || '')}</td></tr>`).join('')}
          </tbody>
        </table>
      `;

      panel.querySelectorAll('[data-checkout]').forEach((btn) =>
        btn.addEventListener('click', async (e) => {
          e.stopPropagation();
          const branch = btn.getAttribute('data-checkout');
          try {
            await Api.checkout(branch);
            toast(`Switched to ${branch}`);
            App.navigate('branches');
            App.refreshTopbar();
          } catch (err) { toast(err.message, 'error'); }
        }));

      panel.querySelectorAll('[data-del]').forEach((btn) =>
        btn.addEventListener('click', async (e) => {
          e.stopPropagation();
          const branch = btn.getAttribute('data-del');
          if (!confirm(`Delete branch ${branch}?`)) return;
          try {
            await Api.deleteBranch(branch, false);
            toast(`Deleted ${branch}`);
            App.navigate('branches');
          } catch (err) { toast(err.message, 'error'); }
        }));

      panel.querySelectorAll('[data-merge]').forEach((btn) => btn.addEventListener('click', async (e) => {
        e.stopPropagation(); const branch = btn.getAttribute('data-merge');
        try {
          const preview = await Api.mergePreview(branch, data.current);
          const stat = preview.stat || preview.diffStat || JSON.stringify(preview);
          if (!confirm(`Merge ${branch} into ${data.current}?\n\n${stat}`)) return;
          const result = await Api.merge({ branch });
          toast(`Merged ${branch}. Snapshot ${result.snapshotId}`, 'success', 5000); App.navigate('branches'); App.refreshTopbar();
        } catch (err) { toast(`${err.message}${err.snapshotId ? ` Snapshot ${err.snapshotId}` : ''}`, 'error', 6000); if (err.conflict) App.navigate('conflicts'); }
      }));
    } catch (e) {
      panel.innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`;
    }
  }
};