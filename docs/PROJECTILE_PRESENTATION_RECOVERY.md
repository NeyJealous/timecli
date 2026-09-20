# Click weapon / projectile recovery — Time Clickers 1.4.5

This document records the reconstructed ClickerPistol, ClickCannon,
ClickLauncher and shared Projectile behavior used by the modern Unity port.

## PortCore API

The auxiliary click-weapon state is part of PortCore **1.2.0**.

PortCore owns:

- Cannon charge;
- Launcher click progress;
- Cannon/Launcher damage values;
- Spread Shots changes to Cannon max projectiles and Launcher rocket count;
- application of precomputed projectile click damage to BoxEnemy state;
- resulting rewards and Arena progression.

Unity owns the same role it had in the original for projectile world-space
trajectory and collision contact. A collision is reported back to PortCore;
Unity does not calculate gameplay damage.

## ClickerPistol

The original Pistol is always unlocked.

On Shoot:

1. roll critical with
   `UnityEngine.Random.value < Skills.GetCriticalChance()`;
2. calculate `Skills.GetClickDamage(isCritical)`;
3. raycast from the current crosshair;
4. apply click damage to the first BoxEnemy hit;
5. create a Tracer;
6. when Spread Shots is active, emit the additional rotated pistol rays;
7. update click/critical statistics.

### Tracer

Recovered `Tracer.Shoot` geometry:

```text
position[0] = start
position[1] = Lerp(start, end, 0.1)
position[2] = Lerp(start, end, 0.9)
position[3] = end
```

Normal:

```text
lifetime = 0.1 s
width    = 0.15
start    = (0, 0.4066853523, 1)
end      = (1, 0.9333333969, 0.4980392456)
```

Critical:

```text
lifetime = 0.2 s
width    = 0.5
start    = (1, 0, 0)
end      = (1, 0.3931033909, 0)
```

Fade:

```text
endAlpha   = Lerp(0, 1, remaining / lifetime)
startAlpha = endAlpha * endAlpha
```

The clean reconstruction is `ClickTracerView.cs`.

## ClickCannon

Unlock augment: `ClickCannonUnlock`.

Every Shoot while unlocked:

```text
chargeProgress += 1
chargeProgress = min(chargeProgress, maximumCharge)
startDechargingTime = Time.time + 1
SpawnFlak()
```

After the one-second grace period:

```text
chargeProgress -= deltaTime * 10
chargeProgress = max(chargeProgress, 0)
```

Normal maximum charge is **5**.

While Spread Shots is active:

```text
maximumCharge = ClickCannonMaxProjectiles.GetModValue()
```

Each shot fires:

```text
projectileCount = floor(chargeProgress)
damagePerProjectile =
    Skills.GetClickDamage(false)
    * ClickCannonDamagePerShot.GetModValue()
    * 0.01

fireCone =
    ClickCannonFireCone.GetModValue()
    / 360
```

For every FlakBullet:

1. instantiate at Cannon Firespot;
2. LookAt raycast target;
3. rotation =
   `Quaternion.Slerp(lookRotation, Random.rotation, fireCone)`;
4. assign precomputed damage.

## ClickLauncher

Unlock augment: `ClickLauncherUnlock`.

Each Shoot increments `clicksProgress`.

```text
clicksRequired =
    ClickLauncherClicks.GetModValue()
```

When the threshold is reached:

```text
SpawnRocket()
clicksProgress = 0
```

Normal rocket count is **1**.

With Spread Shots active:

```text
rocketCount =
    ClickLauncherRockets.GetModValue()
```

Each rocket receives:

```text
damage = Skills.GetClickDamage(false) * 10
```

Rocket speed is modified in `Rocket.SetRocketIndex`:

```text
Projectile.velocity *=
    ClickLauncherRocketSpeed.GetModValue() * 0.01
```

## Shared Projectile

Recovered `Projectile`:

