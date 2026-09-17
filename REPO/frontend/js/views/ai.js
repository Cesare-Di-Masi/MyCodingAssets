const AiView = {
  async render(root) {
    root.innerHTML = `
      <h1>AI ASSISTANT</h1>
      <div class="panel" id="ai-status"><div class="empty-state">Checking Ollama…</div></div>
      <div class="panel">
        <div class="row mb-8">
          <textarea id="ai-prompt" rows="3" placeholder="Ask something about the repository…"></textarea>
        </div>
        <div class="row" style="gap:8px;flex-wrap:wrap">
          <button id="ai-ask" class="primary">ASK</button>
          <button id="ai-context-status">+ STATUS</button>
          <button id="ai-context-health">+ HEALTH</button>
          <button id="ai-context-insights">+ INSIGHTS</button>
        </div>
      </div>
      <div class="panel"><pre id="ai-output" class="file-content" style="max-height:500px">Ready.</pre></div>`;
    let contextParts = [];
    try {
      const s = await Api.aiStatus();
      const el = document.getElementById('ai-status');
      if (!s.ok) {
        el.innerHTML = `<div class="text-warn">Ollama not detected at configured URL. Install it from <a href="https://ollama.com" target="_blank">ollama.com</a> and run <span class="mono">ollama pull llama3.2</span>.</div>`;
      } else {
        el.innerHTML = `<div class="text-accent">✓ Ollama ready · models: ${escapeHtml(s.models.join(', ') || 'none')}</div>`;
      }
    } catch (e) { document.getElementById('ai-status').innerHTML = `<div class="text-danger">${escapeHtml(e.message)}</div>`; }

    async function ask() {
      const prompt = document.getElementById('ai-prompt').value.trim();
      if (!prompt) return;
      document.getElementById('ai-output').textContent = 'Thinking…';
      try {
        const r = await Api.aiAsk(prompt, contextParts.join('\n\n'));
        document.getElementById('ai-output').textContent = r.answer;
      } catch (e) { document.getElementById('ai-output').textContent = 'Error: ' + e.message; }
    }
    async function addContext(kind) {
      try {
        if (kind === 'status') {
          const s = await Api.status();
          contextParts.push('GIT STATUS:\n' + JSON.stringify(s, null, 2));
        } else if (kind === 'health') {
          const h = await Api.health();
          contextParts.push('HEALTH:\n' + JSON.stringify({ todos: h.todos.slice(0, 30), secrets: h.secrets.slice(0, 30) }, null, 2));
        } else if (kind === 'insights') {
          const i = await Api.insights(90);
          contextParts.push('INSIGHTS (90d):\n' + JSON.stringify({ byKind: i.byKind, byAuthor: i.byAuthor.slice(0, 10) }, null, 2));
        }
        toast('Context added');
      } catch (e) { toast(e.message, 'error'); }
    }
    document.getElementById('ai-ask').addEventListener('click', ask);
    document.getElementById('ai-context-status').addEventListener('click', () => addContext('status'));
    document.getElementById('ai-context-health').addEventListener('click', () => addContext('health'));
    document.getElementById('ai-context-insights').addEventListener('click', () => addContext('insights'));
  }
};