import { BaseMonster } from "../BaseMonster.js";
import { MonsterAI } from "../monsterLogic/MonsterAI.js";

export class PredictorMonster extends BaseMonster {
  static BASE_CONFIG = {
    mode: "prediction",
    precision: 0.5,
    aggressiveness: 0.4,
    speed: 0.84,
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
    const cfg = MonsterAI._mergeConfig(PredictorMonster.BASE_CONFIG, overrides);
    super(x, y, cfg);
  }
}
