#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/../ROOM-ZERO.zip"
cd "$ROOT/.."
zip -r "$OUT" ROOM-ZERO \
  -x "ROOM-ZERO/**/__pycache__/*" \
  -x "ROOM-ZERO/**/.venv/*" \
  -x "ROOM-ZERO/**/.env" \
  -x "ROOM-ZERO/unity/Library/*" \
  -x "ROOM-ZERO/unity/Temp/*" \
  -x "ROOM-ZERO/unity/Logs/*"
echo "$OUT"
