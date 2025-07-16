#!/bin/bash

# Path assoluto a questa directory (dove si trova anche update_readme.py)
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PYTHON_SCRIPT="$SCRIPT_DIR/update_readme.py"

# Trova la root del repo git risalendo da SCRIPT_DIR
REPO_ROOT="$(git -C "$SCRIPT_DIR" rev-parse --show-toplevel)"
HOOKS_DIR="$REPO_ROOT/.git/hooks"

# Contenuto dell'hook pre-commit
cat > "$HOOKS_DIR/pre-commit" <<EOF
#!/bin/sh
echo "[HOOK] Running pre-commit: updating README.md"
python3 "$PYTHON_SCRIPT"
git add README.md
EOF

# Contenuto dell'hook post-merge
cat > "$HOOKS_DIR/post-merge" <<EOF
#!/bin/sh
echo "[HOOK] Running post-merge: updating README.md"
python3 "$PYTHON_SCRIPT"
git add README.md
EOF

# Rende gli hook eseguibili
chmod +x "$HOOKS_DIR/pre-commit" "$HOOKS_DIR/post-merge"

echo "✅ Hook installati correttamente."
