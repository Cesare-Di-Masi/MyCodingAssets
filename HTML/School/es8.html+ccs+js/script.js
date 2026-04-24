// 1. Individuo gli elementi sulla pagina
const inputPx = document.getElementById('inputPixel');
const bottone = document.getElementById('btnCalcola');
const output = document.getElementById('risultato');

const inputRem = document.getElementById('inputRem')
const bottone2 = document.getElementById('btnCalcolaPx');
const output2 = document.getElementById('risultato2');

// 2. Cosa succede quando premo il bottone?
bottone.onclick = function () {
    // Leggo il valore inserito (che è un numero)
    let px = inputPx.value;

    // Controllo se l'utente ha scritto qualcosa
    if (px === "" || px <= 0) {
        output.textContent = "Inserisci un numero valido!";
        output.style.color = "red";
    } else {
        // Eseguo il calcolo: REM = PX / 16
        let calcoloRem = (px / 16).toFixed(2);

        // Mostro il risultato
        // Se il calcolo è maggiore di 2 REM la scritta compare in blu
        if (calcoloRem > 2.00) {
            output.textContent = px + "px corrispondono a " + calcoloRem + "rem";
            output.style.color = "#0000FF"
        }
        else {
            output.textContent = px + "px corrispondono a " + calcoloRem + "rem";
            output.style.color = "#28a745";
        }

    }

};

// 3. Cosa succede quando premo il bottone ma per convertire da REM a PX?
bottone2.onclick = function () {
    // Leggo il valore inserito (che è un numero)
    let rem = inputRem.value

    // Controllo se l'utente ha scritto qualcosa
    if (rem === "" || rem <= 0) {
        output2.textContent = "Inserisci un numero valido!";
        output2.style.color = "red";
    } else {
        // Eseguo il calcolo: PX = REM * 16
        let calcoloPx = (rem * 16).toFixed(2);

        // Mostro il risultato
        // Se il calcolo è maggiore di 2 Px la scritta compare in blu
        if (calcoloPx > 2.00) {
            output2.textContent = rem + "rem corrispondono a " + calcoloPx + "px";
            output2.style.color = "#0000FF"
        }
        else {
            output2.textContent = rem + "rem corrispondono a " + calcoloPx + "px";
            output2.style.color = "#28a745";
        }

    }

};