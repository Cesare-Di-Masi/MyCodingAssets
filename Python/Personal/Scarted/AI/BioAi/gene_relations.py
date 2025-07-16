#!/usr/bin/env python3
import os
import json
import uuid
import random
from typing import List, Dict

INPUT_DIR = "./genomi_mutati"
OUTPUT_DIR = "./genomi_relazionati"
os.makedirs(OUTPUT_DIR, exist_ok=True)

# === CONFIGURAZIONE ===
RELATION_TYPES = ["attiva", "inibisce", "coopera", "duplica", "sopprime", "modula", "complementa"]
MAX_RELATIONS_PER_GENE = 3

# === ESTRAZIONE TUTTI I GENI DA GENOMA ===
def collect_all_genes(genome: Dict) -> Dict[str, Dict]:
    gene_map = {}
    for domain in genome["domains"]:
        for gene_class in domain["classes"]:
            for group in gene_class["groups"]:
                for subgroup in group["subgroups"]:
                    for gene in subgroup["genes"]:
                        gene_map[gene["gene_id"]] = gene
    return gene_map

# === GENERAZIONE RELAZIONI COERENTI ===
def assign_gene_relations(gene_map: Dict[str, Dict]):
    gene_ids = list(gene_map.keys())
    for gene_id, gene in gene_map.items():
        gene["relations"] = {}
        related = set()
        num_relations = random.randint(1, MAX_RELATIONS_PER_GENE)
        for _ in range(num_relations):
            relation_type = random.choice(RELATION_TYPES)
            target_id = random.choice(gene_ids)
            while target_id == gene_id or target_id in related:
                target_id = random.choice(gene_ids)
            related.add(target_id)
            if relation_type not in gene["relations"]:
                gene["relations"][relation_type] = []
            gene["relations"][relation_type].append(target_id)

# === CARICAMENTO FILE GENOMA ===
def load_genomes(directory=INPUT_DIR) -> List[str]:
    return [os.path.join(directory, f) for f in os.listdir(directory) if f.endswith(".json")]

# === SALVATAGGIO ===
def save_genome_with_relations(genome: Dict, base_name: str):
    file_name = f"rel_{base_name}"
    with open(os.path.join(OUTPUT_DIR, file_name), "w") as f:
        json.dump(genome, f, indent=2)

# === CICLO COMPLETO ===
def process_genomes():
    print("🔗 Costruzione rete di relazioni tra geni...")
    files = load_genomes()
    for file in files:
        with open(file) as f:
            genome = json.load(f)
        gene_map = collect_all_genes(genome)
        assign_gene_relations(gene_map)
        base_name = os.path.basename(file)
        save_genome_with_relations(genome, base_name)
        print(f"✅ Relazioni aggiunte: rel_{base_name}")

if __name__ == "__main__":
    process_genomes()
