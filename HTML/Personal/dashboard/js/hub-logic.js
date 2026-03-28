const HUB_REGISTRY = [
    { id: 'dashboard', name: 'Dashboard', file: 'views/dashboard.html', icon: 'bi-grid-1x2-fill', pinned: false, order: 0, category: 'System', desc: 'Main Control Center' },
    { id: 'tasks', name: 'Task Manager', file: 'tools/tasks.html', icon: 'bi-check2-square', pinned: true, order: 1, category: 'Productivity', desc: 'Manage your workflow' },
    { id: 'color-utils', name: 'Color Lab', file: 'tools/color-utils.html', icon: 'bi-palette', pinned: true, order: 2, category: 'Design', desc: 'Advanced color utility' },
    { id: 'logger', name: 'System Logs', file: 'tools/logger.html', icon: 'bi-terminal', pinned: true, order: 3, category: 'System', desc: 'Real-time event monitor' },
    { id: 'notes', name: 'Notes', file: 'tools/notes.html', icon: 'bi-journal', pinned: false, order: 4, category: 'Productivity', desc: 'Quick scratchpad' },
    { id: 'stats', name: 'Settings', file: 'views/stats.html', icon: 'bi-sliders', pinned: false, order: 5, category: 'System', desc: 'System preferences' },
    
    // --- Development (10) ---
    { id: 'json-format', name: 'JSON Formatter', file: 'tools/json-format.html', icon: 'bi-filetype-json', pinned: true, order: 10, category: 'Development', desc: 'Clean/Validate JSON' },
    { id: 'base64', name: 'Base64 Tool', file: 'tools/base64.html', icon: 'bi-hash', pinned: false, order: 11, category: 'Development', desc: 'Encode/Decode Base64' },
    { id: 'jwt-debug', name: 'JWT Debugger', file: 'tools/jwt-debug.html', icon: 'bi-shield-lock', pinned: false, order: 12, category: 'Development', desc: 'Inspect JWT tokens' },
    { id: 'regex-tester', name: 'RegEx Tester', file: 'tools/regex-tester.html', icon: 'bi-regex', pinned: false, order: 13, category: 'Development', desc: 'Live regex testing' },
    { id: 'url-codec', name: 'URL Codec', file: 'tools/url-codec.html', icon: 'bi-link-45deg', pinned: false, order: 14, category: 'Development', desc: 'Encode/Decode URLs' },
    { id: 'markdown-pre', name: 'MD Preview', file: 'tools/markdown-pre.html', icon: 'bi-markdown', pinned: false, order: 15, category: 'Development', desc: 'Live MD editor' },
    { id: 'sql-format', name: 'SQL Formatter', file: 'tools/sql-format.html', icon: 'bi-database', pinned: false, order: 16, category: 'Development', desc: 'Prettify SQL queries' },
    { id: 'code-minify', name: 'Code Minifier', file: 'tools/code-minify.html', icon: 'bi-file-zip', pinned: false, order: 17, category: 'Development', desc: 'Minify HTML/CSS/JS' },
    { id: 'api-tester', name: 'API Tester', file: 'tools/api-tester.html', icon: 'bi-send', pinned: true, order: 18, category: 'Development', desc: 'REST client' },
    { id: 'cron-gen', name: 'Cron Gen', file: 'tools/cron-gen.html', icon: 'bi-clock-history', pinned: false, order: 19, category: 'Development', desc: 'Visual cron builder' },

    // --- Design (10) ---
    { id: 'svg-editor', name: 'SVG Editor', file: 'tools/svg-editor.html', icon: 'bi-vector-pen', pinned: false, order: 20, category: 'Design', desc: 'Edit SVG paths' },
    { id: 'gradient-gen', name: 'Gradient Builder', file: 'tools/gradient-gen.html', icon: 'bi-brush', pinned: false, order: 21, category: 'Design', desc: 'Build CSS gradients' },
    { id: 'font-pair', name: 'Font Pairer', file: 'tools/font-pair.html', icon: 'bi-fonts', pinned: false, order: 22, category: 'Design', desc: 'Preview typography' },
    { id: 'icon-explore', name: 'Icon Explorer', file: 'tools/icon-explore.html', icon: 'bi-search-heart', pinned: false, order: 23, category: 'Design', desc: 'Search UI icons' },
    { id: 'img-compress', name: 'Image Optimizer', file: 'tools/img-compress.html', icon: 'bi-file-earmark-image', pinned: false, order: 24, category: 'Design', desc: 'Compress images' },
    { id: 'unit-conv', name: 'Units Converter', file: 'tools/unit-conv.html', icon: 'bi-rulers', pinned: false, order: 25, category: 'Design', desc: 'PX to REM/EM' },
    { id: 'contrast', name: 'Contrast Check', file: 'tools/contrast.html', icon: 'bi-eye', pinned: false, order: 26, category: 'Design', desc: 'WCAG Accessibility' },
    { id: 'favicon-gen', name: 'Favicon Gen', file: 'tools/favicon-gen.html', icon: 'bi-star', pinned: false, order: 27, category: 'Design', desc: 'Generate web icons' },
    { id: 'placeholder', name: 'Placeholders', file: 'tools/placeholder.html', icon: 'bi-image', pinned: false, order: 28, category: 'Design', desc: 'Mockup images/text' },
    { id: 'shadow-gen', name: 'Shadow Builder', file: 'tools/shadow-gen.html', icon: 'bi-layers', pinned: false, order: 29, category: 'Design', desc: 'Advanced CSS shadows' },

    // --- Security (10) ---
    { id: 'pass-gen', name: 'Password Gen', file: 'tools/pass-gen.html', icon: 'bi-key', pinned: true, order: 30, category: 'Security', desc: 'Secure passwords' },
    { id: 'hash-gen', name: 'Hash Tool', file: 'tools/hash-gen.html', icon: 'bi-fingerprint', pinned: false, order: 31, category: 'Security', desc: 'MD5/SHA generator' },
    { id: 'whois', name: 'Whois Lookup', file: 'tools/whois.html', icon: 'bi-globe', pinned: false, order: 32, category: 'Security', desc: 'Domain info' },
    { id: 'dns-check', name: 'DNS Checker', file: 'tools/dns-check.html', icon: 'bi-hdd-network', pinned: false, order: 33, category: 'Security', desc: 'Lookup DNS records' },
    { id: 'ssl-check', name: 'SSL Checker', file: 'tools/ssl-check.html', icon: 'bi-lock', pinned: false, order: 34, category: 'Security', desc: 'Verify certificates' },
    { id: 'ip-info', name: 'IP Detector', file: 'tools/ip-info.html', icon: 'bi-geo-alt', pinned: true, order: 35, category: 'Security', desc: 'Public IP & Geo' },
    { id: 'headers', name: 'Headers Audit', file: 'tools/headers.html', icon: 'bi-shield-check', pinned: false, order: 36, category: 'Security', desc: 'Security headers' },
    { id: 'totp', name: 'TOTP Test', file: 'tools/totp.html', icon: 'bi-phone-vibrate', pinned: false, order: 37, category: 'Security', desc: '2FA simulator' },
    { id: 'ascii', name: 'ASCII Table', file: 'tools/ascii.html', icon: 'bi-alphabet', pinned: false, order: 38, category: 'Security', desc: 'Encoding reference' },
    { id: 'cors-proxy', name: 'CORS Proxy', file: 'tools/cors-proxy.html', icon: 'bi-shuffle', pinned: false, order: 39, category: 'Security', desc: 'Bypass CORS' },

    // --- Productivity (10) ---
    { id: 'pomodoro', name: 'Pomodoro', file: 'tools/pomodoro.html', icon: 'bi-hourglass-split', pinned: true, order: 40, category: 'Productivity', desc: 'Focus timer' },
    { id: 'kanban', name: 'Kanban', file: 'tools/kanban.html', icon: 'bi-layout-three-columns', pinned: true, order: 41, category: 'Productivity', desc: 'Board management' },
    { id: 'timezone', name: 'Time Zones', file: 'tools/timezone.html', icon: 'bi-clock', pinned: false, order: 42, category: 'Productivity', desc: 'Sync meetings' },
    { id: 'expenses', name: 'Expenses', file: 'tools/expenses.html', icon: 'bi-cash-coin', pinned: false, order: 43, category: 'Productivity', desc: 'Daily ledger' },
    { id: 'habits', name: 'Habit Tracker', file: 'tools/habits.html', icon: 'bi-calendar-check', pinned: false, order: 44, category: 'Productivity', desc: 'Daily streaks' },
    { id: 'vault', name: 'Local Vault', file: 'tools/vault.html', icon: 'bi-safe', pinned: false, order: 45, category: 'Productivity', desc: 'Encrypted storage' },
    { id: 'links', name: 'Link Manager', file: 'tools/links.html', icon: 'bi-bookmarks', pinned: false, order: 46, category: 'Productivity', desc: 'Doc collection' },
    { id: 'calculator', name: 'Calculator', file: 'tools/calculator.html', icon: 'bi-calculator', pinned: false, order: 47, category: 'Productivity', desc: 'Advanced math' },
    { id: 'clipboard', name: 'Clip History', file: 'tools/clipboard.html', icon: 'bi-clipboard-plus', pinned: false, order: 48, category: 'Productivity', desc: 'Saved snippets' },
    { id: 'seo-audit', name: 'SEO Audit', file: 'tools/seo-audit.html', icon: 'bi-search', pinned: false, order: 49, category: 'Productivity', desc: 'Site metrics' }
];

