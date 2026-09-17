const FilesView = {
  async render(root) {
    root.innerHTML = `
      <h1>FILES</h1>
      <div class="panel" style="padding:12px">
        <div class="row mb-12">
          <input type="text" id="file-filter" placeholder="Filter paths…" />
        </div>
        <div id="file-tree"></div>
      </div>
    `;
    const treeEl = document.getElementById('file-tree');
    createFileTree(treeEl, (p) => inspectFile(p));
  }
};