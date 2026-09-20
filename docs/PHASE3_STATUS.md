# Phase 3 status — Unity reconstruction

Status: **in progress**

## Baseline

The modern Unity project is pinned to Unity **6000.3.24f1**.

PortCore Phase 2 is frozen at adapter API **1.0.0** and builds for
`netstandard2.1`.

## Added

- minimal Unity project under `unity/TimeCli`;
- PortCore DLL sync scripts for Windows/macOS/Linux;
- `TimeCliBootstrap`;
- Unity-to-PortCore random adapter;
- serializable voxel catalog asset;
- synthetic fallback voxel catalog;
- private binary voxel-catalog import path;
- first `ArenaRuntimeController`;
- placeholder `VoxelBlockView`;
- development HUD;
- one-click Editor command for creating `ArenaPrototype.unity`;
- direct use of tested `HeadlessArenaEngine` for click and Hero auto-fire;
- exact original Qubicle -> Arena block centering/inverted-Z transform.

## Private canonical voxel import

The locally recovered 141-model coordinate corpus can now be converted without
committing the original-derived data:

```bash
python tools/unity/build-private-voxel-catalog.py \
  /path/to/voxel_layouts_private.json \
  unity/TimeCli/Assets/TimeCli/PrivateGenerated/Resources/TimeCliVoxelCatalog.bytes
```

`PrivateGenerated/` is ignored by Git.

At runtime `TimeCliBootstrap` loads catalogs in this order:

1. explicitly assigned `UnityVoxelCatalogAsset`;
2. private `Resources/TimeCliVoxelCatalog.bytes`;
3. generated synthetic debug catalog.

The converter was locally round-trip validated against all **141 models / 8262
occupied voxels / 43 boss-key models**.

## Exact model transform recovered

For each raw Qubicle block, the original Arena computes:

```text
center = size / 2
center.y = 0
center.x -= 0.5
center.z -= 0.5

local = raw - center
local.z *= -1
```

This is implemented in `VoxelCoordinateMath.ToArenaLocalPosition` and has
PortCore regression tests.

## Current playable prototype path

```text
Unity input/frame
    -> TimeCliBootstrap
    -> GameState
    -> HeadlessArenaEngine
    -> tested combat/progression result
    -> ArenaRuntimeController
    -> VoxelBlockView placeholder
```

The development HUD exposes:

- wave/max wave and farm navigation;
- Gold, Team DPS and Click damage;
- Hero +1 / next-upgrade purchasing;
- Click Pistol purchasing;
- Time/Weapon Cube balances;
- Time Warp when pending Time Cubes exist.

## Validation

Latest PortCore validation is green across:

- smoke;
- timeline;
- hero-combat;
- Unity `netstandard2.1` compatibility.

## Next

- open/generate the Arena prototype in Unity and fix any Editor-only compile or
  serialization issues;
- replace synthetic development geometry with the private canonical binary
  catalog;
- reconstruct exact Arena camera/model scale/material presentation;
- begin original UI and visual/audio asset reconstruction.
