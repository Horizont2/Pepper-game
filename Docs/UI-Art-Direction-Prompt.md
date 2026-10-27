# Paprica — UI Art Direction & Generation Prompts

Working document for the UI pass. Section 1 is the master prompt that establishes the
look. Sections 3+ are per-screen prompts that inherit it. Section 6 covers what has to
be true for the result to survive the trip into Figma and then into Unity.

Generate the master prompt first, lock the palette and frame language from whatever comes
back, then run the per-screen prompts with that result attached as a style reference.
Running the screens without a locked reference is how a UI set ends up looking like eight
different games.

---

## 1. Master style prompt

> A cohesive 2D game UI kit for a hand-drawn stylized metroidvania called *Paprica*, about
> a red bell pepper escaping from a kitchen. Hand-painted vector look with confident thick
> dark outlines of uniform weight, flat colour fills with a single soft shade pass, and no
> gradients beyond a gentle top-to-bottom darkening inside panels. Rounded, slightly
> irregular organic shapes — the frames look drawn by hand rather than constructed from
> perfect rectangles, with subtle wobble in the outline. Warm, appetising, kitchen-inspired
> palette: paprika red, deep charcoal, cream parchment, brass gold, pepper-stem green.
> Panels read as enamel cookware and worn wooden cutting boards rather than sci-fi glass or
> metal. Clean, generous negative space, very few elements on screen, nothing busy. Friendly
> but with an edge — the game is cosy on the surface and hostile underneath. Flat 2D, front
> facing, no perspective, no drop shadows on text, no photorealism, no 3D bevels, no neon
> glow, no pixel art. Transparent background. Art direction in the spirit of Hollow Knight's
> restraint, Dead Cells' punch, and Papa Louie's food warmth.

**Negative prompt (append to every generation):**

> pixel art, 8-bit, retro, photorealistic, 3D render, glossy plastic, neon, cyberpunk, sci-fi
> HUD, heavy drop shadows, lens flare, gradient mesh, cluttered, busy, realistic metal,
> glass morphism, blurry, watermark, text artifacts, lorem ipsum, misspelled labels

---

## 2. Palette

Lock these before generating screens. Hex values are the starting point; adjust once, then
never per-screen.

| Role | Hex | Use |
|---|---|---|
| Outline | `#2B1E1A` | Every outline, uniform weight |
| Panel base | `#F2E3C8` | Parchment fill for menus and dialogue |
| Panel shade | `#DCC7A4` | The one shade pass inside panels |
| Surface dark | `#241C1A` | HUD backing, pause dim, letterboxing |
| Paprika red | `#D64541` | Hero colour, health fill, primary buttons |
| Paprika red dark | `#A32F2E` | Health fill shadow, pressed button |
| Stem green | `#6FA85C` | Healing, safe rooms, positive confirmations |
| Brass gold | `#E8B04B` | Selection, currency, highlights, rare items |
| Danger | `#C0392B` | Boss bar, damage, destructive confirmations |
| Steel | `#8C9BA5` | Enemy bars, locked or disabled states |

Contrast rule: every piece of text sits on `#F2E3C8` or `#241C1A`, never directly on
paprika red. Red is for fills and accents, not backgrounds for copy.

---

## 3. HUD (in-game, always visible)

> Minimal in-game HUD overlay for a 2D stylized metroidvania, top-left anchored cluster.
> A horizontal health bar shaped like a hand-drawn enamel tray: thick dark outline, cream
> inner track, paprika red fill with a soft darker red along the bottom edge of the fill,
> and a thin brass highlight along the top. Segmented by subtle notches so individual hit
> points read at a glance. To its left, a small circular portrait frame holding the pepper
> hero's face, framed in brass, slightly tilted for character. Below the health bar, a row
> of three small rounded-square ability slots with thick outlines, each holding room for an
> icon, one showing a cooldown sweep as a darkened wedge. Nothing else on screen. Flat 2D,
> transparent background, no perspective. Very restrained — this covers less than a sixth of
> the screen width.

**Variants to request in the same batch:** full health, one third health (fill shortened,
empty track visible), ability slot on cooldown, ability slot empty/locked.

