const API_URL = 'https://jsonplaceholder.typicode.com/posts?_limit=10';
const dashboardEl = document.getElementById('dashboard');
const statusMessageEl = document.getElementById('status-message');
const refreshBtn = document.getElementById('refresh-btn');

/*
 * Innanzitutto java esegue un'operazione alla volta nel thread principale.
 * Quando usiamo l'await davanti a una promise (come la fetch), stiamo dicendo
 * al motore java di mettere in pausa l'esecuzione esclusivamente di questa funzione asincrona 
 * finché la rete non risponde. Nel mentre, il controllo viene restituito all'event loop del browser. 
 * Questo impedisce il blocco del thread principale, permettendo al browser di continuare 
 * a disegnare l'interfaccia utente, gestire i click degli utenti 
 * o eseguire animazioni senza che la pagina si blocchi in attesa della risposta del server.
 */

async function fetchResources() {
    // Mostro il messaggio di caricamento e svuoto la dashboard
    dashboardEl.innerHTML = '';
    statusMessageEl.textContent = 'Caricamento in corso... ';
    statusMessageEl.className = '';
    
    // Disabilito il pulsante per evitare click multipli sovrapposti
    refreshBtn.disabled = true;

    try {
        // Richiesta dati (Async/Await)
        const response = await fetch(API_URL);
        
        // Controllo validità della risposta HTTP
        if (!response.ok) {
            throw new Error(`Errore HTTP: ${response.status}`);
        }

        const data = await response.json();
        
        // Nascondo il messaggio di caricamento
        statusMessageEl.textContent = 'Ultimo aggiornamento: ' + new Date().toLocaleTimeString();

        // Manipolazione del DOM
        data.forEach(resource => {
            const card = document.createElement('div');
            card.classList.add('card');
            
            // ID pari = ONLINE, ID dispari = OFFLINE
            const isOnline = resource.id % 2 === 0;
            const statusText = isOnline ? 'ONLINE' : 'OFFLINE';
            
            // Assegnazione classe per il bordo
            if (isOnline) {
                card.classList.add('online');
            } else {
                card.classList.add('offline');
            }

            // Template HTML per la card
            card.innerHTML = `
                <div class="status-badge">${statusText} (ID: ${resource.id})</div>
                <h3>Risorsa #${resource.id}: ${resource.title.substring(0, 20)}...</h3>
                <p>Dettagli operativi: ${resource.body.substring(0, 60)}...</p>
            `;

            dashboardEl.appendChild(card);
        });

    } catch (error) {
        console.error("Errore durante il fetch dei dati:", error);
        statusMessageEl.textContent = 'Errore di connessione. Impossibile caricare le risorse.';
        statusMessageEl.className = 'error';
    } finally {
        // Riabilito il pulsante a fine operazione
        refreshBtn.disabled = false;
    }
}

// Event Listener per il pulsante Refresh
refreshBtn.addEventListener('click', fetchResources);

// Caricamento automatico
fetchResources();

// Aggiorna in automatico la pagina ogni 30 secondi
setInterval(fetchResources, 30000);