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

## What is already wired

All three effects are attached in the prefabs. Nothing is instantiated at runtime -
each one is a child object, and the script either plays it or switches it on.

| Effect | Prefab used | Lives under | Driven by |
|---|---|---|---|
| Attack hit | `CFXR Hit A (Red)` | `Player/AttackPoint/HitFx` | `PepperCombat.hitEffect` |
| Parry | `CFXR Impact Glowing HDR (Blue)` | `Player/ParryFx` | `PepperParry.parryEffect` |
| Enemy death, sprite | SunnyLand burst frames | `<Enemy>/DeathFx` | `EnemyDeath.deathFx` |
| Enemy death, particles | `CFXR2 Blood Shape Splash` | `<Enemy>/DeathParticles` | `EnemyDeath.deathParticles` |

Three settings had to be overridden on every nested instance, and they are the reason a
copied-in Cartoon FX prefab does not just work:

- **`clearBehavior`** was `Destroy`. The effect deletes its own GameObject once it finishes,
  so the second hit would find nothing there. Set to `None`.
- **Sorting order** was 0 on every renderer, so the burst drew behind the sprites. Set to 10.
- **Scale** was built for metre-sized characters. The Pepper is 0.33 units tall, so an
  unscaled burst covered most of the screen. Hit and parry run at 0.15, the enemy splash
  at 0.12.

The 3D `Point Light` child is switched off on each: it does nothing under the URP 2D
renderer and still costs. `Play On Awake` is left ticked on the packs' many sub-emitters,
and the scripts clear it with a single `Stop()` in `Awake` instead.

To swap an effect, drop a different Cartoon FX prefab in as a child, apply those three
overrides, and drag its root `ParticleSystem` (or, for the enemy death, its GameObject)
into the matching field.

## Still to do

**Slash Effects FREE needs its materials upgraded** before anything from it can be used.
Its materials reference legacy Built-in render pipeline shaders, which URP renders as
magenta. Run *Window > Rendering > Render Pipeline Converter > Built-in to URP > Material
Upgrade*. This is a menu action in the editor rather than a file change, which is why it
is not already done. Cartoon FX needs none of this - it ships `.cfxrshader` files that a
scripted importer compiles for whichever pipeline is active.

The board asks for a blood effect made of vegetable pieces rather than blood. `CFXR2 Blood
Shape Splash` is the right shape to start from - retint it toward paprika red and stem
green and it reads as pepper rather than gore, which keeps the tone where the mood board
puts it.

## Not needed

`Assets/Customizable Skybox` is a 3D skybox with a low-poly island model. Nothing in a 2D
side-scroller can use it. It is only 772 KB, so it is not urgent, but it can be deleted.
