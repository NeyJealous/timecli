# PortCore <-> Unity adapter contract v1

Status: **frozen for Phase 3**

PortCore API version: **1.0.0**

The purpose of this boundary is to prevent Unity presentation work from
silently changing recovered Time Clickers 1.4.5 gameplay behavior.

## PortCore owns

PortCore is authoritative for:

- Gold / Time Cubes / Weapon Cubes;
- Hero, Click Pistol, Artifact, Weapon Augment and Active Ability progression;
- DPS and click-damage math;
- Arena wave/max-wave/farm/boss state;
- boss failure/retry timing;
- voxel HP decomposition and model selection;
- block allocation and enemy types;
- block HP, damage, targeting and death rewards;
- Hero target selection and auto-fire damage distribution;
- Time Warp / Artifact respec;
- offline earnings.

Unity code must not duplicate these calculations.

## Unity owns

Unity is authoritative only for presentation/platform concerns:

- GameObjects and transforms;
- Camera and rendering;
- colliders and pointer/touch hit testing;
- projectile/VFX animation;
- UI;
- audio/haptics;
- safe area;
- app lifecycle;
- loading private original assets;
- persistence transport / filesystem;
- iOS platform integration.

A visual projectile may take time to fly, but the game-state damage decision
must come from PortCore.

## Time

PortCore accepts elapsed seconds as `double`.

Unity adapters use `Time.timeAsDouble` for runtime combat/boss timing.
Save/offline time uses UTC wall-clock timestamps outside this runtime clock.

Do not pass frame counts into PortCore.

## Randomness

Random decisions are explicit:

- Arena spawn uses `ArenaSpawnRolls`.
- Hero random targeting uses `IRandomSource`.

The Unity adapter currently maps these to `UnityEngine.Random`. Regression
tests use deterministic scripted values.

## Voxel coordinates

PortCore stores raw reconstructed voxel/model-space positions.

The Phase 3 placeholder view currently maps them as:

`Unity position = (x, y, -z) * presentationSpacing`

Final centering/scaling belongs to the Unity adapter and must not mutate the
underlying `VoxelPoint` values.

## Core session objects

The normal Unity runtime owns one:

```text
GameState
HeadlessArenaEngine
HeroAutoFireState per hero (owned by HeadlessArenaEngine)
IVoxelModelCatalog
```

The Unity layer calls commands and reads results. It does not write internal
state directly.

## Main command surface

Current v1 integration surface includes:

- `GameState.TryPurchaseHero`
- `GameState.TryPurchaseClickPistol`
- `GameState.BuyArtifact / SellArtifact`
- `GameState.BuyWeaponAugment / SellWeaponAugment`
- `GameState.TryPurchaseNextAbility`
- `GameState.ActivateAbility`
- `GameState.TimeWarp`
- `GameState.RespecArtifacts`
- `GameState.ApplyOfflineEarnings`
- `HeadlessArenaEngine.SpawnCurrentEnemy`
- `HeadlessArenaEngine.ClickBlock`
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

During Phase 3, a change to recovered gameplay behavior requires:

1. a PortCore regression test;
2. green smoke/timeline/hero-combat CI;
3. green `netstandard2.1` compatibility build;
4. adapter change only if the v1 command/query surface must change.

Presentation-only changes do not require PortCore modifications.
