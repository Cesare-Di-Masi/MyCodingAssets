import { BaseMonster } from "../BaseMonster.js";
import { MonsterAI } from "../monsterLogic/MonsterAI.js";

export class BouncerMonster extends BaseMonster {
  static BASE_CONFIG = {
    mode: "bounce",
    precision: 0.6,
    aggressiveness: 0.7,
    speed: 5.5,
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
      length: 20,
      opacity: 0.5,
    },
  };

  constructor(x, y, overrides = {}) {
    const cfg = MonsterAI._mergeConfig(BouncerMonster.BASE_CONFIG, overrides);
    super(x, y, cfg);
  }
}
