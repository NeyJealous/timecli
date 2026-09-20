# Hero combat presentation recovery

Status: **canonical 1.4.5 behavior recovered and implemented**.

## Key correction

Time Clickers 1.4.5 does **not** instantiate travelling projectile prefabs for
the five automatic Heroes.

`Heroes.Update` calls `Hero.ApplyDamage`. Damage is applied immediately to
the selected `BoxEnemy` blocks, and presentation is then driven by
`BoxEnemy.Outline` / `BoxEnemy.FlashOutline`.

Therefore the modern port deliberately does not create fake Hero bullets.

## Exact weapon colors

Recovered from the canonical serialized `HeroWeapons` component:

| Weapon | RGBA |
|---|---|
| Pulse Pistol | `(0, 0.4066853523, 1, 1)` |
| Flak Cannon | `(1, 0.7019498944, 0, 1)` |
| Spread Rifle | `(1, 0, 0, 1)` |
| Rocket Launcher | `(0, 1, 0.1559889317, 1)` |
| Particle Ball | `(0.5626740456, 0, 1, 1)` |

## Per-weapon presentation

Recovered from `Hero.ApplyDamage` IL:

- Pulse Pistol: primary target gets persistent `Outline`.
- Flak Cannon: each projectile target gets `FlashOutline`.
- Spread Rifle: every retained target gets persistent `Outline`.
- Rocket Launcher: primary target gets `Outline`; splash targets get
  `FlashOutline`.
- Particle Ball: target gets persistent `Outline`; every fifth-shot critical
  burst changes damage only and does not switch presentation mode.

## BoxEnemy outline tween

The canonical `BoxEnemy` prefab contains a `TweenColor` component on the
same renderer as the `OutlineAndBody` material.

Serialized baseline:

- material color: black `(0, 0, 0, 1)`;
- TweenColor end color: black `(0, 0, 0, 1)`;
- `useSharedMaterial = false`.

`Outline(color)`:

1. sets `startColor = color`;
2. sets `endColor = color`;
3. sets duration to zero;
4. plays the tween, making the outline persistent.

`FlashOutline(color)`:

1. ignores a new flash while `nextAllowedFlashTime > Time.time`;
2. advances that gate by exactly **0.2 seconds**;
3. changes only `startColor`;
4. sets duration to exactly **0.2 seconds**;
5. linearly fades to the existing `endColor`.

This detail matters when multiple Heroes target the same block: a flash can
fade back to a persistent outline established by another Hero instead of always
fading to black.

## Modern reconstruction

`OriginalHeroPresentation.cs` owns the promoted constants/rules.

`OriginalBlockGeometry` reconstructs the per-block TweenColor state and uses a
separate `_OutlineColor` material property. The normal, Rainbow and TimeCube /
WeaponCube replacement shaders preserve body/health colors while applying the
Hero color only to the authored outer outline shell.

Authoritative damage remains in PortCore.
