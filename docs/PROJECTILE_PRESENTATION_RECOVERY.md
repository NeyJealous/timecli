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

## Projectile prefab presentation

The remaining primitive-sphere placeholders have been replaced by an optional
private-resource path backed by the canonical 1.4.5 projectile meshes. If the
private resources are absent, the public project still creates clean procedural
fallback geometry.

### FlakBullet

Recovered prefab presentation:

```text
FlakBullet
  root scale = (1, 1, 2)
  Mesh       = "Flak Bullet" (30 vertices / 20 triangles)
  Material   = "Plasma Beam"
  Shader     = "Unlit/Color"
  _Color     = (1, 0.8503905535, 0.4980391860, 1)
```

The original material has no active texture dependency for the recovered
`Unlit/Color` shader. The public clean equivalent is
`TimeCli/ProjectileUnlitColor`.

### RocketPrefab

Recovered hierarchy:

```text
RocketPrefab
  RocketPivot
    RocketProjectile
      ParticleTail  localPosition=(0,0,-0.2419999987)
    RocketAudio
```

`RocketProjectile` uses the canonical 257-vertex / 204-triangle Mesh and the
`Rocket` material:

```text
shader   = Custom/Weapon Diffuse Color
_Color   = (0.3286908865, 1, 0, 1)
_MainTex = tgarocket MOD ACID (128x128)
_CubeMap = Channel_Cubemap
```

The private extraction path produces:

- `TimeCliFlakBulletMesh.txt`
- `TimeCliRocketProjectileMesh.txt`
- `TimeCliRocketProjectileTexture.png`
- `TimeCliRocketProjectileAudio.wav`

The mesh files are UnityPy OBJ text. UnityPy intentionally converts Unity's
left-handed mesh to right-handed OBJ by negating X and reversing triangle
winding; `PrivateProjectileObjMeshLoader` reverses both operations before
constructing the runtime Unity Mesh. Regression tests lock this conversion.

The extractor was validated against the canonical archive:

```text
Flak:   30 vertices / 30 UV / 30 normals / 20 triangles
Rocket: 257 vertices / 257 UV / 257 normals / 204 triangles
Rocket texture: 128x128 PNG
Rocket audio: mono / 32000 Hz / 4.64978125 s
```

The original Rocket AudioSource is `playOnAwake=true`, volume/pitch 1,
logarithmic rolloff, minDistance 10 and maxDistance 100. The modern runtime
explicitly starts the private clip because runtime-created AudioSource
properties are assigned after `AddComponent`.

The original Rocket shader also references `Channel_Cubemap`. UnityPy's
high-level Unity 5.4 Shader converter cannot parse this old compiled program,
so the canonical `m_SubProgramBlob` was decompressed directly as LZ4. Both
GLES and GLES3 programs were recovered and agree.

The canonical vertex program transforms the local normal by the upper-left
3x3 of `unity_ObjectToWorld` and uses that vector directly as the cubemap
lookup direction; it does **not** construct a reflection vector.

The canonical fragment program is equivalent to:

```text
texcol  = sample(_MainTex, uv)
cubeCol = sample(_CubeMap, worldNormal)

base.rgb = texcol.rgb * cubeCol.rgb
base.a   = texcol.a

base = lerp(base, base * _Color.aaaa, texcol.aaaa)
output = base + texcol.a * _Color
```

Thus the MainTex alpha channel is the illumination/color mask. The public
`TimeCli/WeaponDiffuseColor` is now a clean-room translation of this recovered
GLES equation.

`Channel_Cubemap` is a 64x64 ETC_RGB4 cubemap with six faces and seven mip
levels. The private extractor slices the canonical serialized image payload into
six complete face chains and exports the decoded 64x64 base faces as
runtime-readable `.bytes` PNG payloads:

- `TimeCliChannelCubemap_PositiveX.bytes`
- `TimeCliChannelCubemap_NegativeX.bytes`
- `TimeCliChannelCubemap_PositiveY.bytes`
- `TimeCliChannelCubemap_NegativeY.bytes`
- `TimeCliChannelCubemap_PositiveZ.bytes`
- `TimeCliChannelCubemap_NegativeZ.bytes`

`PrivateCubemapLoader` reconstructs the modern Unity Cubemap and regenerates
its mip chain. The six-slice decoder was validated against the canonical
serialized assets; all faces decode to 64x64 and the first sliced face matches
UnityPy's independent Cubemap decode byte-for-byte.

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
        (Lerp(0,1,timer), 0, 0)

Mathf.Lerp clamps at 1, so the projectile remains at (1,0,0)
after the radial ramp completes.
```

The current reconstruction implements root movement, the exact X-axis orbital
ramp, the canonical `ParticleTail` child offset, 0.025-second tail emission
cadence, collision timing, private canonical projectile meshes and optional
private Rocket texture/audio. Original-derived assets remain private inputs and
are not committed publicly.

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
