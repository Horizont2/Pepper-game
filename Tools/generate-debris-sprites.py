#!/usr/bin/env python3
"""
Generates the debris sprites used by DebrisBurst.

Lives outside Assets/ on purpose: Unity never imports it, and the art it writes is
checked in, so this only needs running when someone wants to retune the shapes or the
palette. Run it from the repository root:

    python3 Tools/generate-debris-sprites.py

Every piece is one closed polygon drawn three times - an expanded outline, a flat fill,
then a smaller offset highlight - at 6x and downsampled. That is the whole look: soft
cartoon with a dark warm outline and one light, which is what the rest of the game's art
does. Flat shading rather than a gradient matters at this size: these are 20-odd pixels
across and in motion, and anything subtler than one highlight is mud.
"""

import os
from PIL import Image, ImageDraw

SUPERSAMPLE = 6
OUT = "Assets/Art/Effects/Debris"

# Paprica's own palette. The outline is a deep warm red-brown rather than black, which is
# what keeps a cartoon piece from reading as a hole punched in the screen.
PEPPER = dict(outline=(0x5E, 0x12, 0x12, 255), fill=(0xD4, 0x36, 0x2A, 255), light=(0xF4, 0x6E, 0x4C, 255))
SEED = dict(outline=(0x8A, 0x6A, 0x24, 255), fill=(0xEF, 0xD9, 0x95, 255), light=(0xFA, 0xF0, 0xCE, 255))
CRUMB = dict(outline=(0x3A, 0x2E, 0x26, 255), fill=(0x8A, 0x72, 0x5C, 255), light=(0xB8, 0x9F, 0x84, 255))

# Shapes in a 0..1 box. Deliberately irregular: four pieces cut from the same rounded blob
# read as one sprite repeated, which is exactly what debris must not look like.
SHAPES = {
    "pepper-chunk-a": [(0.50, 0.04), (0.92, 0.38), (0.78, 0.92), (0.26, 0.86), (0.07, 0.44)],
    "pepper-chunk-b": [(0.30, 0.08), (0.88, 0.16), (0.96, 0.62), (0.58, 0.95), (0.12, 0.72), (0.04, 0.30)],
    "pepper-chunk-c": [(0.18, 0.10), (0.74, 0.04), (0.95, 0.46), (0.62, 0.88), (0.22, 0.70)],
    "pepper-sliver": [(0.50, 0.02), (0.86, 0.28), (0.78, 0.74), (0.46, 0.98), (0.18, 0.64), (0.22, 0.24)],
    "pepper-seed": [(0.50, 0.06), (0.80, 0.30), (0.84, 0.66), (0.50, 0.94), (0.16, 0.66), (0.20, 0.30)],
    "crumb-a": [(0.42, 0.06), (0.90, 0.30), (0.82, 0.80), (0.34, 0.94), (0.08, 0.54)],
    "crumb-b": [(0.26, 0.12), (0.78, 0.08), (0.94, 0.54), (0.56, 0.92), (0.14, 0.74)],
}

PALETTES = {
    "pepper-chunk-a": PEPPER, "pepper-chunk-b": PEPPER, "pepper-chunk-c": PEPPER,
    "pepper-sliver": PEPPER, "pepper-seed": SEED, "crumb-a": CRUMB, "crumb-b": CRUMB,
}

# Pixel size of each piece. Small, and the importer's pixels-per-unit does the rest.
SIZES = {
    "pepper-chunk-a": (22, 22), "pepper-chunk-b": (24, 20), "pepper-chunk-c": (20, 20),
    "pepper-sliver": (14, 26), "pepper-seed": (12, 16), "crumb-a": (20, 20), "crumb-b": (22, 18),
}


def scaled(points, factor, box, shift=(0.0, 0.0)):
    """The polygon grown or shrunk about its own centroid, in pixels."""
    cx = sum(p[0] for p in points) / len(points)
    cy = sum(p[1] for p in points) / len(points)
    w, h = box
    return [(((cx + (x - cx) * factor) + shift[0]) * w,
             ((cy + (y - cy) * factor) + shift[1]) * h) for x, y in points]


def render(name):
    w, h = SIZES[name]
    box = (w * SUPERSAMPLE, h * SUPERSAMPLE)
    palette = PALETTES[name]
    points = SHAPES[name]

    image = Image.new("RGBA", box, (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    # Outline first, as the shape grown slightly, then the fill laid on top of it. Drawing
    # a stroke instead would thin out on the sharp corners these shapes are made of.
    draw.polygon(scaled(points, 1.0, box), fill=palette["outline"])
    draw.polygon(scaled(points, 0.84, box), fill=palette["fill"])

    # One light, up and to the left. An ellipse rather than a smaller copy of the outline:
    # repeating the silhouette inside itself reads as a faceted gem, and these are meant to
    # be wet flesh and seed husk.
    cx = sum(p[0] for p in points) / len(points)
    cy = sum(p[1] for p in points) / len(points)
    rx, ry = 0.21 * w * SUPERSAMPLE, 0.19 * h * SUPERSAMPLE
    hx, hy = (cx - 0.11) * w * SUPERSAMPLE, (cy - 0.14) * h * SUPERSAMPLE
    draw.ellipse([hx - rx, hy - ry, hx + rx, hy + ry], fill=palette["light"])

    return image.resize((w, h), Image.LANCZOS)


def main():
    os.makedirs(OUT, exist_ok=True)
    for name in SHAPES:
        path = os.path.join(OUT, name + ".png")
        render(name).save(path)
        print(f"  {path}  {SIZES[name][0]}x{SIZES[name][1]}")


if __name__ == "__main__":
    main()
