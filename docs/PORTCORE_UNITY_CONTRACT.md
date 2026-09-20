# PortCore <-> Unity adapter contract v1

Status: **frozen for Phase 3, additive fidelity corrections allowed**

PortCore API version: **1.2.0**

The purpose of this boundary is to prevent Unity presentation work from
silently changing recovered Time Clickers 1.4.5 gameplay behavior.

## PortCore owns

PortCore is authoritative for:

- Gold / Time Cubes / Weapon Cubes;
- Hero, Click Pistol, Artifact, Weapon Augment and Active Ability progression;
- DPS and click-damage math;
- ClickCannon charge and ClickLauncher click-progress state;
- Cannon/Launcher projectile damage values;
- Arena wave/max-wave/farm/boss state;
- boss failure/retry timing;
- voxel HP decomposition and model selection;
- block allocation and enemy types;
- block HP, damage, targeting and death rewards;
- Hero target selection and auto-fire damage distribution;
- Time/Weapon Cube pickup lifecycle and bank-credit timing;
- Time Warp / Artifact respec;
- offline earnings.

Unity code must not duplicate these calculations.

## Unity owns

Unity is authoritative for presentation/platform concerns:

- GameObjects and transforms;
- Camera and rendering;
- colliders and pointer/touch hit testing;
- projectile world-space trajectory and collision contact;
- VFX/particle animation;
- UI;
- audio/haptics;
- safe area;
- app lifecycle;
- loading private original assets;
- persistence transport / filesystem;
- iOS platform integration.

For projectile weapons, Unity may determine **which collider was physically
hit**, matching the original Unity Physics behavior. It then reports the hit to
PortCore. Unity must not calculate gameplay damage, rewards or wave state.

## Time

PortCore accepts elapsed seconds as `double`.

Unity adapters use `Time.timeAsDouble` for runtime combat/boss/pickup timing.
Save/offline time uses UTC wall-clock timestamps outside this runtime clock.

Do not pass frame counts into PortCore.

## Randomness

Random decisions are explicit where they affect portable game state:

- Arena spawn uses `ArenaSpawnRolls`;
- Hero random targeting uses `IRandomSource`.

Unity presentation uses `UnityEngine.Random` where the original behavior was
itself a Unity visual/physics randomization, for example ClickCannon projectile
cone rotation.

## Voxel coordinates

PortCore stores raw reconstructed voxel/model-space positions.

The recovered Arena mapping is:

```text
center = size / 2
center.y = 0
center.x -= 0.5
center.z -= 0.5
local = raw - center
local.z *= -1
```

Presentation scaling/parent transforms belong to the Unity adapter and must not
mutate the underlying `VoxelPoint` state.

## Core session objects

The normal Unity runtime owns one:

```text
GameState
HeadlessArenaEngine
ClickWeaponRuntime (owned by GameState)
SpecialCubePickupQueue (owned by GameState)
HeroAutoFireState per hero (owned by HeadlessArenaEngine)
IVoxelModelCatalog
```

The Unity layer calls commands and reads results. It does not write internal
gameplay state directly.

## Main command surface

Current v1.2 integration surface includes:

- `GameState.TryPurchaseHero`
- `GameState.TryPurchaseClickPistol`
- `GameState.BuyArtifact / SellArtifact`
- `GameState.BuyWeaponAugment / SellWeaponAugment`
- `GameState.TryPurchaseNextAbility`
- `GameState.ActivateAbility`
- `GameState.TimeWarp`
- `GameState.RespecArtifacts`
- `GameState.ApplyOfflineEarnings`
- `GameState.TryCollectCubePickup`
- `GameState.UpdateCubePickups`
- `GameState.RegisterManualClickWeapons`
- `GameState.UpdateClickWeapons`
- `HeadlessArenaEngine.SpawnCurrentEnemy`
- `HeadlessArenaEngine.ClickBlock`
- `HeadlessArenaEngine.ClickWeaponDamageBlock`
- `HeadlessArenaEngine.FireHero`
- `HeadlessArenaEngine.TickBoss`
- `TimelineProgression.RequestPreviousArena / RequestNextArena`

## Build boundary

`src/PortCore` targets both:

- `net8.0` for tests/tools;
- `netstandard2.1` for Unity.

The Unity project consumes the generated
`TimeClickers.PortCore.dll` from `Assets/Plugins/TimeCli/`.

The DLL is generated, ignored by Git and synchronized with
`tools/unity/sync-portcore.*`.

## Change rule

During Phase 3, a newly recovered gameplay behavior may extend PortCore only
when all of the following are true:

1. the behavior is supported by original 1.4.5 code/data;
2. a regression test is added;
3. all gameplay CI gates remain green;
4. `netstandard2.1` remains green;
5. the Unity adapter is updated only where the command/query boundary changes.

Presentation-only changes do not require PortCore changes.

## v1.1 fidelity correction — collectible cubes

Time/Weapon Cube rewards are not credited at special-block death.

PortCore models:

```text
block death
  -> one pickup object per cube
  -> 2.0 s: collider expands x10
  -> 5.0 s: auto-collection begins if not clicked
  -> 0.5 s TweenPosition
  -> bank credit
```

Manual collection starts the same 0.5-second completion path immediately.

## v1.2 fidelity correction — ClickCannon / ClickLauncher

Recovered auxiliary click-weapon state now lives in PortCore.

ClickCannon:

```text
each click:
  charge += 1
  clamp to max
  decharge starts at now + 1 s
  fire floor(charge) projectiles

after grace period:
  charge -= deltaTime * 10
```

ClickLauncher:

```text
each click:
  clicksProgress += 1

if clicksProgress >= augment-defined threshold:
  fire rocket plan
  clicksProgress = 0
```

Spread Shots modifies Cannon maximum projectiles and Launcher rocket count.
Projectile collision contact remains a Unity Physics responsibility; resulting
BoxEnemy click damage is applied through PortCore.
