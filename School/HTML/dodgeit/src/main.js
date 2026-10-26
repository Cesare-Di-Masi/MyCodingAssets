import { GameManager } from "./manager/GameManager.js";
import { InputState } from "./manager/playerInput.js";

const canvas = document.getElementById("gameCanvas");
const ctx = canvas.getContext("2d");
const scoreLabel = document.getElementById("scoreLabel");
const phaseLabel = document.getElementById("phaseLabel");

const input = new InputState();

canvas.width = window.innerWidth;
canvas.height = window.innerHeight;
const game = new GameManager(canvas);

function resizeCanvas() {
  canvas.width = window.innerWidth;
  canvas.height = window.innerHeight;
  game.resize(canvas.width, canvas.height);
}

window.addEventListener("resize", resizeCanvas);

let lastTime = performance.now();
function loop(currentTime) {
  const deltaTime = currentTime - lastTime;
  lastTime = currentTime;

  game.update(deltaTime, input);
  game.render(ctx);

  scoreLabel.textContent = `Tempo: ${(game.elapsed / 1000).toFixed(1)}s`;
  phaseLabel.textContent = `Fase: ${game.currentPhase}`;

  requestAnimationFrame(loop);
}

requestAnimationFrame(loop);
