# core/config.py
class Config:
    def __init__(self):
        self.SCREEN_WIDTH = 1400
        self.SCREEN_HEIGHT = 900
        self.FPS = 60
        self.DEBUG = True
        
        # Colori tema cyberpunk
        self.COLORS = {
            'background': (10, 10, 30),
            'primary': (0, 255, 255),
            'secondary': (255, 0, 255),
            'success': (0, 255, 0),
            'danger': (255, 0, 0),
            'warning': (255, 255, 0),
            'panel': (20, 20, 40),
            'text': (200, 200, 200),
            'text_bright': (255, 255, 255)
        }
        
        # Percorsi risorse
        self.ASSETS_PATH = "assets/"
        self.FONTS_PATH = "assets/fonts/"