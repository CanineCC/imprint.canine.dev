#!/usr/bin/env python3
"""Render the og:image share card for each site.

A share card is the only part of a page that gets seen without being visited — in a chat
app, a search preview, a model's citation. These are built from the same two things the
sites already own: each site's published theme colours and the brand typeface the pages
themselves are set in, so the card and the page it points at look like one thing.

Deliberately typographic and quiet. This is a placeholder a designer should replace; it is
here because "no image at all" degrades every share to a bare URL, and a plain branded card
beats that while costing nothing to throw away.

    python3 make-social-cards.py --out ./cards

Needs: pillow, fonttools+brotli (to unpack the woff2 the site ships).
"""

from __future__ import annotations

import argparse
import pathlib
import sys

from PIL import Image, ImageDraw, ImageFont

# 1200x630 is the size every major platform crops toward; below ~600x315 they fall back to
# the small text-only card, which is the thing we are trying to avoid.
# Rendered at 2x. The media pipeline derives variants but never upscales past the source,
# so a 1200-wide original caps the widest variant at 960 — below what every platform asks
# for. Supplying 2400 lets it derive one at full card size.
SCALE = 2
WIDTH, HEIGHT = 1200 * SCALE, 630 * SCALE
MARGIN = 88 * SCALE

# Taken from each site's PUBLISHED stylesheet (the dark-scheme half of its light-dark()
# tokens), not invented here — the card inherits the palette the site actually renders.
SITES = {
    "watchdog": {
        "name": "Watchdog",
        "line": "Codebase health, measured.",
        "sub": "One reproducible 0–100 score for your whole product, in every major language.",
        "bg": "#15191e", "primary": "#7faace", "text": "#e4e9ed", "muted": "#8694a1",
    },
    "assay": {
        "name": "Assay",
        "line": "Independent software appraisal.",
        "sub": "For the people who buy software, and cannot read it. A verdict you can defend.",
        "bg": "#1b1a17", "primary": "#c99a6a", "text": "#ece7dd", "muted": "#9a9184",
    },
    "cai": {
        "name": "CAI",
        "line": "The open standard for codebase assurance.",
        "sub": "One reproducible 0–100 number. The method is public — verify it yourself.",
        "bg": "#15191e", "primary": "#6fbfa4", "text": "#e4e9ed", "muted": "#8694a1",
    },
    "www": {
        "name": "Canine Development",
        "line": "We measure whether software is sound.",
        "sub": "An independent software assurance studio. We do not build what we measure.",
        "bg": "#15191e", "primary": "#7faace", "text": "#e4e9ed", "muted": "#8694a1",
    },
}


def wrap(draw: ImageDraw.ImageDraw, text: str, font: ImageFont.FreeTypeFont, limit: int) -> list[str]:
    lines: list[str] = []
    words = text.split()
    current = ""
    for word in words:
        candidate = f"{current} {word}".strip()
        if draw.textlength(candidate, font=font) <= limit:
            current = candidate
        else:
            if current:
                lines.append(current)
            current = word
    if current:
        lines.append(current)
    return lines


def render(slug: str, spec: dict, regular: str, bold: str, out: pathlib.Path) -> pathlib.Path:
    image = Image.new("RGB", (WIDTH, HEIGHT), spec["bg"])
    draw = ImageDraw.Draw(image)

    # A single accent rule, top-left: enough to carry the brand without a logo we would
    # then have to keep in step with the real one.
    draw.rectangle([MARGIN, MARGIN, MARGIN + 96 * SCALE, MARGIN + 6 * SCALE], fill=spec["primary"])

    name_font = ImageFont.truetype(bold, 44 * SCALE)
    line_font = ImageFont.truetype(bold, 62 * SCALE)
    sub_font = ImageFont.truetype(regular, 30 * SCALE)
    foot_font = ImageFont.truetype(regular, 24 * SCALE)

    y = MARGIN + 52 * SCALE
    draw.text((MARGIN, y), spec["name"], font=name_font, fill=spec["primary"])
    y += 86 * SCALE

    usable = WIDTH - 2 * MARGIN
    for line in wrap(draw, spec["line"], line_font, usable):
        draw.text((MARGIN, y), line, font=line_font, fill=spec["text"])
        y += 76 * SCALE

    y += 18 * SCALE
    for line in wrap(draw, spec["sub"], sub_font, usable):
        draw.text((MARGIN, y), line, font=sub_font, fill=spec["muted"])
        y += 42 * SCALE

    draw.text((MARGIN, HEIGHT - MARGIN - 24 * SCALE), "canine.dev", font=foot_font, fill=spec["muted"])

    out.mkdir(parents=True, exist_ok=True)
    path = out / f"{slug}-social.png"
    image.save(path, "PNG", optimize=True)
    return path


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", default="./cards")
    parser.add_argument("--regular", default="/tmp/schibsted-reg.ttf")
    parser.add_argument("--bold", default="/tmp/schibsted-bold.ttf")
    parser.add_argument("--site", action="append", choices=sorted(SITES))
    args = parser.parse_args()

    for name in (args.regular, args.bold):
        if not pathlib.Path(name).exists():
            print(
                f"missing font {name}. Unpack the site's woff2 first:\n"
                "  python3 -c \"from fontTools.ttLib import TTFont; "
                "f=TTFont('src/Imprint.Rendering/wwwroot/fonts/schibsted-var.woff2'); "
                "f.flavor=None; f.save('/tmp/schibsted.ttf')\"",
                file=sys.stderr,
            )
            return 1

    out = pathlib.Path(args.out)
    for slug in args.site or sorted(SITES):
        path = render(slug, SITES[slug], args.regular, args.bold, out)
        print(f"  {slug:<9} {path}  ({path.stat().st_size:,} bytes)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
