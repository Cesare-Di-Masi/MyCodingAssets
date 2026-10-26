#!/usr/bin/env python3
import json
import random
import uuid
import os
from typing import List, Dict

# === PARAMETRI CONFIGURABILI ===
DOMINI = 3
CLASSI_PER_DOMINIO = 2
GRUPPI_PER_CLASSE = 2
SOTTOGRUPPI_PER_GRUPPO = 2
GENI_PER_SOTTOGRUPPO = 3

SALVATAGGIO_DIR = "./genomi_generati"
os.makedirs(SALVATAGGIO_DIR, exist_ok=True)

# === DATABASE DI BASE ===
FUNZIONI_GENICHE = [
    "Regola la densità osmotica cellulare",
    "Attiva la sintesi di neurotrasmettitori artificiali",
    "Inibisce la formazione di tessuti difettosi",
    "Aumenta la capacità sensoriale ambientale",
    "Contribuisce alla rigenerazione dei tessuti danneggiati",
    "Migliora la trasduzione dei segnali sinaptici"
]

RELAZIONI_POSSIBILI = ["attiva", "inibisce", "coopera", "supporta", "duplica", "sopprime"]

# === GENERAZIONE ELEMENTI GENICI ===
def generate_sequence(length=10):
    return ''.join(random.choices("ATCG", k=length))

def generate_gene(subgroup_name: str) -> Dict:
    gene_id = f"{subgroup_name}_{str(uuid.uuid4())[:8]}"
    structure = {
        "promoter": generate_sequence(8),
        "exons": [generate_sequence(6) for _ in range(random.randint(2, 4))],
        "introns": [generate_sequence(4) for _ in range(random.randint(1, 2))],
        "terminator": generate_sequence(6)
    }
    gene = {
        "gene_id": gene_id,
        "function": random.choice(FUNZIONI_GENICHE),
        "structure": structure,
        "regulation": {
            "activated_by": [],
            "inhibited_by": []
        },
        "relations": {},
        "mutability": round(random.uniform(0.01, 0.1), 3),
        "heritability": round(random.uniform(0.8, 1.0), 2)
    }
    return gene

# === COSTRUZIONE GENOMA GERARCHICO ===
def generate_genome() -> Dict:
    genome = {"domains": []}
    for d in range(DOMINI):
        domain = {"name": f"Domain_{d+1}", "classes": []}
        for c in range(CLASSI_PER_DOMINIO):
            gene_class = {"name": f"Class_{d+1}_{c+1}", "groups": []}
            for g in range(GRUPPI_PER_CLASSE):
                group = {"name": f"Group_{d+1}_{c+1}_{g+1}", "subgroups": []}
                for s in range(SOTTOGRUPPI_PER_GRUPPO):
                    subgroup_name = f"Sub_{d+1}_{c+1}_{g+1}_{s+1}"
                    genes = [generate_gene(subgroup_name) for _ in range(GENI_PER_SOTTOGRUPPO)]
                    subgroup = {"name": subgroup_name, "genes": genes}
                    group["subgroups"].append(subgroup)
                gene_class["groups"].append(group)
            domain["classes"].append(gene_class)
        genome["domains"].append(domain)
    return genome

# === SALVATAGGIO FILE JSONL ===
def save_genome(genome: Dict, output_dir=SALVATAGGIO_DIR):
    file_name = f"genome_{str(uuid.uuid4())[:8]}.json"
    with open(os.path.join(output_dir, file_name), "w") as f:
        json.dump(genome, f, indent=2)
    print(f"✅ Genoma salvato: {file_name}")

# === ESECUZIONE PRINCIPALE ===
def main():
    print("🧬 Generazione genoma artificiale complesso...")
    genome = generate_genome()
    save_genome(genome)

if __name__ == "__main__":
    main()
