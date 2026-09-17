function renderDiff(diffText) {
  if (!diffText || !diffText.trim()) return '<div class="empty-state">No differences.</div>';
  const lines = diffText.split('\n');
  return '<div class="diff-content">' + lines.map((line) => {
    let cls = 'diff-line';
    if (line.startsWith('+++') || line.startsWith('---')) cls += ' diff-meta';
    else if (line.startsWith('@@')) cls += ' diff-hunk';
    else if (line.startsWith('+')) cls += ' diff-add';
    else if (line.startsWith('-')) cls += ' diff-del';
    else if (line.startsWith('diff ')) cls += ' diff-meta';
    return `<div class="${cls}">${escapeHtml(line)}</div>`;
  }).join('') + '</div>';
}

/* Side-by-side diff. Parses unified diff into paired left/right rows. */
function renderSplitDiff(diffText) {
  if (!diffText || !diffText.trim()) return '<div class="empty-state">No differences.</div>';
  const lines = diffText.split('\n');
  const left = [];   // { ln, txt, kind }
  const right = [];
  let leftLn = 0, rightLn = 0;

  let i = 0;
  while (i < lines.length) {
    const line = lines[i];

    if (line.startsWith('diff ') || line.startsWith('index ') || line.startsWith('---') || line.startsWith('+++')) {
      left.push({ ln: '', txt: line, kind: 'hunk' });
      right.push({ ln: '', txt: line, kind: 'hunk' });
      i++;
      continue;
    }
    if (line.startsWith('@@')) {
      const m = line.match(/^@@ -(\d+)(?:,\d+)? \+(\d+)(?:,\d+)? @@/);
      if (m) { leftLn = Number(m[1]); rightLn = Number(m[2]); }
      left.push({ ln: '', txt: line, kind: 'hunk' });
      right.push({ ln: '', txt: line, kind: 'hunk' });
      i++;
      continue;
    }

    // Gather a run of +/- until context
    const dels = [];
    const adds = [];
    while (i < lines.length && (lines[i].startsWith('+') || lines[i].startsWith('-'))) {
      if (lines[i].startsWith('-')) dels.push(lines[i].slice(1));
      else if (lines[i].startsWith('+')) adds.push(lines[i].slice(1));
      i++;
    }

    const max = Math.max(dels.length, adds.length);
    for (let k = 0; k < max; k++) {
      if (dels[k] !== undefined) left.push({ ln: leftLn++, txt: dels[k], kind: 'del' });
      else left.push({ ln: '', txt: '', kind: 'empty' });
      if (adds[k] !== undefined) right.push({ ln: rightLn++, txt: adds[k], kind: 'add' });
      else right.push({ ln: '', txt: '', kind: 'empty' });
    }
    if (max > 0) continue;

    // Context line
    if (line.startsWith(' ')) {
      const content = line.slice(1);
      left.push({ ln: leftLn++, txt: content, kind: 'ctx' });
      right.push({ ln: rightLn++, txt: content, kind: 'ctx' });
      i++;
      continue;
    }

    // Backslash "no newline at end of file" marker
    left.push({ ln: '', txt: line, kind: 'ctx' });
    right.push({ ln: '', txt: line, kind: 'ctx' });
    i++;
  }

  const renderPane = (rows) => `<div class="pane">
    ${rows.map((r) => `<div class="row ${r.kind}">
      <div class="ln">${r.ln}</div>
      <div class="txt">${escapeHtml(r.txt)}</div>
    </div>`).join('')}
  </div>`;

  return `<div class="split-diff">${renderPane(left)}${renderPane(right)}</div>`;
}