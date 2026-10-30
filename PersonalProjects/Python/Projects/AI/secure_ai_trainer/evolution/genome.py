import random

class Genome:
    def __init__(self, genes: dict):
        self.genes = genes

    def mutate(self, rate=0.2):
        for key in self.genes:
            if random.random() < rate:
                self.genes[key] += random.uniform(-0.1, 0.1)
                self.genes[key] = max(0.0, min(1.0, self.genes[key]))

    def crossover(self, other: 'Genome'):
        return Genome({
            k: random.choice([self.genes[k], other.genes[k]])
            for k in self.genes
        })
