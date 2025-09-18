# graphics/simulation_engine.py
import time
import random
from core.idle_tasks import IdleTaskManager

class SimulationEngine:
    def __init__(self, leveling, companion_system):
        self.leveling = leveling
        self.companion_system = companion_system
        self.idle_tasks = IdleTaskManager()
        self.last_update = time.time()
        
    def update(self):
        current_time = time.time()
        
        # Aggiorna ogni secondo
        if current_time - self.last_update >= 1.0:
            self._process_idle_tasks()
            self._process_companion_actions()
            self.last_update = current_time
            
    def _process_idle_tasks(self):
        task = self.idle_tasks.get_random_task()
        result = task.execute()
        
        if result['success']:
            xp_gained = random.randint(5, 20)
            self.leveling.add_xp(xp_gained)
            self.companion_system.add_xp(xp_gained // 2)
            
    def _process_companion_actions(self):
        action = random.choice([
            'network_scan', 'vulnerability_check', 
            'password_analysis', 'exploit_research'
        ])
        
        # Esegui azione companion
        if action == 'network_scan':
            self.companion_system.personality.traits['curiosity'] += 0.01
        elif action == 'vulnerability_check':
            self.companion_system.personality.traits['efficiency'] += 0.01
        elif action == 'password_analysis':
            self.companion_system.personality.traits['stealth'] += 0.01
        elif action == 'exploit_research':
            self.companion_system.personality.traits['aggressiveness'] += 0.01