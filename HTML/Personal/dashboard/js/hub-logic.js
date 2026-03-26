/**
 * HUB-LOGIC.JS - Il Motore Centrale
 */

// 1. REGISTRO DEI TOOL
const HUB_REGISTRY = [
    { id: 'dashboard', name: 'Dashboard', file: 'views/dashboard.html', icon: 'bi-grid-1x2-fill', pinned: false, desc: 'Panoramica principale' },
    { id: 'stats', name: 'Statistiche', file: 'views/stats.html', icon: 'bi-graph-up', pinned: false, desc: 'Analisi dati hub' },
    { id: 'tasks', name: 'Task Manager', file: 'tools/tasks.html', icon: 'bi-check2-square', pinned: true, desc: 'Gestione attività quotidiane' },
    { id: 'logger', name: 'System Logs', file: 'tools/logger.html', icon: 'bi-terminal', pinned: true, desc: 'Monitoraggio eventi di sistema' },
    { id: 'calculator', name: 'Calcolatore', file: 'tools/calc.html', icon: 'bi-calculator', pinned: false, desc: 'Tool di calcolo rapido' },
    {id:'test', name:'test', file:'tools/test.html',icon:'bi-calculator',pinned:false,desc:'test'}
];

// 2. FUNZIONE DI NAVIGAZIONE UNIVERSALE
async function navigateTo(toolId) {
    const tool = HUB_REGISTRY.find(t => t.id === toolId);
    if (!tool) return;

    const container = document.getElementById('tool-container');
    const titleDisplay = document.getElementById('active-tool-title');
    
    // Aggiorna interfaccia
    if (titleDisplay) titleDisplay.textContent = tool.name;
    window.location.hash = tool.id;

    try {
        const response = await fetch(tool.file);
        if (!response.ok) throw new Error("File non trovato");
        const html = await response.text();
        
        // Iniezione HTML
        container.innerHTML = html;
        
        // --- TRUCCO PER ESEGUIRE GLI SCRIPT DEL TOOL ---
        const scripts = container.querySelectorAll("script");
        scripts.forEach(oldScript => {
            const newScript = document.createElement("script");
            Array.from(oldScript.attributes).forEach(attr => newScript.setAttribute(attr.name, attr.value));
            newScript.appendChild(document.createTextNode(oldScript.innerHTML));
            oldScript.parentNode.replaceChild(newScript, oldScript);
        });

        // Inizializzazione logiche specifiche
        if (toolId === 'dashboard') {
            generateDashboardCards();
            updateRecentActivity();
        } else if (toolId === 'stats') {
            populateStats();
        }
        
    } catch (err) {
        container.innerHTML = `<div class="alert alert-danger">Errore caricamento tool: ${tool.file}</div>`;
    }
}

// ... dentro hub-logic.js, aggiorna solo la funzione generateDashboardCards ...
function generateDashboardCards() {
    const grid = document.getElementById('pinned-tools-grid');
    if (!grid) return;

    const pinnedTools = HUB_REGISTRY.filter(t => t.pinned);
    grid.innerHTML = pinnedTools.map(tool => `
        <div class="col-md-4 col-lg-3">
            <div class="card card-hub h-100 p-3" onclick="navigateTo('${tool.id}')" style="cursor: pointer;">
                <div class="card-body text-center">
                    <div class="icon-shape mb-3 mx-auto d-flex align-items-center justify-content-center" 
                         style="width: 60px; height: 60px; background: rgba(13, 110, 253, 0.1); border-radius: 15px;">
                        <i class="bi ${tool.icon} fs-2 text-primary"></i>
                    </div>
                    <h5 class="fw-bold mb-1">${tool.name}</h5>
                    <p class="small text-muted mb-0">${tool.desc}</p>
                </div>
            </div>
        </div>
    `).join('');
}

function updateRecentActivity() {
    const list = document.getElementById('recent-activity-list');
    if (!list) return;

    const logs = JSON.parse(localStorage.getItem('hub-activity-logs')) || [];
    list.innerHTML = logs.map(log => `
        <li class="list-group-item bg-transparent border-secondary text-white small py-2">
            <div class="d-flex justify-content-between opacity-50 mb-1" style="font-size: 10px;">
                <span class="fw-bold text-uppercase">${log.tool}</span>
                <span>${log.time}</span>
            </div>
            <span class="text-white-50">${log.action}</span>
        </li>
    `).join('') || '<li class="list-group-item bg-transparent border-0 text-muted">Nessuna attività.</li>';
}

function populateStats() {
    const tableBody = document.getElementById('stats-table-body');
    if (!tableBody) return;

    document.getElementById('stats-active-count').textContent = HUB_REGISTRY.length;
    tableBody.innerHTML = HUB_REGISTRY.map(tool => `
        <tr>
            <td><i class="bi ${tool.icon} me-2 text-primary"></i> ${tool.name}</td>
            <td class="text-muted small font-monospace">${tool.id}</td>
            <td><span class="badge bg-success-subtle text-success">Online</span></td>
            <td>${tool.pinned ? '<i class="bi bi-pin-fill text-danger"></i> Pinned' : '<span class="text-muted">Standard</span>'}</td>
        </tr>
    `).join('');
}

// 4. UTILITY GLOBALI
function logActivity(toolName, action) {
    let logs = JSON.parse(localStorage.getItem('hub-activity-logs')) || [];
    logs.unshift({ time: new Date().toLocaleTimeString(), tool: toolName, action: action });
    localStorage.setItem('hub-activity-logs', JSON.stringify(logs.slice(0, 10)));
}

// Ricerca
document.getElementById('global-search').addEventListener('input', (e) => {
    const term = e.target.value.toLowerCase();
    document.querySelectorAll('#hub-menu .nav-item').forEach(li => {
        const text = li.innerText.toLowerCase();
        li.style.display = text.includes(term) ? 'block' : 'none';
    });
});

// Avvio
window.addEventListener('load', () => {
    const menu = document.getElementById('hub-menu');
    HUB_REGISTRY.forEach(tool => {
        const li = document.createElement('li');
        li.className = 'nav-item mb-1';
        li.innerHTML = `<a class="nav-link text-white opacity-75 px-3 py-2 d-flex align-items-center gap-3 rounded-3" 
                           href="#${tool.id}" onclick="navigateTo('${tool.id}')">
                           <i class="bi ${tool.icon}"></i><span>${tool.name}</span></a>`;
        menu.appendChild(li);
    });
    navigateTo(window.location.hash.replace('#','') || 'dashboard');
});