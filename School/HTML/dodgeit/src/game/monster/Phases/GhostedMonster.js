import { BaseMonster } from "../BaseMonster.js";
import { MonsterAI } from "../monsterLogic/MonsterAI.js";

export class GhostedMonster extends BaseMonster {
  static BASE_CONFIG = {
    mode: "random",
    precision: 0.3,
    aggressiveness: 0.4,
    speed: 1.7,
    size: 15,
    color: {
      hex: "#ffffff",
      hue: 200,
      sat: 10,
      light: 0,
      alpha: 0.1,
    },
    shape: "circle",
    trail: {
      active: false,
      length: 1,
      opacity: 0.5,
    },
  };

  constructor(x, y, overrides = {}) {
    const cfg = MonsterAI._mergeConfig(GhostedMonster.BASE_CONFIG, overrides);
    super(x, y, cfg);
  }
}
