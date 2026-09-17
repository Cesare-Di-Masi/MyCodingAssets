const fs = require('fs');
const path = require('path');
const { DatabaseSync } = require('node:sqlite');

let connection = null;
let currentRoot = null;

function schema(db) {
  db.exec('PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON');
  db.exec(`
    CREATE TABLE IF NOT EXISTS files (
      path TEXT PRIMARY KEY, size INTEGER NOT NULL DEFAULT 0, mtime INTEGER NOT NULL DEFAULT 0,
      ext TEXT NOT NULL DEFAULT '', language TEXT NOT NULL DEFAULT '', hash_sha1 TEXT,
      is_text INTEGER NOT NULL DEFAULT 0, lines INTEGER NOT NULL DEFAULT 0
    );
    CREATE TABLE IF NOT EXISTS dirs (path TEXT PRIMARY KEY, mtime INTEGER NOT NULL DEFAULT 0);
    CREATE TABLE IF NOT EXISTS todos (id INTEGER PRIMARY KEY AUTOINCREMENT, path TEXT, line INTEGER, tag TEXT, text TEXT, indexed_at TEXT);
    CREATE TABLE IF NOT EXISTS secrets (id INTEGER PRIMARY KEY AUTOINCREMENT, path TEXT, line INTEGER, kind TEXT, indexed_at TEXT);
    CREATE TABLE IF NOT EXISTS commits (hash TEXT PRIMARY KEY, author TEXT, email TEXT, date TEXT, subject TEXT, body TEXT, kind TEXT);
    CREATE TABLE IF NOT EXISTS commit_files (commit_hash TEXT, path TEXT, additions INTEGER, deletions INTEGER, PRIMARY KEY(commit_hash, path));
    CREATE TABLE IF NOT EXISTS symbols (id INTEGER PRIMARY KEY AUTOINCREMENT, path TEXT, line INTEGER, kind TEXT, name TEXT);
    CREATE VIRTUAL TABLE IF NOT EXISTS file_fts USING fts5(path UNINDEXED, content);
    CREATE TABLE IF NOT EXISTS meta (key TEXT PRIMARY KEY, value TEXT);
    CREATE INDEX IF NOT EXISTS idx_files_ext ON files(ext);
    CREATE INDEX IF NOT EXISTS idx_todos_path ON todos(path);
    CREATE INDEX IF NOT EXISTS idx_secrets_path ON secrets(path);
    CREATE INDEX IF NOT EXISTS idx_commits_date ON commits(date);
    CREATE INDEX IF NOT EXISTS idx_commit_files_path ON commit_files(path);
    CREATE INDEX IF NOT EXISTS idx_symbols_path ON symbols(path);
    CREATE INDEX IF NOT EXISTS idx_symbols_name ON symbols(name);
  `);
  db.prepare("INSERT OR IGNORE INTO meta (key, value) VALUES ('schema_version', '1')").run();
}

function open(repoRoot) {
  if (connection && currentRoot === repoRoot) return connection;
  if (connection) { try { connection.close(); } catch (e) {} }
  const dir = path.join(repoRoot, '.rcc-index');
  fs.mkdirSync(dir, { recursive: true });
  connection = new DatabaseSync(path.join(dir, 'index.db'));
  currentRoot = repoRoot;
  schema(connection);
  return connection;
}

function close() { if (connection) connection.close(); connection = null; currentRoot = null; }
function transaction(dbOrFn, maybeFn) {
  const db = typeof dbOrFn === 'function' ? connection : dbOrFn;
  const fn = typeof dbOrFn === 'function' ? dbOrFn : maybeFn;
  if (!db || typeof fn !== 'function') throw new TypeError('transaction requires a database and function');
  db.exec('BEGIN');
  try { const result = fn(); db.exec('COMMIT'); return result; }
  catch (error) { try { db.exec('ROLLBACK'); } catch (rollbackError) {} throw error; }
}
function get(repoRoot) { return open(repoRoot); }
function getDb(repoRoot) { return open(repoRoot); }
function closeDb() { close(); }
function schemaVersion(repoRoot) { return getMeta(repoRoot, 'schema_version') || '0'; }
function runMigrations(repoRoot) { schema(open(repoRoot)); return schemaVersion(repoRoot); }
function getMeta(repoRoot, key) { return get(repoRoot).prepare('SELECT value FROM meta WHERE key = ?').get(key)?.value || null; }
function setMeta(repoRoot, key, value) { get(repoRoot).prepare('INSERT INTO meta(key,value) VALUES(?,?) ON CONFLICT(key) DO UPDATE SET value=excluded.value').run(key, String(value)); }
function isPopulated(repoRoot) { try { return Boolean(get(repoRoot).prepare('SELECT 1 FROM files LIMIT 1').get()); } catch (e) { return false; } }
function size(repoRoot) { try { return fs.statSync(path.join(repoRoot, '.rcc-index', 'index.db')).size; } catch (e) { return 0; } }

module.exports = { open, get, getDb, close, closeDb, transaction, schemaVersion, runMigrations, getMeta, setMeta, isPopulated, size };