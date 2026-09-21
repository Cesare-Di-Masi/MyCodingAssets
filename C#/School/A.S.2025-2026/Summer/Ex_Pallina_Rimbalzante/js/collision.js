import { coloreCasuale } from "./utils.js";

// Risolve la collisione palla <-> rettangolo (ostacolo) con rimbalzo realistico
export function resolveCollision(ball, rect) {
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