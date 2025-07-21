import random
from .genome import Genome

def crossover_population(population: list[Genome]) -> list[Genome]:
    new_population = []
    pop_size = len(population)
    for _ in range(pop_size):
        parent1 = random.choice(population)
        parent2 = random.choice(population)
        child = parent1.crossover(parent2)
        new_population.append(child)
    return new_population
