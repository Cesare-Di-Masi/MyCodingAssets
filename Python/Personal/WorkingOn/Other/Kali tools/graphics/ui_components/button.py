# graphics/ui_components/button.py
import pygame

class Button:
    def __init__(self, x, y, width, height, text, config):
        self.rect = pygame.Rect(x, y, width, height)
        self.text = text
        self.config = config
        self.font = pygame.font.Font(None, 24)
        self.hovered = False
        self.clicked = False
        
    def handle_event(self, event):
        if event.type == pygame.MOUSEMOTION:
            self.hovered = self.rect.collidepoint(event.pos)
        elif event.type == pygame.MOUSEBUTTONDOWN:
            if self.rect.collidepoint(event.pos):
                self.clicked = True
                return True
        elif event.type == pygame.MOUSEBUTTONUP:
            self.clicked = False
        return False
        
    def render(self, screen):
        # Colore in base allo stato
        if self.clicked:
            color = self.config.COLORS['panel']
        elif self.hovered:
            color = (50, 50, 100)
        else:
            color = (30, 30, 60)
            
        pygame.draw.rect(screen, color, self.rect)
        pygame.draw.rect(screen, self.config.COLORS['primary'], self.rect, 2)
        
        # Testo
        text_surface = self.font.render(self.text, True, self.config.COLORS['text_bright'])
        text_rect = text_surface.get_rect(center=self.rect.center)
        screen.blit(text_surface, text_rect)