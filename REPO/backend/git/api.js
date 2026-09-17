const https = require('https');

function ghRequest(token, path) {
  return new Promise((resolve, reject) => {
    const opts = {
      hostname: 'api.github.com',
      path,
      headers: {
        'User-Agent': 'Repo-Command-Center',
        'Accept': 'application/vnd.github+json',
        'Authorization': `Bearer ${token}`
      }
    };
    https.get(opts, (res) => {
      let data = '';
      res.on('data', (c) => data += c);
      res.on('end', () => {
        try {
          const json = JSON.parse(data);
          if (res.statusCode >= 400) return reject(new Error(json.message || `HTTP ${res.statusCode}`));
          resolve(json);
        } catch (e) { reject(e); }
      });
    }).on('error', reject);
  });
}

function parseRepoUrl(url) {
  if (!url) return null;
  const m = url.match(/github\.com[:/]([^/]+)\/([^/.]+?)(?:\.git)?$/);
  if (!m) return null;
  return { owner: m[1], repo: m[2] };
}

async function getRepoInfo(git, token, configuredRepo) {
  if (!token) throw new Error('GitHub token not configured. Set "githubToken" in config.json.');
  let owner, repo;
  if (configuredRepo) {
    const m = configuredRepo.match(/^([^/]+)\/(.+)$/);
    if (m) { owner = m[1]; repo = m[2]; }
  }
  if (!owner) {
    try {
      const remote = (await git.raw(['remote', 'get-url', 'origin'])).trim();
      const p = parseRepoUrl(remote);
      if (p) { owner = p.owner; repo = p.repo; }
    } catch (e) {}
  }
  if (!owner) throw new Error('Could not determine repo owner. Set "githubRepo": "owner/name" in config.json.');

  const [info, issues, prs, actions] = await Promise.all([
    ghRequest(token, `/repos/${owner}/${repo}`),
    ghRequest(token, `/repos/${owner}/${repo}/issues?state=open&per_page=20`).catch(() => []),
    ghRequest(token, `/repos/${owner}/${repo}/pulls?state=open&per_page=20`).catch(() => []),
    ghRequest(token, `/repos/${owner}/${repo}/actions/runs?per_page=10`).catch(() => ({ workflow_runs: [] }))
  ]);
  return {
    owner, repo,
    description: info.description,
    stars: info.stargazers_count,
    forks: info.forks_count,
    openIssues: info.open_issues_count,
    defaultBranch: info.default_branch,
    url: info.html_url,
    issues: issues.filter((i) => !i.pull_request).map((i) => ({
      number: i.number, title: i.title, user: i.user.login, state: i.state,
      created: i.created_at, labels: i.labels.map((l) => l.name), url: i.html_url
    })),
    prs: prs.map((p) => ({
      number: p.number, title: p.title, user: p.user.login, state: p.state,
      created: p.created_at, head: p.head.ref, base: p.base.ref, url: p.html_url, draft: p.draft
    })),
    runs: (actions.workflow_runs || []).map((r) => ({
      id: r.id, name: r.name, status: r.status, conclusion: r.conclusion,
      branch: r.head_branch, created: r.created_at, url: r.html_url
    }))
  };
}
module.exports = { getRepoInfo, ghRequest, parseRepoUrl };


async function getCommitAvatars(git, token, configuredRepo, maxPages = 3) {
  if (!token) return {};
  let owner, repo;
  if (configuredRepo) {
    const m = configuredRepo.match(/^([^/]+)\/(.+)$/);
    if (m) { owner = m[1]; repo = m[2]; }
  }
  if (!owner) {
    try {
      const remote = (await git.raw(['remote', 'get-url', 'origin'])).trim();
      const p = parseRepoUrl(remote);
      if (p) { owner = p.owner; repo = p.repo; }
    } catch (e) {}
  }
  if (!owner) return {};

  const map = {};
  for (let page = 1; page <= maxPages; page++) {
    let commits;
    try { commits = await ghRequest(token, `/repos/${owner}/${repo}/commits?per_page=100&page=${page}`); }
    catch (e) { break; }
    if (!commits.length) break;
    for (const c of commits) {
      const avatarUrl = c.author?.avatar_url || c.committer?.avatar_url;
      if (avatarUrl) { map[c.sha] = avatarUrl; map[c.sha.slice(0, 7)] = avatarUrl; }
    }
    if (commits.length < 100) break;
  }
  return map;
}
module.exports = { getRepoInfo, ghRequest, parseRepoUrl, getCommitAvatars };