const SORTED_REGISTRY = [...HUB_REGISTRY].sort((a, b) => (a.order - b.order) || a.name.localeCompare(b.name));

async function navigateTo(toolId) {
    const tool = SORTED_REGISTRY.find(t => t.id === toolId);
    if (!tool) return;

    const container = document.getElementById('tool-container');
    window.location.hash = tool.id;

    // Loading Overlay
    container.innerHTML = `
        <div class="d-flex flex-column align-items-center justify-content-center h-100 py-5 animate-fade-in">
            <div class="spinner-border text-primary mb-3" style="width: 3rem; height: 3rem;" role="status"></div>
            <p class="text-muted fw-medium">Loading ${tool.name}...</p>
        </div>
    `;

    // Sidebar active state
    document.querySelectorAll('#hub-menu .nav-link').forEach(link => {
        link.classList.remove('active');
        if (link.getAttribute('data-id') === tool.id) link.classList.add('active');
    });

    try {
        const response = await fetch(tool.file);
        if (!response.ok) throw new Error("Resource not found");
        const html = await response.text();
        
        setTimeout(() => {
            container.innerHTML = html;
            
            // Scripts re-initialization
            container.querySelectorAll("script").forEach(oldScript => {
                const newScript = document.createElement("script");
                Array.from(oldScript.attributes).forEach(attr => newScript.setAttribute(attr.name, attr.value));
                newScript.appendChild(document.createTextNode(oldScript.innerHTML));
                oldScript.parentNode.replaceChild(newScript, oldScript);
            });

            if (toolId === 'dashboard') generateDashboardCards();
        }, 150);
        
    } catch (err) {
        container.innerHTML = `
            <div class="alert alert-danger rounded-4 border-0 shadow-sm p-4 animate-fade-in">
                <h5 class="alert-heading fw-bold"><i class="bi bi-exclamation-triangle-fill me-2"></i>Load Error</h5>
                <p class="mb-0">Failed to load tool: <code>${tool.file}</code>. Please check if the file exists.</p>
            </div>
        `;
    }
}

