import os
import json

def crea_struttura_cartelle(dati, percorso_base):
    cartella_corrente = os.path.join(percorso_base, dati["nome_cartella"])
    os.makedirs(cartella_corrente, exist_ok=True)

    for elemento in dati.get("contenuti", []):
        tipo = elemento.get("tipo")

        # Se tipo non è specificato ma ha "contenuti", trattalo come cartella
        if tipo == "file":
            percorso_file = os.path.join(cartella_corrente, elemento["nome"])
            with open(percorso_file, "w", encoding="utf-8") as f:
                righe = elemento.get("righe", 0) or 0
                contenuto = f"# File generato automaticamente\n" + "\n" * max(righe - 1, 0)
                f.write(contenuto)

        elif tipo == "cartella" or "contenuti" in elemento:
            crea_struttura_cartelle(elemento, cartella_corrente)

        else:
            print(f"⚠️  Elemento ignorato (manca 'tipo'): {elemento}")

if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Crea struttura da JSON")
    parser.add_argument("input", help="File JSON con la struttura")
    parser.add_argument("-d", "--destinazione", default=".", help="Cartella di destinazione")

    args = parser.parse_args()

    with open(args.input, "r", encoding="utf-8") as f:
        struttura = json.load(f)

    crea_struttura_cartelle(struttura, os.path.abspath(args.destinazione))

    print(f"✅ Struttura creata in '{args.destinazione}'")
