function createFileTree(container, onSelectFile) {
  async function renderDir(relPath, depth, parentEl) {
    const data = await Api.listFiles(relPath);
    for (const entry of data.entries) {
      const row = document.createElement('div');
      row.className = `tree-node ${entry.isDir ? 'dir' : 'file'}`;
      row.setAttribute('role', 'button');
      row.tabIndex = 0;
      row.style.paddingLeft = `${depth * 14 + 8}px`;
      row.innerHTML = `
        <span class="icon">${entry.isDir
          ? '<svg style="width:13px;height:13px" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="m9 18 6-6-6-6"/></svg>'
          : fileIcon(entry.name, 13)}</span>
        <span>${escapeHtml(entry.name)}</span>`;
      parentEl.appendChild(row);
      if (entry.isDir) {
        let expanded = false;
        const childWrap = document.createElement('div');
        parentEl.appendChild(childWrap);
        const toggleDir = async () => {
          expanded = !expanded;
          row.querySelector('.icon').innerHTML = expanded
            ? '<svg style="width:13px;height:13px" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="m6 9 6 6 6-6"/></svg>'
            : '<svg style="width:13px;height:13px" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="m9 18 6-6-6-6"/></svg>';
          if (expanded && childWrap.childElementCount === 0) await renderDir(entry.path, depth + 1, childWrap);
          else if (!expanded) childWrap.innerHTML = '';
        };
        row.addEventListener('click', toggleDir);
        row.addEventListener('keydown', (event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); toggleDir(); } });
      } else {
        const openFile = () => onSelectFile(entry.path);
        row.addEventListener('click', openFile);
        row.addEventListener('keydown', (event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); openFile(); } });
      }
    }
  }
  container.innerHTML = '';
  renderDir('.', 0, container);
}