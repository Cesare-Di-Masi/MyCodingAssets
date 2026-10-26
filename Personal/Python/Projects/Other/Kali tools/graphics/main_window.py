# graphics/main_window.py
import pygame
import sys
from graphics.dashboard import Dashboard
from graphics.companion_visual import CompanionVisual
from graphics.network_map import NetworkMap
from graphics.simulation_engine import SimulationEngine
from core.leveling_system import LevelingSystem
from core.companion_system import CompanionSystem

class MainWindow:
    def __init__(self, config):
        self.config = config
        self.screen = pygame.display.set_mode((config.SCREEN_WIDTH, config.SCREEN_HEIGHT))
        pygame.display.set_caption("Kali Multi-Tool v3.0 - Cyber Simulator")
        
        # Inizializza sistemi
        self.leveling = LevelingSystem()
        self.companion_system = CompanionSystem()
        self.simulation = SimulationEngine(self.leveling, self.companion_system)
        
        # Componenti grafici
        self.dashboard = Dashboard(self.screen, self.config, self.leveling, self.companion_system)
        self.companion_visual = CompanionVisual(self.screen, self.config, self.companion_system)
        self.network_map = NetworkMap(self.screen, self.config)
        
        # Clock per FPS
        self.clock = pygame.time.Clock()
        self.running = True
        
        # Avvia companion in background
        self.companion_system.start()
        
    def run(self):
        while self.running:
            self._handle_events()
            self._update()
            self._render()
            self.clock.tick(self.config.FPS)
            
    def _handle_events(self):
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                self.running = False
            self.dashboard.handle_event(event)
            self.network_map.handle_event(event)
            
    def _update(self):
        self.simulation.update()
        self.companion_visual.update()
        self.dashboard.update()
        self.network_map.update()
        
    def _render(self):
        self.screen.fill(self.config.COLORS['background'])
        
        # Renderizza componenti
        self.network_map.render()
        self.dashboard.render()
        self.companion_visual.render()
        
        pygame.display.flip()