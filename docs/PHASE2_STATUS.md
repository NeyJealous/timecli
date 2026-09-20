# Phase 2 status — gameplay core recovery

## Status

**COMPLETE. Gate passed.**

The Unity-independent gameplay core now reproduces the recovered Time Clickers
1.4.5 progression/combat rules needed by the modern port.

PortCore integration API is frozen as **v1.0.0** for Phase 3.

## Covered systems

- Arena HP, wave/max-wave/farm navigation and boss retry behavior;
- exact Time/Weapon Cube spawn/reward rules;
- Artifact / Weapon Augment balance, dependency graphs, buy/sell/refund/respec;
- all 5 canonical Heroes and all base upgrade trees;
- Promotion / Training / infinite Spec Ops;
- Hero x1 / next-upgrade / next-rank / MAX purchasing;
- Click Pistol progression;
- all 10 Active Abilities, Cooldown and Dimension Shift;
- Gold / Time Cube / Weapon Cube banks;
- BoxEnemy-style block damage/death/rewards;
- Hero auto-fire targeting for Pulse Pistol, Flak Cannon, Spread Rifle,
  Rocket Launcher and Particle Ball;
- Flak/Spread Never Miss behavior;
- Rocket splash targeting;
- Particle fifth-shot Collider behavior;
- Qubicle voxel extraction, HP decomposition, model selection and block
  allocation;
- 141-model local voxel corpus validation through wave 5000;
- deterministic headless Arena orchestration;
- offline earnings;
- Time Warp;
- legacy save compatibility reference.

## Gate tests

GitHub Actions validates four independent gates:

- formula/state smoke regression;
- deterministic timeline: new game -> wave 100 -> Time Cubes -> Time Warp ->
  first Artifact;
- all-Hero auto-fire/targeting scenario;
- `netstandard2.1` build for Unity consumption.

All four gates pass.

## Deferred to Unity/platform phases

These do not change the recovered portable game rules:

- private canonical voxel coordinate import into Unity;
- visual projectile flight/collisions/VFX;
- original scene/UI reconstruction;
- original graphics/audio;
- achievements/stat presentation side effects;
- platform SDK replacements;
- iOS lifecycle/signing.

See `docs/PORTCORE_UNITY_CONTRACT.md` for the frozen Phase 3 boundary.
