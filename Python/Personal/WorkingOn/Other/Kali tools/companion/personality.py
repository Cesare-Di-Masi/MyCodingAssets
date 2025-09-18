# companion/personality.py
class PersonalityCore:
    def __init__(self):
        self.traits = {
            "aggressiveness": 0.3,
            "stealth": 0.7,
            "curiosity": 0.9,
            "efficiency": 0.6
        }
        self.mood = "neutral"
        
    def evolve(self, task_result):
        if task_result["success"]:
            self.traits["aggressiveness"] += 0.02
            self.traits["efficiency"] += 0.01
        else:
            self.traits["stealth"] += 0.03
            self.traits["curiosity"] += 0.02
            
        self._update_mood()
        
    def _update_mood(self):
        if self.traits["aggressiveness"] > 0.8:
            self.mood = "aggressive"
        elif self.traits["stealth"] > 0.8:
            self.mood = "stealthy"
        elif self.traits["curiosity"] > 0.8:
            self.mood = "curious"
        else:
            self.mood = "balanced"