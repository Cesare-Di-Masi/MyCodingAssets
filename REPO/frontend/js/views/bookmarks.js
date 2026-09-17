const BookmarksView = {
  async render(root) {
    const bookmarks = JSON.parse(localStorage.getItem('rcc-bookmarks') || '[]');
    root.innerHTML = `<h1>BOOKMARKS</h1><div class="panel" id="bookmarks-list">${bookmarks.length ? bookmarks.map((p) => `<div class="commit-row" data-bookmark="${escapeHtml(p)}"><span>${fileIcon(p)}</span><span>${escapeHtml(p)}</span><button class="sm danger" data-remove="${escapeHtml(p)}" style="margin-left:auto">REMOVE</button></div>`).join('') : '<div class="empty-state">No bookmarked files.</div>'}</div>`;
    root.querySelectorAll('[data-bookmark]').forEach((el) => el.addEventListener('click', (e) => { if (!e.target.closest('[data-remove]')) Tabs.open('file', { path: el.getAttribute('data-bookmark') }); }));
    root.querySelectorAll('[data-remove]').forEach((el) => el.addEventListener('click', () => { const next = bookmarks.filter((p) => p !== el.getAttribute('data-remove')); localStorage.setItem('rcc-bookmarks', JSON.stringify(next)); BookmarksView.render(root); }));
  }
};