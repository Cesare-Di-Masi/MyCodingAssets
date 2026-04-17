// 1. Inizializzo l'array globale.
let listaSpese = JSON.parse(localStorage.getItem("databaseSpese")) || [];

// 2. Recupero gli elementi chiave del DOM
const form = document.getElementById("budgetForm");
const inputNome = document.getElementById("nomeTask");
const inputCosto = document.getElementById("costoTask");
const listaElementi = document.getElementById("listaElementi");
const totaleTesto = document.getElementById("totaleSpesa");
const budgetAlert = document.getElementById("budgetAlert");
const emptyMessage = document.getElementById("emptyMessage");

// 3. Gestione dell'invio del Form
form.addEventListener("submit", function (event) {
  // Impedisco il ricaricamento della pagina
  event.preventDefault();

  // Recupero i valori e tolgo spazi extra dal nome
  const nome = inputNome.value.trim();
  const costo = parseFloat(inputCosto.value);

  // VALIDAZIONE CORRETTA: uso 'costo' e non 'cost'
  if (nome === "" || isNaN(costo) || costo <= 0) {
    alert("Errore: Inserisci un nome valido e un costo maggiore di 0.");
    return;
  }

  // Creo un oggetto che rappresenta il singolo task
  const nuovaSpesa = {
    id: Date.now(),
    nome: nome,
    costo: costo,
  };

  // Aggiungo all'array
  listaSpese.push(nuovaSpesa);

  // Salvo nel LocalStorage
  salvaDati();

  // Resetto i campi di input
  form.reset();

  // Aggiorno il DOM e i calcoli
  aggiornaInterfaccia();
});

// 4. Funzione per rimuovere una spesa
function eliminaSpesa(idDaEliminare) {
  listaSpese = listaSpese.filter((spesa) => spesa.id !== idDaEliminare);
  salvaDati();
  aggiornaInterfaccia();
}

// 5. Funzione Core: Ricrea la lista nel DOM e ricalcola il totale
function aggiornaInterfaccia() {
  // Svuoto l'HTML della lista per evitare duplicati
  listaElementi.innerHTML = "";

  let totaleCalcolato = 0;

  // Gestione del messaggio "Lista vuota"
  if (listaSpese.length === 0) {
    emptyMessage.classList.remove("d-none");
  } else {
    emptyMessage.classList.add("d-none");
  }

  // Ciclo l'array per creare gli elementi
  listaSpese.forEach((spesa) => {
    totaleCalcolato += spesa.costo;

    const li = document.createElement("li");
    li.className =
      "list-group-item d-flex justify-content-between align-items-center px-0";

    li.innerHTML = `
            <span class="fw-medium">${spesa.nome}</span>
            <div>
                <span class="badge bg-secondary rounded-pill me-3">${spesa.costo.toFixed(2)} €</span>
                <button class="btn btn-sm btn-outline-danger" onclick="eliminaSpesa(${spesa.id})">
                    <i class="bi bi-trash"></i> Elimina
                </button>
            </div>
        `;

    listaElementi.appendChild(li);
  });

  // 6. Aggiornamento UI del Totale e Allerta Budget
  totaleTesto.textContent = totaleCalcolato.toFixed(2) + " €";

  if (totaleCalcolato > 500) {
    totaleTesto.classList.remove("text-success");
    totaleTesto.classList.add("text-danger");
    budgetAlert.classList.remove("d-none");
  } else {
    totaleTesto.classList.remove("text-danger");
    totaleTesto.classList.add("text-success");
    budgetAlert.classList.add("d-none");
  }
}

// Funzione Helper per salvare nel LocalStorage
function salvaDati() {
  localStorage.setItem("databaseSpese", JSON.stringify(listaSpese));
}

// Chiamo l'aggiornamento iniziale appena il file JS viene letto
aggiornaInterfaccia();
