import { SUBSTEPS } from "./config.js";

export class Obstacle {
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