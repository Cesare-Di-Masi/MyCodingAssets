# graphics/network_map.py
import pygame
import random
import math
from graphics.ui_components.panel import Panel

class NetworkMap:
    def __init__(self, screen, config):
        self.screen = screen
        self.config = config
        
        # Genera rete
        self.nodes = []
        self.connections = []
        self.selected_node = None
        self.generate_network()
        
        # Font
        self.font = pygame.font.Font(None, 18)
        
    def generate_network(self):
        # Crea nodi
        node_types = ["router", "server", "pc", "iot", "mobile"]
        for i in range(12):
            x = random.randint(400, 1200)
            y = random.randint(450, 750)
            self.nodes.append({
                'id': i,
                'x': x,
                'y': y,
                'type': random.choice(node_types),
                'security': random.randint(1, 5),
                'compromised': False,
                'name': f"{random.choice(['Corp', 'Tech', 'Net', 'Sys'])}-{random.randint(1, 99)}"
            })
            
        # Crea connessioni
        for i in range(len(self.nodes)):
            for j in range(i+1, len(self.nodes)):
                if random.random() < 0.25:  # 25% di connessione
                    self.connections.append((i, j))
                    
    def handle_event(self, event):
        if event.type == pygame.MOUSEBUTTONDOWN:
            mouse_x, mouse_y = pygame.mouse.get_pos()
            
            # Controlla nodi
            for node in self.nodes:
                dist = math.sqrt((mouse_x - node['x'])**2 + (mouse_y - node['y'])**2)
                if dist < 20:
                    self.selected_node = node
                    self._show_node_info(node)
                    
    def _show_node_info(self, node):
        # Mostra informazioni sul nodo
        print(f"Nodo selezionato: {node['name']} ({node['type']}) - Sicurezza: {node['security']}")
        
    def update(self):
        # Simula cambiamenti nella rete
        if random.random() < 0.01:  # 1% di cambiamento per frame
            node = random.choice(self.nodes)
            node['security'] = max(1, min(5, node['security'] + random.randint(-1, 1)))
            
    def render(self):
        # Disegna connessioni
        for conn in self.connections:
            node1 = self.nodes[conn[0]]
            node2 = self.nodes[conn[1]]
            
            # Colore in base allo stato
            if node1['compromised'] and node2['compromised']:
                color = self.config.COLORS['danger']
            elif node1['compromised'] or node2['compromised']:
                color = self.config.COLORS['warning']
            else:
                color = self.config.COLORS['text']
                
            pygame.draw.line(self.screen, color, 
                           (node1['x'], node1['y']), 
                           (node2['x'], node2['y']), 2)
            
        # Disegna nodi
        for node in self.nodes:
            # Colore in base al tipo
            if node['compromised']:
                color = self.config.COLORS['danger']
            elif node['type'] == 'server':
                color = self.config.COLORS['primary']
            elif node['type'] == 'router':
                color = self.config.COLORS['warning']
            elif node['type'] == 'iot':
                color = self.config.COLORS['secondary']
            else:
                color = self.config.COLORS['text']
                
            # Disegna nodo
            pygame.draw.circle(self.screen, color, (node['x'], node['y']), 20)
            pygame.draw.circle(self.screen, self.config.COLORS['text_bright'], 
                             (node['x'], node['y']), 20, 2)
            
            # Indicatore sicurezza
            for i in range(node['security']):
                pygame.draw.circle(self.screen, self.config.COLORS['warning'], 
                                 (node['x'] - 15 + i*8, node['y'] - 25), 3)
                
            # Nome nodo
            name_text = self.font.render(node['name'], True, self.config.COLORS['text'])
            name_rect = name_text.get_rect(center=(node['x'], node['y'] + 35))
            self.screen.blit(name_text, name_rect)