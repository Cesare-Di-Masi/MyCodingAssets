import time
from evolution.genome import Genome
from env_wrappers.cipher_wrapper import ChannelManager
from env_wrappers.malware_wrapper import DefenseSystem

class GeneticDefender:
    def __init__(self, env, genome: Genome):
        self.env = env
        self.genome = genome
        self.channels = ChannelManager()
        self.defenses = [DefenseSystem(f"Defense_{i}") for i in range(8)]
        self.timer = time.time()

    def step(self):
        compromised = self.channels.monitor()
        self.adapt_defenses(compromised)
        if time.time() - self.timer > 30:
            self.rotate_defenses()
            self.timer = time.time()
        return self.env.step(None)  # stub RL step

    def adapt_defenses(self, compromised_ids):
        for d in self.defenses:
            d.reconfigure(compromised=len(compromised_ids))

    def rotate_defenses(self):
        self.channels.rotate_keys(rate=self.genome.genes["key_rotation_rate"])
        for d in self.defenses:
            d.reconfigure(force=True)
