# Roadmap

## Phase 1 — forensic recovery

Status: **complete**

- identify engine/runtime;
- recover build scene list;
- inventory managed types/components;
- recover core economy/progression formulas;
- identify platform SDK dependencies;
- recover save-format cryptography.

**Gate passed:** APK is suitable for deterministic reconstruction.

## Phase 2 — gameplay source recovery

Status: **gate candidate**

Completed:

- portable economy/progression core;
- all 5 canonical hero trees and purchase modes;
- infinite Promotion / Training / Spec Ops;
- Click Pistol and Active Ability progression;
- Artifact / Weapon Augment canonical data, dependencies and economy;
- Time Cube / Weapon Cube rules and respec behavior;
- Arena farm/navigation/boss state;
- block-level damage/death/rewards;
- complete 141-model Qubicle corpus extraction locally;
- VoxelLibrary HP decomposition/model selection;
- exact block-allocation/fallback rules;
- headless Arena engine;
- offline earnings;
- deterministic CI scenario from new game through wave 100 -> Time Cubes ->
  Time Warp -> Artifact purchase.

Remaining before freeze:

- remaining Hero auto-fire target-selection mechanics;
- PortCore/Unity adapter contract;
- rerun real-layout integration once the private originals repository receives
  the recovered coordinate data.

**Gate:** freeze the portable API once the remaining Hero attack rules are
covered by regression tests.

## Phase 3 — asset and scene reconstruction

- create the modern Unity project/adapters;
- import private reconstructed voxel/model data;
- rebuild Splash, Arena and TimeWarp;
- rebuild Artifacts, WeaponAugments and TimelineSummary additive scenes;
- restore MonoBehaviour fields/references;
- validate anchors, camera and layout;
- restore original visual/audio assets from the private originals workspace.

**Gate:** Arena opens and completes the tested headless loop without missing
runtime references.

## Phase 4 — modern Unity migration / playable prototype

- connect PortCore to Unity presentation;
- replace obsolete APIs and incompatible shaders;
- implement touch input and visual projectiles;
- preserve tested game math/timing.

**Gate:** desktop/editor build completes the core loop offline.

## Phase 5 — platform abstraction

First prototype:

- Google Play Games -> local/no-op;
- Steamworks -> disabled;
- Amazon/Kongregate -> disabled;
- ads -> disabled;
- IAP/store -> disabled;
- cloud -> local save only.

**Gate:** launch/save/progression do not depend on legacy SDKs.

## Phase 6 — save compatibility

- validate encrypt/decrypt against original Android exports;
- reconstruct JSON model mapping;
- add regression fixtures;
- support Android export -> reconstructed build import.

**Gate:** the same progress round-trips correctly.

## Phase 7 — iOS adaptation

- touch and multi-touch;
- safe area / notch / Dynamic Island;
- lifecycle save on background;
- iOS paths;
- audio interruptions;
- memory-pressure handling.

## Phase 8 — ARM64 build/signing

- Unity iOS export;
- Xcode project;
- ARM64 device build;
- signing/provisioning;
- real-device regression test.

Final device build requires macOS + Xcode.
