# companion/evolution.py
class EvolutionEngine:
    def __init__(self):
        self.evolution_tree = {
            1: ["Basic Network Scan", "Simple Phishing"],
            2: ["Advanced Port Scan", "Credential Harvesting"],
            3: ["Wi-Fi Handshake Capture", "SQL Injection Scanner"],
            5: ["Evil Twin Attack", "Metasploit Integration"],
            8: ["AI Vulnerability Prediction", "Automated Exploitation"],
            10: ["Quantum Resistance Testing", "Neural Network Attacks"],
            15: ["Zero-Day Exploit Generation", "Autonomous Hacking"]
        }
        
    def evolve(self, level):
        print(f"NEXUS evoluzione al livello {level}!")
        
    def get_unlocked_features(self, level):
        unlocked = []
        for lvl, features in self.evolution_tree.items():
            if lvl <= level:
                unlocked.extend(features)
        return unlocked