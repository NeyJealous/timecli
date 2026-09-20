# TimeCli Unity project

Modern Unity presentation/runtime project for the reconstructed Time Clickers
1.4.5 gameplay core.

Pinned editor:

`6000.3.24f1 (4e7b9b5b6244)`

## 1. Sync PortCore

Windows:

```powershell
.\tools\unity\sync-portcore.ps1
```

macOS/Linux:

```bash
./tools/unity/sync-portcore.sh
```

This builds the frozen PortCore API for `netstandard2.1` and copies
`TimeClickers.PortCore.dll` into `Assets/Plugins/TimeCli/`.

The DLL is generated output and is not committed.

## 2. Open the project

Open:

`unity/TimeCli`

with the pinned Unity editor.

## 3. Generate the first Arena scene

In the Unity menu:

`TimeCli -> Setup -> Create Arena Prototype Scene`

This creates:

`Assets/TimeCli/Scenes/ArenaPrototype.unity`

with:

- `TimeCliBootstrap`;
- `ArenaRuntimeController`;
- `PrototypeHud`;
- a camera and light.

Press Play.

## Current prototype behavior

The project can run before private original assets are imported.

Without an assigned canonical `UnityVoxelCatalogAsset`, the bootstrap uses a
synthetic voxel catalog that exercises the real PortCore selection, allocation,
damage, reward, Hero auto-fire and wave-progression code.

The placeholder view uses primitive cubes. Click a cube to apply Click Pistol
damage. The development HUD exposes Gold/wave/DPS, Hero purchases, Click Pistol
purchases, farm navigation and Time Warp when pending Time Cubes exist.

## Runtime path

```text
Unity input/frame
    -> TimeCliBootstrap
    -> GameState
    -> HeadlessArenaEngine
    -> tested PortCore result
    -> ArenaRuntimeController
    -> VoxelBlockView
```

## Important boundary

Unity presentation code must not reimplement gameplay formulas.

See:

`docs/PORTCORE_UNITY_CONTRACT.md`

Canonical voxel coordinates and original visual/audio assets remain private
reconstruction inputs. The synthetic catalog and placeholder colors are
development-only.
