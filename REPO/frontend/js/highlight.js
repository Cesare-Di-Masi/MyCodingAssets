const Highlight = (() => {
  const EXT_TO_PRISM = {
    js: 'javascript', mjs: 'javascript', cjs: 'javascript',
    jsx: 'jsx', ts: 'typescript', tsx: 'tsx',
    json: 'json', md: 'markdown', markdown: 'markdown',
    html: 'markup', htm: 'markup', xml: 'markup', svg: 'markup',
    css: 'css', scss: 'css', sass: 'css', less: 'css',
    py: 'python', rb: 'ruby', go: 'go', rs: 'rust',
    java: 'java', kt: 'kotlin', kts: 'kotlin',
    cs: 'csharp', cpp: 'cpp', cc: 'cpp', cxx: 'cpp', c: 'c', h: 'c', hpp: 'cpp',
    php: 'php', swift: 'swift', scala: 'scala', lua: 'lua',
    sh: 'bash', bash: 'bash', zsh: 'bash', fish: 'bash',
    yml: 'yaml', yaml: 'yaml', toml: 'toml', ini: 'ini',
    sql: 'sql', diff: 'diff', patch: 'diff',
    dockerfile: 'docker'
  };

  function langFor(name) {
    const base = String(name || '').split('/').pop().toLowerCase();
    if (base === 'dockerfile') return 'docker';
    if (base === 'makefile') return 'makefile';
    const ext = base.split('.').pop();
    return EXT_TO_PRISM[ext] || null;
  }

  function hasPrism() {
    return typeof window.Prism !== 'undefined' && window.Prism.highlight;
  }

  function highlight(code, lang) {
    if (!hasPrism() || !lang) return escapeHtml(code);
    try {
      const grammar = Prism.languages[lang];
      if (!grammar) return escapeHtml(code);
      return Prism.highlight(code, grammar, lang);
    } catch (e) {
      return escapeHtml(code);
    }
  }

  return { langFor, highlight, hasPrism };
})();