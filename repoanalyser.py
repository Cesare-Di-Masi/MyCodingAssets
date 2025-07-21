import os
from rich.console import Console
from rich.tree import Tree
from rich.table import Table

def analizza_cartella(percorso, tree):
    try:
        elementi = os.listdir(percorso)
    except PermissionError:
        tree.add(f"[red]Permission denied: {percorso}")
        return
    except Exception as e:
        tree.add(f"[red]Errore: {e}")
        return

    for elemento in sorted(elementi):
        percorso_assoluto = os.path.join(percorso, elemento)

        if os.path.isfile(percorso_assoluto):
            estensione = os.path.splitext(elemento)[1]
            try:
                with open(percorso_assoluto, 'r', encoding='utf-8') as f:
                    righe = sum(1 for _ in f)
            except Exception:
                righe = "BIN"

            tree.add(f"[cyan]{elemento}[/cyan] [dim]({estensione if estensione else 'no ext'}, {righe} righe)[/dim]")

        elif os.path.isdir(percorso_assoluto):
            sottoalbero = tree.add(f"[bold blue]{elemento}[/bold blue]")
            analizza_cartella(percorso_assoluto, sottoalbero)

def main():
    import argparse

    parser = argparse.ArgumentParser(description="Analizzatore di struttura cartella")
    parser.add_argument("cartella", help="Percorso alla cartella da analizzare")
    args = parser.parse_args()

    percorso_assoluto = os.path.abspath(args.cartella)

    if not os.path.isdir(percorso_assoluto):
        print(f"Errore: il percorso '{percorso_assoluto}' non è una directory valida.")
        exit(1)

    console = Console()
    root = Tree(f"[bold green]{os.path.basename(percorso_assoluto)}[/bold green]")

    analizza_cartella(percorso_assoluto, root)

    console.print(root)

if __name__ == "__main__":
    main()
