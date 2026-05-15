/**
 * InputState - Gestisce gli input da tastiera
 * Mantiene un oggetto globale con lo stato di tutti i tasti
 */
export class InputState {
  constructor() {
    this.keys = {};

    // Event listeners per tastiera
    window.addEventListener("keydown", (e) => {
      this.keys[e.code] = true;
    });

    window.addEventListener("keyup", (e) => {
      this.keys[e.code] = false;
    });

    // Reset quando la finestra perde il focus (evita tasti "bloccati")
    window.addEventListener("blur", () => {
      this.keys = {};
    });
  }

  /**
   * Restituisce il valore di un tasto
   * @param {string} code - Codice del tasto (es. "ArrowUp", "KeyW")
   * @returns {boolean}
   */
  isPressed(code) {
    return this.keys[code] ?? false;
  }

  /**
   * Controlla se una combinazione di tasti è premuta
   * @param {string[]} codes - Array di codici
   * @returns {boolean}
   */
  isAnyPressed(...codes) {
    return codes.some((code) => this.keys[code]);
  }
}
