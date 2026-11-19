#!/usr/bin/env python3
"""
Generates the sprites used by DebrisBurst and AmbientDrifters.

Lives outside Assets/ on purpose: Unity never imports it, and the art it writes is
checked in, so this only needs running when someone wants to retune the shapes or the
palette. Run it from the repository root:

    python3 Tools/generate-effect-sprites.py

Every piece is one closed polygon drawn three times - an expanded outline, a flat fill,
then a smaller offset highlight - at 6x and downsampled. That is the whole look: soft
cartoon with a dark warm outline and one light, which is what the rest of the game's art
does. Flat shading rather than a gradient matters at this size: these are 20-odd pixels
across and in motion, and anything subtler than one highlight is mud.
"""

import math
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


AMBIENT_OUT = "Assets/Art/Effects/Ambient"
IMPACT_OUT = "Assets/Art/Effects/Impact"

# Warm all the way through. The kitchen these effects live in is lit by gas and copper, and
# a cold highlight in the middle of it reads as belonging to a different game.
HOT = (0xFF, 0xF2, 0xD6, 255)      # core, barely coloured
MID = (0xFF, 0xA8, 0x3C, 255)      # the body of the flash
EDGE = (0xD1, 0x36, 0x22, 255)     # paprika, at the tips where it cools


def _lerp(a, b, t):
    return a + (b - a) * t


def _fade(image, alpha):
    if alpha >= 1.0:
        return image
    image.putalpha(image.getchannel("A").point(lambda v: int(v * alpha)))
    return image


