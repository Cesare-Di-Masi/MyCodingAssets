import os
import json
from typing import List, Dict


def chiedi_sottocartelle(nome_cartella: str) -> List[str]:
    input_raw = input(
        f"[📁 {nome_cartella}] Sottocartelle (virgola per separare, invio per nessuna): "
    ).strip()
    return [s.strip() for s in input_raw.split(",") if s.strip()] if input_raw else []


def stampa_albero(cartella: str, struttura: Dict[str, List[str]], indent: str = "") -> None:
    """Stampa ad albero la struttura delle cartelle, in stile `tree`"""
    basename = os.path.basename(cartella)
    print(f"{indent}📁 {basename}")

    figli = struttura.get(cartella, [])
    figli_sorted = sorted(figli, key=lambda x: os.path.basename(x).lower())

    for i, figlia in enumerate(figli_sorted):
        is_last = i == len(figli_sorted) - 1
        connettore = "└──" if is_last else "├──"
        nuovo_indent = indent + ("    " if is_last else "│   ")
        print(f"{indent}{connettore} ", end="")
        stampa_albero(figlia, struttura, nuovo_indent)


def salva_struttura_json(struttura: Dict[str, List[str]], root: str, file_path: str = "struttura_cartelle.json") -> None:
    """Salva la struttura in un file JSON ordinato alfabeticamente"""

    def costruisci_nodo(percorso: str) -> Dict:
        figli = sorted(struttura.get(percorso, []), key=lambda x: os.path.basename(x).lower())
        return {
            "nome": os.path.basename(percorso),
            "figli": [costruisci_nodo(figlia) for figlia in figli]
        }

    struttura_dati = costruisci_nodo(root)

    try:
        with open(file_path, "w", encoding="utf-8") as f:
            json.dump(struttura_dati, f, ensure_ascii=False, indent=2)
        print(f"\n💾 Struttura salvata in '{file_path}'")
    except IOError as e:
        print(f"❌ Errore durante il salvataggio: {e}")


def crea_albero_cartelle() -> None:
    root = input("📁 Nome della cartella principale: ").strip()
    if not root:
        print("❌ Nessun nome fornito. Uscita.")
        return

    os.makedirs(root, exist_ok=True)
    struttura: Dict[str, List[str]] = {root: []}
    coda: List[str] = [root]

    while coda:
        corrente = coda.pop(0)
        sottocartelle = chiedi_sottocartelle(corrente)

        percorsi = [os.path.join(corrente, nome) for nome in sottocartelle]

        for percorso in percorsi:
            try:
                os.makedirs(percorso, exist_ok=True)
                struttura[corrente].append(percorso)
                struttura[percorso] = []
                coda.append(percorso)
            except Exception as e:
                print(f"❌ Errore creando '{percorso}': {e}")

    print("\n✅ Struttura creata con successo.")

    if input("👁 Stampare l’albero? (s/n): ").strip().lower() == 's':
        print()
        stampa_albero(root, struttura)

    if input("💾 Salvare la struttura in JSON? (s/n): ").strip().lower() == 's':
        salva_struttura_json(struttura, root)

    print("\n📌 Suggerimenti:")
    print("• Usa il comando 'tree' per vedere la struttura dal terminale (se disponibile).")
    print("• Il file JSON può essere usato per ricreare la struttura in altri ambienti.")
    print("• Puoi modificare il JSON per aggiungere nuove cartelle e ricaricarlo.")


if __name__ == "__main__":
    try:
        crea_albero_cartelle()
    except KeyboardInterrupt:
        print("\n🛑 Operazione interrotta manualmente.")
