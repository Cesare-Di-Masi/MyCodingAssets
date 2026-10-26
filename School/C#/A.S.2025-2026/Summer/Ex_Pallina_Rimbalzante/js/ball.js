import { SUBSTEPS } from "./config.js";

export class Ball {
  constructor(elementId, x, y, vx, vy, diametro, onRimbalzo = () => {}) {
    this.element = document.getElementById(elementId);
    this.x = x;
    this.y = y;
    this.vx = vx;
    this.vy = vy;
    this.diametro = diametro;
    this.radius = diametro / 2;

    // Callback invocata ad ogni rimbalzo (il contatore vive in main.js)
    this.onRimbalzo = onRimbalzo;

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
        this.onRimbalzo();
        this.puoContareX = false;
      }
    } else if (this.x + this.diametro >= w && this.vx > 0) {
      this.x = w - this.diametro;
      this.vx *= -1;
      if (this.puoContareX) {
        this.onRimbalzo();
        this.puoContareX = false;
      }
    }

    if (this.y <= 0 && this.vy < 0) {
      this.y = 0;
      this.vy *= -1;
      if (this.puoContareY) {
        this.onRimbalzo();
        this.puoContareY = false;
      }
    } else if (this.y + this.diametro >= h && this.vy > 0) {
      this.y = h - this.diametro;
      this.vy *= -1;
      if (this.puoContareY) {
        this.onRimbalzo();
        this.puoContareY = false;
      }
    }
  }

  render() {
    this.element.style.transform = `translate(${this.x}px, ${this.y}px)`;
  }
}