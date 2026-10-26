#!/usr/bin/env node
'use strict';

const fs = require('node:fs');
const path = require('node:path');

const repositoryRoot = path.resolve(__dirname, '..', '..');
const outputDirectory = __dirname;
const treeOutput = path.join(outputDirectory, 'Repository-Tree.md');
const countsOutput = path.join(outputDirectory, 'Repository-Counts.json');
const excludedDirectoryNames = new Set(['.git', 'node_modules', '.rcc-index']);
const generatedFiles = new Set([
  path.relative(repositoryRoot, treeOutput).split(path.sep).join('/'),
  path.relative(repositoryRoot, countsOutput).split(path.sep).join('/'),
]);

const stats = {
  generatedAt: new Date().toISOString(),
  root: path.basename(repositoryRoot),
  directories: 0,
  files: 0,
  symlinks: 0,
  bytes: 0,
  excludedDirectories: 0,
  extensions: Object.create(null),
};

function compareNames(left, right) {
  const lowerLeft = left.toLocaleLowerCase('en');
  const lowerRight = right.toLocaleLowerCase('en');
  return lowerLeft.localeCompare(lowerRight, 'en') || left.localeCompare(right, 'en');
}

function fileExtension(name) {
  const extension = path.extname(name).toLocaleLowerCase('en');
  return extension || '[senza estensione]';
}

function readEntries(directory, relativeDirectory = '') {
  const entries = fs.readdirSync(directory, { withFileTypes: true })
    .filter((entry) => {
      const relativePath = path.posix.join(relativeDirectory.split(path.sep).join('/'), entry.name);
      if (generatedFiles.has(relativePath)) return false;
      if (entry.isDirectory() && excludedDirectoryNames.has(entry.name.toLocaleLowerCase('en'))) {
        stats.excludedDirectories += 1;
        return false;
      }
      return true;
    })
    .sort((left, right) => compareNames(left.name, right.name));

  for (const entry of entries) {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      stats.directories += 1;
      entry.children = readEntries(fullPath, path.join(relativeDirectory, entry.name));
    } else if (entry.isSymbolicLink()) {
      stats.symlinks += 1;
    } else if (entry.isFile()) {
      stats.files += 1;
      const size = fs.statSync(fullPath, { throwIfNoEntry: false })?.size ?? 0;
      stats.bytes += size;
      const extension = fileExtension(entry.name);
      stats.extensions[extension] = (stats.extensions[extension] ?? 0) + 1;
    }
  }

  return entries;
}

function formatTree(entries, prefix = '') {
  const lines = [];
  entries.forEach((entry, index) => {
    const isLast = index === entries.length - 1;
    const branch = isLast ? '└── ' : '├── ';
    const suffix = entry.isDirectory() ? '/' : entry.isSymbolicLink() ? ' @' : '';
    lines.push(`${prefix}${branch}${entry.name}${suffix}`);
    if (entry.isDirectory()) {
      lines.push(...formatTree(entry.children, `${prefix}${isLast ? '    ' : '│   '}`));
    }
  });
  return lines;
}

function main() {
  const entries = readEntries(repositoryRoot);
  const extensionLines = Object.entries(stats.extensions)
    .sort(([left], [right]) => compareNames(left, right))
    .map(([extension, count]) => `| \`${extension}\` | ${count} |`);

  const markdown = [
    `# Albero directory di ${stats.root}`,
    '',
    `Generato automaticamente: ${stats.generatedAt}`,
    '',
    'Conteggi (esclusi `.git`, `node_modules` e i due report generati):',
    '',
    `- Directory: ${stats.directories}`,
    `- File: ${stats.files}`,
    `- Collegamenti simbolici: ${stats.symlinks} (elencati ma mai seguiti)`,
    `- Dimensione dei file regolari: ${stats.bytes} byte`,
    `- Directory escluse: ${stats.excludedDirectories}`,
    '',
    '## File per estensione',
    '',
    '| Estensione | File |',
    '| --- | ---: |',
    ...extensionLines,
    '',
    '## Struttura',
    '',
    '```text',
    `${stats.root}/`,
    ...formatTree(entries),
    '```',
    '',
  ].join('\n');

  const json = `${JSON.stringify(stats, null, 2)}\n`;
  fs.writeFileSync(treeOutput, markdown, 'utf8');
  fs.writeFileSync(countsOutput, json, 'utf8');

  console.log(`Directory: ${stats.directories}`);
  console.log(`File: ${stats.files}`);
  console.log(`Collegamenti simbolici: ${stats.symlinks}`);
  console.log(`Dimensione: ${stats.bytes} byte`);
  console.log(`Albero: ${path.relative(repositoryRoot, treeOutput)}`);
  console.log(`Conteggi: ${path.relative(repositoryRoot, countsOutput)}`);
}

try {
  main();
} catch (error) {
  console.error(`Generazione dell'albero non riuscita: ${error.message}`);
  process.exitCode = 1;
}
