# companion/skills.py
class SkillManager:
    def __init__(self):
        self.learned_skills = ["Basic Network Scan"]
        self.available_skills = [
            "Advanced Port Scan", "Wi-Fi Cracking", "SQL Injection",
            "Phishing Framework", "Password Analysis", "Exploit Development",
            "AI Vulnerability Prediction", "Quantum Cryptography"
        ]
        
    def learn_random_skill(self):
        if self.available_skills:
            skill = random.choice(self.available_skills)
            self.learned_skills.append(skill)
            self.available_skills.remove(skill)
            print(f"NEXUS ha imparato: {skill}")
            
    def get_learned_skills(self):
        return self.learned_skills