# Paprica — UI Art Direction & Generation Prompts

Everything needed to generate the game's interface art, then rebuild it as components in
Figma. No UI is assembled in code — these prompts produce the art, Figma produces the
components, Unity receives finished sprites.

## How to use this document

1. Generate **§2 Master style** first. Keep the best result. That image is now the style
   reference attached to every later generation.
2. Lock **§3 Palette** and the font as Figma styles before drawing anything.
3. Generate **§4 The battle screen** as one whole composition. This is the single most
   important image in the document: it decides proportion, density and where everything
   sits. Everything after it is a detail pass on a layout that already works.
4. Only then generate **§5 individual elements**, with both §2 and §4 attached as
   references. Generating elements before the whole composition is how a UI set ends up
   looking like eight different games stapled together.
5. **§6 full screens**, **§7 icons**, **§8 state sheets** as the demo needs them.

Every prompt below inherits §2. Append the negative prompt to all of them.

---

## 2. Master style prompt

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

**Negative prompt — append to every generation:**

> pixel art, 8-bit, retro, photorealistic, 3D render, glossy plastic, neon, cyberpunk, sci-fi
> HUD, heavy drop shadows, lens flare, gradient mesh, cluttered, busy, realistic metal,
> glass morphism, blurry, watermark, text artifacts, lorem ipsum, misspelled labels,
> inconsistent line weight, multiple art styles

---

## 3. Palette

Lock these before generating screens. Adjust once, then never per-screen.

| Role | Hex | Use |
|---|---|---|
| Outline | `#2B1E1A` | Every outline, uniform weight |
| Panel base | `#F2E3C8` | Parchment fill for menus and dialogue |
| Panel shade | `#DCC7A4` | The one shade pass inside panels |
| Surface dark | `#241C1A` | HUD backing, pause dim, letterboxing |
| Paprika red | `#D64541` | Hero colour, health fill, primary buttons |
| Paprika red dark | `#A32F2E` | Health chip trail, pressed button |
| Stem green | `#6FA85C` | Healing, safe rooms, positive confirmations |
| Brass gold | `#E8B04B` | Selection, currency, highlights, rare items |
| Danger | `#C0392B` | Boss bar, damage vignette, destructive confirmations |
| Steel | `#8C9BA5` | Enemy bars, locked or disabled states |

**Contrast rule.** Text sits on `#F2E3C8` or `#241C1A`, never directly on paprika red. Red
is for fills and accents, not backgrounds for copy.

**Meaning rule.** Red is the player. Crimson is the boss. Steel is a regular enemy. Green is
only ever healing or safety. Brass is only ever "you can interact with this" or "this is
selected". Never reuse a colour for a second meaning — the player learns these in the first
five minutes and should never have to relearn them.

---

## 4. The battle screen — one full composition

**This is the priority image.** Generate it as a single full 16:9 frame, not as parts.

### 4.1 Standard combat

> A complete in-game HUD for a hand-drawn stylized 2D metroidvania, shown as one full
> widescreen 16:9 game screen. Behind the interface, a warm dimly lit kitchen interior seen
> from the side as a platformer stage — tiled counters, a stove, hanging pans — heavily
> blurred and darkened so it never competes with the interface. The gameplay is there only
> to prove the UI reads over a real scene.
>
> **Top-left**, one compact status cluster and nothing else: a circular brass-framed
> portrait of a red bell pepper character with a determined expression, tilted a few
> degrees off vertical; immediately to its right a horizontal health bar built like a
> hand-drawn enamel tray — thick dark outline, cream inner track, paprika red fill with a
> darker red shadow along the bottom edge of the fill and a thin brass highlight along the
> top, divided by subtle notches so individual hit points read at a glance, currently about
> three quarters full with a slightly longer darker red trail behind the fill edge;
> directly beneath the bar a row of three small rounded-square ability slots with thick dark
> outlines — the first holds a dash-boot icon and is fully lit, the second is half covered
> by a dark radial sweep showing it recharging, the third is empty and greyed out to show it
> is not yet unlocked.
>
> **Top-right**, a single small resource counter: a hand-drawn chili pepper glyph with a
> number beside it. Nothing else in that corner.
>
> **Bottom-centre**, a wide thin boss health bar: the same hand-drawn enamel tray language
> but much wider and flatter than the player's, deep crimson fill over a dark charcoal
> track, a thin brass rule above it, a small hand-drawn flourish capping each end, and room
> for a short boss name in small caps centred directly beneath.
>
> **Mid-field**, floating just above a small enemy silhouette, a tiny undecorated capsule
> health bar roughly sixty pixels wide, steel grey track with a paprika red fill.
>
> The entire interface lives in the outer margins. The whole centre of the screen is empty
> — that is where the game is played. No frame around the screen edge, no minimap, no
> quest text, no clutter anywhere.
>
> Flat 2D, front facing, no perspective, thick uniform dark outlines, flat fills with a
> single soft shade pass, warm kitchen palette of paprika red, cream parchment, deep
> charcoal and brass gold.

