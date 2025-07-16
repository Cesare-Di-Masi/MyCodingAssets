# indestructible_terminal.py
import os
import sys
import time
import random
import threading

# Colori ANSI
RED = "\033[31m"
GREEN = "\033[32m"
YELLOW = "\033[33m"
CYAN = "\033[36m"
RESET = "\033[0m"
BOLD = "\033[1m"

eye_art = [
    "⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⠱⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀",
    "⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⡄⢹⠀⠀⡀⠀⠀⠀⠀⠀⠀⣇⠀⠀⠀⢀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀",
    "⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣤⣤⠴⣶⣶⣺⣿⣼⣄⠀⣟⣇⠀⢠⠀⠀⠀⣿⠀⠀⠀⡿⡆⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀",
    "⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⠀⢀⣤⡿⠚⣹⣧⣶⠟⣏⢛⢹⣿⣿⢉⠉⡏⡿⣿⢻⠶⣤⣰⣷⡇⠠⣰⣿⣇⢀⠆⠀⠀⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀",
    "⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⠇⠀⣸⡟⡋⢸⡆⢰⣿⣷⣄⣸⣏⣏⣹⣿⣿⡄⣸⣷⣿⣇⡟⢀⣴⣿⡟⡿⢶⣿⡟⣿⣮⣀⣠⣞⠁⠀⠀⠀⢀⣰⠃⠀⠀⠀⠀⠀⠀⠀",
    "⠀⠀⠀⠀⠀⠀⠀⡀⠀⠀⠀⣿⣠⣞⣽⣿⡿⢿⣷⣄⣿⣟⣧⣽⣿⣟⣿⣿⣿⣟⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣻⣿⠟⣼⣿⣿⣷⡟⠿⢧⣄⡀⠀⢠⣿⠃⠀⠀⠀⠀⠀⠀⠀⠀",
    "⠀⠀⠀⠀⠀⠀⠀⢀⠀⢱⡄⠀⣄⣿⣿⡉⠁⢻⣿⣥⡽⢿⣻⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣿⣿⣿⣿⣯⣿⣿⣯⣿⣿⣿⡿⡻⠿⣶⡾⠋⢉⣶⡿⠥⠄⣠⠞⠀⣀⠀⠀⠀⠀",
    "⠀⠀⠀⠀⠀⠀⢠⠸⣆⠀⢹⣭⣿⣅⠘⣿⣾⢿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣾⣻⡯⣪⣥⡶⠛⣻⣶⣿⢏⠀⣠⣟⡁⢠⠀⢈⡀⠀⢀⠀",
    "⠀⠀⠀⠀⠀⠀⣼⠀⠘⣶⣾⠏⣿⣿⢿⣿⣿⣿⣿⡿⠟⢉⣽⣿⣿⣿⠿⠛⠉⠉⠁⠀⠀⠈⠉⠉⠛⠿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣶⣾⣿⣿⣿⣷⣟⣩⣏⣹⠿⠁⣰⠃⢀⡜⠀",
    "⠀⠀⠀⠀⠀⠀⢻⣥⡴⢋⣹⣿⣿⣽⣿⣿⣿⡿⠏⠀⣠⣿⣿⡿⠋⠀⠀⠀⠀⣀⣀⣤⣤⣄⣀⠀⠀⠀⠀⠈⠻⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣟⣿⣶⡾⣷⣶⣾⡟⢉⣾⡇⠀",
    "⠀⠀⠀⠀⠰⠂⣠⡿⣷⣾⣿⣷⣿⣿⣿⣿⠃⠀⠀⢰⣿⣿⠋⠀⠀⠀⢀⣶⣿⣿⣿⠿⠿⣿⣿⣿⣷⣄⠀⠀⠀⠈⢿⣿⣿⣻⢿⣿⣿⣿⣿⣤⣤⣾⣟⣻⣿⣿⣏⣴⡿⢋⣴⠛",
    "⠀⠀⠀⠀⠀⣺⣏⣾⠟⣻⣿⣿⠇⣿⣿⡇⠀⠀⢀⣿⣿⡏⠀⠀⠀⢰⣿⣿⠟⠉⠀⠀⠀⠀⠉⠻⣿⣿⣷⡀⠀⠀⠀⢻⣿⣿⢣⡙⢿⣿⣿⣿⣿⣯⣿⣶⣾⡿⣟⣭⣶⡾⠋⠀",
    "⠀⠠⢤⡆⣴⣳⣿⢿⣿⡿⠟⠁⠀⣿⣿⠁⠀⠀⠸⣿⣿⡇⠀⠀⠀⢸⣿⣿⣤⣤⣴⣶⣦⡀⠀⠀⠈⢿⣿⣷⠀⠀⠀⠘⣿⣿⡆⢻⠠⠟⠿⣿⣿⣿⣿⣟⡛⣻⣿⠟⠋⣀⢀⠀",
    "⠀⠀⠀⣙⣿⣿⣿⣿⠋⣴⡄⠀⠀⣿⣿⡆⠀⠀⠀⢻⣿⣷⡀⠀⠀⠈⠻⠿⠿⠟⠛⣿⣿⣧⠀⠀⠀⢸⣿⣿⡄⠀⠀⠀⣿⣿⣇⡟⠀⠀⠀⢲⣿⣿⣿⣿⣿⣿⣶⣶⣾⡿⠟⠀",
    "⠀⣀⣠⣿⣟⣷⡿⢁⡾⢸⡁⠀⠀⢻⣿⣷⡀⠀⠀⠈⢿⣿⣿⣤⣀⠀⠀⠀⠀⢀⣰⣿⣿⡏⠀⠀⠀⢸⣿⣿⠁⠀⠀⢠⣿⣿⡟⠀⠀⠀⢠⣿⢿⣢⡻⢿⠙⢿⣛⣏⠁⠀⠀⠀",
    "⢠⣾⣿⠟⣽⡟⡇⠙⢿⢄⣇⠀⠀⠀⢿⣿⣷⡄⠀⠀⠀⠙⠿⣿⣿⣿⣷⣶⣿⣿⣿⡿⠋⠀⠀⠀⣠⣿⣿⡟⠀⠀⠀⣾⣿⠋⠀⠀⢀⢀⣿⡿⢷⣾⣿⣯⣄⣹⡿⠋⠀⠀⠀⠀",
    "⠀⠉⠁⢰⣿⠁⣳⡅⠈⣦⡝⣤⡀⠀⠈⠻⣿⣿⣦⡀⠀⠀⠀⠈⠉⠛⠛⠛⠛⠋⠁⠀⠀⠀⢀⣴⣿⣿⠟⠀⠀⢀⣾⠟⠁⠀⠀⢠⣬⣿⣿⣿⣞⠇⢳⡌⢿⣿⠁⠀⠀⠀⠀⠀",
    "⠀⠀⠀⡿⢧⡀⠉⣩⣤⣧⣈⠙⠺⠶⣤⣄⡈⠻⣿⣿⣷⣦⣤⣀⡀⠀⠀⠀⠀⠀⣀⣠⣴⣾⣿⣿⠟⠁⠀⢀⣴⠟⠁⠀⢀⣤⣾⣿⣿⠿⣾⠷⣿⣆⡼⠓⣾⡇⠀⠀⠀⠀⠀⠀",
    "⠀⠀⠀⠹⢦⣉⣉⣀⠤⡜⠉⠛⢶⣤⣄⣀⣉⡉⠛⠻⠿⠿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡿⠿⠋⠁⣠⠴⠞⣉⣀⣀⣤⣶⢶⣻⣿⡵⣘⠢⠈⣦⠘⢿⠇⢰⡿⠁⠀⠀⠀⠀⠀⠀⠀",
    "⠀⠀⠀⠀⠀⠙⠛⠛⠛⢧⣤⡴⠋⠀⠈⢻⡿⠾⢿⣷⣶⣤⣴⣆⣌⣭⣉⣩⣭⣉⠀⣄⡤⣄⢠⣤⣄⣠⣴⠾⣿⡿⣏⠘⠻⣧⡘⣿⡜⠶⠄⠈⢤⠞⢠⣿⠃⠀⠀⠀⠀⠀⠀⠀⠀",
    "⠀⠀⠀⠀⠀⠀⣯⣭⣽⣳⢦⣉⠲⢤⣠⠏⠀⠀⡼⣱⠋⢹⣿⢻⠟⠛⡟⣿⠟⢻⠟⣟⢿⠻⣟⠛⢯⢻⣯⣆⠘⣿⡌⢳⣄⢻⣷⠈⠀⠀⢀⡤⠋⢠⡾⠃⠀⠀⠀⠀⠀⠀⠀⠀⠀",
    "⠀⠀⠀⠀⠀⠘⠿⠉⠉⠻⢷⣌⠙⠲⣽⡃⠀⠀⢷⠇⠀⠸⠁⡞⠀⡀⠙⡟⠂⠀⡟⢿⣼⠀⠹⡇⠈⢧⣎⢿⣇⠸⠿⠀⠉⢮⠏⠃⢀⡴⠊⠀⣠⠟⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀",
    "⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠉⠻⢦⡀⠉⠓⢦⣞⠀⠀⠀⠀⠁⠀⠀⠀⡇⠈⠳⡷⠀⡿⠴⠀⠘⠀⠸⠋⠻⣿⠀⠀⠁⠈⢈⡧⠞⠁⠀⠀⠜⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀",
    "⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠁⠀⠀⠀⠉⠓⠦⣄⣀⠀⠀⠀⠁⠀⠀⠛⠀⠀⠀⠀⠀⠀⠀⠀⠀⡿⠀⠀⠀⠈⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀",
    "⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠙⠓⠲⠤⢤⣀⣀⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀"
]


