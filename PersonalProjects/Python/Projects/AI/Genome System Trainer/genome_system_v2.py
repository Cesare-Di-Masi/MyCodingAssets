
"""
GENOME INTELLIGENT SYSTEM v2.0 - Single File Version
Auto-generating, mutating, validating, and scoring synthetic genomes using AI and custom logic.
All logic contained in a single monolithic Python file.
"""

# ========== IMPORTS ==========
import os
import random
import string
import time
import json
import uuid
import logging
import requests
from collections import defaultdict
from typing import Dict, List, Tuple, Union

# ========== GLOBAL CONFIGURATION ==========

GENOME_STORAGE_PATH = "genomes/"
GENE_CHARSET = string.ascii_uppercase + string.digits
GENOME_LENGTH_RANGE = (100, 30000)
MAX_MUTATION_RATE = 0.05
MAX_GENE_RELATIONS = 15
VALIDATION_ENDPOINT = "https://api-inference.huggingface.co/models/bert-base-uncased"
HUGGINGFACE_API_KEY = os.getenv("HF_API_KEY", "YOUR API KEY HERE")

# Initialize logging
logging.basicConfig(level=logging.INFO, format="[%(asctime)s] %(levelname)s: %(message)s")

# ========== UTILS ==========

def ensure_storage_dir():
    os.makedirs(GENOME_STORAGE_PATH, exist_ok=True)

def generate_random_gene(length: int = 8) -> str:
    return ''.join(random.choices(GENE_CHARSET, k=length))

def save_genome_to_disk(genome_id: str, data: Dict):
    with open(os.path.join(GENOME_STORAGE_PATH, f"{genome_id}.json"), "w") as f:
        json.dump(data, f, indent=2)

def load_genome(genome_id: str) -> Dict:
    path = os.path.join(GENOME_STORAGE_PATH, f"{genome_id}.json")
    with open(path, "r") as f:
        return json.load(f)

def request_validation(text: str) -> bool:
    headers = {"Authorization": f"Bearer {HUGGINGFACE_API_KEY}"}
    payload = {"inputs": text}
    try:
        response = requests.post(VALIDATION_ENDPOINT, headers=headers, json=payload, timeout=15)
        if response.status_code == 200:
            return True
        else:
            logging.warning(f"Validation failed with status: {response.status_code}")
            return False
    except Exception as e:
        logging.error(f"Validation request error: {e}")
        return False

# ========== GENOME STRUCTURE ==========
class Genome:
    def __init__(self, genome_id: str = None):
        self.id = genome_id or str(uuid.uuid4())
        self.genes: List[str] = []
        self.relations: Dict[str, List[str]] = defaultdict(list)
        self.metadata: Dict = {"score": 0, "valid": False, "length": 0}

    def generate_genes(self, count: int):
        for _ in range(count):
            gene = generate_random_gene()
            self.genes.append(gene)

    def create_relations(self):
        for gene in self.genes:
            targets = random.sample(self.genes, random.randint(1, MAX_GENE_RELATIONS))
            self.relations[gene] = [t for t in targets if t != gene]

    def validate(self):
        as_text = " ".join(self.genes)
        self.metadata["valid"] = request_validation(as_text)

    def score(self):
        valid_bonus = 10 if self.metadata["valid"] else -5
        relation_bonus = sum([len(v) for v in self.relations.values()]) / max(1, len(self.genes))
        diversity_score = len(set("".join(self.genes))) / len(GENE_CHARSET)
        self.metadata["score"] = valid_bonus + relation_bonus + diversity_score
        self.metadata["length"] = len(self.genes)

    def mutate(self):
        mutation_count = int(len(self.genes) * MAX_MUTATION_RATE)
        for _ in range(mutation_count):
            idx = random.randint(0, len(self.genes) - 1)
            self.genes[idx] = generate_random_gene()

    def export(self) -> Dict:
        return {
            "id": self.id,
            "genes": self.genes,
            "relations": self.relations,
            "metadata": self.metadata,
        }

# ========== TRAINING MANAGER ==========

class GenomeTrainer:
    def __init__(self):
        self.iteration = 0
        self.population: List[Genome] = []

    def run_training_cycle(self, size: int = 10):
        logging.info(f"Training cycle {self.iteration + 1} started.")
        for _ in range(size):
            genome = Genome()
            genome.generate_genes(random.randint(*GENOME_LENGTH_RANGE))
            genome.create_relations()
            genome.validate()
            genome.score()
            genome.mutate()
            self.population.append(genome)
            save_genome_to_disk(genome.id, genome.export())
        self.iteration += 1
        logging.info(f"Cycle {self.iteration} complete with {len(self.population)} genomes.")

    def get_top_genomes(self, top_n: int = 5) -> List[Genome]:
        sorted_genomes = sorted(self.population, key=lambda g: g.metadata["score"], reverse=True)
        return sorted_genomes[:top_n]

# ========== MAIN LOOP ==========

def main():
    ensure_storage_dir()
    trainer = GenomeTrainer()

    while True:
        trainer.run_training_cycle(size=500)
        top = trainer.get_top_genomes()
        logging.info("Top genomes:")
        for g in top:
            logging.info(f"ID: {g.id}, Score: {g.metadata['score']}, Valid: {g.metadata['valid']}")
        time.sleep(1)

# ========== EXECUTION ==========

if __name__ == "__main__":
    main()