### 4.2 Exploration state

Same prompt, with these changes:

> No boss bar at all. No enemy present and no floating enemy bar. Health bar full, no
> trailing chip. All three ability slots lit and ready. Bottom-right corner carries a small
> interaction prompt: a hand-drawn key cap glyph beside two words of label, sitting just
> above a glowing treasure chest in the scene.

### 4.3 Low health state

Same prompt as 4.1, with these changes:

> Health bar nearly empty — a short paprika red sliver remaining, the cream track exposed
> across most of its length, the darker red chip trail still catching up behind it. A soft
> dark crimson vignette creeps in from all four screen edges, strongest at the corners,
> leaving the centre clear. The portrait shows the pepper character hurt and gritting its
> teeth. Nothing else changes — no extra warning icons, no flashing text.

### 4.4 What to check in the result

Before approving the composition, hold it against these. Each one is a real failure mode:

- Cover the middle of the frame with your hand. The UI should still look balanced — it
  lives in the margins, and the margins have to work on their own.
- Shrink the image to phone size. The health bar and the boss bar must still read. If they
  do not, the proportions are wrong, not the detail level.
- Count the elements. More than six distinct things on screen in standard combat means it
  is too busy for a game where the player is reading enemy tells.
- Check the player's eye path: portrait → health → abilities should be one short sweep, not
  a scatter across the corner.

---

## 5. Battle screen elements

Generate these with §2 and §4 attached, as transparent-background assets for Figma.

### 5.1 Player status cluster

> The player status cluster from a hand-drawn stylized 2D game HUD, isolated on a
> transparent background: a circular brass-framed character portrait tilted a few degrees,
> a horizontal segmented health bar to its right built as a hand-drawn enamel tray with
> thick dark outline, cream track and paprika red fill, and a row of three rounded-square
> ability slots beneath the bar. One coherent group, drawn as it sits in the corner of the
> screen, nothing else around it. Flat 2D, thick uniform outlines.

### 5.2 Health bar — exploded parts

The bar cannot ship as one image. Generate it as separate pieces so code can drive the fill.

> Four separate pieces of one hand-drawn game health bar, laid out in a row on a transparent
> background, each drawn in isolation and all the same length:
> 1. the empty track — thick dark outline with a cream interior and subtle vertical notches,
>    nothing inside it;
> 2. the fill — a solid paprika red bar with a darker red shade along its bottom edge and a
>    thin brass highlight along its top, no outline of its own, exactly fitting the inside
>    of the track;
> 3. the chip trail — the same shape in dark desaturated red, flat, no highlight;
> 4. the frame overlay — the outline and the brass end caps only, with a fully transparent
>    interior.
> Flat 2D, thick uniform outlines, no perspective.

### 5.3 Ability slot

> A single ability slot from a hand-drawn stylized game HUD, on a transparent background: a
> rounded square with a thick dark outline and a cream interior with a soft inner shade so
> it reads as a shallow recess, a thin brass inner rule, and four small brass corner pins.
> Empty in the middle, ready to receive an icon. Drawn at large size so it stays crisp when
> reduced to sixty-four pixels. Flat 2D.

Also generate: the slot locked (steel grey, a small padlock overlay), and the radial
cooldown sweep as a separate solid dark wedge shape on transparent.

### 5.4 Boss bar

> A wide boss health bar for a 2D stylized game, isolated on a transparent background.
> Hand-drawn enamel tray shape, much wider and thinner than a player bar, with a thick dark
> outline, a deep crimson fill over a dark charcoal empty track, a thin brass rule running
> above it, and a decorative hand-drawn flourish capping each end — a small chili pepper
> motif worked into each cap. Space for a boss name in small caps centred directly beneath.
> Ominous but restrained, not noisy. Flat 2D.

