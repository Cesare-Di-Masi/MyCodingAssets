const GithubView = {
  async render(root) {
    root.innerHTML = `<h1>GITHUB</h1><div class="panel" id="gh-auth"><div class="empty-state">Checking authentication…</div></div><div class="panel" id="gh-panel"><div class="empty-state">Loading…</div></div>`;
    const auth = document.getElementById('gh-auth');
    const panel = document.getElementById('gh-panel');
    try {
      const me = await Api.githubMe();
      if (me.authenticated) {
        auth.innerHTML = `<div class="row" style="align-items:center;gap:10px"><img src="${escapeHtml(me.avatarUrl || '')}" alt="" style="width:32px;height:32px;border-radius:50%"><strong>${escapeHtml(me.login)}</strong><span class="text-dim">${escapeHtml(me.name || '')}</span><button class="sm" id="gh-logout" style="margin-left:auto">Sign out</button></div>`;
        document.getElementById('gh-logout').addEventListener('click', async () => { await Api.githubLogout(); toast('Signed out of GitHub'); GithubView.render(root); });
      } else {
        auth.innerHTML = `<div class="row" style="align-items:center;gap:10px;flex-wrap:wrap"><strong>GitHub authentication</strong><button class="sm" id="gh-signin">Sign in with GitHub</button><input id="gh-manual-token" type="password" aria-label="GitHub token" placeholder="Paste token manually" /><button class="sm" id="gh-manual-save">Save token</button><span class="text-dim" id="gh-auth-status">Device flow uses local-only token storage.</span></div>`;
        document.getElementById('gh-manual-save').addEventListener('click', async () => { try { await Api.githubManual(document.getElementById('gh-manual-token').value); GithubView.render(root); } catch (e) { document.getElementById('gh-auth-status').textContent = e.message; } });
        document.getElementById('gh-signin').addEventListener('click', async () => {
          const status = document.getElementById('gh-auth-status');
          try {
            const flow = await Api.githubAuthStart();
            status.innerHTML = `Enter <strong class="mono" style="font-size:18px">${escapeHtml(flow.user_code)}</strong> at <a href="${escapeHtml(flow.verification_uri)}" target="_blank">${escapeHtml(flow.verification_uri)}</a>`;
            let done = false; let interval = flow.interval || 5;
            while (!done) { await new Promise((resolve) => setTimeout(resolve, interval * 1000)); const result = await Api.githubAuthPoll(flow.device_code); if (result.status === 'pending') { status.textContent = 'Waiting for GitHub authorization…'; if (result.slowDown) interval += 5; continue; } done = true; if (result.status === 'complete') GithubView.render(root); else status.textContent = `GitHub authorization ${result.status}.`; }
          } catch (e) { status.textContent = e.message.includes('not configured') ? 'Set `githubClientId` in config.json to enable device-flow login. You can also paste a token manually.' : e.message; }
        });
      }
      const data = await Api.github();
      if (!data.configured) {
        panel.innerHTML = `<div class="empty-state">${escapeHtml(data.reason)}</div>
          <div style="margin-top:12px;font-size:12px;color:var(--text-dim)">
            Add <span class="mono">"githubToken": "ghp_..."</span> to <span class="mono">config.json</span>
            and optionally <span class="mono">"githubRepo": "owner/name"</span>.
          </div>`;
        return;
      }
      if (data.error) { panel.innerHTML = `<div class="empty-state text-danger">${escapeHtml(data.error)}</div>`; return; }
      panel.innerHTML = `
        <div class="stat-grid mb-16">
          <div class="stat-box"><div class="label">Repo</div><div class="value" style="font-size:14px">${escapeHtml(data.owner + '/' + data.repo)}</div></div>
          <div class="stat-box"><div class="label">Stars</div><div class="value">${data.stars}</div></div>
          <div class="stat-box"><div class="label">Forks</div><div class="value">${data.forks}</div></div>
          <div class="stat-box"><div class="label">Open Issues</div><div class="value">${data.openIssues}</div></div>
        </div>
        <h2>Open Issues</h2>
        ${data.issues.length ? data.issues.map((i) => `
          <div class="commit-row"><a href="${i.url}" target="_blank" style="color:inherit">
            <span class="commit-hash">#${i.number}</span>
            <span> ${escapeHtml(i.title)}</span>
            <div class="commit-meta">@${escapeHtml(i.user)} · ${fmtDate(i.created)} ${i.labels.map(l => `<span class="pill">${escapeHtml(l)}</span>`).join(' ')}</div>
          </a></div>`).join('') : '<div class="text-dim">None.</div>'}
        <h2 style="margin-top:20px">Open Pull Requests</h2>
        ${data.prs.length ? data.prs.map((p) => `
          <div class="commit-row"><a href="${p.url}" target="_blank" style="color:inherit">
            <span class="commit-hash">#${p.number}</span>
            <span> ${escapeHtml(p.title)}</span>
            <div class="commit-meta">@${escapeHtml(p.user)} · ${escapeHtml(p.head)} → ${escapeHtml(p.base)} ${p.draft ? '<span class="pill">draft</span>' : ''}</div>
          </a></div>`).join('') : '<div class="text-dim">None.</div>'}
        <h2 style="margin-top:20px">Recent Actions</h2>
        ${data.runs.length ? data.runs.map((r) => `
          <div class="commit-row"><a href="${r.url}" target="_blank" style="color:inherit">
            <span class="pill ${r.conclusion === 'success' ? 'A' : r.conclusion === 'failure' ? 'D' : 'U'}">${escapeHtml(r.conclusion || r.status)}</span>
            <span> ${escapeHtml(r.name)} · ${escapeHtml(r.branch || '')}</span>
            <div class="commit-meta">${fmtDate(r.created)}</div>
          </a></div>`).join('') : '<div class="text-dim">None.</div>'}`;
    } catch (e) { panel.innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`; }
  }
};