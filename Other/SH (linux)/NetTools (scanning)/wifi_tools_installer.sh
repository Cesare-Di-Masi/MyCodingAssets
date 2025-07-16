#!/bin/bash

echo "[*] Updating APT repositories..."
sudo apt update

echo "[*] Installing Wi-Fi cracking dependencies..."
sudo apt install -y \
  aircrack-ng \
  hcxdumptool \
  hcxtools \
  hashcat \
  build-essential \
  cmake \
  libpcap-dev \
  libcurl4-openssl-dev \
  pkg-config \
  git \
  net-tools

echo "[*] Installation complete!"

echo
echo "[*] Tools installed:"
echo "  - aircrack-ng"
echo "  - hcxdumptool"
echo "  - hcxtools (includes hcxpcapngtool)"
echo "  - hashcat"
echo "  - build tools for source-based installs"
echo
echo "You’re now ready to capture WPA handshakes and test them. Use responsibly."

