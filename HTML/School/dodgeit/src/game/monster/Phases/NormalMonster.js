import { BaseMonster } from "../BaseMonster.js";
import { MonsterAI } from "../monsterLogic/MonsterAI.js";

export class NormalMonster extends BaseMonster {
  static BASE_CONFIG = {
    mode: "random",
    precision: 0.5,
    aggressiveness: 0.5,
    speed: 1.2,
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
      length: 10,
      opacity: 0.5,
    },
  };

  constructor(x, y, overrides = {}) {
    const cfg = MonsterAI._mergeConfig(NormalMonster.BASE_CONFIG, overrides);
    super(x, y, cfg);
  }
}
