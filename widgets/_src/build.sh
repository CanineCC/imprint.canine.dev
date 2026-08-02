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
    /home/jimmy/.bun/install/cache/@esbuild/linux-x64@0.27.1@@@1/bin/esbuild \
    "$(command -v esbuild 2>/dev/null || true)"; do
    if [ -n "$candidate" ] && [ -x "$candidate" ]; then ESBUILD="$candidate"; break; fi
  done
fi
: "${ESBUILD:?esbuild not found — set \$ESBUILD to an esbuild binary}"
# Bundles are content-hashed by the publisher, so the esbuild version is part of the output: a
# different one renames internal variables, rotates every hash, and re-downloads the whole widget
# set for no behavioural change. Say which one built this, so a surprising diff is explicable.
echo "esbuild $("$ESBUILD" --version) — $ESBUILD"
SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUT="$(dirname "$SRC")"

TAGS=(
  cai-score-card
  cai-card-gallery
  cai-band-scale
  cai-lens-gauges
  cai-trend
  cai-survey-list
  cai-language-board
  cai-link-cards
  cai-composition-bar
  cai-evidence-flow
  cai-c4-heat
  cai-findings
  cai-language-support
  cai-public-reports
  cai-verifier
  cai-calculator
  cai-report-index
  wd-embed
  contact-form
)

# Build only what was named, when any were: a newer esbuild renames internal variables, so a
# blanket rebuild rotates the content hash of every untouched widget and forces every visitor to
# re-download bundles whose behaviour did not change.
if [ "$#" -gt 0 ]; then TAGS=("$@"); fi

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
