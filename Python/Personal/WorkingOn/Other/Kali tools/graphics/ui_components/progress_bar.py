# graphics/ui_components/progress_bar.py
import pygame

class ProgressBar:
    def __init__(self, x, y, width, height, config):
        self.rect = pygame.Rect(x, y, width, height)
        self.config = config
        self.font = pygame.font.Font(None, 18)
        
    def render(self, screen, current, maximum):
        # Sfondo
        pygame.draw.rect(screen, self.config.COLORS['panel'], self.rect)
        pygame.draw.rect(screen, self.config.COLORS['text'], self.rect, 1)
        
        # Progresso
        if maximum > 0:
            progress = min(current / maximum, 1.0)
            progress_width = int(self.rect.width * progress)
            progress_rect = pygame.Rect(self.rect.x, self.rect.y, progress_width, self.rect.height)
            pygame.draw.rect(screen, self.config.COLORS['success'], progress_rect)
            
        # Testo
        text = f"{current}/{maximum}"
        text_surface = self.font.render(text, True, self.config.COLORS['text_bright'])
        text_rect = text_surface.get_rect(center=self.rect.center)
        screen.blit(text_surface, text_rect)