def impact_frame(size, step, steps, spikes=9):
    """
    One frame of a cartoon impact starburst: tapered spikes off a bright core.

    The spikes alternate long and short. Evenly sized ones draw a cog, and the eye reads a
    cog as a wheel rather than as a blow landing.
    """
    t = step / (steps - 1)
    box = size * SUPERSAMPLE
    image = Image.new("RGBA", (box, box), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    c = box / 2.0

    outer = _lerp(0.22, 0.50, t) * box
    base = _lerp(0.11, 0.15, t) * box    # where a spike is widest
    taper = _lerp(0.52, 0.14, t)         # how wide a spike is there
    spin = t * 0.22                      # a little rotation so it never sits still

    # The core collapses as the spikes throw outward. Growing it instead leaves a pale
    # disc sitting in the middle of the burst, which reads as a hole punched in the frame.
    core = _lerp(0.19, 0.0, min(1.0, t * 1.4)) * box

    for colour, reach in ((EDGE, 1.0), (MID, 0.74), (HOT, 0.40)):
        for i in range(spikes):
            a = (i / spikes + spin) * math.tau
            length = outer * reach * (1.0 if i % 2 == 0 else 0.68)
            half = (math.tau / spikes) * 0.5 * taper
            draw.polygon([
                (c + math.cos(a) * length, c + math.sin(a) * length),
                (c + math.cos(a + half) * base, c + math.sin(a + half) * base),
                (c + math.cos(a - half) * base, c + math.sin(a - half) * base),
            ], fill=colour)

    if core > 0.5:
        draw.ellipse([c - core, c - core, c + core, c + core], fill=HOT)

    # Opaque while it reads, then out - but never all the way, or the last frame is a
    # blank sprite sitting in the array doing nothing.
    alpha = 1.0 if t < 0.45 else _lerp(1.0, 0.16, (t - 0.45) / 0.55)
    return _fade(image.resize((size, size), Image.LANCZOS), alpha)


STEAM_LIGHT = (0xF7, 0xF0, 0xE4, 255)
STEAM_SHADE = (0xD6, 0xC7, 0xB2, 255)

# Fixed layout, so the puff is the same cloud moving rather than a new one each frame.
# Angles are uneven on purpose: evenly spaced blobs make a flower.
STEAM_BLOBS = [
    (-0.30, 0.30, 1.05), (0.55, 0.62, 0.86), (1.45, 0.95, 0.78),
    (2.35, 0.55, 0.92), (3.05, 0.88, 0.70), (3.95, 0.72, 0.82),
    (5.05, 0.45, 0.74),
]


def steam_frame(size, step, steps):
    """
    One frame of a puff of steam venting and thinning out.

    This is what a kitchen appliance does when it stops working, and it has to read as
    clearly NOT an explosion. The difference is entirely in the motion: a blast expands
    fast and evenly and hollows into a ring, while steam rises, drifts apart slowly and
    thins. So the cloud here moves UP across its frames and its blobs shrink as they
    separate - at no point does it open into a ring.
    """
    t = step / (steps - 1)
    box = size * SUPERSAMPLE
    image = Image.new("RGBA", (box, box), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    c = box / 2.0

    spread = _lerp(0.04, 0.40, t) * box
    rise = _lerp(0.13, -0.30, t) * box      # screen y, so negative is upward
    radius = _lerp(0.22, 0.07, t) * box
    shade = radius * 0.28

    for angle, distance, scale in STEAM_BLOBS:
        x = c + math.cos(angle) * spread * distance
        y = c + math.sin(angle) * spread * distance * 0.9 + rise
        r = radius * scale

        draw.ellipse([x - r, y - r + shade, x + r, y + r + shade], fill=STEAM_SHADE)
        draw.ellipse([x - r, y - r, x + r, y + r], fill=STEAM_LIGHT)

    # Thin from the start - steam is never opaque - and keep a trace on the last frame
    alpha = _lerp(0.88, 0.07, t ** 1.15)
    return _fade(image.resize((size, size), Image.LANCZOS), alpha)


def ring_frame(size, step, steps):
    """
    One frame of an expanding ring: the parry throwing a blow back off.

    A ring says "pushed away from here" in a way a burst cannot - a burst is something
    breaking, and a parry is the opposite of that.
    """
    t = step / (steps - 1)
    box = size * SUPERSAMPLE
    image = Image.new("RGBA", (box, box), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    c = box / 2.0

    radius = _lerp(0.13, 0.47, t) * box
    thickness = _lerp(0.13, 0.022, t) * box

    for colour, grow in ((EDGE, 1.25), (MID, 1.0), (HOT, 0.55)):
        r = radius
        w = thickness * grow
        draw.ellipse([c - r - w, c - r - w, c + r + w, c + r + w], fill=colour)

    # Punch the middle out last, so every layer is cut by the same hole
    hole = radius - thickness * 0.5
    if hole > 0:
        draw.ellipse([c - hole, c - hole, c + hole, c + hole], fill=(0, 0, 0, 0))

    alpha = 1.0 if t < 0.3 else _lerp(1.0, 0.14, (t - 0.3) / 0.7)
    return _fade(image.resize((size, size), Image.LANCZOS), alpha)


def mote(size, softness):
    """
    A round mote, white so AmbientDrifters can tint each one from its own palette.

    Alpha falls off as a power curve rather than linearly: linear gives a flat disc with a
    visible rim, and these have to read as something hanging in the light rather than a
    sticker. A high softness is airborne dust, a low one is a speck of ground spice.
    """
    box = size * SUPERSAMPLE
    image = Image.new("RGBA", (box, box), (255, 255, 255, 0))
    pixels = image.load()
    centre = (box - 1) / 2.0

    for y in range(box):
        for x in range(box):
            d = ((x - centre) ** 2 + (y - centre) ** 2) ** 0.5 / centre
            if d >= 1.0:
                continue
            pixels[x, y] = (255, 255, 255, int(255 * (1.0 - d) ** softness))

    return image.resize((size, size), Image.LANCZOS)


MOTES = {"mote-soft": (24, 1.6), "mote-fleck": (10, 0.7)}

IMPACT_SIZE, IMPACT_FRAMES = 64, 6
RING_SIZE, RING_FRAMES = 72, 6
STEAM_SIZE, STEAM_FRAMES = 72, 8


def main():
    os.makedirs(OUT, exist_ok=True)
    for name in SHAPES:
        path = os.path.join(OUT, name + ".png")
        render(name).save(path)
        print(f"  {path}  {SIZES[name][0]}x{SIZES[name][1]}")

    os.makedirs(AMBIENT_OUT, exist_ok=True)
    for name, (size, softness) in MOTES.items():
        path = os.path.join(AMBIENT_OUT, name + ".png")
        mote(size, softness).save(path)
        print(f"  {path}  {size}x{size}")

    os.makedirs(IMPACT_OUT, exist_ok=True)
    for i in range(IMPACT_FRAMES):
        path = os.path.join(IMPACT_OUT, f"impact-burst-{i + 1}.png")
        impact_frame(IMPACT_SIZE, i, IMPACT_FRAMES).save(path)
    print(f"  {IMPACT_OUT}/impact-burst-1..{IMPACT_FRAMES}.png  {IMPACT_SIZE}x{IMPACT_SIZE}")

    for i in range(RING_FRAMES):
        path = os.path.join(IMPACT_OUT, f"parry-ring-{i + 1}.png")
        ring_frame(RING_SIZE, i, RING_FRAMES).save(path)
    print(f"  {IMPACT_OUT}/parry-ring-1..{RING_FRAMES}.png  {RING_SIZE}x{RING_SIZE}")

    for i in range(STEAM_FRAMES):
        path = os.path.join(IMPACT_OUT, f"steam-puff-{i + 1}.png")
        steam_frame(STEAM_SIZE, i, STEAM_FRAMES).save(path)
    print(f"  {IMPACT_OUT}/steam-puff-1..{STEAM_FRAMES}.png  {STEAM_SIZE}x{STEAM_SIZE}")


if __name__ == "__main__":
    main()
