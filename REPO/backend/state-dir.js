const fs = require('fs');
const os = require('os');
const path = require('path');

function getBaseDirectory() {
  if (process.env.LOCALAPPDATA) return process.env.LOCALAPPDATA;
  if (process.env.XDG_DATA_HOME) return process.env.XDG_DATA_HOME;
  if (process.platform === 'win32') return path.join(os.homedir(), 'AppData', 'Local');
  if (process.platform === 'darwin') return path.join(os.homedir(), 'Library', 'Application Support');
  return path.join(os.homedir(), '.local', 'share');
}

const STATE_DIR = path.resolve(getBaseDirectory(), 'RepoCommandCenter');

function ensureStateDirectory() {
  fs.mkdirSync(STATE_DIR, { recursive: true, mode: 0o700 });
  if (process.platform !== 'win32') {
    try { fs.chmodSync(STATE_DIR, 0o700); } catch (e) {}
  }
}

function stateFile(name) {
  if (typeof name !== 'string' || path.basename(name) !== name) throw new Error('Invalid state filename');
  return path.join(STATE_DIR, name);
}

module.exports = { STATE_DIR, ensureStateDirectory, stateFile };
