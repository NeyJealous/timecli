# TimeCli Unity project

This is the modern Unity presentation/runtime project for the reconstructed
Time Clickers gameplay core.

Pinned editor:

`6000.3.24f1 (4e7b9b5b6244)`

## Before opening

Build and copy PortCore into the Unity project:

### Windows

```powershell
.\tools\unity\sync-portcore.ps1
```

### macOS / Linux

```bash
./tools/unity/sync-portcore.sh
```

The generated `TimeClickers.PortCore.dll` is intentionally not committed.

## First prototype

The current adapter layer can run without private original assets. When no
canonical voxel catalog asset is assigned, it creates a synthetic debug catalog
that exercises the same PortCore spawning/combat pipeline.

This is only a development fallback. The canonical voxel coordinates and
original visual/audio assets remain private reconstruction inputs.

## Current runtime path

```text
TimeCliBootstrap
    -> GameState
    -> HeadlessArenaEngine
    -> ArenaRuntimeController
    -> VoxelBlockView
```

`ArenaRuntimeController` drives the tested PortCore rules and creates simple
cube placeholders. Visual projectiles, original materials/models, UI and audio
are presentation work and must not alter PortCore gameplay state.
