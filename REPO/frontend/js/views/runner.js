const RunnerView = {
  async render(root) {
    root.innerHTML = `
      <h1>RUNNER</h1>
      <div class="panel mb-12">
        <div class="row wrap" style="gap:10px;align-items:center">
          <span class="text-dim">Detected commands</span>
          <button id="runner-refresh" class="sm">Refresh</button>
        </div>
        <div id="runner-command-list" class="mt-12" style="display:grid;gap:10px"></div>
      </div>
      <div class="panel">
        <div class="row" style="justify-content:space-between;align-items:center;gap:12px">
          <strong>Output</strong>
          <div class="row" style="gap:8px">
            <button id="runner-stop" class="sm danger">Stop</button>
            <button id="runner-clear" class="sm">Clear</button>
          </div>
        </div>
        <pre id="runner-output" class="mono" style="margin-top:12px;max-height:420px;overflow:auto;white-space:pre-wrap">No run started.</pre>
      </div>
    `;

    const list = document.getElementById('runner-command-list');
    const output = document.getElementById('runner-output');
    const stopBtn = document.getElementById('runner-stop');
    const clearBtn = document.getElementById('runner-clear');
    const refreshBtn = document.getElementById('runner-refresh');
    let currentRunId = null;
    let currentCmd = null;

    function setOutput(text) {
      output.textContent = text;
      output.scrollTop = output.scrollHeight;
    }

    function renderCommands(commands) {
      list.innerHTML = commands.length
        ? commands.map((cmd) => `
            <button class="runner-command" data-cmd="${escapeHtml(cmd.cmd)}" data-args="${escapeHtml(JSON.stringify(cmd.args || []))}" data-cwd="${escapeHtml(cmd.cwd || '.')}" style="width:100%;justify-content:space-between;text-align:left">
              <span>${escapeHtml(cmd.label)}</span>
              <span class="text-dim mono">${escapeHtml(cmd.cmd)} ${escapeHtml((cmd.args || []).join(' '))}</span>
            </button>`).join('')
        : '<div class="empty-state">No commands detected for this repo.</div>';

      list.querySelectorAll('.runner-command').forEach((btn) => {
        btn.addEventListener('click', async () => {
          const cmd = btn.getAttribute('data-cmd');
          const args = JSON.parse(btn.getAttribute('data-args') || '[]');
          const cwd = btn.getAttribute('data-cwd') || '.';
          currentCmd = { cmd, args, cwd };
          try {
            const res = await Api.run(cmd, args, cwd);
            currentRunId = res.runId;
            setOutput(`$ ${cmd} ${args.join(' ')}\n`);
            await streamRun(res.runId);
          } catch (e) {
            setOutput(`error: ${e.message}\n`);
          }
        });
      });
    }

    async function refresh() {
      try {
        const data = await Api.runDetect();
        renderCommands(data.commands || []);
      } catch (e) {
        list.innerHTML = `<div class="empty-state text-danger">${escapeHtml(e.message)}</div>`;
      }
    }

    async function streamRun(runId) {
      const stream = new EventSource(`/api/run/stream/${runId}`);
      stream.addEventListener('meta', (event) => {
        const meta = JSON.parse(event.data);
        setOutput(`$ ${meta.cmd} ${meta.args.join(' ')}\n`);
      });
      stream.addEventListener('message', (event) => {
        const msg = JSON.parse(event.data);
        if (msg && msg.text) setOutput((output.textContent || '') + msg.text);
      });
      stream.addEventListener('exit', (event) => {
        const exit = JSON.parse(event.data);
        setOutput((output.textContent || '') + `\nexit ${exit.exitCode ?? 'unknown'}\n`);
        stream.close();
      });
      stream.onerror = () => { stream.close(); };
    }

    stopBtn.addEventListener('click', async () => {
      if (!currentRunId) {
        toast('No active run to stop.', 'error');
        return;
      }
      try {
        await Api.stopRun(currentRunId);
        toast('Runner stop requested');
      } catch (e) {
        toast(e.message, 'error');
      }
    });

    clearBtn.addEventListener('click', () => setOutput('No run started.'));
    refreshBtn.addEventListener('click', () => refresh());
    await refresh();
  }
};