```text
Start:
    moveDir = transform.forward

Update:
    position += moveDir * deltaTime * velocity

TimedDespawn:
    wait maxLifetime
    Destroy(gameObject)
```

The captured `moveDir` is important: later Rocket child/pivot rotation does
not curve the root trajectory.

Serialized values:

| Projectile | Velocity | Max lifetime |
|---|---:|---:|
| FlakBullet | 40 | 1.5 s |
| RocketPrefab | 40 | 3.0 s |

## ProjectileDamager

Recovered collision loop:

```text
every 0.0333333313 s:
    movement = currentPosition - previousPosition
    magnitude = movement.magnitude

    if Physics.Raycast(
        previousPosition,
        movement,
        out hit,
        magnitude,
        hitboxMask):

        ProcessHit(hit)

    previousPosition = currentPosition
```

Default `hitboxMask = 2560`.

ProcessHit:

```text
colliders = Physics.OverlapSphere(
    hit.point,
    1.0,
    hitboxMask)

for each collider:
    apply precomputed click damage

if weaponType == RocketLauncher:
    SmallExplosion(hit.point)

destroy projectile root
```

The modern adapter mirrors that division:

`ClickWeaponProjectileView` performs trajectory/contact detection and calls
`HeadlessArenaEngine.ClickWeaponDamageBlock`, which sends the damage through
the same PortCore BoxEnemy click-damage/reward path.

## Rocket impact SmallExplosion

The active 1.4.5 Rocket impact path is `VirtualPS.SmallExplosion(hit.point)`.
It does **not** instantiate the serialized `RocketExplosion` prefab.

The shared scene object is:

```text
SmallExplosion
  -> Fire
```

`Fire` is a legacy `EllipsoidParticleEmitter + ParticleAnimator +
ParticleRenderer`. `VirtualPS.SmallExplosion` moves the shared transform to
the impact point, plays its AudioSource clip through the Explosions one-shot
queue at volume **0.5**, and calls:

```text
particleEmitter.Emit((int)particleEmitter.maxEmission)
```

Recovered emitter values:

| Property | Original value |
|---|---:|
| particles per impact | 10 |
| start size | random 1.5-3.0 |
| lifetime / energy | random 0.1-0.3 s |
| local velocity | (0, 6, 0) |
| random velocity | (+/-6, +/-6, +/-6) |
| emitter velocity scale | 0.05 |
| random angular velocity | +/-200 deg/s |
| random initial rotation | enabled |
| world-space simulation | enabled |
| ellipsoid | (0.2, 0, 0.2) |
| damping | 0.1 |
| renderer length scale | 2 |
| renderer velocity scale | 0 |

The legacy five-color ParticleAnimator sequence is also promoted exactly from
the serialized bytes. Unity's legacy damping semantics treat **1** as unchanged,
**0** as an immediate stop and **2** as doubling speed per second, so the modern
adapter uses exponential integration for the recovered **0.1** damping value.

Recovered material chain:

```text
Fire
  -> NukeFireB
  -> Particles/Additive
  -> FireB (128x128 RGB24, 8 mips)
```

The clean shader replacement is `TimeCli/ParticleAdditive` and preserves
`ZWrite Off`, `Cull Off`, `Blend SrcAlpha One` and `ColorMask RGB`.

Recovered impact audio:

```text
Misc_MechAbstract_Impact_02
stereo / 48000 Hz / 16-bit / 2.3352709 s
```

The original `OneShotAudio.Explosions` queue uses six AudioSources, a
0.1-second queue cooldown, logarithmic rolloff, min distance 15, max distance
100, and pitch tied to `Time.timeScale`.

Original `FireB` and the impact WAV stay out of the public repository. The
private extraction path produces:

- `TimeCliSmallExplosionTexture.png`
- `TimeCliSmallExplosionImpact.wav`

If those resources are absent, the public build uses a clean procedural visual
fallback and remains silent for the original-derived impact audio.

