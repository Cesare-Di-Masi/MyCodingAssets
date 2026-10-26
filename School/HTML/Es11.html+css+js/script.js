// Array per memorizzare i voti
const voti = [];

// Riferimenti agli elementi DOM
const inputVoto = document.getElementById("votoInput");
const btnAggiungi = document.getElementById("addBtn");
const listaUl = document.getElementById("listaVoti");
const mediaDisplay = document.getElementById("mediaDisplay");
const errorMsg = document.getElementById("error-msg");
const statusIcon = document.getElementById("statusIcon");

btnAggiungi.addEventListener("click", () => {
  const valore = parseFloat(inputVoto.value);

  // 1. Validazione
  if (isNaN(valore) || valore < 1 || valore > 10) {
    errorMsg.textContent = "Inserire un numero valido tra 1 e 10";
    return;
  }

  // Reset errore e input
  errorMsg.textContent = "";
  inputVoto.value = "";

  // 2. Aggiunta all'array e alla lista UI
  voti.push(valore);
  const li = document.createElement("li");
  li.textContent = `Voto: ${valore}`;
  listaUl.appendChild(li);

  // 3. Calcolo Media
  calcolaMedia();
});

function calcolaMedia() {
  const somma = voti.reduce((acc, curr) => acc + curr, 0);
  const media = (somma / voti.length).toFixed(2);

  mediaDisplay.textContent = media;

  // 4. Logica Colore e Icona
  if (media >= 6) {
    mediaDisplay.className = "sufficienza";
    statusIcon.textContent = "✅";
  } else {
    mediaDisplay.className = "insufficienza";
    statusIcon.textContent = "❌";
  }
}
