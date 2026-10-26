#!/usr/bin/env python3
import json
import os
import random
import uuid
import copy
from typing import List, Dict

# === CONFIGURAZIONE ===
INPUT_DIR = "./genomi_generati"
OUTPUT_DIR = "./genomi_mutati"
MUTATION_LOG_FILE = "./mutation_logs.jsonl"
os.makedirs(OUTPUT_DIR, exist_ok=True)

# === FUNZIONI DI UTILITÀ ===
def random_base():
    return random.choice(["A", "T", "C", "G"])

def mutate_base(base):
    return random.choice([b for b in "ATCG" if b != base])

def reverse_seq(seq: str) -> str:
    return seq[::-1]

def duplicate_sequence(seq: str) -> str:
    return seq + seq

# === CLASSE MUTATORE GENOMICO ===
class GenomeMutator:
    def __init__(self, genome: Dict):
        self.genome = genome
        self.mutation_log : list[dict[str, str]] = []

    def apply_mutations(self):
        for domain in self.genome["domains"]:
            for gene_class in domain["classes"]:
                for group in gene_class["groups"]:
                    for subgroup in group["subgroups"]:
                        for gene in subgroup["genes"]:
                            self._mutate_gene(gene)

    def _mutate_gene(self, gene: Dict):
        # Scegli mutazioni casuali multiple
        mutation_types = ["point", "deletion", "duplication", "transposition", "inversion"]
        selected = random.sample(mutation_types, k=random.randint(1, 3))

        for mutation in selected:
            if mutation == "point":
                self._point_mutation(gene)
            elif mutation == "deletion":
                self._deletion(gene)
            elif mutation == "duplication":
                self._duplication(gene)
            elif mutation == "transposition":
                self._transposition(gene)
            elif mutation == "inversion":
                self._inversion(gene)

    def _log_mutation(self, gene_id: str, mutation_type: str, target: str, effect: str):
        entry = {
            "mutation_id": str(uuid.uuid4()),
            "gene_id": gene_id,
            "mutation_type": mutation_type,
            "target": target,
            "effect": effect
        }
        self.mutation_log.append(entry)

    def _point_mutation(self, gene: Dict):
        # Mutazione su un esone a caso
        exon_list = gene["structure"]["exons"]
        if not exon_list:
            return
        i = random.randint(0, len(exon_list)-1)
        exon = list(exon_list[i])
        if not exon:
            return
        pos = random.randint(0, len(exon)-1)
        original = exon[pos]
        exon[pos] = mutate_base(exon[pos])
        exon_list[i] = ''.join(exon)
        self._log_mutation(gene["gene_id"], "point", f"exons[{i}]", f"{original}→{exon[pos]}")

    def _deletion(self, gene: Dict):
        if gene["structure"]["introns"]:
            removed = gene["structure"]["introns"].pop()
            self._log_mutation(gene["gene_id"], "deletion", "introns", f"Rimosso: {removed}")

    def _duplication(self, gene: Dict):
        if gene["structure"]["promoter"]:
            original = gene["structure"]["promoter"]
            gene["structure"]["promoter"] = duplicate_sequence(original)
            self._log_mutation(gene["gene_id"], "duplication", "promoter", "Promoter duplicato")

    def _transposition(self, gene: Dict):
        if gene["structure"]["exons"] and gene["structure"]["introns"]:
            exon = gene["structure"]["exons"].pop()
            gene["structure"]["introns"].insert(0, exon)
            self._log_mutation(gene["gene_id"], "transposition", "exon→intron", f"Eson trasferito: {exon}")

    def _inversion(self, gene: Dict):
        if gene["structure"]["terminator"]:
            original = gene["structure"]["terminator"]
            gene["structure"]["terminator"] = reverse_seq(original)
            self._log_mutation(gene["gene_id"], "inversion", "terminator", f"{original}→{gene['structure']['terminator']}")

# === UTILITÀ PRINCIPALE ===
def load_genomes(directory=INPUT_DIR) -> List[str]:
    return [os.path.join(directory, f) for f in os.listdir(directory) if f.endswith(".json")]

def save_mutated_genome(genome: Dict, filename: str):
    out_path = os.path.join(OUTPUT_DIR, f"mut_{filename}")
    with open(out_path, "w") as f:
        json.dump(genome, f, indent=2)

def save_mutation_log(log: List[Dict]):
    with open(MUTATION_LOG_FILE, "a") as f:
        for entry in log:
            f.write(json.dumps(entry) + "\n")

def main():
    print("🧪 Inizio mutazioni genetiche...")
    files = load_genomes()
    for file_path in files:
        with open(file_path) as f:
            genome = json.load(f)
        mutator = GenomeMutator(genome)
        mutator.apply_mutations()
        base_name = os.path.basename(file_path)
        save_mutated_genome(mutator.genome, base_name)
        save_mutation_log(mutator.mutation_log)
        print(f"🔧 Genoma mutato salvato: mut_{base_name}")

if __name__ == "__main__":
    main()
