// 1. Caricamento dati iniziali dal LocalStorage
let listaSpese = JSON.parse(localStorage.getItem('spese')) || [];

// Riferimenti agli elementi HTML
const form = document.getElementById('spese-form');
const listaUl = document.getElementById('lista-ul');
const displayTotale = document.getElementById('display-totale');
const messaggioAlert = document.getElementById('messaggio-alert');

/**
 * Funzione per aggiornare l'interfaccia (DOM)
 */
function renderizzaPagina() {
    listaUl.innerHTML = ''; // Svuota la lista esistente
    let totale = 0;

    listaSpese.forEach((item, index) => {
        totale += parseFloat(item.costo);

        // Creazione dinamica del tag <li>
        const li = document.createElement('li');
        li.className = 'list-group-item';
        li.innerHTML = `
            <span><strong>${item.nome}</strong>: ${item.costo.toFixed(2)}€</span>
            <button class="btn btn-outline-danger btn-sm" onclick="eliminaTask(${index})">Elimina</button>
        `;
        listaUl.appendChild(li);
    });

    // Aggiornamento Totale
    displayTotale.innerText = `Totale Spesa: ${totale.toFixed(2)}€`;

    // Logica Alert Budget (Soglia 500€)
    if (totale > 500) {
        displayTotale.classList.add('budget-alert');
        messaggioAlert.style.display = 'block';
    } else {
        displayTotale.classList.remove('budget-alert');
        messaggioAlert.style.display = 'none';
    }

    // Salvataggio permanente
    localStorage.setItem('spese', JSON.stringify(listaSpese));
}

/**
 * Gestione invio Form
 */
form.addEventListener('submit', (e) => {
    e.preventDefault(); // Impedisce il refresh della pagina

    const nomeInput = document.getElementById('nome-attivita');
    const costoInput = document.getElementById('costo-previsto');

    const nome = nomeInput.value.trim();
    const costo = parseFloat(costoInput.value);

    // Validazione specifica (Nome non vuoto, costo > 0)
    if (nome === "" || costo <= 0 || isNaN(costo)) {
        alert("Inserisci un nome valido e un costo superiore a 0€.");
        return;
    }

    // Aggiunta all'array di oggetti
    listaSpese.push({ nome: nome, costo: costo });

    // Pulizia campi e aggiornamento UI
    form.reset();
    renderizzaPagina();
});

/**
 * Funzione per eliminare un task
 * (Esposta globalmente per essere usata nell'attributo onclick)
 */
window.eliminaTask = function(index) {
    listaSpese.splice(index, 1); // Rimuove l'elemento dall'array
    renderizzaPagina();
};

// Avvio iniziale al caricamento della pagina
renderizzaPagina();