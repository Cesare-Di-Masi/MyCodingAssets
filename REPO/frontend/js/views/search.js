const SearchView = {
  async render(root) {
    root.innerHTML = `
      <h1>GLOBAL SEARCH</h1>
      <div class="panel">
        <div class="row mb-12">
          <input type="text" id="search-input" placeholder="Search repository..." />
          <button id="search-btn" class="primary">SEARCH</button>
        </div>
        <div class="row wrap" style="gap:6px">
          <label class="checkbox-row"><input type="radio" name="mode" value="filename" checked /> FILENAME</label>
          <label class="checkbox-row"><input type="radio" name="mode" value="content" /> CONTENT</label>
          <label class="checkbox-row"><input type="radio" name="mode" value="regex" /> REGEX</label>
          <label class="checkbox-row"><input type="radio" name="mode" value="pickaxe" /> PICKAXE (history)</label>
        </div>
      </div>
      <div class="panel" id="search-results"><div class="empty-state">Enter a query.</div></div>`;
    const input = document.getElementById("search-input");
    const resultsEl = document.getElementById("search-results");

    async function runSearch() {
      const q = input.value.trim();
      const mode = root.querySelector("input[name=mode]:checked").value;
      if (!q) return;
      resultsEl.innerHTML = '<div class="empty-state">Searching…</div>';
      try {
        if (mode === "pickaxe") {
          const data = await Api.pickaxe(q);
          if (!data.commits.length)
            return (resultsEl.innerHTML =
              '<div class="empty-state">No matching commits.</div>');
          resultsEl.innerHTML =
            `<div class="text-dim mb-12">${data.commits.length} commits touched "${escapeHtml(q)}"</div>` +
            data.commits
              .map(
                (c) => `<div class="commit-row" data-hash="${c.hash}">
              <span class="commit-hash">${c.short}</span> <span class="commit-msg">${escapeHtml(c.subject)}</span>
              <div class="commit-meta">${escapeHtml(c.author)} · ${fmtDate(c.date)}</div></div>`,
              )
              .join("");
          resultsEl
            .querySelectorAll("[data-hash]")
            .forEach((el) =>
              el.addEventListener("click", () =>
                showCommitInInspector(el.getAttribute("data-hash")),
              ),
            );
          return;
        }
        const data = await Api.search(q, mode);
        if (data.results[0]?.error)
          return (resultsEl.innerHTML = `<div class="empty-state text-danger">${escapeHtml(data.results[0].error)}</div>`);
        if (!data.results.length)
          return (resultsEl.innerHTML =
            '<div class="empty-state">No results.</div>');
        resultsEl.innerHTML =
          `<div class="text-dim mb-12">${data.results.length} result${data.results.length === 1 ? "" : "s"} · ${data.mode}</div>` +
          data.results
            .map((r) => {
              if (r.type === "filename") {
                return `<div class="commit-row" data-path="${escapeHtml(r.path)}" style="grid-template-columns:20px 1fr">
    ${fileIcon(r.path, 14)}
    <span class="link">${escapeHtml(r.path)}</span>
  </div>`;
              }
              return `<div class="commit-row" data-path="${escapeHtml(r.path)}" style="grid-template-columns:20px 1fr">
  ${fileIcon(r.path, 14)}
  <div>
    <div class="link mb-8">${escapeHtml(r.path)}</div>
    ${r.matches.map((m) => `<div class="text-dim mono" style="font-size:11px"><span class="text-accent">${m.line}</span>: ${escapeHtml(m.text)}</div>`).join("")}
  </div>
</div>`;
            })
            .join("");
        resultsEl
          .querySelectorAll("[data-path]")
          .forEach((el) =>
            el.addEventListener("click", () =>
              inspectFile(el.getAttribute("data-path")),
            ),
          );
      } catch (e) {
        resultsEl.innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`;
      }
    }
    document.getElementById("search-btn").addEventListener("click", runSearch);
    input.addEventListener("keydown", (e) => {
      if (e.key === "Enter") runSearch();
    });
    input.focus();
  },
};
