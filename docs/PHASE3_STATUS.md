# Phase 3 status — Unity reconstruction

Status: **in progress**

## Baseline

The modern Unity project is pinned to Unity **6000.3.24f1**.

PortCore Phase 2 remains gate-complete. During Phase 3, newly recovered original
behavior has produced additive compatibility corrections; the current adapter
API is **1.2.0** and still targets `netstandard2.1`.

## Validation gates

GitHub Actions now validates:

- PortCore smoke;
- deterministic timeline;
- Hero combat;
- ClickCannon / ClickLauncher state;
- `netstandard2.1` Unity compatibility;
- Unity Runtime/Editor adapter compile preflight.

The latest completed preflight baseline is green. A real Unity Editor import is
still a separate gate because serialization, ShaderLab import and native Editor
behavior cannot be validated by the .NET stub compile.

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
- reconstructed BoxEnemy geometry/health;
- Click Pistol critical roll + original tracer presentation;
- ClickCannon transient charge/fire state;
- ClickLauncher click-progress/fire state;
- Cannon/Launcher projectile movement and collision reporting;
- ClickLauncher Rocket tail emission at the recovered 0.025 s cadence,
  with a clean procedural fallback particle;
- Time/Weapon Cube deferred pickups and collection presentation;
- clean replacements for original special-enemy shaders.

Gameplay damage/rewards/wave state remain PortCore-owned.

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

## Private original special-cube textures

A public extraction tool can pull the required original-derived textures from
`sharedassets1.assets` or the APK Data split parts into the ignored private
Resources directory:

```bash
python tools/unity/extract-private-special-textures.py \
  /path/to/Data-or-sharedassets1.assets \
  unity/TimeCli/Assets/TimeCli/PrivateGenerated/Resources
```

Recovered targets:

- `TimeCliTimeCubePickupTexture.png`
- `TimeCliWeaponCubePickupTexture.png`
- `TimeCliWeaponCubeTexture.png`

The public project falls back to procedural clean visuals when those private
files are absent.

## Original Arena presentation recovered

Implemented:

- Arena root transform:
  `(0, -3.3399999, 5.3499999)`, Y rotation about `-60°`;
- perspective gameplay camera:
  `(0, 1, -10)`, X rotation about `-6.469°`, FOV 60;
- exact Qubicle centering/inverted-Z transform;
- unit voxel spacing;
- BoxEnemy unit collider;
- reconstructed 24-vertex / 32-triangle Body mesh;
- exact health-fill deformation;
- original serialized enemy palette;
- Rainbow / TimeCube modern shader replacements;
- active WidgetGold TimeCube/WeaponCube collection anchors.

See:

- `docs/ARENA_PRESENTATION_RECOVERY.md`
- `docs/SPECIAL_CUBE_RECOVERY.md`

## Click weapon / projectile recovery

Recovered and implemented:

- exact ClickPistol critical roll;
- 4-point Pistol Tracer;
- normal/critical tracer width, colors and fade timing;
- original Pistol/Cannon/Launcher rest fire-spot positions;
- Cannon charge + 1-second grace + 10 charge/s decay;
- Cannon damage-per-projectile and fire cone;
- Launcher click threshold, rocket count and rocket-speed augment;
- Projectile moveDir capture;
- Flak velocity 40 / lifetime 1.5 s;
- Rocket velocity 40 / lifetime 3 s;
- projectile collision sampling at ~30 Hz;
- overlap impact radius 1;
- original hitbox mask 2560;
- additional-rocket orbital pivot behavior;
- rocket explosion visual lifetime 0.5 s.

See `docs/PROJECTILE_PRESENTATION_RECOVERY.md`.

## Batch first-run workflow

Windows:

```powershell
.\tools\unity\create-arena-prototype.ps1
```

With private canonical voxels and special-cube textures:

```powershell
.\tools\unity\create-arena-prototype.ps1 \
  -PrivateVoxelJson "C:\path\voxel_layouts_private.json" \
  -OriginalDataSource "C:\path\to\apk\assets\bin\Data"
```

macOS/Linux:

```bash
UNITY_PATH="/path/to/Unity" \
PRIVATE_VOXEL_JSON="/path/to/voxel_layouts_private.json" \
ORIGINAL_DATA_SOURCE="/path/to/apk/assets/bin/Data" \
bash tools/unity/create-arena-prototype.sh
```

The batch process builds PortCore, prepares optional private reconstruction
data, launches Unity in batch mode and invokes the Arena scene generator.

## Current prototype path

```text
Unity input
    -> GameState / click-weapon state
    -> PortCore click damage plans
    -> Unity trajectory / collision contact
    -> HeadlessArenaEngine authoritative damage
    -> reconstructed BoxEnemy / pickup presentation
```

## Next gate

The immediate gate remains an **actual Unity 6000.3.24f1 batch import/run** on
a machine with that Editor installed.

Code-side work can continue in parallel. The next reconstruction targets are:

- exact original Rocket tail particle/material fidelity (the recovered
  0.025-second emission cadence is now implemented);
- exact click-weapon hierarchy/pivot animation (public forensic extractor is
  now ready; original-derived transform report still needs to be generated);
- original Hero projectile presentation.
