from agents.base_defender import GeneticDefender
from evolution.genome import Genome

def create_genetic_defender(env, genome_cfg=None):
    genes = genome_cfg or {"key_rotation_rate": 0.5}
    genome = Genome(genes)
    agent = GeneticDefender(env, genome)
    return agent
