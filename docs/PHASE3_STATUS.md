# Phase 3 status — Unity reconstruction

Status: **in progress**

## Baseline

The modern Unity project is pinned to Unity **6000.3.24f1**.

PortCore Phase 2 is frozen at adapter API **1.0.0** and now builds for
`netstandard2.1`.

## Added

- minimal Unity project skeleton under `unity/TimeCli`;
- generated PortCore DLL sync scripts for Windows/macOS/Linux;
- `TimeCliBootstrap`;
- Unity-to-PortCore random adapter;
- serializable voxel catalog asset;
- synthetic fallback voxel catalog;
- first `ArenaRuntimeController`;
- placeholder `VoxelBlockView`;
- direct use of the tested `HeadlessArenaEngine` for click and Hero auto-fire.

The fallback catalog exists only so the modern project can be brought up before
private original-derived voxel layouts are imported.

## First visual milestone

The initial Arena prototype should prove this pipeline:

```text
Unity frame/input
    -> PortCore command
    -> tested combat/progression result
    -> placeholder cube view refresh
    -> next enemy / next wave
```

No gameplay formula should be reimplemented in MonoBehaviours.

## Next

- generate/open the Arena prototype scene;
- add development HUD for Gold, wave, DPS, purchase actions and farm navigation;
- import canonical private voxel catalog into a ScriptableObject;
- restore exact original model centering/scaling;
- begin replacement of placeholder cubes with reconstructed original visuals.