**Boss bar** (separate generation, anchored bottom-centre):

> A wide boss health bar for a 2D stylized game, bottom-centre anchored. Hand-drawn enamel
> tray shape with thick dark outline, much wider and thinner than the player bar, deep
> crimson fill over a dark charcoal empty track, with a thin brass rule above it and a
> decorative hand-drawn flourish capping each end. Space for a boss name in small caps
> centred directly beneath. Ominous but not noisy. Flat 2D, transparent background.

**Enemy bar** (tiny, floats above enemies):

> A very small floating enemy health bar for a 2D game: a short rounded capsule with thick
> dark outline, steel grey empty track, paprika red fill, no decoration at all. Reads clearly
> at 60 pixels wide. Flat 2D, transparent background.

---

## 4. Screens

### 4.1 Main menu

> Main menu screen for a hand-drawn stylized 2D metroidvania about a pepper escaping a
> kitchen. Vertical stack of four menu buttons centred slightly left of middle: rounded
> hand-drawn plaques that look like small wooden cutting boards with thick dark outlines,
> cream faces, and a brass pin in each corner. The selected button is larger, tilted a few
> degrees, filled paprika red with cream text, and carries a small hand-drawn chili pepper
> as a selection marker to its left. Unselected buttons are cream with dark text. The game
> logo sits above, hand-lettered, warm and chunky. Generous empty space on the right of the
> frame reserved for character art. Warm kitchen background heavily blurred and darkened so
> it never competes with the buttons. Flat 2D, no perspective, cosy and confident.

### 4.2 Pause

> Pause overlay for a 2D stylized game. The gameplay behind is darkened with a warm charcoal
> wash, not a neutral grey. A single centred parchment panel with a thick dark hand-drawn
> outline and softly irregular rounded corners, like a sheet of baking paper pinned at the
> top with two brass tacks. Inside, a short vertical list of options and a hand-drawn
> divider rule. The panel occupies roughly a third of the screen height. Nothing in the
> corners, nothing decorative beyond the tacks. Flat 2D, calm, uncluttered.

### 4.3 Dialogue / interaction box

> Dialogue box for a 2D stylized game, bottom-anchored, spanning most of the screen width
> but only a fifth of its height. Parchment panel with thick dark hand-drawn outline and a
> slightly uneven bottom edge like torn paper. On the left, a square character portrait
> frame with a brass border, tilted a couple of degrees. To the right of it, room for two to
> three lines of body text and, above them, a small name plate — a tiny paprika red banner
> with cream text. A small hand-drawn chili pepper glyph in the bottom-right corner marks
> "continue". Flat 2D, transparent background, no drop shadow under the text.

### 4.4 Inventory / chest

> Inventory screen for a hand-drawn stylized 2D metroidvania. A grid of six by four item
> slots: rounded squares with thick dark outlines and cream faces, each with a subtle inner
> shade so it reads as a recess. The selected slot is outlined in brass and scaled slightly
> up. Equipped items carry a small brass corner notch. To the right of the grid, a vertical
> detail panel on parchment showing a large item illustration area, a name area, and a short
> description area separated by hand-drawn rules. The whole thing sits on a darkened warm
> background. Clean, airy, nothing ornate — the decoration is in the line quality, not in
> added filigree. Flat 2D, no perspective.

### 4.5 Map

> Map screen for a 2D metroidvania, drawn as a hand-sketched kitchen floor plan on
> parchment. Rooms are simple rounded rectangles with thick dark outlines connected by short
> corridor lines. Visited rooms are filled cream, unvisited but adjacent rooms are shown as
> dashed outlines only, and the room the player is in is filled paprika red with a small
> pulsing marker. A handful of tiny hand-drawn icons mark save points, locked doors, and
> items. No grid lines, no coordinates, no minimap chrome. It should read like something
> sketched by hand on the back of a recipe card. Flat 2D, top-down schematic, warm and
> legible.

### 4.6 Settings