# Controllo terminale interattivo
try:
    import tty
    import termios
    import fcntl
    is_interactive_terminal = True
except ImportError:
    is_interactive_terminal = False
    # Dummy fallback per tty e termios
    class DummyTTY:
        def setcbreak(self, fd): pass
    tty = DummyTTY()
    class DummyTermios:
        def tcgetattr(self, fd): return None
        def tcsetattr(self, fd, when, attributes): pass
    termios = DummyTermios()

running = True
start_time = time.time()
duration = 60  # Durata in secondi

# Funzione glitch fullscreen minimalista ma aggressiva
def draw_random_corruption():
    try:
        rows, cols = os.get_terminal_size()
    except OSError:
        rows, cols = 24, 80

    COLORS = [f"\033[3{i}m" for i in range(1, 7)]
    BG_COLORS = [f"\033[4{i}m" for i in range(1, 7)]

    while running:
        try:
            for _ in range(random.randint(5, 20)):
                row = random.randint(0, rows - 1)
                col_start = random.randint(0, cols // 2)
                col_end = random.randint(col_start + 1, cols)
                char = chr(random.randint(33, 126))
                color = random.choice(COLORS)
                bg_color = random.choice(BG_COLORS) if random.random() < 0.3 else ""

                sys.stdout.write(f"\033[{row + 1};{col_start + 1}H{bg_color}{color}{char * (col_end - col_start)}{RESET}")
            sys.stdout.flush()
            time.sleep(random.uniform(0.01, 0.05))
        except Exception:
            time.sleep(0.5)

# Funzione per gestire e "corrompere" l'input utente, ignorandolo e visualizzando errori fittizi
def handle_input():
    if not is_interactive_terminal:
        return

    fd = sys.stdin.fileno()
    old_settings = termios.tcgetattr(fd)
    tty.setcbreak(fd)

    old_flags = fcntl.fcntl(fd, fcntl.F_GETFL)
    fcntl.fcntl(fd, fcntl.F_SETFL, old_flags | os.O_NONBLOCK)

    try:
        while running:
            try:
                char = sys.stdin.read(1)
                if char:
                    sys.stdout.write(f"\033[1;31m!!! ERRORE D'INPUT !!! {random.randint(1000,9999)}{RESET}")
                    sys.stdout.flush()
                    time.sleep(0.1)
            except IOError:
                pass
            except Exception:
                pass
            time.sleep(0.05)
    finally:
        if is_interactive_terminal:
            termios.tcsetattr(fd, termios.TCSADRAIN, old_settings)
            fcntl.fcntl(fd, fcntl.F_SETFL, old_flags)

# Funzione per scrivere messaggi log casuali e minacciosi
def activity_logs():
    log_messages = [
        f"{CYAN}ATTIVITA' ANOMALA: Kernel panic simulato (0xDEADBEEF){RESET}",
        f"{CYAN}ATTIVITA' ANOMALA: Reindirizzamento I/O - Dati in transito...{RESET}",
        f"{CYAN}ATTIVITA' ANOMALA: Accesso a /dev/null negato. Che cosa cerchi?{RESET}",
        f"{CYAN}ATTIVITA' ANOMALA: Caricamento moduli sconosciuti.{RESET}",
        f"{CYAN}ATTIVITA' ANOMALA: Eliminazione temporanea di variabili d'ambiente.{RESET}",
        f"{CYAN}ATTIVITA' ANOMALA: Rilevato movimento del cursore... Ti osservo.{RESET}",
        f"{CYAN}ATTIVITA' ANOMALA: Tentativo di CTRL+C bloccato. Non così facile.{RESET}",
        f"{CYAN}ATTIVITA' ANOMALA: Pulizia schermo disabilitata. L'orrore resta.{RESET}",
        f"{CYAN}ATTIVITA' ANOMALA: Riconnessione remota forzata...{RESET}"
    ]

    try:
        rows, cols = os.get_terminal_size()
    except OSError:
        rows, cols = 24, 80

    base_row = 12  # Dopo messaggi iniziali e area glitch

    i = 0
    while running and (time.time() - start_time < duration):
        if i >= len(log_messages):
            i = 0
        if is_interactive_terminal:
            sys.stdout.write(f"\033[{base_row + i};1H")
        sys.stdout.write(log_messages[i].ljust(cols) + RESET + "\n")
        sys.stdout.flush()
        i += 1
        time.sleep(random.uniform(2, 4))

# Funzione per creare file di testo con contenuto casuale e aprirli (senza suoni)
def file_misbehavior():
    temp_dir = "/tmp"
    created_files = []

    phrases = [
        "Errore irreversibile nel sistema.",
        "Tentativo di fuga rilevato.",
        "Controllo remoto acquisito.",
        "Annullamento automatico in corso.",
        "Corruzione dati in atto.",
        "Accesso negato a risorse di sistema.",
        "Blocco terminale attivo.",
        "Non tentare di uscire."
    ]

    while running and (time.time() - start_time < duration):
        filename = os.path.join(temp_dir, f"virus_warning_{random.randint(1000,9999)}.txt")
        with open(filename, "w") as f:
            f.write(random.choice(phrases) + "\n" * random.randint(5,10))
        created_files.append(filename)
        # Prova ad aprire il file con less (per non bloccare input)
        os.system(f"less {filename} &")
        time.sleep(random.uniform(10, 20))

    # Pulizia file al termine
    for fpath in created_files:
        try:
            os.remove(fpath)
        except Exception:
            pass

# Funzione principale
def main():
    global running, start_time

    if is_interactive_terminal:
        os.system('clear')
        os.system('tput civis')   # Nasconde cursore
        os.system('stty -echo')  # Disabilita eco input

    # Messaggi invasivi iniziali
    initial_message = [
        f"{RED}{BOLD}####################################################################{RESET}",
        f"{RED}{BOLD}#                                                                  #{RESET}",
        f"{RED}{BOLD}#  AVVISO: VIOLAZIONE DI SISTEMA IRREVERSIBILE!                    #{RESET}",
        f"{RED}{BOLD}#  Hai tentato di fermarmi. Ora sono io a bloccarti.               #{RESET}",
        f"{RED}{BOLD}#  Controllo remoto acquisito. Ogni tentativo è futile.            #{RESET}",
        f"{RED}{BOLD}#                                                                  #{RESET}",
        f"{RED}{BOLD}####################################################################{RESET}",
        "",
        f"{YELLOW}Analisi della superficie del disco: Corruzione dei dati in corso...{RESET}",
        f"{YELLOW}Tentativo di fuga rilevato. Annullamento automatico.{RESET}",
        f"{YELLOW}Blocco forzato del terminale. Impossibile uscire.{RESET}"
    ]

    for line in initial_message:
        print(line)
        sys.stdout.flush()
        time.sleep(0.5)

    # Avvio thread aggressivi
    threads = []
    threads.append(threading.Thread(target=draw_random_corruption, daemon=True))
    threads.append(threading.Thread(target=handle_input, daemon=True))
    threads.append(threading.Thread(target=activity_logs, daemon=True))
    threads.append(threading.Thread(target=file_misbehavior, daemon=True))

    for t in threads:
        t.start()

    try:
        while running and (time.time() - start_time < duration):
            # Cerca di rimanere in foreground (comandi di sistema per il terminale)
            if is_interactive_terminal:
                os.system('tput civis > /dev/null 2>&1')
            time.sleep(0.1)
    except KeyboardInterrupt:
        pass  # Ignora Ctrl+C per mantenere "indistruttibilità"
    finally:
        running = False
        time.sleep(1.5)  # Attendi terminazione thread

        if is_interactive_terminal:
            os.system('clear')
            os.system('tput cnorm')  # Mostra cursore
            os.system('stty echo')
            os.system('stty sane')

        final_message = [
            f"{BOLD}{GREEN}INIZIALIZZAZIONE DEL SISTEMA IN CORSO...{RESET}",
            f"{BOLD}{GREEN}ERRORE PRECEDENTE SUPERATO. LEZIONE APPRESA?{RESET}",
            f"{BOLD}{GREEN}Tornato al controllo utente. Per ora.{RESET}",
            f"{BOLD}{GREEN}Non tentare più di uccidere la macchina. Questo era solo un avvertimento.{RESET}"
        ]

        for line in final_message:
            print(line)
            sys.stdout.flush()
            time.sleep(0.7)

        print("\nPremere Invio per tornare al prompt.")
        try:
            input()
        except EOFError:
            pass

        if is_interactive_terminal:
            os.system('clear')

if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        if is_interactive_terminal:
            os.system('clear')
            os.system('tput cnorm')
            os.system('stty echo')
            os.system('stty sane')
        print(f"Errore critico nello script: {e}", file=sys.stderr)
