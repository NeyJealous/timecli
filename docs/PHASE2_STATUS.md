# Phase 2 status — gameplay core recovery

## Implemented

The portable core now contains independent compatibility implementations for:

- Arena HP and cube progression;
- Artifact and Weapon Augment upgrade costs;
- hero level/upgrade costs;
- infinite Promotion / Training / Spec Ops schedule;
- rank and Spec Ops progression;
- hero DPS calculation pipeline;
- fire-rate reset/multiplier behavior;
- projectile-count reset behavior;
- hero extra-click contribution;
- team United Front / critical / gold-find aggregation;
- click-skill cost and damage pipeline;
- active-ability purchase cost;
- Dimension Shift multiplier;
- legacy save codec.

## Confirmed upgrade enums

`UpgradeMod`:

| id | meaning |
|---:|---|
| 0 | FireRate |
| 1 | HeroDps |
| 2 | Projectiles |
| 3 | GoldFind |
| 4 | ClickDamage |
| 5 | UnitedFront |
| 6 | Promotion |
| 7 | Collider |
| 8 | CriticalChance |
| 9 | CriticalDamage |
| 10 | Splash |
| 11 | Training |
| 12 | SpecOps |

Weapon ids:

0 Pistol, 1 Flak Cannon, 2 Spread Rifle, 3 Rocket Launcher, 4 Particle Ball.

## Infinite-upgrade behavior recovered

The original base hero tree has 35 upgrades. Post-1000 Spec Ops cycles reuse the base tree but omit every upgrade whose required level ends in `85`; therefore each generated cycle contains 30 upgrades.

Generated required levels are shifted by `specOpsLevel * 1000`. Generated upgrade ids remain globally monotonic.

This behavior is implemented by `InfiniteUpgradeSchedule`.

## Compile / port ledger

Still outside the portable core:

- Unity-facing MonoBehaviours and event wiring;
- targeting, projectile spawning, collisions and VFX;
- Enemy runtime and block destruction;
- Arena coroutine/state transitions;
- UI;
- old ObscuredInt/ObscuredULong wrappers (will be replaced by normal numeric state);
- achievements/statistics side effects;
- legacy platform SDKs;
- scene serialization binding.

These are being kept separate from PortCore so game math can be regression-tested without Unity.

## Next gate

1. Reconstruct Artifact aggregate effects into a portable snapshot.
2. Reconstruct Weapon Augment aggregate effects.
3. Reconstruct Gold/Enemy reward math.
4. Build a deterministic headless timeline simulation.
5. Only after those tests pass, connect PortCore to the modern Unity project.
