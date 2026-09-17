const ChangelogView = {
  async render(root) {
    root.innerHTML = `
      <h1>CHANGELOG</h1>
      <div class="panel">
        <div class="row" style="gap:8px;flex-wrap:wrap">
          <input type="text" id="cl-from" placeholder="from tag/ref (e.g. v1.0.0)" style="max-width:200px" />
          <input type="text" id="cl-to" placeholder="to (default HEAD)" style="max-width:200px" />
          <input type="text" id="cl-version" placeholder="version label (v2.0.0)" style="max-width:180px" />
          <button id="cl-gen" class="primary">GENERATE</button>
          <button id="cl-copy">COPY</button>
        </div>
      </div>
      <div class="panel"><pre id="cl-output" class="file-content" style="max-height:600px">Ready.</pre></div>`;
    async function run() {
      const from = document.getElementById('cl-from').value.trim();
      const to = document.getElementById('cl-to').value.trim();
      const version = document.getElementById('cl-version').value.trim();
      document.getElementById('cl-output').textContent = 'Generating…';
      try {
        const data = await Api.changelog(from, to, version);
        document.getElementById('cl-output').textContent = data.markdown || '(no commits)';
      } catch (e) { document.getElementById('cl-output').textContent = 'Error: ' + e.message; }
    }
    document.getElementById('cl-gen').addEventListener('click', run);
    document.getElementById('cl-copy').addEventListener('click', () => {
      const text = document.getElementById('cl-output').textContent;
      navigator.clipboard.writeText(text).then(() => toast('Copied to clipboard')).catch(() => toast('Copy failed', 'error'));
    });
  }
};