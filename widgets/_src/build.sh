#!/usr/bin/env bash
# Bundle each CAI marketing island into ONE self-contained ESM file in widgets/.
# Uses the esbuild already vendored in the cms.canine.dev workspace.
set -euo pipefail

ESBUILD="/home/jimmy/RiderProjects/cms.canine.dev/node_modules/.bin/esbuild"
SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUT="$(dirname "$SRC")"

TAGS=(
  cai-score-card
  cai-card-gallery
  cai-band-scale
  cai-composition-bar
  cai-evidence-flow
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
