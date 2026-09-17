const Api = (() => {
  async function req(method, url, body) {
    const repo = typeof State !== 'undefined' ? State.activeRepo : null;
    if (repo && !/[?&]repo=/.test(url)) url += `${url.includes('?') ? '&' : '?'}repo=${encodeURIComponent(repo)}`;
    const opts = { method, headers: {} };
    const token = localStorage.getItem('rcc-bearer-token');
    if (token) opts.headers.Authorization = `Bearer ${token}`;
    if (body !== undefined) { opts.headers['Content-Type'] = 'application/json'; const payload = body && typeof body === 'object' && !Array.isArray(body) ? { ...body } : body; if (repo && payload && typeof payload === 'object' && !Array.isArray(payload) && !Object.prototype.hasOwnProperty.call(payload, 'repo')) payload.repo = repo; opts.body = JSON.stringify(payload); }
    const res = await fetch(url, opts);
    const data = await res.json().catch(() => ({}));
    if (!res.ok) throw new Error(data.error || res.statusText);
    return data;
  }
  return {
    overview: () => req('GET', '/api/overview'),
    listFiles: (p) => req('GET', `/api/files?path=${encodeURIComponent(p || '.')}`),
    readFile:  (p) => req('GET', `/api/file?path=${encodeURIComponent(p)}`),
    writeFile: (p, content) => req('PUT', '/api/file', { path: p, content }),
    renameFile: (from, to) => req('POST', '/api/file/rename', { from, to }),
    deleteFile: (p) => req('DELETE', '/api/file', { path: p }),
    authStatus: () => req('GET', '/api/auth/status'),
    verifyToken: (token) => req('POST', '/api/auth/verify', { token }),
    runDetect: () => req('GET', '/api/run/detect'),
    run: (cmd, args, cwd) => req('POST', '/api/run', { cmd, args: args || [], cwd: cwd || '.' }),
    stopRun: (runId) => req('POST', '/api/run/stop', { runId }),
    search: (q, mode) => req('GET', `/api/search?q=${encodeURIComponent(q)}&mode=${mode}`),
    history: (limit, branch, q) => {
      const params = new URLSearchParams({ limit: limit || 100 });
      if (branch) params.set('branch', branch);
      if (q) params.set('q', q);
      return req('GET', `/api/history?${params}`);
    },
    commit: (hash) => req('GET', `/api/commit/${hash}`),
    commitDiff: (hash, file) => req('GET', `/api/diff/${hash}${file ? `?file=${encodeURIComponent(file)}` : ''}`),
    graph: () => req('GET', '/api/graph'),
    blame: (p) => req('GET', `/api/blame?path=${encodeURIComponent(p)}`),
    timeline: (p) => req('GET', `/api/timeline?path=${encodeURIComponent(p)}`),
    pickaxe: (q, file) => req('GET', `/api/pickaxe?q=${encodeURIComponent(q)}${file ? `&file=${encodeURIComponent(file)}` : ''}`),
    insights: (days) => req('GET', `/api/insights?days=${days || 365}`),
    status: () => req('GET', '/api/status'),
    workingDiff: (file, staged) => req('GET', `/api/diff?file=${encodeURIComponent(file)}&staged=${!!staged}`),
    stage: (files) => req('POST', '/api/stage', { files }),
    unstage: (files) => req('POST', '/api/unstage', { files }),
    doCommit: (message, amend) => req('POST', '/api/commit', { message, amend }),
    branches: () => req('GET', '/api/branches'),
    checkout: (branch) => req('POST', '/api/checkout', { branch }),
    createBranch: (name, from) => req('POST', '/api/create-branch', { name, from }),
    deleteBranch: (name, force) => req('POST', '/api/delete-branch', { name, force }),
    compare: (a, b) => req('GET', `/api/compare?a=${encodeURIComponent(a)}&b=${encodeURIComponent(b)}`),
    mergePreview: (from, into) => req('GET', `/api/merge-preview?from=${encodeURIComponent(from)}&into=${encodeURIComponent(into)}`),
    merge: (body) => req('POST', '/api/merge', body),
    mergeAbort: () => req('POST', '/api/merge/abort', {}),
    mergeContinue: (message) => req('POST', '/api/merge/continue', { message }),
    cherryPick: (hashes) => req('POST', '/api/cherry-pick', { hashes }),
    cherryPickAbort: () => req('POST', '/api/cherry-pick/abort', {}),
    cherryPickContinue: () => req('POST', '/api/cherry-pick/continue', {}),
    revert: (hash, noCommit) => req('POST', '/api/revert', { hash, noCommit }),
    revertAbort: () => req('POST', '/api/revert/abort', {}),
    revertContinue: () => req('POST', '/api/revert/continue', {}),
    reset: (ref, mode) => req('POST', '/api/reset', { ref, mode }),
    operationState: () => req('GET', '/api/state/operation'),
    stashList: () => req('GET', '/api/stash'),
    stashPush: (message, includeUntracked) => req('POST', '/api/stash', { message, includeUntracked }),
    stashPop: (ref) => req('POST', '/api/stash/pop', { ref }),
    stashApply: (ref) => req('POST', '/api/stash/apply', { ref }),
    stashDrop: (ref) => req('POST', '/api/stash/drop', { ref }),
    stashShow: (ref) => req('GET', `/api/stash/show?ref=${encodeURIComponent(ref || '')}`),
    health: () => req('GET', '/api/health'),
    duplicates: () => req('GET', '/api/duplicates'),
    gitignore: () => req('GET', '/api/gitignore'),
    addGitignore: (rules) => req('POST', '/api/gitignore', { rules }),
    deps: () => req('GET', '/api/deps'),
    reflog: () => req('GET', '/api/reflog'),
    reflogReset: (hash, mode) => req('POST', '/api/reflog/reset', { hash, mode }),
    tags: () => req('GET', '/api/tags'),
    createTag: (name, message, ref) => req('POST', '/api/tags', { name, message, ref }),
    deleteTag: (name) => req('POST', '/api/tags/delete', { name }),
    pushTag: (name, remote) => req('POST', '/api/tags/push', { name, remote }),
    remotes: () => req('GET', '/api/remotes'),
    addRemote: (name, url) => req('POST', '/api/remotes', { name, url }),
    deleteRemote: (name) => req('POST', '/api/remotes/delete', { name }),
    fetchRemote: (name) => req('POST', '/api/remotes/fetch', { name }),
    pullRemote: (name) => req('POST', '/api/remotes/pull', { name }),
    pushRemote: (name, branch) => req('POST', '/api/remotes/push', { name, branch }),
    bisectState: () => req('GET', '/api/bisect'),
    bisectStart: () => req('POST', '/api/bisect/start', {}),
    bisectMark: (kind) => req('POST', '/api/bisect/mark', { kind }),
    bisectReset: () => req('POST', '/api/bisect/reset', {}),
    conflicts: () => req('GET', '/api/conflicts'),
    conflictRead: (file) => req('GET', `/api/conflicts/read?file=${encodeURIComponent(file)}`),
    conflictResolve: (file, resolution) => req('POST', '/api/conflicts/resolve', { file, resolution }),
    snapshots: () => req('GET', '/api/snapshots'),
    snapshotCreate: (label) => req('POST', '/api/snapshots', { label }),
    snapshotRestore: (id) => req('POST', '/api/snapshots/restore', { id }),
    snapshotDelete: (id) => req('POST', '/api/snapshots/delete', { id }),
    ownership: () => req('GET', '/api/ownership'),
    changelog: (from, to, version) => {
      const p = new URLSearchParams();
      if (from) p.set('from', from);
      if (to) p.set('to', to);
      if (version) p.set('version', version);
      return req('GET', `/api/changelog?${p}`);
    },
    github: () => req('GET', '/api/github'),
    githubMe: () => req('GET', '/api/auth/github/me'),
    githubCommitAvatars: () => req('GET', '/api/github/commit-avatars'),
    githubAuthStart: () => req('POST', '/api/auth/github/start', {}),
    githubAuthPoll: (device_code) => req('POST', '/api/auth/github/poll', { device_code }),
    githubManual: (token) => req('POST', '/api/auth/github/manual', { token }),
    githubLogout: () => req('POST', '/api/auth/github/logout', {}),
    saveSettings: (settings) => req('POST', '/api/settings', settings),
    aiStatus: () => req('GET', '/api/ai/status'),
    aiAsk: (prompt, context) => req('POST', '/api/ai/ask', { prompt, context }),
    repos: () => req('GET', '/api/repos'),
    currentRepo: () => req('GET', '/api/repos/current'),
    activateRepo: (repo) => req('POST', '/api/repos/activate', { repo }),
    addRepo: (path) => req('POST', '/api/repos', { path }),
    deleteRepo: (path) => req('DELETE', '/api/repos', { path }),
    terminal: (command) => req('POST', '/api/terminal', { command })
    ,indexStatus: (repo) => req('GET', `/api/index/status${repo ? `?repo=${encodeURIComponent(repo)}` : ''}`)
    ,indexRebuild: () => req('POST', '/api/index/rebuild', {})
    ,indexSymbols: (p) => req('GET', `/api/index/symbols?path=${encodeURIComponent(p)}`)
    ,searchSymbols: (q) => req('GET', `/api/index/symbols/search?q=${encodeURIComponent(q)}`)
  };
})();

// Live file watcher (SSE)
const Watcher = (() => {
  let es = null;
  const listeners = new Set();
  function connect() {
    if (es) return;
    try {
      es = new EventSource('/api/watch');
      es.onmessage = (e) => {
        try { const data = JSON.parse(e.data); listeners.forEach((fn) => fn(data)); } catch (_) {}
      };
      es.onerror = () => { try { es.close(); } catch (_) {} es = null; setTimeout(connect, 5000); };
    } catch (_) {}
  }
  return {
    on(fn) { listeners.add(fn); connect(); return () => listeners.delete(fn); }
  };
})();