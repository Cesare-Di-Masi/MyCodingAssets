# Repo Command Center — V1

A local-first Git/GitHub repository dashboard. A Node.js/Express backend does the
real filesystem and Git work (via `simple-git`); a vanilla HTML/CSS/JS frontend
gives you a fast, keyboard-friendly terminal-style UI on top of it.

This is **not** a hosted web app — it runs on your machine against a Git
repository on your machine, and talks to `localhost` only.

## What's included (V1 — 7 screens)

1. **Overview** — file/dir/commit counts, size, top languages, largest files, working-tree state
2. **Files** — lazy-loading file tree browser with a file inspector panel
3. **Search** — filename or content search across the whole repo
4. **History** — full commit log, click a commit to see its diff
5. **Changes** — stage/unstage files, view diffs, write commit messages, commit
6. **Branches** — list local/remote branches, checkout with one click
7. **Terminal** — an in-app terminal restricted to a whitelist of `git` subcommands

Plus an early V2 screen — **Repository Health** — that scans for TODO/FIXME
comments, obvious hardcoded secrets, and duplicate files.

## Setup

```bash
cd repo-manager
npm install
```

The index uses Node's built-in `node:sqlite` API. Node.js 22.5 or newer is
required for the index; no native SQLite dependency or C++ toolchain is needed.

Edit `config.json` and point `repoPath` at the Git repository you want to manage:

```json
{
  "repoPath": "/path/to/your/giant/repo",
  "port": 4747
}
```

(`repoPath: "."` manages whatever repo this app itself is copied into, if you
put it inside one.)

Then:

```bash
npm start
```

Open **http://localhost:4747**.

## Notes on the architecture

- The frontend never talks to Git or the filesystem directly — it only calls
  the local backend's REST API (`backend/server.js` and the modules under
  `backend/git/` and `backend/filesystem/`). This is the split described in
  the design doc: HTML/JS is the control surface, Node.js is the engine.
- The in-app **Terminal** only allows a fixed whitelist of `git` subcommands
  (`status`, `log`, `diff`, `branch`, `show`, `blame`, `remote`, `stash`,
  `add`, `reset`, `commit`, `checkout`, `fetch`) — anything else, including
  arbitrary shell commands, is rejected server-side. Tighten or loosen the
  list in `ALLOWED_GIT_SUBCOMMANDS` at the top of `backend/server.js`.
- Large-repo safety: the index in `.rcc-index/index.db` stores files, content,
  TODOs, secrets, symbols, and commit metadata. It is rebuilt in the background
  on first start and refreshed incrementally; indexed endpoints fall back to
  filesystem scans if the database is unavailable.
- GitHub REST integration (issues/PRs/Actions) and the AI assistant layer
  from the design docs are intentionally **not** in V1 — the doc's own
  advice was to get the local Git/filesystem core solid first. The `GitHub`
  nav item, dependency graphs, snapshots, etc. are natural V2/V3 additions
  on top of this same backend shape (`backend/github/` would slot in next
  to `backend/git/`).

## Folder structure

```
repo-manager/
├── backend/
│   ├── server.js          Express app + all API routes
│   ├── git/                status, history, diff, commit, branches, operations
│   ├── index/              node:sqlite database, indexer, and queries
│   ├── auth/               local GitHub credential store and device flow
│   └── filesystem/         explorer, search, analyzer, watcher
├── frontend/
│   ├── index.html
│   ├── css/main.css
│   └── js/
│       ├── api.js, state.js, app.js
│       ├── views/          one file per screen
│       └── components/     fileTree, diffViewer
├── config.json
└── package.json
```
