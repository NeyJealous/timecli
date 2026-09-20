# Phase 3 status — Unity reconstruction

Status: **in progress**

## Baseline

The modern Unity project is pinned to Unity **6000.3.24f1**.

PortCore Phase 2 is frozen at adapter API **1.0.0** and builds for
`netstandard2.1`.

## Unity adapter gate

A dedicated `UnityAdapter.Compile` preflight now compiles all current Runtime
and Editor C# scripts against a minimal Unity API surface. This already caught
and fixed real adapter errors before the first Unity Editor launch.

Current CI gates are green:

- PortCore smoke;
- deterministic timeline;
- Hero combat;
- `netstandard2.1` Unity compatibility;
- Unity Runtime/Editor adapter compile preflight.

This is not a replacement for a real Unity Editor import: serialization,
shader import, scene import and native Editor behavior still need the actual
Unity executable.

## Current runtime

Implemented under `unity/TimeCli`:

- `TimeCliBootstrap`;
- `ArenaRuntimeController`;
- `PrototypeHud`;
- Unity random adapter;
- serializable and binary voxel catalog adapters;
- synthetic development voxel catalog;
- private canonical voxel-catalog import path;
- one-click Editor Arena scene generator;
- click damage and all Hero auto-fire through the tested
  `HeadlessArenaEngine`.

## Private canonical voxel import

The locally recovered 141-model coordinate corpus can be converted without
committing original-derived coordinates:

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

## Original Arena presentation recovered

The placeholder presentation has now been replaced with a procedural
reconstruction of the original Arena block geometry.

Recovered and implemented:

- original `Arena` root transform:
  `(0, -3.3399999, 5.3499999)`, Y rotation about `-60°`;
- original perspective gameplay camera:
  position `(0, 1, -10)`, X rotation about `-6.469°`, FOV 60,
  near 0.3, far 1000, depth -2;
- exact Qubicle model centering and inverted-Z transform;
- unit voxel spacing;
- original BoxEnemy unit `BoxCollider`;
- reconstructed `Body` mesh: 24 vertices / 32 triangles;
- exact health-fill vertex deformation;
- original serialized enemy palette;
- clean modern vertex-color shader for the reconstructed mesh.

Details are recorded in
`docs/ARENA_PRESENTATION_RECOVERY.md`.

The old Unity 5 compiled `Custom/SimpleEnemy`, Rainbow and TimeCube shader
binaries are not copied into the modern project. Their visual behavior is being
reconstructed cleanly.

## Batch first-run workflow

The first real Unity compile + scene generation is now automated.

Windows:

```powershell
.\tools\unity\create-arena-prototype.ps1
```

Optional private canonical voxels:

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

The batch process:

1. builds/syncs PortCore;
2. optionally builds the private voxel binary;
3. launches Unity in batch mode;
4. imports/compiles the project;
5. invokes the Editor scene generator;
6. writes `ArenaPrototype.unity`;
7. stores the Unity log under `out/unity/`.

## Current prototype path

```text
Unity input/frame
    -> TimeCliBootstrap
    -> GameState
    -> HeadlessArenaEngine
    -> tested combat/progression result
    -> ArenaRuntimeController
    -> reconstructed BoxEnemy mesh
```

The development HUD exposes wave/farm navigation, Gold, Team DPS, Click
damage, Hero and Click Pistol purchasing, cube balances and Time Warp.

## Next gate

The remaining immediate Phase 3 gate is an **actual Unity 6000.3.24f1 batch
import/run** on a machine with that Editor installed.

After that succeeds:

- run the prototype against the private 141-model catalog;
- validate framing/scale against the original Arena;
- reconstruct Rainbow / TimeCube / WeaponCube presentation;
- reconstruct visual projectile motion without moving gameplay authority out
  of PortCore;
- begin original HUD/UI scene reconstruction.
