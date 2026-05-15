import { VisualEntity } from "../../VisualEntity.js";

const _Behaviors = new Map();

/**
 * Register a new AI movement behavior.
 */
export function registerBehavior(name, behavior) {
  if (_Behaviors.has(name)) {
    console.warn(`[MonsterAI] Overwriting existing behavior "${name}".`);
  }
  _Behaviors.set(name, behavior);
}

export function getRegisteredBehaviors() {
  return [..._Behaviors.keys()];
}

// ── Built-in behaviors ────────────────────────────────────────────────────

registerBehavior("mirror", {
  update(monster, twin, player) {
    monster.x = twin.x * -1;
    monster.y = twin.y * -1;
  },
});

registerBehavior("static", {
  update() {},
});

registerBehavior("hunt", {
  update(monster, player) {
    _pushHistory(monster, player);
    const idx = Math.max(
      0,
      monster.history.length - 1 - (monster.cfg.latency || 0),
    );
    const past = monster.history[idx] ?? { x: monster.x, y: monster.y };
    monster.target = _applyError(
      past,
      monster.cfg.precision ?? 0.5,
      monster.cfg.jitter,
    );
  },
});

registerBehavior("random", {
  init(monster) {
    monster._behaviorState.isAttacking = false;
    monster._behaviorState.attackCooldown = 0;
    _chooseNewEdge(monster);
  },
  update(monster, player, dt) {
    _pushHistory(monster, player);

    if (monster._behaviorState.attackCooldown > 0) {
      monster._behaviorState.attackCooldown -= dt;
    }

    if (monster._behaviorState.isAttacking) {
      const dx = monster.target.x - monster.x;
      const dy = monster.target.y - monster.y;
      const dist = Math.hypot(dx, dy);

      if (dist < (monster.cfg.attackRange ?? 45)) {
        monster._behaviorState.isAttacking = false;
        // Aggressiveness reduces cooldown: higher aggressiveness = lower cooldown
        const baseCooldown = 2.0;
        monster._behaviorState.attackCooldown =
          baseCooldown * (1 - (monster.cfg.aggressiveness ?? 0.5));
        _chooseNewEdge(monster);
      }
      return;
    }

    const idx = Math.max(
      0,
      monster.history.length - 1 - (monster.cfg.latency || 0),
    );
    const past = monster.history[idx] ?? { x: monster.x, y: monster.y };

    const alignedX = Math.abs(monster.x - past.x) < 30;
    const alignedY = Math.abs(monster.y - past.y) < 30;

    // Aggressiveness increases attack chance: higher aggressiveness = higher probability
    const attackThreshold = 0.3 + (monster.cfg.aggressiveness ?? 0.5) * 0.6;

    if ((alignedX || alignedY) && monster._behaviorState.attackCooldown <= 0) {
      if (Math.random() < attackThreshold) {
        const isPrecise = Math.random() < (monster.cfg.precision ?? 0.5);
        const errorAmount = isPrecise ? 50 : 400;
        monster.target = _applyError(
          { x: past.x, y: past.y },
          1 - errorAmount / 1000,
          monster.cfg.jitter,
        );
        monster._behaviorState.isAttacking = true;
      } else if (Math.random() < 0.05) {
        // Small chance to change edge if not attacking
        _chooseNewEdge(monster);
      }
    } else {
      monster.target = monster._behaviorState.targetEdge;
      const dist = Math.hypot(
        monster.target.x - monster.x,
        monster.target.y - monster.y,
      );
      if (dist < 40) _chooseNewEdge(monster);
    }
  },
});

registerBehavior("orbit", {
  update(monster, player) {
    const speed = monster.cfg.orbitSpeed ?? 0.002;
    const radius = monster.cfg.orbitRadius ?? 150;
    const angle = monster.internalTime * speed;
    monster.target = {
      x: player.x + Math.cos(angle) * radius,
      y: player.y + Math.sin(angle) * radius,
    };
  },
});

registerBehavior("prediction", {
  update(monster, player) {
    _pushHistory(monster, player);
    if (monster.history.length < 5) {
      monster.target = { x: player.x, y: player.y };
      return;
    }
    const last = monster.history.at(-1);
    const prev = monster.history.at(-5);
    // Prediction power scaling: higher precision makes the prediction more focused
    const p =
      (monster.cfg.predictionPower ?? 10) * (monster.cfg.precision ?? 0.5) * 2;

    monster.target = {
      x: player.x + ((last.x - prev.x) / 5) * p,
      y: player.y + ((last.y - prev.y) / 5) * p,
    };
  },
});