function generateDashboardCards() {
    const grid = document.getElementById('pinned-tools-grid');
    if (!grid) return;

    const pinnedTools = SORTED_REGISTRY.filter(t => t.pinned);
    grid.innerHTML = pinnedTools.map(tool => `
        <div class="col-xl-4 col-md-6">
            <div class="card border-0 shadow-sm rounded-4 h-100 p-2" 
                 style="background: var(--bg-secondary); cursor: pointer; transition: all 0.3s ease;" 
                 onclick="navigateTo('${tool.id}')">
                <div class="card-body d-flex align-items-center">
                    <div class="bg-dark bg-opacity-5 p-3 rounded-4 me-3">
                        <i class="bi ${tool.icon} fs-3" style="color: var(--text-main);"></i>
                    </div>
                    <div>
                        <h6 class="fw-800 mb-1">${tool.name}</h6>
                        <p class="small text-muted mb-0">${tool.category}</p>
                    </div>
                </div>
            </div>
        </div>
    `).join('');
}

window.addEventListener('load', () => {
    const menu = document.getElementById('hub-menu');
    let lastCategory = '';

    SORTED_REGISTRY.forEach(tool => {
        if (tool.category !== lastCategory) {
            const label = document.createElement('div');
            label.className = 'category-label';
            label.textContent = tool.category;
            menu.appendChild(label);
            lastCategory = tool.category;
        }

        const a = document.createElement('a');
        a.href = `#${tool.id}`;
        a.className = 'nav-link';
        a.setAttribute('data-id', tool.id);
        a.innerHTML = `<i class="bi ${tool.icon}"></i> <span>${tool.name}</span>`;
        a.onclick = (e) => { e.preventDefault(); navigateTo(tool.id); };
        menu.appendChild(a);
    });

    const initialTool = window.location.hash.replace('#','') || 'dashboard';
    navigateTo(initialTool);
});

// Advanced Global Search (Cmd+K)
document.getElementById('global-search').addEventListener('input', (e) => {
    const term = e.target.value.toLowerCase();
    document.querySelectorAll('#hub-menu .nav-link').forEach(link => {
        const matches = link.textContent.toLowerCase().includes(term);
        link.style.display = matches ? 'flex' : 'none';
    });
    // Hide categories if no children visible
    document.querySelectorAll('.category-label').forEach(label => {
        let next = label.nextElementSibling;
        let hasVisible = false;
        while(next && next.classList.contains('nav-link')) {
            if(next.style.display !== 'none') hasVisible = true;
            next = next.nextElementSibling;
        }
        label.style.display = hasVisible ? 'block' : 'none';
    });
});

function logActivity(module, action) {
    const logs = JSON.parse(localStorage.getItem('hub-logs') || '[]');
    logs.unshift({ timestamp: new Date().toLocaleString(), module, action });
    localStorage.setItem('hub-logs', JSON.stringify(logs.slice(0, 50)));
}

// Mock API Data for Dashboard
async function fetchHubData() {
    return {
        serverStatus: 'Ottimale',
        uptime: '99.9%',
        lastUpdate: new Date().toLocaleTimeString()
    };
}