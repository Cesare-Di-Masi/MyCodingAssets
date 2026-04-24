const output = document.getElementById('output');
const btn = document.getElementById('startBtn');
const min = 0;
const max = 5000;
 
// Funzione che simula il download di un file
function scaricaFile(nomeFile, tempo) {
   return new Promise((resolve, reject) => {
        output.innerHTML += `<p>Inizio download: ${nomeFile}...</p>`;
        
        if (tempo > 3000) {
            // Se il tempo previsto supera i 3 secondi (3000 ms), simuliamo il timeout
            // Facciamo scattare il messaggio di errore esattamente a 3 secondi
            setTimeout(() => {
                reject(`\u274C Errore: ${nomeFile} non scaricato (ha impiegato piu' di 3 secondi).`);
            }, 3000);
        } else {
            // Se il tempo è entro i 3 secondi, il download ha successo
            setTimeout(() => {
                resolve(`\u2705 ${nomeFile} scaricato con successo in ${tempo/1000} secondi!`);
            }, tempo);
        }
   });
}
 
// Funzione che genera un tempo casuale
function generaTempoCasuale() {
    return Math.floor(Math.random() * (max - min + 1)) + min;
}

// Gestore dell'evento click
btn.addEventListener('click', async () => {
   output.innerHTML = "<p><em>Richiesta inviata al server...</em></p>";
   btn.disabled = true; // Disabilita il tasto durante il download
 
   // Eseguiamo i download in "parallelo"
   const d1 = scaricaFile("Database Utenti", generaTempoCasuale());
   const d2 = scaricaFile("Immagini Profilo", generaTempoCasuale());
   const d3 = scaricaFile("Configurazione Sistema", generaTempoCasuale());
 
   // Utilizziamo Promise.allSettled per aspettare tutti i processi,
   // sia quelli andati a buon fine (fulfilled) che quelli falliti (rejected)
   const risultati = await Promise.allSettled([d1, d2, d3]);
 
   // Stampiamo i risultati finali in base all'esito
   risultati.forEach(res => {
        if (res.status === 'fulfilled') {
            // Se la Promise è stata risolta (resolve)
            output.innerHTML += `<p style="color: green;">${res.value}</p>`;
        } else {
            // Se la Promise è stata rifiutata (reject)
            output.innerHTML += `<p style="color: red;">${res.reason}</p>`;
        }
   });
 
   // output.innerHTML += "<h3>Tutti i processi completati!</h3>";
   btn.disabled = false;
});