Generate the same exploded parts as §5.2: track, fill, chip trail, frame overlay.

### 5.5 Enemy bar

> A very small floating enemy health bar for a 2D game, on a transparent background: a short
> rounded capsule with a thick dark outline, a steel grey empty track and a paprika red
> fill, no decoration whatsoever. Must read clearly at sixty pixels wide. Flat 2D.

### 5.6 Resource counter

> A small resource counter for a hand-drawn stylized 2D game, on a transparent background: a
> hand-drawn chili pepper glyph in paprika red with a thick dark outline, beside a small
> parchment plaque with a thick outline where a number will sit. Compact, reads instantly,
> no frame around the pair. Flat 2D.

### 5.7 Interaction prompt

> An interaction prompt for a 2D stylized game, on a transparent background: a small
> hand-drawn keyboard key cap with a thick dark outline, cream face and a soft shade along
> its lower edge so it reads as physically pressable, beside a short parchment label plaque.
> The whole thing is small and unobtrusive — it appears beside an object, not across the
> screen. Also generate a controller button variant of the same shape. Flat 2D.

### 5.8 Pickup toast

> A small pickup notification for a hand-drawn stylized 2D game, on a transparent
> background: a narrow horizontal parchment plaque with a thick dark outline and a slightly
> torn bottom edge, a square item icon slot framed in brass at its left end, and room for
> one short line of text to its right. Small and quiet — it slides in, says what was picked
> up, and leaves. Flat 2D.

### 5.9 Damage vignette

> A full-screen damage vignette overlay for a 2D game, on a transparent background: a soft
> dark crimson glow creeping in from all four edges of a 16:9 frame, strongest in the
> corners, fading completely to transparent well before the centre. Organic and slightly
> uneven at its inner edge, as if painted rather than computed. No shapes, no icons, no
> text, no cracks. Flat 2D.

### 5.10 Minimap frame

> A small corner minimap frame for a hand-drawn stylized 2D metroidvania, on a transparent
> background: a rounded rectangular window with a thick dark outline and brass corner pins,
> its interior fully transparent so the map renders through it. Roughly the size of four
> ability slots. A tiny hand-drawn compass rose tucked into one corner of the frame. Flat 2D.

### 5.11 Quest tracker

> A quest tracker for a hand-drawn stylized 2D game, on a transparent background: no panel
> at all, just a short stack of text rows with a small hand-drawn chili pepper bullet
> marking the active objective and small empty circle bullets for the rest, plus a thin
> hand-drawn rule above the group. Deliberately almost invisible — it sits on the gameplay
> without a background behind it. Flat 2D.

---

## 6. Full screens

### 6.1 Main menu

> Main menu screen for a hand-drawn stylized 2D metroidvania about a pepper escaping a
> kitchen. Vertical stack of four menu buttons centred slightly left of middle: rounded
> hand-drawn plaques that look like small wooden cutting boards with thick dark outlines,
> cream faces, and a brass pin in each corner. The selected button is larger, tilted a few
> degrees, filled paprika red with cream text, and carries a small hand-drawn chili pepper
> as a selection marker to its left. Unselected buttons are cream with dark text. The game
> logo sits above, hand-lettered, warm and chunky. Generous empty space on the right of the
> frame reserved for character art. Warm kitchen background heavily blurred and darkened so
> it never competes with the buttons. Flat 2D, no perspective, cosy and confident.

### 6.2 Pause

> Pause overlay for a 2D stylized game. The gameplay behind is darkened with a warm charcoal
> wash, not a neutral grey. A single centred parchment panel with a thick dark hand-drawn
> outline and softly irregular rounded corners, like a sheet of baking paper pinned at the
> top with two brass tacks. Inside, a short vertical list of options and a hand-drawn
> divider rule. The panel occupies roughly a third of the screen height. Nothing in the
> corners, nothing decorative beyond the tacks. Flat 2D, calm, uncluttered.

### 6.3 Dialogue

> Dialogue box for a 2D stylized game, bottom-anchored, spanning most of the screen width
> but only a fifth of its height. Parchment panel with thick dark hand-drawn outline and a
> slightly uneven bottom edge like torn paper. On the left, a square character portrait
> frame with a brass border, tilted a couple of degrees. To the right of it, room for two to
> three lines of body text and, above them, a small name plate — a tiny paprika red banner
> with cream text. A small hand-drawn chili pepper glyph in the bottom-right corner marks
> "continue". Flat 2D, transparent background, no drop shadow under the text.

