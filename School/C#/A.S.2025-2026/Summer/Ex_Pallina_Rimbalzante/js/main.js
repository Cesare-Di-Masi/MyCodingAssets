import { SUBSTEPS } from "./config.js";
import { Ball } from "./ball.js";
import { Obstacle } from "./obstacle.js";
import { resolveCollision } from "./collision.js";
import { initInput } from "./input.js";

const area = document.getElementById("area");
const bottone = document.getElementById("startStop");
const contatoreEl = document.getElementById("contatore");

let running = false;
let contatore = 0;

// --- Palla ---------------------------------------------------------------
const pallina = new Ball("pallina", 50, 50, 300, 240, 30, () => {
  contatore++;
  contatoreEl.textContent = "Rimbalzi: " + contatore;
});

// --- Ostacoli ------------------------------------------------------------
const ostacoli = [
  new Obstacle(area, 200, 200, 60, 60, 50, 50, true),
  new Obstacle(area, 350, 80, 80, 40, 500, 500, true),
  new Obstacle(area, 100, 300, 150, 20, 89, 89, false),
];

// --- Input (drag & drop + tastiera) --------------------------------------
initInput(area, ostacoli);

// --- Game loop -----------------------------------------------------------
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

    ostacoli.forEach((ostacolo) => ostacolo.updateFisica(areaWidth, areaHeight, dt));

    ostacoli.forEach((ostacolo) => {
      resolveCollision(pallina, ostacolo);
    });
  }

  pallina.render();
  ostacoli.forEach((o) => o.render());

  requestAnimationFrame(loop);
}

// --- Controlli UI --------------------------------------------------------
bottone.addEventListener("click", () => {
  running = !running;
  bottone.textContent = running ? "Stop" : "Start";
});

// Avvia il loop una sola volta: evita di creare catene di requestAnimationFrame duplicate
requestAnimationFrame(loop);