registerBehavior("bounce", {
  //dopo un numero tra 5-10 rimbalzi cambia direzione in modo random, e così via
  init(monster) {
    const angle = Math.random() * 2 * Math.PI;
    monster._behaviorState.bounceDir = { x: Math.cos(angle), y: Math.sin(angle) };
    monster._behaviorState.bounceCount = 0;
    let maxBounces = 5 + Math.floor(Math.random() * 6);
    monster._behaviorState.maxBounces = maxBounces;
    if(monster._behaviorState.bounceCount >= monster._behaviorState.maxBounces) {
      const newAngle = Math.random() * 0.5 * Math.PI;
      monster._behaviorState.bounceDir = { x: Math.cos(newAngle), y: Math.sin(newAngle) };
      monster._behaviorState.bounceCount = 0;
      monster._behaviorState.maxBounces = 5 + Math.floor(Math.random() * 6);
    }
  },
  update() {},
});

registerBehavior("grid", {
  init(monster) {
    monster._behaviorState.currentDir = Math.floor(Math.random() * 4);
    monster._behaviorState.lastTurnPos = { x: monster.x, y: monster.y };
  },

  update(monster, player, dt) {
    _pushHistory(monster, player);

    // --- 1. BIAS DI PREDIZIONE DINAMICO ---
    // Più aggressività = più anticipo. Più precisione = mira più accurata.
    // Range del moltiplicatore: da 0.2 (quasi nulla) a 3.5 (molto forte)
    const agg = monster.cfg.aggressiveness ?? 0.5;
    const pre = monster.cfg.precision ?? 0.5;
    const predictionScale = agg * 2.0 + pre * 1.5;

    const idx = Math.max(0, monster.history.length - 1 - 10);
    const pastPos = monster.history[idx] ?? player;

    const targetPos = {
      x: player.x + (player.x - pastPos.x) * predictionScale,
      y: player.y + (player.y - pastPos.y) * predictionScale,
    };

    // --- 2. GESTIONE BORDI (ANTI-INCASTRAMENTO) ---
    const bounds = monster.bounds || { width: 800, height: 600 };
    const margin = 40; // Distanza dal bordo per iniziare a girare
    let forceTurn = false;

    const cur = monster._behaviorState.currentDir;
    // Controllo se sto andando contro un muro
    if (cur === 0 && monster.x > bounds.width - margin) forceTurn = true; // Destra
    if (cur === 2 && monster.x < margin) forceTurn = true; // Sinistra
    if (cur === 1 && monster.y > bounds.height - margin) forceTurn = true; // Giù
    if (cur === 3 && monster.y < margin) forceTurn = true; // Su

    // --- 3. LOGICA DI MOVIMENTO E SVOLTA ---
    const distFromLastTurn = Math.hypot(
      monster.x - monster._behaviorState.lastTurnPos.x,
      monster.y - monster._behaviorState.lastTurnPos.y,
    );

    // Gira se forzato dal bordo o se la logica di caccia lo richiede
    if (forceTurn || distFromLastTurn > 40) {
      const dx = targetPos.x - monster.x;
      const dy = targetPos.y - monster.y;
      let wantDir = cur;

      if (cur === 0 || cur === 2) {
        // Orizzontale -> valuta Verticale
        if (forceTurn || Math.abs(dy) > Math.abs(dx) + 100 * (1 - pre)) {
          wantDir = dy > 0 ? 1 : 3;
          // Se siamo al bordo Y, forza la direzione opposta al muro
          if (monster.y < margin) wantDir = 1;
          if (monster.y > bounds.height - margin) wantDir = 3;
        }
      } else {
        // Verticale -> valuta Orizzontale
        if (forceTurn || Math.abs(dx) > Math.abs(dy) + 100 * (1 - pre)) {
          wantDir = dx > 0 ? 0 : 2;
          if (monster.x < margin) wantDir = 0;
          if (monster.x > bounds.width - margin) wantDir = 2;
        }
      }

      // Applica la svolta (evitando i 180° stile Tron)
      if (wantDir !== (cur + 2) % 4 && wantDir !== cur) {
        monster._behaviorState.currentDir = wantDir;
        monster._behaviorState.lastTurnPos = { x: monster.x, y: monster.y };
      }
    }

    // --- 4. OUTPUT TARGET ---
    const dirs = [
      { x: 1, y: 0 },
      { x: 0, y: 1 },
      { x: -1, y: 0 },
      { x: 0, y: -1 },
    ];
    const finalDir = dirs[monster._behaviorState.currentDir];

    monster.target = {
      x: monster.x + finalDir.x * 100,
      y: monster.y + finalDir.y * 100,
    };
  },
});

// ── Shared behavior helpers ───────────────────────────────────────────────

