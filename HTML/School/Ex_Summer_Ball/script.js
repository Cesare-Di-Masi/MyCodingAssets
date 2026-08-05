// =========================================================
// 1. RECUPERO ELEMENTI DOM E STATO GLOBALE
// =========================================================
const area = document.getElementById("area");
const bottone = document.getElementById("startStop");
const contatoreEl = document.getElementById("contatore");
const distanzaEl = document.getElementById("distanza");

let isDragging = false;
let offsetX = 0;
let offsetY = 0;

let running = false;
let contatore = 0;
const SUBSTEPS = 240;

function coloreCasuale() {
  const r = Math.floor(Math.random() * 256).toString(16).padStart(2, "0");
  const g = Math.floor(Math.random() * 256).toString(16).padStart(2, "0");
  const b = Math.floor(Math.random() * 256).toString(16).padStart(2, "0");
  return `#${r}${g}${b}`;
}

// =========================================================
// 2. CLASSE PALLINA
// =========================================================
class Ball {
  constructor(elementId, x, y, vx, vy, diametro) {
    this.element = document.getElementById(elementId);
    this.x = x;
    this.y = y;
    this.vx = vx;
    this.vy = vy;
    this.diametro = diametro;
    this.radius = diametro / 2;

    this.puoContareX = true;
    this.puoContareY = true;

    this.element.style.left = "0px";
    this.element.style.top = "0px";
    this.element.style.width = this.diametro + "px";
    this.element.style.height = this.diametro + "px";

    this.render();
  }

  updateFisica(areaWidth, areaHeight, dt) {
    this.x += (this.vx * dt) / SUBSTEPS;
    this.y += (this.vy * dt) / SUBSTEPS;

    this.controllaRimbalzoBordi(areaWidth, areaHeight);
  }

  controllaRimbalzoBordi(w, h) {
    const tolleranza = 0.001;

    if (this.x > tolleranza && this.x + this.diametro < w - tolleranza)
      this.puoContareX = true;
    if (this.y > tolleranza && this.y + this.diametro < h - tolleranza)
      this.puoContareY = true;

    if (this.x <= 0 && this.vx < 0) {
      this.x = 0;
      this.vx *= -1;
      if (this.puoContareX) {
        contatore++;
        this.puoContareX = false;
      }
    } else if (this.x + this.diametro >= w && this.vx > 0) {
      this.x = w - this.diametro;
      this.vx *= -1;
      if (this.puoContareX) {
        contatore++;
        this.puoContareX = false;
      }
    }

    if (this.y <= 0 && this.vy < 0) {
      this.y = 0;
      this.vy *= -1;
      if (this.puoContareY) {
        contatore++;
        this.puoContareY = false;
      }
    } else if (this.y + this.diametro >= h && this.vy > 0) {
      this.y = h - this.diametro;
      this.vy *= -1;
      if (this.puoContareY) {
        contatore++;
        this.puoContareY = false;
      }
    }
  }

  render() {
    contatoreEl.textContent = "Rimbalzi: " + contatore;
    this.element.style.transform = `translate(${this.x}px, ${this.y}px)`;
  }
}

// =========================================================
// 3. CLASSE OSTACOLO
// =========================================================
class Obstacle {
  constructor(areaContainer, x, y, width, height, vx, vy, isMoving = true) {
    this.element = document.createElement("div");
    this.element.classList.add("ostacolo");

    this.element.style.width = width + "px";
    this.element.style.height = height + "px";
    this.element.style.left = "0px";
    this.element.style.top = "0px";

    areaContainer.appendChild(this.element);

    this.x = x;
    this.y = y;
    this.width = width;
    this.height = height;
    this.vx = vx;
    this.vy = vy;
    this.isMoving = isMoving;
    this.wasMoving = isMoving; // Memorizza lo stato iniziale del movimento

    this.render();
  }

  updateFisica(areaWidth, areaHeight, dt) {
    if (!this.isMoving) return;

    this.x += (this.vx * dt) / SUBSTEPS;
    this.y += (this.vy * dt) / SUBSTEPS;

    if (this.x <= 0) {
      this.x = 0;
      this.vx *= -1;
    } else if (this.x + this.width >= areaWidth) {
      this.x = areaWidth - this.width;
      this.vx *= -1;
    }

    if (this.y <= 0) {
      this.y = 0;
      this.vy *= -1;
    } else if (this.y + this.height >= areaHeight) {
      this.y = areaHeight - this.height;
      this.vy *= -1;
    }
  }

  render() {
    this.element.style.transform = `translate(${this.x}px, ${this.y}px)`;
  }
}

// =========================================================
// 4. LOGICA DI COLLISIONE
// =========================================================
function resolveCollision(ball, rect) {
  const ballCenterX = ball.x + ball.radius;
  const ballCenterY = ball.y + ball.radius;

  const closestX = Math.max(rect.x, Math.min(ballCenterX, rect.x + rect.width));
  const closestY = Math.max(rect.y, Math.min(ballCenterY, rect.y + rect.height));

  const dx = ballCenterX - closestX;
  const dy = ballCenterY - closestY;

  const dist2 = dx * dx + dy * dy;
  const radius2 = ball.radius * ball.radius;

  if (dist2 > radius2) return;

  const angle = Math.atan2(dy, dx);
  const nx = Math.cos(angle);
  const ny = Math.sin(angle);

  const dot = ball.vx * nx + ball.vy * ny;

  if (dot < 0) {
    ball.vx = ball.vx - 2 * dot * nx;
    ball.vy = ball.vy - 2 * dot * ny;
    ball.element.style.background = coloreCasuale();
  }

  ball.x = closestX + nx * ball.radius - ball.radius;
  ball.y = closestY + ny * ball.radius - ball.radius;
}

// =========================================================
// 5. INIZIALIZZAZIONE E GAME LOOP
// =========================================================
const pallina = new Ball("pallina", 50, 50, 300, 240, 30);

const ostacoli = [
  new Obstacle(area, 200, 200, 60, 60, 50, 50, true),
  new Obstacle(area, 350, 80, 80, 40, 500, 500, true),
  new Obstacle(area, 100, 300, 150, 20, 89, 89, false),
];

let lastTime = 0;
function loop(timestamp) {
  if (!running) {
    lastTime = timestamp;
    requestAnimationFrame(loop);
    return;
  }

  let dt = 0;
  if (lastTime !== 0) {
    dt = (timestamp - lastTime) / 1000;
  }
  lastTime = timestamp;

  const areaWidth = area.clientWidth;
  const areaHeight = area.clientHeight;

  for (let s = 0; s < SUBSTEPS; s++) {
    pallina.updateFisica(areaWidth, areaHeight, dt);

    ostacoli.forEach((ostacolo) =>
      ostacolo.updateFisica(areaWidth, areaHeight, dt),
    );

    ostacoli.forEach((ostacolo) => {
      resolveCollision(pallina, ostacolo);
    });
  }

  pallina.render();
  ostacoli.forEach((o) => o.render());

  requestAnimationFrame(loop);
}



bottone.addEventListener("click", () => {
  running = !running;
  bottone.textContent = running ? "Stop" : "Start";
  if (running) {
    requestAnimationFrame(loop);
  }
});

let selectedElement = null;
let tempoInizio = 0;
const SOGLIA_MS = 250;

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