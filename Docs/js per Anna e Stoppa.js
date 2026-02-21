async function fetchComuni() {
    
    return [
        {
            "nome": "Roma",
            "codice": "058091",
            "codiceCatastale": "H501",
            "cap": ["00118", "00121", "00199"],
            "popolazione": 2872800,
            "provincia": { "nome": "Roma", "sigla": "RM" },
            "regione": { "nome": "Lazio" }
        },
        {
            "nome": "Milano",
            "codice": "015146",
            "codiceCatastale": "F205",
            "cap": ["20121", "20162"],
            "popolazione": 1366180,
            "provincia": { "nome": "Milano", "sigla": "MI" },
            "regione": { "nome": "Lombardia" }
        },
        {
            "nome": "Napoli",
            "codice": "063049",
            "codiceCatastale": "F839",
            "cap": ["80121", "80147"],
            "popolazione": 962702,
            "provincia": { "nome": "Napoli", "sigla": "NA" },
            "regione": { "nome": "Campania" }
        }
    ];
}

function normalizeComuni(raw) {
    return raw.map(c => ({
        nome: c.nome,
        codiceIstat: c.codice,
        cap: c.cap ? c.cap[0] : "N/D", // Prende il primo CAP della lista
        provincia: c.provincia.nome,
        sigla: c.provincia.sigla,
        regione: c.regione.nome,
        abitanti: c.popolazione ? c.popolazione.toLocaleString('it-IT') : "N/D"
    }));
}

let comuniCache = [];

async function updateComuni() {
    console.log("Caricamento comuni italiani...");
    try {
        // const res = await fetch("https://comuni-ita.nicolorebaioli.dev/comuni");
        // const raw = await res.json();
        
        const raw = await fetchComuni(); // Usa i dati blindati sopra
        comuniCache = normalizeComuni(raw);
        
        console.log(`Caricati ${comuniCache.length} comuni.`);
        displayComuni(comuniCache);
    } catch (e) {
        console.error("Errore nel caricamento dei comuni:", e);
    }
}

function displayComuni(list) {
    const container = document.getElementById("resultsGrid");
    if (!container) return;

    container.innerHTML = list.map(c => `
        <div class="card">
            <h3>${c.nome} (${c.sigla})</h3>
            <p><strong>Regione:</strong> ${c.regione}</p>
            <p><strong>Popolazione:</strong> ${c.abitanti}</p>
            <p><strong>CAP:</strong> ${c.cap}</p>
            <small>Codice ISTAT: ${c.codiceIstat}</small>
        </div>
    `).join("");
}

// Avvio
document.addEventListener("DOMContentLoaded", updateComuni);

//ricerca
document.getElementById("searchBar").addEventListener("input", (e) => {
    const q = e.target.value.toLowerCase();
    const filtered = comuniCache.filter(c => 
        c.nome.toLowerCase().includes(q) || 
        c.provincia.toLowerCase().includes(q)
    );
    displayComuni(filtered);
});