function _chooseNewEdge(monster) {
  const bounds = monster.bounds || { width: 800, height: 600 };
  const padding = monster.cfg.patrolRadius ?? 60;
  const side = Math.floor(Math.random() * 4);
  const randomPos = Math.random() * 0.8 + 0.1;

  switch (side) {
    case 0:
      monster._behaviorState.targetEdge = {
        x: bounds.width * randomPos,
        y: padding,
      };
      break;
    case 1:
      monster._behaviorState.targetEdge = {
        x: bounds.width - padding,
        y: bounds.height * randomPos,
      };
      break;
    case 2:
      monster._behaviorState.targetEdge = {
        x: bounds.width * randomPos,
        y: bounds.height - padding,
      };
      break;
    case 3:
      monster._behaviorState.targetEdge = {
        x: padding,
        y: bounds.height * randomPos,
      };
      break;
  }
}

function _pushHistory(monster, player) {
  if (!monster.history) monster.history = [];
  monster.history.push({ x: player.x, y: player.y });
  if (monster.history.length > 50) monster.history.shift();
}

function _applyError(point, precision, jitter = 0) {
  // Higher precision = lower random offset
  const error = (1 - precision) * 200;
  const jitterAmount = jitter * 50;
  return {
    x: point.x + (Math.random() * 2 - 1) * (error + jitterAmount),
    y: point.y + (Math.random() * 2 - 1) * (error + jitterAmount),
  };
}

export class MonsterAI extends VisualEntity {
  constructor(x, y, cfg) {
    super(x, y, cfg.size || 12.5, cfg.color || 0);
    this.cfg = cfg;
    this.target = { x, y };
    this.history = [];
    this.internalTime = 0;
    this.bounds = { width: 800, height: 600 };
    this._behaviorState = {};

    // Default AI values if not provided
    this.cfg.precision = this.cfg.precision ?? 0.5;
    this.cfg.aggressiveness = this.cfg.aggressiveness ?? 0.5;
    this.cfg.intelligence = this.cfg.intelligence ?? 0.5;
    this.cfg.latency = this.cfg.latency ?? 0;
    this.cfg.reactionSpeed = this.cfg.reactionSpeed ?? 0.8;
    this.cfg.speed = this.cfg.speed ?? 1.2;
    this.cfg.predictionPower = this.cfg.predictionPower ?? 10;
    this.cfg.orbitSpeed = this.cfg.orbitSpeed ?? 0.002;
    this.cfg.orbitRadius = this.cfg.orbitRadius ?? 150;
    this.cfg.attackRange = this.cfg.attackRange ?? 45;
    this.cfg.patrolRadius = this.cfg.patrolRadius ?? 60;
    this.cfg.jitter = this.cfg.jitter ?? 0.1;

    this._initBehavior(cfg.mode);
  }

  setCanvasBounds(width, height) {
    this.bounds = { width, height };
  }

  setMode(mode) {
    this.cfg.mode = mode;
    this._behaviorState = {};
    this._initBehavior(mode);
  }

  update(dt, player) {
    this.internalTime += dt;
    _Behaviors.get(this.cfg.mode)?.update(this, player, dt);

    if (this.cfg.mode === "bounce") {
      this._moveBounce(dt);
    } else if (this.cfg.mode !== "static") {
      this._moveToTarget(dt);
    }
  }

  _initBehavior(mode) {
    const behavior = _Behaviors.get(mode);
    if (!behavior) {
      console.warn(
        `[MonsterAI] Unknown behavior "${mode}" — falling back to "static".`,
      );
      this.cfg.mode = "static";
      return;
    }
    behavior.init?.(this);
  }

  _moveToTarget(dt) {
    const dx = this.target.x - this.x;
    const dy = this.target.y - this.y;
    const dist = Math.hypot(dx, dy);
    if (dist < 1) return;

    // Aggressiveness can slightly boost speed: max 30% boost
    const speedBoost = 1 + (this.cfg.aggressiveness ?? 0.5) * 0.3;
    const step = (this.cfg.speed || 1) * speedBoost * dt;

    this.x += (dx / dist) * Math.min(step, dist);
    this.y += (dy / dist) * Math.min(step, dist);
  }

  _moveBounce(dt) {
    const d = this._behaviorState.bounceDir;
    const b = this.bounds;
    const speedBoost = 1 + (this.cfg.aggressiveness ?? 0.5) * 0.3;
    const sp = (this.cfg.speed || 1) * speedBoost * dt;

    this.x += d.x * sp;
    this.y += d.y * sp;

    if (this.x < 0 || this.x > b.width) {
      d.x *= -1;
      this.x = Math.max(0, Math.min(b.width, this.x));
    }
    if (this.y < 0 || this.y > b.height) {
      d.y *= -1;
      this.y = Math.max(0, Math.min(b.height, this.y));
    }
  }

  static _mergeConfig(base, overrides) {
    const result = { ...base };
    for (const [key, val] of Object.entries(overrides)) {
      const isPlainObject =
        val !== null && typeof val === "object" && !Array.isArray(val);
      result[key] = isPlainObject
        ? MonsterAI._mergeConfig(base[key] ?? {}, val)
        : val;
    }
    return result;
  }
}
