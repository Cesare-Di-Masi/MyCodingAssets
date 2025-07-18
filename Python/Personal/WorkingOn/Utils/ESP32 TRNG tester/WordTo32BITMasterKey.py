import hashlib
import base64
import argparse
import sys
import os

DEFAULT_KEY_PHRASE = "SiumMaster_Default"

def read_key_from_file(path):
    if not os.path.isfile(path):
        print(f"[!] File non trovato: {path}")
        sys.exit(1)
    with open(path, 'r', encoding='utf-8') as f:
        return f.read().strip()

def generate_master_key(input_phrase: str) -> dict:
    key_bytes = hashlib.sha256(input_phrase.encode('utf-8')).digest()
    return {
        "input": input_phrase,
        "master_key_bytes": key_bytes,
        "hex": key_bytes.hex(),
        "base64": base64.b64encode(key_bytes).decode('utf-8')
    }

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generatore Master Key a 256-bit (32 byte) da parola/frase.")
    parser.add_argument('--key', type=str, help='Fornisci la frase per generare la master key.')
    parser.add_argument('--file', type=str, help='Percorso a un file contenente la frase.')
    args = parser.parse_args()

    if args.key:
        phrase = args.key.strip()
    elif args.file:
        phrase = read_key_from_file(args.file)
    else:
        try:
            phrase = input("Inserisci la frase segreta (invio per usare default): ").strip()
        except KeyboardInterrupt:
            print("\n[!] Interrotto.")
            sys.exit(1)
        if not phrase:
            print("[*] Nessuna frase fornita, uso quella predefinita.")
            phrase = DEFAULT_KEY_PHRASE

    result = generate_master_key(phrase)
    print(f"\n[+] Master Key generata da frase: '{result['input']}'")
    print(f"    - HEX    : {result['hex']}")
    print(f"    - Base64 : {result['base64']}")
