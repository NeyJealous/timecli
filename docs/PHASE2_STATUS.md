# Phase 2 status — gameplay core recovery

## Status

**Late-stage / gate candidate.** The economy, progression, Arena state,
voxel selection/allocation, block damage/rewards and full timeline orchestration
now run without Unity.

## Implemented

The portable core contains compatibility implementations for:

- Arena HP, wave progression, cube spawn rules and per-timeline reward history;
- lower-wave farming plus previous/next/direct unlocked-wave navigation;
- boss timer, timeout, 0.5 s same-wave retry and successful boss completion;
- Artifact / Weapon Augment canonical balance, exact prerequisite graphs,
  buy/sell/refund and Artifact respec;
- all 5 heroes, their 35-upgrade trees, infinite Promotion/Training/Spec Ops
  progression and x1/next-upgrade/next-rank/max purchasing;
- hero DPS, rate-of-fire, projectile-count, United Front, critical,
  gold-find and click-contribution math;
- Click Pistol progression and purchasing;
- all 10 Active Abilities, sequential purchasing, Cooldown and Dimension Shift;
- Gold / Time Cube / Weapon Cube banks;
- BoxEnemy-style block HP, targeting mask, click/normal damage, overkill and
  gold/cube rewards;
- Qubicle/Unity TextAsset voxel extraction tooling;
- VoxelLibrary HP decomposition, model filtering and exact boss override;
- Artifact enemy-count transformations;
- Arena voxel block-allocation/fallback logic, special cube replacement,
  Rainbow conversion and first-enemy path;
- headless Arena orchestration joining spawn -> block destruction -> model
  completion -> wave progression;
- offline earnings with the original 172800-second cap and fallback formula;
- aggregate GameState;
- legacy save-format codec reference.

## Voxel corpus recovery

The complete Android 1.4.5 corpus has been parsed locally:

- 141 voxel models;
- 43 numeric boss-key models;
- exact private block coordinates recovered;
- four expected colors only;
- model selection validated for every wave 1..5000;
- zero waves without an eligible model;
- all Time Cube candidate models have the required White replacement slot;
- all Weapon Cube candidate models from wave 1000 have the required
  Black/Yellow replacement slot.

The public repository intentionally stores the parser/logic but not the
original-derived coordinate corpus.

## Confirmed Arena behavior

Boss failure does **not** move the player to the previous wave:

`timer expires -> clear current boss -> 0.5 s delay -> restart same boss wave`.

Lower regular waves are true farm mode. Once the player selects a wave below
`MaxWave`, only the first completed model sets the kill counter to one;
subsequent clears do not increment it, so the game does not automatically
return to the highest unlocked wave.

## CI validation

GitHub Actions now runs two independent jobs:

1. `PortCore.Smoke` — formula/state regression suite.
2. `PortCore.TimelineScenario` — deterministic new-game simulation through
   wave 100, guaranteed Time Cube reward, Time Warp and first Artifact purchase.

Both jobs pass on the current gate candidate.

## Remaining before Phase 2 is frozen

- reconstruct/validate the remaining Hero auto-fire target-selection behavior
  for Flak Cannon, Spread Rifle, Rocket Launcher and Particle Ball;
- define the stable PortCore <-> Unity adapter contract;
- import the private canonical voxel layouts when `timecli-originals` is
  populated, then run the same headless scenario against the real layout catalog.

Achievement/stat side effects, VFX, audio, UI, physical projectile visuals,
scene serialization and legacy platform SDKs remain Unity/platform work rather
than blockers for the portable progression core.
