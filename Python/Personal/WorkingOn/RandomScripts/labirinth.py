import os
import random
import string
import uuid
import shutil
import subprocess
import ctypes
from time import sleep

# === CONFIG ===
MIN_ROOMS = 60
MAX_ROOMS = 120
BASE_DIR = os.path.abspath("labirinto")
WIN_CODE = ''.join(random.choices(string.ascii_uppercase + string.digits, k=8))
ROOMS = []
TOTAL_ROOMS = random.randint(MIN_ROOMS, MAX_ROOMS)


def is_dev_mode_enabled():
    """Controlla se la modalità sviluppatore è attiva leggendo il registro di Windows a 32 e 64 bit"""
    try:
        import winreg

        for access_flag in [0, winreg.KEY_WOW64_32KEY, winreg.KEY_WOW64_64KEY]:
            try:
                key = winreg.OpenKey(
                    winreg.HKEY_LOCAL_MACHINE,
                    r"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock",
                    0,
                    winreg.KEY_READ | access_flag
                )
                val, _ = winreg.QueryValueEx(key, "AllowDevelopmentWithoutDevLicense")
                if val == 1:
                    return True
            except FileNotFoundError:
                continue  # chiave non trovata in questo ramo
    except Exception:
        pass

    return False



def require_dev_mode():
    if not is_dev_mode_enabled():
        print("\n🚫 La modalità sviluppatore di Windows NON è attiva.")
        print("⚠️  Per creare symlink senza privilegi amministrativi, devi attivarla.")
        print("1. Vai su: Impostazioni → Aggiornamento e sicurezza → Per sviluppatori")
        print("2. Attiva: Modalità sviluppatore")
        print("3. Poi riavvia questo script.")
        exit(1)


def random_dirname():
    return uuid.uuid4().hex[:8]


def clear_labyrinth():
    if os.path.exists(BASE_DIR):
        shutil.rmtree(BASE_DIR)
    os.makedirs(BASE_DIR)


def create_labyrinth():
    global ROOMS
    ROOMS = []
    clear_labyrinth()

    queue = [(BASE_DIR, 0)]
    ROOMS.append((BASE_DIR, 0))
    room_count = 1

    while room_count < TOTAL_ROOMS and queue:
        current_path, depth = queue.pop(0)

        if random.random() < 0.2:
            continue  # dead-end, nessun link da qui

        # Creo una stanza casuale da linkare
        target_room = random.choice([r[0] for r in ROOMS])
        link_name = os.path.join(current_path, random_dirname())
        try:
            subprocess.run(
                ['cmd', '/c', f'mklink /D "{link_name}" "{target_room}"'],
                check=True,
                stdout=subprocess.DEVNULL,
                stderr=subprocess.DEVNULL
            )
        except subprocess.CalledProcessError:
            pass  # può fallire se errore di sistema

        # Creo una nuova stanza reale
        new_room = os.path.join(current_path, random_dirname())
        os.makedirs(new_room)
        ROOMS.append((new_room, depth + 1))
        queue.append((new_room, depth + 1))
        room_count += 1

    # === Loop Room: alcune stanze portano a stanze casuali, da zone distanti ===
    for room_path, _ in ROOMS:
        if random.random() < 0.15:
            loop_target = random.choice([r[0] for r in ROOMS if r[0] != room_path])
            loop_link = os.path.join(room_path, random_dirname())
            try:
                subprocess.run(
                    ['cmd', '/c', f'mklink /D "{loop_link}" "{loop_target}"'],
                    check=True,
                    stdout=subprocess.DEVNULL,
                    stderr=subprocess.DEVNULL
                )
            except subprocess.CalledProcessError:
                pass

    # === Inserimento del file vincente ===
    max_depth = max(depth for _, depth in ROOMS)
    deep_rooms = [path for path, depth in ROOMS if depth >= max_depth // 2]
    win_room = random.choice(deep_rooms)
    with open(os.path.join(win_room, "key.txt"), "w") as f:
        f.write(WIN_CODE)


def open_cmd_in_labyrinth():
    subprocess.Popen(f'start cmd /K "cd /D {BASE_DIR}"', shell=True)


def verify_code():
    print("\n🔐 Inserisci il codice trovato in 'key.txt':")
    code = input("➡️  ").strip().upper()
    if code == WIN_CODE:
        print("🎉 Codice corretto! Hai vinto!")
    else:
        print("❌ Codice errato. Riprova!")


def main():
    print("🔍 Verifica modalità sviluppatore...\n")
    require_dev_mode()

    print(f"🔧 Generazione labirinto con {TOTAL_ROOMS} stanze...\n")
    create_labyrinth()
    print("✅ Labirinto creato in:", BASE_DIR)
    print("📂 Ora si aprirà il prompt dei comandi nella radice del labirinto.")
    print("🧭 Usa comandi come `cd`, `dir`, `tree` per esplorare.")
    print("🕵️ Cerca `key.txt` (ma occhio ai link che ti confondono!).\n")

    sleep(2)
    open_cmd_in_labyrinth()
    verify_code()


if __name__ == "__main__":
    main()
