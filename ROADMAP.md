# Roadmap

## Phase 1 — forensic recovery

Status: **baseline complete**

- identify engine/runtime;
- recover build scene list;
- inventory managed types/components;
- recover core economy/progression formulas;
- identify platform SDK dependencies;
- recover save-format cryptography.

**Gate:** APK is proven suitable for deterministic reconstruction.

## Phase 2 — gameplay source recovery

- reconstruct the project's own gameplay classes into a compilable source tree;
- separate first-party gameplay code from bundled SDK code;
- map MonoScript references to recovered classes;
- reconstruct serializable models/enums/delegates;
- maintain a compile-error ledger for unresolved old-Unity APIs.

**Gate:** gameplay source tree exists with a documented dependency graph.

## Phase 3 — asset and scene reconstruction

- export required Texture2D/Sprite/AudioClip/Mesh/Material/Animation data locally;
- rebuild Splash, Arena and TimeWarp;
- rebuild Artifacts, WeaponAugments and TimelineSummary additive scenes;
- restore MonoBehaviour fields/references;
- validate anchors, camera and layout.

**Gate:** Arena opens without missing gameplay references.

## Phase 4 — modern Unity migration

- create a clean supported Unity project;
- port reconstructed gameplay code;
- import reconstructed scene/content mappings;
- replace obsolete Unity APIs and incompatible shaders;
- preserve original gameplay math and timing.

**Gate:** desktop/editor build completes the core loop offline.

## Phase 5 — platform abstraction

First prototype:

- Google Play Games -> local/no-op;
- Steamworks -> disabled;
- Amazon/Kongregate -> disabled;
- ads -> disabled;
- IAP/store -> disabled;
- cloud -> local save only.

**Gate:** gameplay launch/save/progression do not depend on legacy SDKs.

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
