# COLORI ANSI
NC="\033[0m"
BLUE="\033[1;34m"
GREEN="\033[1;32m"
YELLOW="\033[1;33m"
RED="\033[1;31m"
MAGENTA="\033[1;35m"
CYAN="\033[1;36m"
BOLD="\033[1m"

separator() {
    printf "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}\n"
}

title() {
    clear
    separator
    printf "${CYAN}${BOLD}             ✦ NETWORK MONITORING TOOL ✦${NC}\n"
    printf "${MAGENTA}               Powered by Kali Linux${NC}\n"
    separator

    ip=$(curl -s http://checkip.amazonaws.com)
    printf "${GREEN}[IP Pubblico] ${YELLOW}%s${NC}\n" "$ip"
    separator
}

scan_full_network() {
    iface_info=$(ip route | grep default | awk '{print $5}' | head -n1)
    default_subnet=$(ip -o -f inet addr show dev "$iface_info" | awk '{print $4}')

    printf "\n${BLUE}[➔] Inserisci la subnet da scansionare (default: ${YELLOW}%s${BLUE}): ${NC}" "$default_subnet"
    read subnet
    echo "$subnet" | grep -qE '^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+/[0-9]+$'
    if [ $? -ne 0 ]; then
        printf "${YELLOW}[!] Subnet non valida o vuota. Uso la subnet di default: ${CYAN}%s${NC}\n" "$default_subnet"
        subnet="$default_subnet"
    fi

    printf "${YELLOW}[⏳] Scansione completa in corso sulla subnet ${CYAN}%s${NC}...\n" "$subnet"
    nmap -sS -O -sV "$subnet" -oN result_full_scan.txt

    printf "\n${GREEN}[✔] Risultati della scansione:${NC}\n"
    printf "${BOLD}${CYAN}%-20s %-18s %-20s %-30s %-20s${NC}\n" "IP" "MAC Address" "Host" "OS" "Produttore"
    printf "${CYAN}------------------------------------------------------------------------------------------------------------${NC}\n"

    awk '/Nmap scan report/{ip=$NF} /MAC Address:/{mac=$3; vendor=substr($0,index($0,$4))} /OS details:/{os=substr($0,index($0,$3))} /Service Info:/{host=$NF; print ip, mac, host, os, vendor}' result_full_scan.txt |
    while read ip mac host os vendor; do
        printf "%-20s %-18s %-20s %-30s %-20s\n" "$ip" "$mac" "$host" "$os" "$vendor"
    done

    printf "\nPremi INVIO per tornare al menu..."
    read junk
}

ping_host() {
    printf "\n${BLUE}[➔] Inserisci l'IP o dominio da pingare: ${NC}"
    read target
    printf "${YELLOW}[⏳] Ping in corso su %s...${NC}\n" "$target"
    ping -c 4 "$target"
    printf "\nPremi INVIO per tornare al menu..."
    read
}

monitor_bandwidth() {
    printf "\n${YELLOW}[⏳] Avvio monitoraggio banda (iftop)...${NC}\n"
    sudo iftop
    printf "\nPremi INVIO per tornare al menu..."
    read
}

monitor_traffic() {
    printf "\n${YELLOW}[⏳] Avvio monitoraggio traffico (nload)...${NC}\n"
    sudo nload
    printf "\nPremi INVIO per tornare al menu..."
    read
}

active_connections() {
    printf "\n${CYAN}[➔] Connessioni di rete attive:${NC}\n"
    netstat -tunap
    printf "\nPremi INVIO per tornare al menu..."
    read
}

packet_capture() {
    printf "\n${BLUE}[➔] Inserisci l'interfaccia di rete (es. eth0): ${NC}"
    read iface
    printf "${YELLOW}[⏳] Avvio tcpdump su %s... Premi Ctrl+C per uscire.${NC}\n" "$iface"
    sudo tcpdump -i "$iface"
    printf "\nPremi INVIO per tornare al menu..."
    read
}

routing_info() {
    printf "\n${CYAN}[➔] Tabella di routing:${NC}\n"
    ip route show
    printf "\nPremi INVIO per tornare al menu..."
    read
}

traceroute_path() {
    printf "\n${BLUE}[➔] Inserisci IP o dominio da tracciare: ${NC}"
    read target
    traceroute "$target"
    printf "\nPremi INVIO per tornare al menu..."
    read
}

dig_dns() {
    printf "\n${BLUE}[➔] Inserisci dominio da risolvere (dig): ${NC}"
    read domain
    dig "$domain"
    printf "\nPremi INVIO per tornare al menu..."
    read
}

nslookup_dns() {
    printf "\n${BLUE}[➔] Inserisci dominio da risolvere (nslookup): ${NC}"
    read domain
    nslookup "$domain"
    printf "\nPremi INVIO per tornare al menu..."
    read
}

view_arp() {
    printf "\n${CYAN}[➔] Tabella ARP:${NC}\n"
    arp -a
    printf "\nPremi INVIO per tornare al menu..."
    read
}

mtr_monitor() {
    printf "\n${BLUE}[➔] Inserisci IP o dominio da monitorare con MTR: ${NC}"
    read target
    sudo mtr "$target"
    printf "\nPremi INVIO per tornare al menu..."
    read
}

wifi_analyzer() {
    printf "\n${BLUE}[➔] Inserisci l'interfaccia Wi-Fi (es. wlan0): ${NC}"
    read iface
    printf "\n${YELLOW}[⏳] Scansione delle reti Wi-Fi in corso...${NC}\n"
    sudo iwlist "$iface" scan | grep -E "ESSID|Quality|Signal level|Encryption"
    printf "\nPremi INVIO per tornare al menu..."
    read
}

angry_ip_scan() {
    printf "\n${BLUE}[➔] Inserisci la subnet per scansione IP (es. 192.168.1.0/24): ${NC}"
    read subnet
    printf "${YELLOW}[⏳] Scansione IP rapida in corso...${NC}\n"
    fping -a -g "$subnet" 2>/dev/null
    printf "\nPremi INVIO per tornare al menu..."
    read
}

launch_wireshark() {
    printf "\n${YELLOW}[⏳] Avvio Wireshark...${NC}\n"
    sudo wireshark &
    printf "\nPremi INVIO per tornare al menu..."
    read
}

main() {
    title
      while true; do
        separator
        printf "${BOLD}${BLUE} MENU PRINCIPALE - Scegli un'opzione:${NC}\n"
        printf "${YELLOW} 1${NC}) Scansione completa rete (IP, MAC, OS, vendor...)\n"
        printf "${YELLOW} 2${NC}) Esegui Ping\n"
        printf "${YELLOW} 3${NC}) Monitoraggio banda (iftop)\n"
        printf "${YELLOW} 4${NC}) Monitoraggio traffico (nload)\n"
        printf "${YELLOW} 5${NC}) Connessioni attive\n"
        printf "${YELLOW} 6${NC}) Cattura pacchetti (tcpdump)\n"
        printf "${YELLOW} 7${NC}) Tabella di routing\n"
        printf "${YELLOW} 8${NC}) Traceroute\n"
        printf "${YELLOW} 9${NC}) DNS (dig)\n"
        printf "${YELLOW}10${NC}) DNS (nslookup)\n"
        printf "${YELLOW}11${NC}) Tabella ARP\n"
        printf "${YELLOW}12${NC}) MTR\n"
        printf "${YELLOW}13${NC}) Wi-Fi Analyzer\n"
        printf "${YELLOW}14${NC}) Scansione IP rapida (Angry IP Style)\n"
        printf "${YELLOW}15${NC}) Avvia Wireshark\n"
        printf "${RED} q${NC}) Esci\n"
        separator
        printf "\n${BOLD}➔ Seleziona (1-15 o q): ${NC}"
        read opt
        case "$opt" in
            1) scan_full_network ;;
            2) ping_host ;;
            3) monitor_bandwidth ;;
            4) monitor_traffic ;;
            5) active_connections ;;
            6) packet_capture ;;
            7) routing_info ;;
            8) traceroute_path ;;
            9) dig_dns ;;
            10) nslookup_dns ;;
            11) view_arp ;;
            12) mtr_monitor ;;
            13) wifi_analyzer ;;
            14) angry_ip_scan ;;
            15) launch_wireshark ;;
            q) printf "\n${RED}[✖] Uscita...${NC}\n"; break ;;
            *) printf "${RED}[ERRORE] Opzione non valida!${NC}\n" ;;
        esac
    done
}

main

