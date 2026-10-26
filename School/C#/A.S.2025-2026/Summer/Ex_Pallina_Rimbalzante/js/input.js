import { SOGLIA_MS } from "./config.js";

// Gestisce drag & drop degli ostacoli e il tasto Spazio per congelarli/sbloccarli
export function initInput(area, ostacoli) {
  let selectedElement = null;
  let isDragging = false;
  let offsetX = 0;
  let offsetY = 0;
  let tempoInizio = 0;

  area.addEventListener("mousedown", (e) => {
    tempoInizio = Date.now();

    const rect = area.getBoundingClientRect();
    const mouseX = e.clientX - rect.left;
    const mouseY = e.clientY - rect.top;

    let colpito = false;

    for (let i = ostacoli.length - 1; i >= 0; i--) {
      const element = ostacoli[i];

      if (
        mouseX >= element.x &&
        mouseX <= element.x + element.width &&
        mouseY >= element.y &&
        mouseY <= element.y + element.height
      ) {
        isDragging = true;

        // Salva lo stato reale del movimento prima di congelarlo per il drag
        element.wasMoving = element.isMoving;
        element.isMoving = false;

        if (selectedElement && selectedElement !== element) {
          selectedElement.element.classList.remove("selected");
        }

        element.element.classList.add("selected");
        offsetX = mouseX - element.x;
        offsetY = mouseY - element.y;
        selectedElement = element;

        colpito = true;
        break;
      }
    }

    if (!colpito && selectedElement) {
      selectedElement.element.classList.remove("selected");
      selectedElement = null;
    }
  });

  area.addEventListener("mousemove", (e) => {
    if (!isDragging || !selectedElement) return;

    const rect = area.getBoundingClientRect();
    selectedElement.x = (e.clientX - rect.left) - offsetX;
    selectedElement.y = (e.clientY - rect.top) - offsetY;

    // Render immediato durante il movimento per evitare l'effetto ritardo visivo
    selectedElement.render();
  });

  area.addEventListener("mouseup", () => {
    if (!isDragging) return;
    isDragging = false;

    if (!selectedElement) return;

    const durataPressione = Date.now() - tempoInizio;

    if (durataPressione < SOGLIA_MS) {
      // Click rapido: ripristina lo stato precedente, ma lascia l'evidenziazione
      selectedElement.isMoving = selectedElement.wasMoving;
    } else {
      // Drag lungo: ripristina lo stato originale e pulisce la selezione
      selectedElement.element.classList.remove("selected");
      selectedElement.isMoving = selectedElement.wasMoving;
      selectedElement = null;
    }
  });

  area.addEventListener("mouseleave", () => {
    if (!isDragging || !selectedElement) return;

    isDragging = false;
    selectedElement.element.classList.remove("selected");
    // Ripristina lo stato corretto anche se il mouse scappa fuori dal canvas
    selectedElement.isMoving = selectedElement.wasMoving;
    selectedElement = null;
  });

  window.addEventListener("keydown", (e) => {
    if (!selectedElement) return;

    if (e.code === "Space" || e.key === " ") {
      e.preventDefault();
      // Inverte lo stato attivo e aggiorna anche la memoria storica dell'oggetto
      selectedElement.isMoving = !selectedElement.isMoving;
      selectedElement.wasMoving = selectedElement.isMoving;
    }
  });
}