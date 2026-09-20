# Roadmap

## Phase 1 — forensic recovery

Status: **complete**

- identified Unity 5.4.2f2 / Mono runtime;
- recovered scene, managed-code and serialized-data structure;
- recovered gameplay formulas and save-format behavior;
- identified legacy platform dependencies.

**Gate passed.**

## Phase 2 — portable gameplay core

Status: **complete**

- canonical economy/progression data;
- Hero / Click Pistol / Active Ability progression;
- complete Hero auto-fire targeting and damage distribution;
- Artifacts / Weapon Augments / currencies / Time Warp;
- Arena farm/navigation/boss rules;
- block damage/rewards;
- Qubicle voxel parsing/selection/allocation;
- deterministic headless Arena;
- offline progression;
- timeline and combat regression scenarios;
- `netstandard2.1` Unity compatibility.

**Gate passed:** PortCore API frozen at v1.0.0.

## Phase 3 — Unity reconstruction

Status: **in progress**

Current:

- Unity 6.3 LTS project skeleton and PortCore DLL sync pipeline;
- bootstrap + random + voxel-catalog adapters;
- canonical/private voxel catalog import with synthetic fallback;
- recovered Arena transform, camera, BoxEnemy geometry/health and palette;
- recovered Arena HUD as the default prototype HUD;
- Time/Weapon Cube and Gold pickup presentation;
- Click Pistol tracer, Cannon/Launcher projectile motion and collision;
- Automatic Fire schedules and reconstructed Rocket tail cadence;
- exact click-weapon hierarchy, aim/vertical curves, Shoot recoil and
  Appear/Disappear visibility curves promoted from canonical 1.4.5 data;
- canonical SmallExplosion, RocketTail and Sparks presentation paths;
- canonical Flak/Rocket projectile visual binding with private mesh import;
- canonical click-weapon private meshes/MainTex/cubemaps, reconstructed
  WeaponDiffuseColor shader math, firing audio and shared delayed PistolWield
  appear audio.

Next:

- remaining actively-called VirtualPS progression presentation:
  LevelUp / Upgrade / ArtifactBuy/Sell / Promote / Training / SpecOps /
  DimensionShift;
- remaining combat/UI audio fidelity;
- remaining Splash / Arena / TimeWarp presentation;
- Artifacts / WeaponAugments / TimelineSummary additive screens;
- exact audio reconstruction.

**Gate:** Unity Arena runs the tested PortCore loop without gameplay logic
duplication or missing runtime references.

## Phase 4 — playable modern prototype

- presentation-quality projectile movement/collisions;
- complete player input;
- UI and progression screens;
- obsolete API/shader replacements;
- desktop/editor offline gameplay regression.

**Gate:** complete offline core loop in the modern Unity editor/player.

## Phase 5 — platform abstraction

- Google Play Games -> local/no-op/replacement;
- Steam/Amazon/Kongregate -> disabled or abstracted;
- ads -> disabled;
- IAP/store -> disabled initially;
- cloud -> local save first.

**Gate:** launch/save/progression are independent of legacy SDKs.

## Phase 6 — save compatibility

- validate legacy encrypt/decrypt against original exports;
- reconstruct complete JSON model mapping;
- migration/versioning;
- Android export -> reconstructed build import.

**Gate:** original progress round-trips correctly.

## Phase 7 — iOS adaptation

- touch/multi-touch;
- safe area / notch / Dynamic Island;
- lifecycle save/background handling;
- iOS paths;
- audio interruptions;
- memory-pressure handling.

## Phase 8 — ARM64 build/signing

- Unity iOS export;
- Xcode project;
- ARM64 device build;
- signing/provisioning;
- real-device regression tests.

Final device build requires macOS + Xcode.
