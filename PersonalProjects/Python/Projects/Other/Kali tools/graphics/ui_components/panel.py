# graphics/ui_components/panel.py
import pygame

class Panel:
    def __init__(self, x, y, width, height, title, config):
        self.rect = pygame.Rect(x, y, width, height)
        self.title = title
        self.config = config
        self.font = pygame.font.Font(None, 24)
        
    def render(self, screen):
        # Sfondo pannello
        pygame.draw.rect(screen, self.config.COLORS['panel'], self.rect)
        pygame.draw.rect(screen, self.config.COLORS['primary'], self.rect, 2)
        
        # Titolo
        title_text = self.font.render(self.title, True, self.config.COLORS['primary'])
        title_rect = title_text.get_rect(midtop=(self.rect.centerx, self.rect.y + 5))
        screen.blit(title_text, title_rect)