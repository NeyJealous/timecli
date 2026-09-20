# Hero auto-fire recovery

This document records the independently reconstructed behavior of
`Hero.AcquireTarget` and `Hero.ApplyDamage` from Time Clickers 1.4.5.

The implementation lives in `HeroAutoFire.cs` and is covered by the
`PortCore.HeroCombatScenario` CI job.

## Shared timing

A hero does nothing while its level is zero.

For a firing hero:

1. Rocket Launcher runs target acquisition before the fire timer check.
2. The fire interval is calculated as a **float**:
   `1f / rateOfFire`.
3. If the timer is ready, `nextFireTime = Time.time + interval`.
4. `numTimesFired` increments.
5. Shot budget is `DPS * interval`.
6. Weapon-specific distribution is applied.

The float interval is preserved in PortCore because it affects long-running
deterministic timing.

## Target balancing

Each live Arena block has:

- a weapon target bit-mask;
- a separate integer `targetCount`.

The bit-mask is used by Artifact targeted-click bonuses. `targetCount` is a
load-balancing value used by Spread Rifle and single-target weapons.

Flak Cannon sets its target bit but **does not increment targetCount**.

## Pulse Pistol

- one persistent target;
- if the old target dies, acquire a new one;
- scan live Arena blocks in order;
- choose the first block with the lowest available `targetCount` threshold;
- set the Pistol target bit;
- increment `targetCount`;
- apply the full shot budget.

## Flak Cannon

Base projectile count: **4**.

For every actual volley:

1. remove Flak target bits from the previous fixed target array;
2. create a list containing every live block index;
3. randomly remove entries until list size is at most projectile count;
4. when **Never Miss Flak** is active and the list is shorter than projectile
   count, append random live indices until full — duplicates are allowed;
5. set the Flak target bit on each selected block;
6. divide the shot budget evenly by projectile count;
7. apply one damage event for every selected entry.

The original Hero owns a fixed **10-slot** Flak target array. Canonical upgrade
data never exceeds this capacity.

## Spread Rifle

Base projectile count: **3**.

Targets persist between volleys while alive.

On a volley:

1. dead targets are removed from the persistent list;
2. desired unique target count is
   `min(projectileCount, liveBlockCount)`;
3. scan blocks in Arena order using an increasing `targetCount` threshold;
4. newly selected unique targets receive the Spread target bit and
   `targetCount += 1`;
5. with **Never Miss Spread**, random live blocks are appended until projectile
   count is reached — duplicates are allowed, and these duplicate entries do
   not change target bits or `targetCount`;
6. shot budget is divided by projectile count and applied to every entry in the
   persistent target list.

## Rocket Launcher

Base splash multiplier: **0.25**.

Rocket target acquisition happens even when the weapon is not yet ready to
fire.

When a target is needed:

1. choose the least-targeted live block using the same threshold scan as the
   single-target weapons;
2. set Rocket target bit;
3. increment targetCount;
4. build the splash list from all other live blocks whose **squared local
   distance** to the primary target is less than
   `Artifacts.GetRocketSpashDistance()`.

On fire:

- primary target receives full shot budget;
- each stored splash target receives
  `shotBudget * currentSplashMultiplier`.

The splash list is rebuilt when the primary target dies.

The default Artifact splash-distance threshold is **1.1**; the unlocked
diagonal Artifact changes it to **3.0**. These are compared to squared
distance, matching the original.

## Particle Ball

Particle Ball uses the normal persistent single-target acquisition rule.

Base Collider multiplier: **5**.

Every fifth firing event:

```text
shotBudget *= currentColliderMultiplier
numTimesFired = 0
```

The Collider multiplier is rebuilt from the current rank's Hero upgrades and
then multiplied by the Artifact Particle Collider multiplier.

## Rank reset behavior

Promotion, Training and Spec Ops reset weapon-local upgrade values to their
base values before later upgrades in that rank are applied.

This affects:

- projectile count;
- Particle Ball Collider;
- Rocket Launcher splash.

Canonical bases:

| Hero | Base projectiles | Base Collider | Base splash |
|---|---:|---:|---:|
| Pulse Pistol | 1 | 5 | 0.15 |
| Flak Cannon | 4 | 5 | 0.15 |
| Spread Rifle | 3 | 5 | 0.15 |
| Rocket Launcher | 1 | 5 | 0.25 |
| Particle Ball | 1 | 5 | 0.15 |

## Regression coverage

The dedicated hero-combat scenario validates:

- Flak random subset selection;
- Flak Never Miss duplicate filling;
- Flak target-mask cleanup;
- Spread targetCount balancing;
- persistent Spread targets;
- Spread Never Miss duplicate behavior;
- Rocket least-targeted acquisition;
- Rocket squared-distance splash;
- Rocket reacquisition before the fire timer expires;
- Particle fifth-shot Collider boost;
- rank resets for projectiles/splash/collider;
- execution through `HeadlessArenaEngine`.
