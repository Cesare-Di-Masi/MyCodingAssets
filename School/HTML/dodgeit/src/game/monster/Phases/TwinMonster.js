import { BaseMonster } from "../BaseMonster.js";
import { MonsterAI } from "../monsterLogic/MonsterAI.js";

export class TwinMonster extends BaseMonster {
  static BASE_CONFIG = {
    mode: "random",
    precision: 0.5,
    aggressiveness: 0.6,
    speed: 1.0,
    size: 15,
    color: {
      hex: "#ff0000", // Light blue
      hue: 200,
      sat: 100,
      light: 50,
      alpha: 1,
    },
    shape: "circle",
    trail: {
      active: true,
      length: 10,
      opacity: 0.5,
    },
  };

  constructor(x, y, overrides = {}) {
    const cfg = MonsterAI._mergeConfig(TwinMonster.BASE_CONFIG, overrides);
    super(x, y, cfg);
    this.twin = { x, y };
  }

  update(dt, player) {
    const prevX = this.x;
    const prevY = this.y;

    super.update(dt, player);

    const centerX = this.bounds.width / 2;
    const centerY = this.bounds.height / 2;

    this.twin.x = centerX - (this.x - centerX);
    this.twin.y = centerY - (this.y - centerY);
  }

  render(ctx) {
    super.render(ctx);

    const colorStr = this._getColorString(this.cfg.color);
    const radius = this.size * (this.currentScale ?? 1);
    
    ctx.save();
    ctx.beginPath();
    ctx.arc(this.twin.x, this.twin.y, radius, 0, Math.PI * 2);
    ctx.fillStyle = 360;
    ctx.shadowBlur = 15;
    ctx.shadowColor = colorStr;
    ctx.fill();
    ctx.shadowBlur = 0;
    ctx.restore();
  }
}