> Settings screen for a stylized 2D game on a parchment panel with a thick hand-drawn
> outline. A left column of category tabs shaped like small paper tabs, the active one
> pushed out and filled paprika red. On the right, a vertical list of rows, each with a
> label on the left and a control on the right: hand-drawn horizontal sliders with a round
> brass knob, and toggle switches shaped like small wooden pegs that slide in a track.
> Generous row spacing. Nothing technical-looking, no boxes within boxes. Flat 2D, calm.

### 4.7 Loading screen

> Loading screen for a 2D stylized game about a pepper escaping a kitchen. Mostly empty warm
> charcoal field. Bottom-right, a small hand-drawn animation-ready pepper character mid-run.
> Bottom-left, a short thin progress bar with a thick dark outline and paprika red fill.
> Centre-bottom, room for a single line of hint text in cream. Nothing else — the screen is
> deliberately almost empty. Flat 2D.

---

## 5. Icon set

> A set of game UI icons in a hand-drawn stylized 2D style, each on a transparent
> background, uniform thick dark outlines, flat fills with one soft shade, consistent
> optical weight, all readable at 48 by 48 pixels. Warm kitchen palette of paprika red,
> cream, brass gold and stem green. Icons: heart, chili pepper, wooden spoon weapon, pinch of
> salt, cooking pot, key, treasure chest, map scroll, speech bubble, gear, save point candle,
> locked padlock, dash boots, shield lid. Simple silhouettes, no inner detail smaller than
> the outline weight. Flat 2D, no perspective, no shadows.

---

## 6. Figma and Unity requirements

What has to be true, regardless of what the generator produces.

**Canvas and scale.** Design at 1920×1080, which is the reference resolution the Unity
Canvas Scaler will be set to. Set the Canvas Scaler to *Scale With Screen Size*, reference
1920×1080, match 0.5 so the layout survives both 16:9 and wider.

**Minimum touch and read sizes.** Body text no smaller than 20px at the reference
resolution, labels no smaller than 24px, buttons at least 48px tall. Icons must still read
at 48×48 — generate them larger, then check them downscaled before approving.

**Nine-slice everything that stretches.** Panels, bars, buttons and dialogue boxes have to be
authored so their corners stay fixed while their middles stretch. Keep corner decoration
inside the corner region and never let it cross the slice line. Export those with a flat,
untextured middle section. Anything that cannot nine-slice — a flourish, a pinned tack —
ships as a separate overlay sprite.

**States.** Every interactive element needs four: normal, hovered/selected, pressed, and
disabled. Build them as Figma variants on one component so a change to the base propagates.
Disabled is the steel colour at reduced opacity, never a blur.

**Bars are two pieces.** Every health or progress bar exports as a separate empty track and
a separate fill, so the fill can be driven by a value in code. Never export a bar at a fixed
percentage as one image.

**Layout, not pixels.** Build with Auto Layout in Figma so spacing is a token and not a
hand-placed offset. Spacing scale: 4, 8, 16, 24, 40. Corner radius scale: 8, 16, 24. Outline
weight: one value everywhere, 4px at reference resolution.

**Colour as styles.** The palette in section 2 goes into Figma as colour styles before any
screen is built. Nothing uses a raw hex.

**Export.** PNG at 1× and 2×, transparent background, trimmed to content with a 4px padding
so the outline is never clipped. Sprites go to `Assets/Art/UI/` grouped by screen. Set them
to Sprite (2D and UI), filter mode Bilinear, compression None while the demo is in
development — the kit is small and the artefacts are not worth the memory saving.

**Font.** Pick one display face for the logo and headings and one clean face for body copy,
and check the body face has full Latin Extended coverage — the team writes in Polish and
Ukrainian, so ą ć ę ł ń ó ś ź ż and і ї є ґ all have to render. Verify before committing to a
font, not after the first build.

---

## 7. Order of work

1. Generate and lock the master style (section 1).
2. Lock the palette and font in Figma as styles.
3. HUD first — it is the only UI the demo strictly needs, and it proves the style in motion.
4. Dialogue box and pause, which are the next two the demo will touch.
5. Main menu.
6. Inventory, map, settings, loading — after the demo plays.

The demo deadline is 7 January. Items 1 through 4 are the ones that have to exist for it;
5 through 6 are polish if there is time.
