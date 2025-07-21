import os
import json

def analizza_cartella(percorso):
    struttura = {
        "nome_cartella": os.path.basename(percorso),
        "contenuti": []
    }

    for elemento in os.listdir(percorso):
        percorso_assoluto = os.path.join(percorso, elemento)

        if os.path.isfile(percorso_assoluto):
            try:
                with open(percorso_assoluto, 'r', encoding='utf-8') as f:
                    righe = len(f.readlines())
            except Exception:
                righe = None  # Es. file binario

            struttura["contenuti"].append({
                "tipo": "file",
                "nome": elemento,
                "righe": righe,
                "estensione": os.path.splitext(elemento)[1]
            })

        elif os.path.isdir(percorso_assoluto):
            struttura["contenuti"].append(
                analizza_cartella(percorso_assoluto)
            )

    return struttura

if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Analizzatore di cartella nel repo.")
    parser.add_argument("What repo?", help="Percorso relativo alla cartella da analizzare")
    parser.add_argument("-o", "--output", default="struttura.json", help="File di output JSON")

    args = parser.parse_args()
    percorso_cartella = os.path.abspath(args.cartella)

    if not os.path.isdir(percorso_cartella):
        print(f"Errore: il percorso {percorso_cartella} non è una directory valida.")
        exit(1)

    treeJson = analizza_cartella(percorso_cartella)

    with open(args.output, "w", encoding="utf-8") as f:
        json.dump(treeJson, f, indent=2, ensure_ascii=False)

    print(f"Analisi completata. Output salvato in '{args.output}'")
