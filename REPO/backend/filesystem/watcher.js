const fs = require('fs');
const path = require('path');
const { EventEmitter } = require('events');

class RepoWatcher extends EventEmitter {
  constructor(repoRoot, ignoreDirs) {
    super();
    this.repoRoot = repoRoot;
    this.ignoreDirs = new Set(ignoreDirs);
    this.watchers = new Map();
    this.debounceTimer = null;
    this.pending = new Set();
  }
  start() { this.watchDir(this.repoRoot); }
  watchDir(dir) {
    if (this.watchers.has(dir)) return;
    let w;
    try { w = fs.watch(dir, { persistent: false }, (event, name) => this.onEvent(dir, name)); }
    catch (e) { return; }
    this.watchers.set(dir, w);
    let entries;
    try { entries = fs.readdirSync(dir); } catch (e) { return; }
    for (const name of entries) {
      if (this.ignoreDirs.has(name)) continue;
      const full = path.join(dir, name);
      let st; try { st = fs.statSync(full); } catch (e) { continue; }
      if (st.isDirectory()) this.watchDir(full);
    }
  }
  onEvent(dir, name) {
    if (!name) return;
    if (this.ignoreDirs.has(name)) return;
    const full = path.join(dir, name);
    const rel = path.relative(this.repoRoot, full).split(path.sep).join('/');
    this.pending.add(rel);
    clearTimeout(this.debounceTimer);
    this.debounceTimer = setTimeout(() => {
      const files = Array.from(this.pending);
      this.pending.clear();
      this.emit('change', files);
    }, 250);
  }
}
module.exports = { RepoWatcher };