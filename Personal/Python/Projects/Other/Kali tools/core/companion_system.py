# core/companion_system.py
import threading
import time
import random
from core.idle_tasks import IdleTaskManager
from companion.evolution import EvolutionEngine
from companion.personality import PersonalityCore
from companion.skills import SkillManager

class CompanionSystem:
    def __init__(self):
        self.level = 1
        self.xp = 0
        self.active = False
        
        # Sistemi companion
        self.evolution = EvolutionEngine()
        self.personality = PersonalityCore()
        self.skills = SkillManager()
        self.idle_tasks = IdleTaskManager()
        
    def start(self):
        self.active = True
        companion_thread = threading.Thread(target=self._run_companion)
        companion_thread.daemon = True
        companion_thread.start()
        
    def _run_companion(self):
        while self.active:
            # Esegui task
            task = self.idle_tasks.get_random_task()
            result = task.execute()
            
            # Guadagna XP
            xp_gained = random.randint(1, 10)
            self.xp += xp_gained
            
            # Controlla evoluzione
            if self.xp >= self.xp_for_next_level():
                self._level_up()
                
            # Evoluzione personalità
            self.personality.evolve(result)
            
            # Impara abilità
            if random.random() < 0.05:
                self.skills.learn_random_skill()
                
            time.sleep(60)  # Esegui ogni minuto
            
    def _level_up(self):
        self.level += 1
        self.xp = 0
        self.evolution.evolve(self.level)
        print(f"NEXUS raggiunto il livello {self.level}!")
        
    def xp_for_next_level(self):
        return self.level * 75
        
    def get_unlocked_features(self):
        return self.evolution.get_unlocked_features(self.level)