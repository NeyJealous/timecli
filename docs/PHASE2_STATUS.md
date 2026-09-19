# Phase 2 status — gameplay core recovery

## Implemented

The portable core now contains independent compatibility implementations for:

- Arena HP, wave progression and cube spawn rules;
- lower-wave farming and previous/next wave navigation;
- boss timer, timeout and same-wave restart behavior;
- Artifact and Weapon Augment canonical balance tables;
- exact Artifact / Weapon Augment prerequisite graphs;
- one-level buy, sell/refund, max-level checks and total-spent accounting;
- Artifact respec semantics using all lifetime + pending Time Cubes;
- hero level/upgrade costs;
- infinite Promotion / Training / Spec Ops schedule;
- all 5 canonical hero trees (35 base upgrades each);
- hero purchase modes: x1 / next upgrade / next rank / max;
- rank and Spec Ops progression;
- hero DPS calculation pipeline;
- fire-rate reset/multiplier behavior;
- projectile-count reset behavior;
- hero extra-click contribution;
- team United Front / critical / gold-find aggregation;
- Click Pistol progression and purchasing;
- block-level BoxEnemy-style HP and damage state;
- targeted-click multipliers and enemy-color click multipliers;
- normal / rainbow / Time Cube / Weapon Cube death rewards;
- per-timeline Time/Weapon Cube reward-wave history;
- Gold / Time Cube / Weapon Cube banks;
- all 10 Active Ability definitions and runtime states;
- sequential Active Ability purchasing;
- Cooldown behavior;
- Dimension Shift stack/reset behavior;
- offline earnings with the original 172800-second cap and fallback formula;
- headless aggregate GameState;
- legacy save-format codec reference.

## Confirmed Arena behavior

Boss failure in Time Clickers 1.4.5 does **not** move the player to the previous wave.

The original flow is:

`boss timer expires -> clear boss -> wait 0.5 s -> restart the same boss wave`.

Lower regular waves act as explicit farm waves: once the player manually moves below `MaxWave`, the first cleared enemy sets the per-wave counter to one, but subsequent clears do not increase it, so the game does not automatically return to the highest wave.

This is now represented by `TimelineProgression`.

## Progression dependency behavior

An Artifact or Weapon Augment is locked until **all** entries in its `requires` array own at least one level.

Selling follows the original rule:

- level 0: cannot sell;
- level >1: may sell one level;
- level 1: cannot sell if any currently owned dependent node requires it.

Selling refunds exactly the price paid for the previous level.

## Headless combat boundary

`EnemyBlockState` now mirrors the independently testable part of `BoxEnemy`:

- max/current HP;
- target mask;
- targeted click amplification;
- color-specific click amplification;
- overkill;
- block death;
- gold/cube reward output.

`EnemyModelState` aggregates blocks into one spawned voxel model.

The remaining model-construction work is intentionally separate because exact block counts, positions and color/type distribution come from Unity VoxelModel asset data rather than from the gameplay assembly alone.

## Compile / port ledger

Still outside the portable core:

- extraction of canonical VoxelModel block layouts into portable model definitions;
- exact hero projectile target-selection / collision behavior;
- Rocket splash and Particle Ball collision geometry;
- achievement modifiers and statistics side effects;
- UI;
- Unity-facing MonoBehaviours and event wiring;
- scene serialization binding;
- legacy platform SDKs.

## Phase 2 gate remaining

1. Extract canonical VoxelModel layouts used by Arena.
2. Build deterministic model-spawn definitions from those layouts.
3. Connect model completion to `TimelineProgression` in one end-to-end headless simulation.
4. Add reference scenarios from new game through boss / Time Cube / Time Warp.
5. Freeze PortCore API and start the modern Unity adapter layer.
