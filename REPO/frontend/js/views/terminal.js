const TerminalView = {
  async render(root) {
    root.innerHTML = `
      <h1>TERMINAL</h1>
      <div class="text-dim mb-12" style="font-size:11px">
        Only "git ..." commands are permitted. Try: status, log --oneline -20, diff, branch -a, blame &lt;file&gt;
      </div>
      <div class="panel">
        <div id="terminal-output">$ ready\n</div>
        <div id="terminal-input-row">
          <input type="text" id="terminal-input" placeholder="git status" autocomplete="off" />
          <button id="terminal-run" class="primary">RUN</button>
        </div>
      </div>
    `;

    const output = document.getElementById('terminal-output');
    const input = document.getElementById('terminal-input');
    const runBtn = document.getElementById('terminal-run');
    const history = [];
    let historyIdx = -1;

    function append(text) {
      output.textContent += text;
      output.scrollTop = output.scrollHeight;
    }

    async function run() {
      const cmd = input.value.trim();
      if (!cmd) return;
      history.push(cmd);
      historyIdx = history.length;
      append(`\n$ ${cmd}\n`);
      input.value = '';
      try {
        const res = await Api.terminal(cmd);
        append((res.output || '(no output)') + '\n');
      } catch (e) {
        append(`error: ${e.message}\n`);
      }
    }

    runBtn.addEventListener('click', run);
    input.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') run();
      else if (e.key === 'ArrowUp') {
        if (historyIdx > 0) { historyIdx--; input.value = history[historyIdx] || ''; }
        e.preventDefault();
      } else if (e.key === 'ArrowDown') {
        if (historyIdx < history.length - 1) { historyIdx++; input.value = history[historyIdx] || ''; }
        else { historyIdx = history.length; input.value = ''; }
        e.preventDefault();
      }
    });
    input.focus();
  }
};