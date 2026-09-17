const Theme = (() => {
  const themes = ['dark', 'light', 'contrast'];
  function apply(theme) {
    const resolved = theme === 'system' ? (matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark') : theme;
    document.documentElement.dataset.theme = resolved;
    localStorage.setItem('rcc-theme', theme);
  }
  function cycle() {
    const current = localStorage.getItem('rcc-theme') || 'dark';
    apply(themes[(themes.indexOf(current) + 1) % themes.length]);
  }
  const saved = localStorage.getItem('rcc-theme');
  apply(saved || (matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark'));
  return { apply, cycle };
})();
