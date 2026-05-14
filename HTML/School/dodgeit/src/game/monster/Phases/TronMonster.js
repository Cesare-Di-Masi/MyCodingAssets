import { BaseMonster } from "../BaseMonster.js";
import { MonsterAI } from "../monsterLogic/MonsterAI.js";

export class TronMonster extends BaseMonster {
  static BASE_CONFIG = {
    mode: "grid",
    precision: 0.3,
    aggressiveness: 0.7,
    speed: 1,
    size: 15,
    color: {
      hex: "#ffffff", // Light blue
      hue: 200,
      sat: 100,
      light: 50,
      alpha: 1,
    },
    shape: "circle",
    trail: {
      active: true,
      length: 30,
      opacity: 1,
    },
  };

  constructor(x, y, overrides = {}) {
    const cfg = MonsterAI._mergeConfig(TronMonster.BASE_CONFIG, overrides);
    super(x, y, cfg);
  }
}
