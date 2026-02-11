let aircraftCache = [];

// Carica i dati appena la pagina è pronta
document.addEventListener("DOMContentLoaded", () => {
    updateAircraft();
    // In produzione useresti un intervallo, per ora lo carichiamo una volta
});

async function updateAircraft() {
    console.log("Recupero dati in corso (Modalità Simulazione)...");

    try {
        // Simuliamo la fetch che ora restituisce dati pronti
        const raw = await fetchAircraft();
        aircraftCache = normalizeAircraft(raw);
        
        console.log(`Successo! ${aircraftCache.length} aerei caricati.`);
        console.table(aircraftCache); // Ora vedrai la tabella piena!
        
        // Se hai una funzione per mostrare i dati, chiamala qui
        if (typeof displayResults === "function") {
            displayResults(aircraftCache);
        }
    } catch (e) {
        console.error("Errore:", e);
    }
}

/* da usare quando openSky non è bloccato
async function fetchAircraft(abortSignal) {
    const url = "https://opensky-network.org/api/states/all";

    const res = await fetch(url, {
        method: "GET",
        mode: "no-cors", // <--- Questo dice al browser di non controllare le regole CORS:
        //meccanismi di sicurezza basati su intestazioni HTTP che permettono a un server di autorizzare risorse web 
        //(come API o script) a essere richieste da un dominio diverso da quello di origine.
        signal: abortSignal 
    });

    console.log("Risposta ricevuta (Modalità no-cors):", res);
    // Nota: res.json() qui fallirà perché il corpo della risposta è protetto
    return []; 
}
*/

async function fetchAircraft() {
    // Questi sono dati "finti" ma nel formato ESATTO di OpenSky
    // [icao24, callsign, origin_country, time_position, last_contact, longitude, latitude, baro_altitude, ...]
    return [
        ["4b1813", "SWR123", "Switzerland", 1611, 1611, 8.54, 47.45, 10000, false, 250, 180, 0, null],
        ["3c6544", "DLH456", "Germany", 1611, 1611, 11.54, 48.45, 11000, false, 240, 90, 0, null],
        ["3005f2", "ITA111", "Italy", 1611, 1611, 12.10, 41.90, 5000, false, 180, 45, 0, null],
        ["440081", "BAW99 ", "United Kingdom", 1611, 1611, -0.12, 51.50, 12000, false, 280, 270, 0, null]
    ];
}

function normalizeAircraft(raw) { //normalizziamo così possiamo accedere ai dati come se fossero "oggetti"
    return raw.map(a => ({
        icao: a[0],
        callsign: (a[1] || "N/A").trim(),
        country: a[2] || "Unknown",
        lon: a[5],
        lat: a[6],
        altitude: a[7] ? `${Math.round(a[7])}m` : "N/A",
        speed: a[9] ? `${Math.round(a[9] * 3.6)}km/h` : "0km/h" // Conversione m/s a km/h
    }));
}