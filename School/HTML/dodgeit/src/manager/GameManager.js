import { NormalMonster } from "../game/monster/Phases/NormalMonster.js";
import { GhostedMonster } from "../game/monster/Phases/GhostedMonster.js";
import { TwinMonster } from "../game/monster/Phases/TwinMonster.js";
import { BouncerMonster } from "../game/monster/Phases/BouncerMonster.js";
import { TronMonster } from "../game/monster/Phases/TronMonster.js";
import { PredictorMonster } from "../game/monster/Phases/PredictorMonster.js";

import { PlayerBaseMovementClass } from "../game/player/PlayerLogic/PlayerBaseMovement.js";

// Definizione delle fasi del gioco con i rispettivi mostri e configurazioni
const Phases = [
  { monster: NormalMonster, overrides: {} },
  { monster: GhostedMonster, overrides: {} },
  { monster: TwinMonster, overrides: {} },
  { monster: BouncerMonster, overrides: {} },
  { monster: TronMonster, overrides: {} },
  { monster: PredictorMonster, overrides: {} },
];

let currentMonster;
export class GameManager {
  constructor(canvas) {
    this.width = canvas.width;
    this.height = canvas.height;
    this.elapsed = 0;
    this.gameOver = false;

    // Settings
    this.config = {
      minSafeSpawnDistance: 300,
      initialDelay: 1500,
    };

    this.player = new PlayerBaseMovementClass(this.width / 2, this.height / 2);
    this.monsters = [];

    // Inizializza il primo mostro
    this.MonsterManager();
  }


  MonsterManager() {
    let rng=Math.floor(Math.random() * Phases.length)
    if(currentMonster == null)
      currentMonster = this.spawnMonster(Phases[rng].monster, Phases[rng].overrides);
    //dopo un tempo radnom fra 3-10 secondi switcha ad una fase random fra le altre, e così via, si sostituisce al precendente mostro
    setTimeout(() => {
      const nextPhase = Phases[Math.floor(Math.random() * Phases.length)];
      let newMonster = this.spawnMonster(nextPhase.monster, nextPhase.overrides);
      newMonster.x = currentMonster.x
      newMonster.y = currentMonster.y
      this.monsters = this.monsters.filter(m => m !== currentMonster);
      currentMonster = null;
      currentMonster = newMonster;
      this.MonsterManager();
    }, 3000 + Math.random() * 15000);
  }

  /**
   * Crea e aggiunge un nuovo mostro al gioco.
   */
  spawnMonster(MonsterClass, overrides = {}) {
    const pos = this._getSafeSpawnPosition();
    //creiamo un cerchio dove spawner il mostro, oggetto del canvas
    const monster = new MonsterClass(pos.x, pos.y, overrides);
    monster.setCanvasBounds(this.width, this.height);
    this.monsters.push(monster);
    return monster;
  }

  _getSafeSpawnPosition() {
    let x, y, dist;
    let attempts = 0;
    const minSafe = Math.min(
      this.config.minSafeSpawnDistance,
      Math.min(this.width, this.height) / 3,
    );

    do {
      x = Math.random() * this.width;
      y = Math.random() * this.height;
      dist = Math.hypot(x - this.player.x, y - this.player.y);
      attempts++;
      if (attempts > 100) break;
    } while (dist < minSafe);

    return { x, y };
  }

  update(dt, input) {
    if (this.gameOver) return;
    this.elapsed += dt;

    // Update Player
    this.player.update(dt, input.keys);
    this._clampEntity(this.player);

    // Update Monsters
    if (this.elapsed >= this.config.initialDelay) {
      for (const monster of this.monsters) {
        monster.update(dt, this.player);
        this._clampEntity(monster);

        // Collision Detection
        if (this._checkCollision(this.player, monster)) {
          this.gameOver = true;
          this._setupGameOverInput();
          console.log("Game Over!");
        }
      }
    }
  }

  _clampEntity(entity) {
    // Durante l'attacco di alcuni comportamenti (come random), il mostro può uscire dai bordi
    if (entity._behaviorState?.isAttacking) return;

    const margin = entity.size || 0;
    entity.x = Math.max(margin, Math.min(this.width - margin, entity.x));
    entity.y = Math.max(margin, Math.min(this.height - margin, entity.y));
  }

  _checkCollision(entityA, entityB) {
    const dist = Math.hypot(entityA.x - entityB.x, entityA.y - entityB.y);
    return dist < entityA.size + entityB.size;
  }

  render(ctx) {
    ctx.clearRect(0, 0, this.width, this.height);

    this.player.render(ctx);

    if (this.elapsed >= this.config.initialDelay) {
      for (const monster of this.monsters) {
        monster.render(ctx);
      }
    }

    if (this.gameOver) {
      this._renderGameOver(ctx);
    }
  }

  _setupGameOverInput() {
    if (this._inputBound) return;
    this._inputBound = true;

    const controller = new AbortController();
    const { signal } = controller;

    const ReloadPage = (event) => {
      event.preventDefault();
      controller.abort();
      location.reload();
    };

    // 1. Blocca il menu a tendina del tasto destro del mouse
    window.addEventListener("contextmenu", (e) => e.preventDefault(), { signal, once: true });

    // 2. Intercetta qualsiasi click del mouse (sinistro, centrale, destro)
    window.addEventListener("pointerdown", ReloadPage, { signal, once: true });

    // 3. Intercetta la pressione di un qualsiasi tasto della tastiera
    window.addEventListener("keydown", ReloadPage, { signal, once: true });
  }

   _renderGameOver(ctx) {
    ctx.save();
    ctx.fillStyle = "rgba(0, 0, 0, 0.7)";
    ctx.fillRect(0, 0, this.width, this.height);

    ctx.fillStyle = "red";
    ctx.font = "bold 48px Arial";
    ctx.textAlign = "center";
    ctx.fillText("GAME OVER", this.width / 2, this.height / 2);

    ctx.fillStyle = "white";
    ctx.font = "20px Arial";
    ctx.fillText(
      "Premi un tasto o clicca ovunque per rigiocare",
      this.width / 2,
      this.height / 2 + 60,
    );
    ctx.restore();
  }

  resize(width, height) {
    this.width = width;
    this.height = height;
    this.monsters.forEach((m) => m.setCanvasBounds(width, height));
  }
}
