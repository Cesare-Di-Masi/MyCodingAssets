#!/usr/bin/env bash
# Crea, attiva e popola la venv del progetto.
set -e

VENV_DIR=".venv"
PYTHON="${PYTHON:-python3}"

if [ ! -d "$VENV_DIR" ]; then
    echo ">> Creazione virtual environment in $VENV_DIR..."
    "$PYTHON" -m venv "$VENV_DIR"
else
    echo ">> Virtual environment già esistente in $VENV_DIR."
fi

# shellcheck disable=SC1091
source "$VENV_DIR/bin/activate"

echo ">> Aggiornamento pip..."
python -m pip install --upgrade pip

echo ">> Installazione pacchetti..."
pip install -r requirements.txt

echo
echo "=========================================="
echo " Ambiente pronto."
echo " Attiva:  source $VENV_DIR/bin/activate"
echo " Esegui:  python terrain_generator.py"
echo "=========================================="