A separate serialized `RocketExplosion` prefab/script with a 0.5-second
self-destruct exists in the assets, but the recovered `ProjectileDamager`
impact code does not instantiate it. It is therefore not used as the canonical
click-launcher impact effect.

## Rocket tail presentation

The canonical 1.4.5 Rocket tail is **not** a per-rocket particle prefab.
`RocketTailParticles` calls `VirtualPS.RocketTail` every **0.025 s**.
`VirtualPS` moves one shared `Toon Rocket 02 PS` ParticleSystem to the
rocket's current position/rotation and performs:

```text
rocketTailPS.Emit((int)rocketTailPS.emissionRate)
```

The serialized emission rate is **10**, so each 25 ms invocation emits exactly
**10 particles** while continuous emission itself is disabled.

Recovered ParticleSystem values:

| Property | Original value |
|---|---:|
| simulation | world space |
| duration | 5 s |
| looping | true |
| lifetime | random 0.5–1.0 s |
| start speed | random 2.5–5.0 |
| start size | random 0.10–0.25 |
| start rotation | random 0–62.83185196 rad |
| max particles | 500 |
| cone radius | 0.01 |
| cone angle | 2.83° |
| cone length | 5 |
| cone arc | 360° |
| random direction | enabled |
| force over lifetime | random -2.5..+2.5 on X/Y/Z |
| force space | world |
| force randomize per frame | true |
| renderer | billboard / OldestInFront |
| max particle screen size | 0.5 |
| renderer length scale | 2 |

The exact 12-key Size-over-Lifetime curve and both randomized
Color-over-Lifetime gradients are promoted in
`OriginalRocketTailPresentation` and regression-tested.

Recovered material chain:

```text
Toon Rocket 02 PS
  -> Circle PRT MAT Alpha
  -> Mobile-Particle-Alpha
  -> Smoke Toon PRT TEX MOD (128x128 RGBA32, 8 mips)
```

The old shader uses transparent alpha blending with `ZWrite Off`,
`Cull Off`, and `Blend SrcAlpha OneMinusSrcAlpha`. The public project
contains a clean equivalent shader (`TimeCli/ParticleAlpha`).

The original `Smoke Toon PRT TEX MOD` texture remains private original-derived
data. The normal private-data extraction path exports it as
`TimeCliRocketTailTexture.png`; the runtime loads that Resource when present
and otherwise uses a clean procedural radial texture. All other Rocket-tail
ParticleSystem behavior is now reconstructed from confirmed canonical values.

## Rocket orbital presentation

Serialized Rocket values:

```text
rotationSpeed = 360 degrees/s
tail spawn delay = 0.025 s
serialized dormant RocketExplosion prefab self-destruct = 0.5 s
```

For rocket index 0, the Rocket orbit component is disabled.

For additional rockets:

```text
rotationOffset =
    360 / (rocketCount - 1) * rocketIndex

pivot.localRotation =
    Euler(0, 0, Time.time * 360 + rotationOffset)

while timer < 1:
    timer += deltaTime * 5
    projectile.localPosition =
        (0, Lerp(0,1,timer), 0)

after ramp:
    projectile.localPosition = (1,0,0)
```

The current clean Unity reconstruction implements the root movement, orbital
pivot, 0.025-second tail emission cadence and collision timing. Original
mesh/particle/audio assets remain private inputs and are not committed
publicly.

## Recovered click-weapon fire spots

Arena scene rest positions:

```text
ClickPistol  ( 0.600000024, 0.301000000, -7.30099994)
ClickCannon  ( 0.069999860, 0.315999930, -7.30079996)
ClickLauncher(-0.599999850, 0.205999970, -7.49800003)
```

Runtime aim pivots can rotate these hierarchy points; the constants are the
scene-authored fallback positions used before the complete original weapon
hierarchy is rebuilt.
