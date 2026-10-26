import { MonsterAI } from "./monsterLogic/MonsterAI.js";

export class BaseMonster extends MonsterAI {
    static BASE_CONFIG = {
        mode: "random",
        precision: 0.5,      // Precisione nel puntare il giocatore (0-1)
        aggressiveness: 0.5, // Frequenza attacchi e bonus velocità (0-1)
        intelligence: 0.5,   // Qualità delle decisioni (0-1)
        reactionSpeed: 0.8,  // Velocità di risposta ai cambi di direzione (0-1)
        speed: 1.2,          // Velocità base di movimento
        attackRange: 45,     // Distanza di ingaggio/attacco
        patrolRadius: 60,    // Raggio di pattugliamento/padding dai bordi
        jitter: 0.1,         // Quantità di variazione casuale nel movimento (0-1)
        size: 15,
        color: {
            hex: "#ff0000",
            hue: 0,
            sat: 100,
            light: 50,
            alpha: 1
        },
        shape: "circle",
        trail: {
            active: true,
            length: 15,
            interval: 30,
            opacity: 0.6
        },
        pulse: {
            active: false,
            speed: 0.005,
            minScale: 0.8,
            maxScale: 1.2
        }
    };

    constructor(x, y, overrides = {}) {
        const cfg = MonsterAI._mergeConfig(BaseMonster.BASE_CONFIG, overrides);
        super(x, y, cfg);
        this.currentScale = 1;
    }

    update(dt, player) {
        // AI and Movement logic from MonsterAI
        super.update(dt, player);
        // Visual updates
        this._updateVisuals(dt);
    }

    _updateVisuals(dt) {
        this.maxTrailLength = this.cfg.trail.length;
        this.intervalBetweenTrailPoints = this.cfg.trail.interval;
        
        // Use VisualEntity's trail update
        super.updateVisuals(dt);

        if (this.cfg.pulse.active) {
            const { speed, minScale, maxScale } = this.cfg.pulse;
            this.currentScale = minScale + ((Math.sin(this.internalTime * speed) + 1) / 2) * (maxScale - minScale);
        } else {
            this.currentScale = 1.0;
        }
    }

    render(ctx) {
        ctx.save();
        // globalAlpha is now handled by _getColorString in fillStyle/strokeStyle
        
        if (this.cfg.trail.active) {
            this._renderTrail(ctx);
        }
        
        this._renderShape(ctx);
        ctx.restore();
    }

    _renderTrail(ctx) {
        if (this.trailPoints.length < 2) return;
        ctx.beginPath();
        
        const color = this.cfg.color;
        let trailColor;
        if (color.hex) {
            trailColor = { ...color, alpha: this.cfg.trail.opacity };
        } else {
            trailColor = {
                hue: color.hue,
                sat: 70,
                light: 50,
                alpha: this.cfg.trail.opacity
            };
        }

        ctx.strokeStyle = this._getColorString(trailColor);
        ctx.lineWidth = this.size * 0.5;
        ctx.lineCap = "round";
        ctx.lineJoin = "round";
        ctx.moveTo(this.x, this.y);
        this.trailPoints.forEach(p => ctx.lineTo(p.x, p.y));
        ctx.stroke();
    }

    _renderShape(ctx) {
        const s = this.size * this.currentScale;
        ctx.beginPath();
        
        const colorStr = this._getColorString(this.cfg.color);
        ctx.fillStyle = colorStr;
        
        // Glow effect
        ctx.shadowBlur = 15;
        ctx.shadowColor = colorStr;

        switch (this.cfg.shape) {
            case "square":
                ctx.rect(this.x - s, this.y - s, s * 2, s * 2);
                break;
            case "triangle":
                ctx.moveTo(this.x, this.y - s);
                ctx.lineTo(this.x - s, this.y + s);
                ctx.lineTo(this.x + s, this.y + s);
                ctx.closePath();
                break;
            default: // circle
                ctx.arc(this.x, this.y, s, 0, Math.PI * 2);
        }
        
        ctx.fill();
        ctx.shadowBlur = 0;
    }
}
