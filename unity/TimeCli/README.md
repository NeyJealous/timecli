# TimeCli Unity project

Modern Unity presentation/runtime project for the reconstructed Time Clickers
1.4.5 gameplay core.

Pinned editor:

`6000.3.24f1 (4e7b9b5b6244)`

## Fastest first run

With Unity installed, the complete setup is automated.

Windows:

```powershell
.\tools\unity\create-arena-prototype.ps1
```

If Unity is installed elsewhere:

```powershell
.\tools\unity\create-arena-prototype.ps1 \
  -UnityPath "D:\Unity\6000.3.24f1\Editor\Unity.exe"
```

macOS/Linux:

```bash
UNITY_PATH="/path/to/Unity" bash tools/unity/create-arena-prototype.sh
```

The script builds PortCore, launches Unity in batch mode, compiles/imports the
project and creates:

`Assets/TimeCli/Scenes/ArenaPrototype.unity`

The Unity log is written to:

`out/unity/arena-prototype-batch.log`

## Private canonical voxel catalog

Optionally provide the locally recovered private JSON before the first run.

Windows:

```powershell
.\tools\unity\create-arena-prototype.ps1 \
  -PrivateVoxelJson "C:\path\voxel_layouts_private.json"
```

macOS/Linux:

```bash
UNITY_PATH="/path/to/Unity" \
PRIVATE_VOXEL_JSON="/path/to/voxel_layouts_private.json" \
bash tools/unity/create-arena-prototype.sh
```

The generated binary lives under `Assets/TimeCli/PrivateGenerated/` and is
ignored by Git.

## Manual workflow

Build/sync PortCore:

```powershell
.\tools\unity\sync-portcore.ps1
```

or:

```bash
bash tools/unity/sync-portcore.sh
```

Then open `unity/TimeCli` in Unity and run:

`TimeCli -> Setup -> Create Arena Prototype Scene`

## Current prototype

The presentation is no longer arbitrary primitive cubes. It now procedurally
reconstructs the original BoxEnemy `Body` geometry, health-fill deformation,
enemy palette, unit collider, Arena root transform and gameplay camera.

Without private canonical voxel data, a synthetic voxel catalog is still used
for development, but damage/rewards/Hero auto-fire/wave progression all run
through PortCore.

The development HUD provides:

- wave/max wave and farm navigation;
- Gold / DPS / Click damage;
- Hero purchasing;
- Click Pistol purchasing;
- Time/Weapon Cube balances;
- Time Warp.

## Runtime boundary

```text
Unity presentation/input
    -> TimeCliBootstrap
    -> GameState / HeadlessArenaEngine
    -> PortCore result
    -> ArenaRuntimeController
    -> reconstructed block view
```

Unity presentation code must not duplicate gameplay formulas.

See:

- `docs/PORTCORE_UNITY_CONTRACT.md`
- `docs/ARENA_PRESENTATION_RECOVERY.md`
- `docs/PHASE3_STATUS.md`
