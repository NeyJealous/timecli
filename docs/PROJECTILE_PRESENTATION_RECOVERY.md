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

## Rocket tail presentation

Recovered scheduling value:

```text
tail emission interval = 0.025 s
```

The Unity reconstruction now preserves that cadence independently of frame
rate, including multiple emissions when a frame spans more than one interval.
The emitted public-project particles are intentionally a clean procedural
fallback. Exact original particle mesh/material/serialized visual parameters
have not yet been promoted into the public reconstruction and are not claimed
as recovered here.

## Rocket orbital presentation

Serialized Rocket values:

```text
rotationSpeed = 360 degrees/s
tail spawn delay = 0.025 s
explosion lifetime = 0.5 s
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
