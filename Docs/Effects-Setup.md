# Effects setup

The two installed packs are 3D particle packs. They work fine in a 2D game, but four
things bite every time and none of them produce an error message — the effect simply does
not appear, or appears magenta. Work through these before concluding a prefab is broken.

## The four gotchas

**1. Sorting order — the effect plays behind the sprites.**
A `ParticleSystemRenderer` defaults to sorting order 0, the same as every sprite in the
scene. Set *Renderer → Sorting Layer* and *Order in Layer* on every effect to something
above gameplay — order **10** works with the current scene, where the background is at
-100 and gameplay sits at 0. This is by far the most common reason an effect "does not
work".

**2. Magenta materials — Slash Effects FREE only.**
Its materials reference legacy Built-in render pipeline shaders, which URP cannot render.
Fix with *Window → Rendering → Render Pipeline Converter → Built-in to URP → Material
Upgrade*, or set each material's shader by hand to `Universal Render Pipeline/Particles/
Unlit`. Cartoon FX Remaster does **not** need this — it ships `.cfxrshader` files that a
scripted importer compiles for whichever pipeline is active, so it adapts to URP on import.

**3. Point Light children — Cartoon FX only.**
Several CFXR prefabs carry a `Point Light` child. That is a 3D light and does nothing under
the URP 2D renderer. Delete it from any copy used here; it costs performance and renders
nothing.

**4. Particles drift out of the camera plane.**
These systems emit in 3D, so particles move along Z and vanish or get clipped. In *Shape*,
pick a 2D shape (Circle, or Box with Z scale 0), and clear any Z component in *Velocity
over Lifetime*. Keeping the effect's local Z at 0 is not enough on its own.

## Wiring

Nothing is instantiated at runtime. Each effect is a child object sitting on the prefab,
and the script calls `Play()` on it.

| Effect | Parent it under | Assign to |
|---|---|---|
| Attack hit | `Player/AttackPoint` | `PepperCombat → hitEffect` |
| Parry | `Player` (root) | `PepperParry → parryEffect` |
| Enemy death | enemy prefab root | already wired as the `DeathFx` child |

Steps for each: drag the pack prefab in as a child, uncheck **Play On Awake** (the script
drives it), set the sorting order, then drag the child's `ParticleSystem` into the matching
field on the script.

For enemy death, the generated prefabs already carry a `DeathFx` child built from the
SunnyLand burst frames, shipped inactive and switched on by `EnemyDeath`. To use a particle
effect instead, replace that child's contents and leave **Play On Awake** *on* — it is
activated rather than played, so it needs to start by itself.

## Suggested prefabs

Starting points from what is installed, not final choices:

| Where | Prefab |
|---|---|
| Attack hit | `CFXR Prefabs/Impacts/CFXR Hit A (Red)` |
| Attack hit, alt | `CFXR Prefabs/Impacts/CFXR2 Ground Hit` |
| Parry | `CFXR Prefabs/Impacts/CFXR Impact Glowing HDR (Blue)` |
| Parry, alt | `CFXR Prefabs/Sword Trails/Plain/CFXR4 Sword Hit PLAIN (Cross)` |
| Enemy death | `CFXR Prefabs/Liquids/CFXR2 Blood Shape Splash` |
| Dash trail | already a TrailRenderer on the player — no pack needed |
| Slash arc | `Matthew Guz/Slash Effects FREE/Prefab/Basic Slash Blue` |

The board calls for a blood effect made of vegetable pieces rather than blood. `CFXR2 Blood
Shape Splash` is the right shape to start from — retint it toward paprika red and stem
green and it reads as pepper rather than gore, which also keeps the game's tone where the
mood board puts it.

`Matthew Guz` slashes are mesh-based and face a fixed direction, so a copy has to be
rotated per facing. Worth it for the attack arc, not worth it for small impacts.

## Not needed

`Assets/Customizable Skybox` is a 3D skybox with a low-poly island model. Nothing in a 2D
side-scroller can use it. It is only 772 KB, so it is not urgent, but it can be deleted.
