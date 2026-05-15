export class VisualEntity {
  constructor(x, y, size, colorCfg = 120) {
    this.x = x;
    this.y = y;
    this.size = size;

    // Handle legacy hue or new color configuration
    if (typeof colorCfg === "number") {
      this.color = {
        hue: colorCfg,
        sat: 100,
        light: 50,
        alpha: 1,
      };
    } else {
      this.color = {
        hex: colorCfg.hex,
        hue: colorCfg.hue ?? 0,
        sat: colorCfg.sat ?? 100,
        light: colorCfg.light ?? 50,
        alpha: colorCfg.alpha ?? 1,
      };
    }

    this.trailPoints = [];
    this.maxTrailLength = 20;
    this.trailTimer = 0;
    this.intervalBetweenTrailPoints = 30; // Millisecondi tra i punti della scia
  }

  updateVisuals(deltaTime) {
    this.trailTimer += deltaTime;
    if (this.trailTimer >= this.intervalBetweenTrailPoints) {
      this.trailTimer = 0;
      this.trailPoints.unshift({ x: this.x, y: this.y });
      if (this.trailPoints.length > this.maxTrailLength) {
        this.trailPoints.pop();
      }
    }
  }

  _getColorString(colorObj) {
    if (colorObj.hex) {
      if (colorObj.alpha < 1) {
        // Convert hex to rgba
        const r = parseInt(colorObj.hex.slice(1, 3), 16);
        const g = parseInt(colorObj.hex.slice(3, 5), 16);
        const b = parseInt(colorObj.hex.slice(5, 7), 16);
        return `rgba(${r}, ${g}, ${b}, ${colorObj.alpha})`;
      }
      return colorObj.hex;
    }
    return `hsla(${colorObj.hue}, ${colorObj.sat}%, ${colorObj.light}%, ${colorObj.alpha})`;
  }

  render(ctx) {
    const color = this.cfg?.color ?? this.color;

    // Disegna la scia
    if (this.trailPoints.length > 1) {
      ctx.beginPath();
      const trailCfg = this.cfg?.trail ?? {};
      let trailColor;

      if (color.hex) {
        trailColor = { ...color, alpha: trailCfg.opacity ?? 0.5 };
      } else {
        trailColor = {
          hue: trailCfg.hue ?? color.hue,
          sat: trailCfg.sat ?? color.sat,
          light: trailCfg.light ?? Math.max(0, Math.min(100, color.light - 10)),
          alpha: trailCfg.opacity ?? 0.5,
        };
      }

      ctx.strokeStyle = this._getColorString(trailColor);
      ctx.lineWidth = this.size / 2;
      ctx.lineCap = "round";
      ctx.lineJoin = "round";
      ctx.moveTo(this.x, this.y);
      this.trailPoints.forEach((p) => ctx.lineTo(p.x, p.y));
      ctx.stroke();
    }

    // Disegna la testa
    const mainColorStr = this._getColorString(color);
    ctx.beginPath();
    ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
    ctx.fillStyle = mainColorStr;

    ctx.shadowBlur = 15;
    ctx.shadowColor = mainColorStr;
    ctx.fill();
    ctx.shadowBlur = 0;
  }
}
