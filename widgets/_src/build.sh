#!/usr/bin/env bash
# Bundle each CAI marketing island into ONE self-contained ESM file in widgets/.
# Uses the esbuild already vendored in the cms.canine.dev workspace.
set -euo pipefail

# esbuild lookup: honour an $ESBUILD override, else try known vendored locations, else PATH.
ESBUILD="${ESBUILD:-}"
if [ -z "$ESBUILD" ] || [ ! -x "$ESBUILD" ]; then
  for candidate in \
    /home/jimmy/RiderProjects/cms.canine.dev/node_modules/.bin/esbuild \
    /home/jimmy/RiderProjects/cms/node_modules/.bin/esbuild \
    "$(command -v esbuild 2>/dev/null || true)"; do
    if [ -n "$candidate" ] && [ -x "$candidate" ]; then ESBUILD="$candidate"; break; fi
  done
fi
: "${ESBUILD:?esbuild not found — set \$ESBUILD to an esbuild binary}"
SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUT="$(dirname "$SRC")"

TAGS=(
  cai-score-card
  cai-card-gallery
  cai-band-scale
  cai-composition-bar
  cai-evidence-flow
  cai-c4-heat
  cai-findings
  cai-language-support
  cai-verifier
  cai-calculator
  cai-report-index
  contact-form
)

for tag in "${TAGS[@]}"; do
  "$ESBUILD" "$SRC/$tag.js" \
    --bundle \
    --format=esm \
    --minify \
    --target=es2022 \
    --legal-comments=none \
    --outfile="$OUT/$tag.js"
  echo "built widgets/$tag.js ($(wc -c < "$OUT/$tag.js") bytes)"
done
