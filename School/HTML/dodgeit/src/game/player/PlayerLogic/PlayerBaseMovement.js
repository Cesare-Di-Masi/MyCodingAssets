import { VisualEntity } from "../../VisualEntity.js";

export class PlayerBaseMovementClass extends VisualEntity {
    constructor(x, y) {
        super(x, y, 12, 120); // 120 è Verde
        this.baseSpeed = 1;
        this.crouchModifier = 0.5;
        this.isCrouching = false;

        // Dash settings
        this.isDashing = false;
        this.dashTimer = 0;
        this.dashDuration = 100; // ms
        this.dashCooldown = 0;
        this.dashCooldownTime = 0; // ms
        this.dashMultiplier = 4.0;
        this.dashDirection = { x: 0, y: 0 };
        this.lastDashKeyState = false;

        this.inputsInverted = false;
        this.modifiersSwapped = false;
    }

    update(deltaTime, keys) {
        // Update Cooldowns
        if (this.dashCooldown > 0) {
            this.dashCooldown -= deltaTime;
            if (this.dashCooldown < 0) {
                this.dashCooldown = 0;
            }
        }

        // Handle Dash Logic
        if (this.isDashing) {
            this.dashTimer -= deltaTime;
            if (this.dashTimer <= 0) {
                this.isDashing = false;
            } else {
                this.x += this.dashDirection.x * this.baseSpeed * this.dashMultiplier * deltaTime;
                this.y += this.dashDirection.y * this.baseSpeed * this.dashMultiplier * deltaTime;
                super.updateVisuals(deltaTime);
                // Aggiornato per includere il click sinistro anche durante lo stato di dash attivo
                this.lastDashKeyState = keys['ShiftLeft'] || keys['ShiftRight'] || keys['MouseLeft'];
                return; // Skip normal movement while dashing
            }
        }

        this.isCrouching = keys['ControlLeft'] || keys['ControlRight'];

        let moveX = 0;
        let moveY = 0;

        let up = keys['ArrowUp'] || keys['KeyW'];
        let down = keys['ArrowDown'] || keys['KeyS'];
        let left = keys['ArrowLeft'] || keys['KeyA'];
        let right = keys['ArrowRight'] || keys['KeyD'];

        if (this.inputsInverted) {
            [up, down] = [down, up];
            [left, right] = [right, left];
        }

        if (up) moveY -= 1;
        if (down) moveY += 1;
        if (left) moveX -= 1;
        if (right) moveX += 1;

        // Corretto il refuso e integrato keys['MouseLeft']
        const dashKeyPressed = keys['ShiftLeft'] || keys['ShiftRight'] || keys['MouseLeft'];
        const dashJustPressed = dashKeyPressed && !this.lastDashKeyState;

        // Trigger Dash with Shift / MouseLeft + movement
        if (dashJustPressed && this.dashCooldown <= 0 && (moveX !== 0 || moveY !== 0)) {
            this.isDashing = true;
            this.dashTimer = this.dashDuration;
            this.dashCooldown = this.dashCooldownTime;

            const mag = Math.hypot(moveX, moveY);
            this.dashDirection = { x: moveX / mag, y: moveY / mag };
        }

        this.lastDashKeyState = dashKeyPressed;

        if (moveX !== 0 || moveY !== 0) {
            let speed = this.baseSpeed;
            if (this.isCrouching) {
                speed *= this.crouchModifier;
            }
            if (moveX !== 0 && moveY !== 0) {
                speed *= 0.707;
            }
            this.x += moveX * speed * deltaTime;
            this.y += moveY * speed * deltaTime;
        }

        super.updateVisuals(deltaTime);
    }
}