### 6.4 Inventory

> Inventory screen for a hand-drawn stylized 2D metroidvania. A grid of six by four item
> slots: rounded squares with thick dark outlines and cream faces, each with a subtle inner
> shade so it reads as a recess. The selected slot is outlined in brass and scaled slightly
> up. Equipped items carry a small brass corner notch. To the right of the grid, a vertical
> detail panel on parchment showing a large item illustration area, a name area, and a short
> description area separated by hand-drawn rules. The whole thing sits on a darkened warm
> background. Clean, airy, nothing ornate — the decoration is in the line quality, not in
> added filigree. Flat 2D, no perspective.

### 6.5 Map

> Map screen for a 2D metroidvania, drawn as a hand-sketched kitchen floor plan on
> parchment. Rooms are simple rounded rectangles with thick dark outlines connected by short
> corridor lines. Visited rooms are filled cream, unvisited but adjacent rooms are shown as
> dashed outlines only, and the room the player is in is filled paprika red with a small
> marker. A handful of tiny hand-drawn icons mark save points, locked doors, and items. No
> grid lines, no coordinates, no minimap chrome. It should read like something sketched by
> hand on the back of a recipe card. Flat 2D, top-down schematic, warm and legible.

### 6.6 Settings

> Settings screen for a stylized 2D game on a parchment panel with a thick hand-drawn
> outline. A left column of category tabs shaped like small paper tabs, the active one
> pushed out and filled paprika red. On the right, a vertical list of rows, each with a
> label on the left and a control on the right: hand-drawn horizontal sliders with a round
> brass knob, and toggle switches shaped like small wooden pegs that slide in a track.
> Generous row spacing. Nothing technical-looking, no boxes within boxes. Flat 2D, calm.

### 6.7 Loading

> Loading screen for a 2D stylized game about a pepper escaping a kitchen. Mostly empty warm
> charcoal field. Bottom-right, a small hand-drawn pepper character mid-run. Bottom-left, a
> short thin progress bar with a thick dark outline and paprika red fill. Centre-bottom,
> room for a single line of hint text in cream. Nothing else — the screen is deliberately
> almost empty. Flat 2D.

### 6.8 Death

> Death screen for a hand-drawn stylized 2D game. The final frame of gameplay behind,
> desaturated almost to grey and darkened heavily. Centred, a short hand-lettered line of
> text and, beneath it, two small cutting-board buttons. Above the text, a single small
> hand-drawn motif: a wilted chili pepper. No blood, no skull, no harsh red flash — the tone
> is rueful rather than grim. Enormous empty space around everything. Flat 2D.

### 6.9 Save point

> A save point interaction overlay for a hand-drawn stylized 2D game, on a transparent
> background: a small parchment plaque with a thick dark outline, a lit hand-drawn candle
> motif at its left, and room for one short line of text. Warm brass glow radiating gently
> behind the plaque. Reassuring and quiet. Flat 2D.

---

## 7. Icon set

> A set of game UI icons in a hand-drawn stylized 2D style, each on a transparent
> background, uniform thick dark outlines, flat fills with one soft shade, consistent
> optical weight, all readable at 48 by 48 pixels. Warm kitchen palette of paprika red,
> cream, brass gold and stem green. Icons: heart, chili pepper, wooden spoon weapon, pinch
> of salt, cooking pot, key, treasure chest, map scroll, speech bubble, gear, save point
> candle, locked padlock, dash boots, shield lid, parry spark, healing herb, boss skull-lid.
> Simple silhouettes, no inner detail finer than the outline weight, each icon filling the
> same optical area so none looks larger than the others. Flat 2D, no perspective, no
> shadows.

---

## 8. State and variant sheets

Generate one sheet per interactive element, all states side by side in a single image. This
is what guarantees the states match each other — generating them separately never does.

> One hand-drawn stylized game UI button shown four times in a row on a transparent
> background, identical in size and shape, differing only in treatment:
> 1. **normal** — cream face, dark outline, dark text area;
> 2. **selected** — paprika red face, cream text area, scaled very slightly larger, a small
>    chili pepper marker to its left;
> 3. **pressed** — the same as selected but visibly pushed down and darker, with the top
>    highlight removed;
> 4. **disabled** — steel grey face at reduced opacity, outline softened, no marker.
> Flat 2D, thick uniform outlines, no blur on any state.

