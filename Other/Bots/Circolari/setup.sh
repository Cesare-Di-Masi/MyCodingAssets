#!/usr/bin/env bash
# Setup script per il bot circolari — crea il venv e installa tutte le dipendenze.
# Uso: chmod +x setup.sh && ./setup.sh

set -e  # esce subito se un comando fallisce

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "==> Controllo Python 3..."
if ! command -v python3 &> /dev/null; then
    echo "ERRORE: python3 non trovato. Installalo prima di continuare."
    echo "  Arch/Manjaro: sudo pacman -S python python-pip"
    echo "  Debian/Ubuntu: sudo apt install python3 python3-venv python3-pip"
    exit 1
fi
python3 --version

echo "==> Creo l'ambiente virtuale (venv/)..."
if [ ! -d "venv" ]; then
    python3 -m venv venv
else
    echo "    venv/ esiste già, salto."
fi

echo "==> Attivo il venv e aggiorno pip..."
source venv/bin/activate
pip install --upgrade pip --quiet

echo "==> Installo le dipendenze..."
pip install -r requirements.txt

echo "==> Controllo file .env..."
if [ ! -f ".env" ]; then
    if [ -f ".env.example" ]; then
        cp .env.example .env
        echo "    Creato .env da .env.example — APRILO E METTI BOT_TOKEN e CHAT_ID veri prima di lanciare il bot."
    else
        cat > .env << 'EOF'
BOT_TOKEN=metti_qui_il_tuo_token
CHAT_ID=metti_qui_il_tuo_chat_id
EOF
        echo "    Creato .env vuoto — APRILO E METTI BOT_TOKEN e CHAT_ID veri prima di lanciare il bot."
    fi
    chmod 600 .env
else
    echo "    .env già presente, non lo tocco."
fi

echo ""
echo "==> Fatto! Per usare il bot:"
echo "    source venv/bin/activate"
echo "    python circolari_bot.py --dry-run"