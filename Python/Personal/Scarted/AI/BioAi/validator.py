#!/usr/bin/env python3
import os
import json
from typing import List, Dict, Tuple

INPUT_DIR = "./genomi_relazionati"
REPORT_DIR = "./report_validazione"
SUMMARY_FILE = os.path.join(REPORT_DIR, "validation_summary.jsonl")

os.makedirs(REPORT_DIR, exist_ok=True)

# === UTILITÀ ===
def load_genomes(directory=INPUT_DIR) -> List[str]:
    return [os.path.join(directory, f) for f in os.listdir(directory) if f.endswith(".json")]

def save_report(filename: str, report: Dict):
    path = os.path.join(REPORT_DIR, filename)
    with open(path, "w") as f:
        json.dump(report, f, indent=2)

def append_summary(report: Dict):
    with open(SUMMARY_FILE, "a") as f:
        f.write(json.dumps(report) + "\n")

# === CONTROLLO STRUTTURA GERARCHICA ===
def check_structure(genome: Dict) -> Tuple[int, List[str]]:
    score = 100
    errors = []
    if "domains" not in genome or not genome["domains"]:
        score -= 50
        errors.append("Mancanza di domini principali.")
    for domain in genome.get("domains", []):
        if "classes" not in domain or not domain["classes"]:
            score -= 10
            errors.append(f"Dominio '{domain.get('name', 'N/A')}' privo di classi.")
        for gene_class in domain.get("classes", []):
            if "groups" not in gene_class or not gene_class["groups"]:
                score -= 10
                errors.append(f"Classe '{gene_class.get('name', 'N/A')}' priva di gruppi.")
            for group in gene_class.get("groups", []):
                for subgroup in group.get("subgroups", []):
                    for gene in subgroup.get("genes", []):
                        if not gene.get("gene_id") or "structure" not in gene:
                            score -= 5
                            errors.append(f"Gene non valido o privo di struttura.")
    return max(score, 0), errors

# === CONTROLLO FUNZIONE E STRUTTURA GENICA ===
def check_functional_data(genome: Dict) -> Tuple[int, List[str]]:
    score = 100
    errors = []
    for domain in genome.get("domains", []):
        for gene_class in domain.get("classes", []):
            for group in gene_class.get("groups", []):
                for subgroup in group.get("subgroups", []):
                    for gene in subgroup.get("genes", []):
                        s = gene.get("structure", {})
                        if not gene.get("function"):
                            score -= 5
                            errors.append(f"{gene['gene_id']}: funzione mancante.")
                        if not s.get("promoter") or not s.get("exons") or not s.get("terminator"):
                            score -= 5
                            errors.append(f"{gene['gene_id']}: struttura incompleta.")
                        if s.get("mutability", 0) < 0 or s.get("mutability", 1) > 1:
                            score -= 2
                            errors.append(f"{gene['gene_id']}: mutabilità fuori range.")
    return max(score, 0), errors

# === CONTROLLO RELAZIONI GENICHE ===
def check_relations(genome: Dict) -> Tuple[int, List[str]]:
    score = 100
    errors = []
    gene_ids = set()
    for domain in genome.get("domains", []):
        for gene_class in domain.get("classes", []):
            for group in gene_class.get("groups", []):
                for subgroup in group.get("subgroups", []):
                    for gene in subgroup.get("genes", []):
                        gene_ids.add(gene.get("gene_id"))
    for domain in genome.get("domains", []):
        for gene_class in domain.get("classes", []):
            for group in gene_class.get("groups", []):
                for subgroup in group.get("subgroups", []):
                    for gene in subgroup.get("genes", []):
                        rels = gene.get("relations", {})
                        for rel_type, targets in rels.items():
                            for t in targets:
                                if t == gene["gene_id"]:
                                    score -= 10
                                    errors.append(f"{gene['gene_id']}: relazione con sé stesso.")
                                if t not in gene_ids:
                                    score -= 5
                                    errors.append(f"{gene['gene_id']}: relazione con gene inesistente '{t}'.")
    return max(score, 0), errors

# === CONTROLLO INTEGRITÀ OPERATIVA ===
def check_operational_validity(genome: Dict) -> Tuple[int, List[str]]:
    score = 100
    errors = []
    valid_genes = 0
    for domain in genome.get("domains", []):
        for gene_class in domain.get("classes", []):
            for group in gene_class.get("groups", []):
                for subgroup in group.get("subgroups", []):
                    for gene in subgroup.get("genes", []):
                        s = gene.get("structure", {})
                        if s.get("promoter") and s.get("exons") and s.get("terminator"):
                            valid_genes += 1
    if valid_genes == 0:
        score = 0
        errors.append("Nessun gene operativo valido.")
    elif valid_genes < 5:
        score -= 30
        errors.append("Numero di geni validi troppo basso.")
    return max(score, 0), errors

# === CICLO PRINCIPALE ===
def validate_genome_file(file_path: str) -> Dict:
    base = os.path.basename(file_path)
    with open(file_path) as f:
        genome = json.load(f)

    struct_score, struct_errors = check_structure(genome)
    func_score, func_errors = check_functional_data(genome)
    rel_score, rel_errors = check_relations(genome)
    op_score, op_errors = check_operational_validity(genome)

    total_valid = all([struct_score >= 60, func_score >= 60, rel_score >= 60, op_score >= 60])
    report = {
        "file": base,
        "structure_score": struct_score,
        "function_score": func_score,
        "relation_score": rel_score,
        "genome_integrity_score": op_score,
        "is_valid": total_valid,
        "errors": struct_errors + func_errors + rel_errors + op_errors
    }
    return report

def main():
    print("🔍 Validazione genomi in corso...")
    files = load_genomes()
    for f in files:
        report = validate_genome_file(f)
        save_report(f"{report['file']}.report.json", report)
        append_summary(report)
        print(f"🧾 Validato: {report['file']} – Valido: {report['is_valid']}")

if __name__ == "__main__":
    main()
