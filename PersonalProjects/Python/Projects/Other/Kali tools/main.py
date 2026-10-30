# main.py - Entry point
import pygame
import sys
from graphics.main_window import MainWindow
from core.config import Config

def main():
    pygame.init()
    config = Config()
    window = MainWindow(config)
    window.run()
    pygame.quit()
    sys.exit()

if __name__ == "__main__":
    main()