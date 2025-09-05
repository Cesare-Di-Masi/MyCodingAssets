#!/usr/bin/env bash
# params: target profile outpath
TARGET="$1"
PROFILE="$2"
OUT="$3"

# safe defaults: quick scan for 'safe', full otherwise
if [ "$PROFILE" = "safe" ]; then
  nmap -sn "$TARGET" -oX "$OUT"
else
  # full SYN scan, service detection, OS guess
  nmap -sS -sV -O --top-ports 200 "$TARGET" -oX "$OUT"
fi
echo "nmap finished, output: $OUT"
