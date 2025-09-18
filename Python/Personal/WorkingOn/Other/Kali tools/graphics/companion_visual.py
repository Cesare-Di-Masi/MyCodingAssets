# graphics/companion_visual.py
import pygame
import random
import math
from graphics.ui_components.notification import Notification

class CompanionVisual:
    def __init__(self, screen, config, companion_system):
        self.screen = screen
        self.config = config
        self.companion_system = companion_system
        
        # Posizione e dimensioni
        self.x = 1000
        self.y = 500
        self.size = 80
        self.base_size = 80
        
        # Animazione
        self.animation_frame = 0
        self.pulse_phase = 0
        
        # Azioni
        self.idle_actions = ["scan", "learn", "rest", "explore"]
        self.current_action = "idle"
        self.action_timer = 0
        
        # Particelle
        self.particles = []
        
    def update(self):
        self.animation_frame += 1
        self.pulse_phase += 0.1
        
        # Cambia azione periodicamente
        self.action_timer += 1
        if self.action_timer > 120:  # Ogni 2 secondi a 60 FPS
            self.current_action = random.choice(self.idle_actions)
            self.action_timer = 0
            
        # Movimento casuale
        if self.current_action == "explore":
            self.x += random.randint(-3, 3)
            self.y += random.randint(-3, 3)
            
        # Limiti schermo
        self.x = max(50, min(self.x, self.config.SCREEN_WIDTH - 50))
        self.y = max(50, min(self.y, self.config.SCREEN_HEIGHT - 50))
        
        # Aggiorna particelle
        self._update_particles()
        
        # Effetto pulsante
        self.size = self.base_size + math.sin(self.pulse_phase) * 5
        
    def _update_particles(self):
        # Aggiungi nuove particelle
        if random.random() < 0.1:
            self.particles.append({
                'x': self.x + random.randint(-20, 20),
                'y': self.y + random.randint(-20, 20),
                'life': 30,
                'color': random.choice([
                    self.config.COLORS['primary'],
                    self.config.COLORS['secondary'],
                    self.config.COLORS['success']
                ])
            })
            
        # Aggiorna particelle esistenti
        for particle in self.particles[:]:
            particle['life'] -= 1
            particle['y'] -= 1
            if particle['life'] <= 0:
                self.particles.remove(particle)
                
    def render(self):
        # Renderizza particelle
        for particle in self.particles:
            alpha = particle['life'] / 30
            size = int(3 * alpha)
            if size > 0:
                pygame.draw.circle(self.screen, particle['color'], 
                                 (int(particle['x']), int(particle['y'])), size)
        
        # Aura
        aura_radius = int(self.size // 2 + 15 + math.sin(self.pulse_phase * 2) * 5)
        aura_color = self.config.COLORS['primary'] if self.companion_system.level >= 10 else self.config.COLORS['secondary']
        pygame.draw.circle(self.screen, aura_color, (int(self.x), int(self.y)), aura_radius, 2)
        
        # Corpo principale
        body_color = self.config.COLORS['primary']
        if self.companion_system.level >= 15:
            body_color = self.config.COLORS['success']
        elif self.companion_system.level >= 5:
            body_color = self.config.COLORS['warning']
            
        pygame.draw.circle(self.screen, body_color, (int(self.x), int(self.y)), self.size // 2)
        
        # Occhi
        eye_offset = self.size // 4
        pygame.draw.circle(self.screen, (0, 0, 0), 
                         (int(self.x - eye_offset), int(self.y - 5)), 5)
        pygame.draw.circle(self.screen, (0, 0, 0), 
                         (int(self.x + eye_offset), int(self.y - 5)), 5)
        
        # Nome e livello
        name_text = self.font.render("NEXUS", True, self.config.COLORS['text_bright'])
        level_text = self.font.render(f"Lv.{self.companion_system.level}", True, self.config.COLORS['warning'])
        
        name_rect = name_text.get_rect(center=(self.x, self.y + self.size // 2 + 20))
        level_rect = level_text.get_rect(center=(self.x, self.y + self.size // 2 + 45))
        
        self.screen.blit(name_text, name_rect)
        self.screen.blit(level_text, level_rect)
        
        # Indicatore azione
        if self.current_action != "idle":
            action_text = self.small_font.render(self.current_action.upper(), True, self.config.COLORS['text'])
            action_rect = action_text.get_rect(center=(self.x, self.y - self.size // 2 - 20))
            self.screen.blit(action_text, action_rect)