Repeat the same sheet structure for: ability slot (ready / recharging / locked), toggle
(on / off / disabled), slider (empty / half / full), inventory slot (empty / filled /
selected / equipped), tab (active / inactive).

---

## 9. Figma and Unity requirements

What has to be true regardless of what the generator produces.

**Canvas and scale.** Design at 1920×1080, the reference resolution for Unity's Canvas
Scaler. Set the scaler to *Scale With Screen Size*, reference 1920×1080, match 0.5, so the
layout survives both 16:9 and wider.

**Safe margins.** Keep every HUD element at least 40px from the screen edge at reference
resolution. Anything closer gets clipped on a TV or a phone with rounded corners.

**Minimum sizes.** Body text no smaller than 20px at reference resolution, labels no smaller
than 24px, buttons at least 48px tall. Icons must still read at 48×48 — generate larger,
then check downscaled before approving.

**Nine-slice everything that stretches.** Panels, bars, buttons and dialogue boxes must be
authored so corners stay fixed while middles stretch. Keep corner decoration inside the
corner region and never let it cross a slice line. Export those with a flat, untextured
middle. Anything that cannot nine-slice — a flourish, a pinned tack — ships as a separate
overlay sprite.

**Bars are separate pieces.** Every health or progress bar exports as a separate empty
track, fill, chip trail and frame overlay, exactly as §5.2 describes. Never export a bar at
a fixed percentage as one image — code drives the fill, and a baked-in percentage makes
that impossible.

**States.** Every interactive element needs normal, selected/hovered, pressed and disabled.
Build them as Figma variants on one component so a change to the base propagates. Disabled
is the steel colour at reduced opacity, never a blur.

**Layout, not pixels.** Build with Auto Layout so spacing is a token, not a hand-placed
offset. Spacing scale: 4, 8, 16, 24, 40. Corner radius scale: 8, 16, 24. Outline weight: one
value everywhere, 4px at reference resolution.

**Colour as styles.** The §3 palette goes into Figma as colour styles before any screen is
built. Nothing uses a raw hex.

**Export.** PNG at 1× and 2×, transparent background, trimmed to content with 4px padding so
the outline is never clipped. Sprites go to `Assets/Art/UI/` grouped by screen. Set them to
Sprite (2D and UI), filter mode Bilinear, compression None while the demo is in development
— the kit is small and the artefacts are not worth the memory saving.

**Font.** One display face for the logo and headings, one clean face for body copy. Check
the body face has full Latin Extended and Cyrillic coverage — the team writes in Polish and
Ukrainian, so **ą ć ę ł ń ó ś ź ż** and **і ї є ґ** all have to render. Verify this before
committing to a font, not after the first build. A font that fails here will force a
redesign of every screen that uses it.

---

## 10. Order of work

1. §2 master style, generated and locked.
2. §3 palette and the font, as Figma styles.
3. §4.1 the battle screen as one composition — the layout decision.
4. §5.1–5.5 the HUD pieces, exploded for export. This is the only UI the demo strictly
   needs, and it proves the style over live gameplay.
5. §6.3 dialogue and §6.2 pause — the next two the demo touches.
6. §6.1 main menu.
7. §5.6–5.11, §6.4–6.9, §7 icons, §8 state sheets — once the demo plays.

The demo deadline is 7 January. Steps 1–5 have to exist for it. Everything after is polish
if there is time.

---

## 11. Handover to Unity

The UI hierarchy is built by hand in the Unity editor from these sprites — it is never
generated in code. Two runtime components exist to drive it, and both take their references
in the Inspector:

- `Assets/Scripts/UI/HealthBarUI.cs` — give it a `HealthComponent`, the fill `Image`, and
  optionally the chip-trail `Image`. It serves the player bar, the boss bar and the floating
  enemy bar; only the references differ. The fill images must be set to *Filled / Horizontal
  / Origin Left*.
- `Assets/Scripts/UI/AbilityCooldownUI.cs` — give it the `PepperController`, the radial
  overlay `Image` and the icon `Image`. The overlay must be *Filled / Radial 360 / Origin
  Top*.

Nothing else about the interface lives in code.
