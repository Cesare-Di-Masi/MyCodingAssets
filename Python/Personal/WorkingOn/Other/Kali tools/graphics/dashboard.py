# graphics/dashboard.py
import pygame
from graphics.ui_components.panel import Panel
from graphics.ui_components.button import Button
from graphics.ui_components.progress_bar import ProgressBar

class Dashboard:
    def __init__(self, screen, config, leveling, companion_system):
        self.screen = screen
        self.config = config
        self.leveling = leveling
        self.companion_system = companion_system
        
        # Font
        self.font = pygame.font.Font(None, 24)
        self.small_font = pygame.font.Font(None, 18)
        
        # Pannelli
        self.user_panel = Panel(20, 20, 300, 150, "User Status", config)
        self.companion_panel = Panel(20, 180, 300, 150, "NEXUS Companion", config)
        self.tools_panel = Panel(20, 340, 300, 200, "Available Tools", config)
        self.log_panel = Panel(340, 20, 600, 300, "Activity Log", config)
        
        # Pulsanti azione
        self.action_buttons = [
            Button(340, 340, 180, 40, "Network Scan", config),
            Button(530, 340, 180, 40, "WiFi Attack", config),
            Button(720, 340, 180, 40, "Web Exploit", config),
            Button(340, 390, 180, 40, "Phishing", config),
            Button(530, 390, 180, 40, "Password Crack", config),
            Button(720, 390, 180, 40, "System Hack", config)
        ]
        
        # Barre progresso
        self.user_xp_bar = ProgressBar(40, 100, 260, 20, config)
        self.companion_xp_bar = ProgressBar(40, 260, 260, 20, config)
        
        # Log attività
        self.activity_logs = [
            "System initialized",
            "NEXUS companion activated",
            "Network mapping started"
        ]
        
    def handle_event(self, event):
        for button in self.action_buttons:
            if button.handle_event(event):
                self._execute_action(button.text)
                
    def _execute_action(self, action):
        self.activity_logs.append(f"Executing: {action}")
        self.leveling.add_xp(50)
        
    def update(self):
        # Limita log a 20 elementi
        if len(self.activity_logs) > 20:
            self.activity_logs = self.activity_logs[-20:]
            
    def render(self):
        # Pannello utente
        self.user_panel.render(self.screen)
        user_text = self.font.render(f"Level: {self.leveling.user_level}", True, self.config.COLORS['text_bright'])
        self.screen.blit(user_text, (40, 50))
        self.user_xp_bar.render(self.screen, self.leveling.user_xp, self.leveling.xp_for_next_level())
        
        # Pannello companion
        self.companion_panel.render(self.screen)
        comp_text = self.font.render(f"Level: {self.companion_system.level}", True, self.config.COLORS['primary'])
        self.screen.blit(comp_text, (40, 210))
        self.companion_xp_bar.render(self.screen, self.companion_system.xp, self.companion_system.xp_for_next_level())
        mood_text = self.small_font.render(f"Mood: {self.companion_system.personality.mood}", True, self.config.COLORS['warning'])
        self.screen.blit(mood_text, (40, 290))
        
        # Pannello strumenti
        self.tools_panel.render(self.screen)
        y_offset = 370
        for tool in self.companion_system.get_unlocked_features():
            tool_text = self.small_font.render(f"• {tool}", True, self.config.COLORS['text'])
            self.screen.blit(tool_text, (40, y_offset))
            y_offset += 25
            
        # Pannello log
        self.log_panel.render(self.screen)
        y_offset = 50
        for log in self.activity_logs:
            log_text = self.small_font.render(log, True, self.config.COLORS['text'])
            self.screen.blit(log_text, (360, y_offset))
            y_offset += 25
            
        # Pulsanti azione
        for button in self.action_buttons:
            button.render(self.screen)