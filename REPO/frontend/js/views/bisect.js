const BisectView = {
  async render(root) {
    root.innerHTML = `
      <h1>BISECT</h1>
      <div class="panel" id="bisect-status"><div class="empty-state">Checking state…</div></div>`;
    const panel = document.getElementById('bisect-status');

    async function load() {
      const s = await Api.bisectState();
      if (!s.active) {
        panel.innerHTML = `
          <div class="text-dim mb-12">Guided binary search: find the commit that broke something.</div>
          <button id="bisect-start" class="primary">START BISECT</button>`;
        document.getElementById('bisect-start').addEventListener('click', async () => {
          try { await Api.bisectStart(); toast('Bisect started'); load(); } catch (e) { toast(e.message, 'error'); }
        });
        return;
      }
      let head = '';
      try { head = (await Api.status()).current; } catch (e) {}
      panel.innerHTML = `
        <div class="mb-12">Bisect in progress on <span class="commit-hash">${escapeHtml(head || 'HEAD')}</span></div>
        <div class="text-dim mb-12" style="font-size:11px">Test the current checkout. Mark it GOOD (works), BAD (broken), or SKIP.</div>
        <div class="row" style="gap:8px;flex-wrap:wrap">
          <button id="b-good" class="primary">MARK GOOD</button>
          <button id="b-bad" class="danger">MARK BAD</button>
          <button id="b-skip">SKIP</button>
          <button id="b-reset">RESET</button>
        </div>
        ${s.log?.length ? `<div class="text-dim" style="margin-top:12px;font-size:11px">${s.log.map(l => `${l.kind} @ ${l.at}`).join('<br>')}</div>` : ''}`;
      const mark = async (kind) => {
        try {
          const r = await Api.bisectMark(kind);
          if (r.complete && r.culprit) {
            toast(`Culprit: ${r.culprit.slice(0, 7)}`, 'warn', 8000);
            await Api.bisectReset();
          } else toast(`Marked ${kind}`);
          load(); App.refreshTopbar();
        } catch (e) { toast(e.message, 'error'); }
      };
      document.getElementById('b-good').addEventListener('click', () => mark('good'));
      document.getElementById('b-bad').addEventListener('click', () => mark('bad'));
      document.getElementById('b-skip').addEventListener('click', () => mark('skip'));
      document.getElementById('b-reset').addEventListener('click', async () => {
        try { await Api.bisectReset(); toast('Bisect reset'); load(); } catch (e) { toast(e.message, 'error'); }
      });
    }
    load();
  }
};