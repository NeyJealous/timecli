# TimeCli Unity project

Modern Unity presentation/runtime project for the reconstructed Time Clickers
1.4.5 gameplay core.

Pinned editor:

`6000.3.24f1 (4e7b9b5b6244)`

Current PortCore adapter API: **1.2.0**.

## Fastest first run

With Unity installed:

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
UNITY_PATH="/path/to/Unity" \
bash tools/unity/create-arena-prototype.sh
```

The script builds PortCore, launches Unity in batch mode, imports/compiles the
project and creates:

`Assets/TimeCli/Scenes/ArenaPrototype.unity`

Unity log:

`out/unity/arena-prototype-batch.log`

## Private canonical voxel catalog

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

## Private original special-cube textures

The setup script can also extract the recovered TimeCube / WeaponCube textures
from the original APK Data directory or a reassembled `sharedassets1.assets`.

Windows:

```powershell
.\tools\unity\create-arena-prototype.ps1 \
  -OriginalDataSource "C:\path\to\apk\assets\bin\Data"
```

Combined:

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

All generated original-derived files live under
`Assets/TimeCli/PrivateGenerated/`, which is ignored by Git.

## Private click-weapon hierarchy recovery

The exact original Pistol/Cannon/Launcher parent/pivot chain is intentionally
not guessed. A forensic extractor is available:

```bash
python -m pip install UnityPy
python tools/unity/extract-private-weapon-hierarchy.py \
  /path/to/apk/assets/bin/Data \
  unity/TimeCli/Assets/TimeCli/PrivateGenerated/Resources/TimeCliWeaponHierarchy.json

python tools/unity/validate-private-weapon-hierarchy.py \
  unity/TimeCli/Assets/TimeCli/PrivateGenerated/Resources/TimeCliWeaponHierarchy.json
```

The normal Arena setup scripts perform both steps automatically when
`OriginalDataSource` is supplied and UnityPy is installed. The resulting
private JSON becomes a Unity `TextAsset`; runtime rebuilds only the transform
hierarchy and binds its verified fire spots over the public fallback. Use
`-RequireWeaponHierarchy` on Windows or
`REQUIRE_WEAPON_HIERARCHY=1` on macOS/Linux to fail setup instead of falling
back when UnityPy is unavailable.

See `docs/CLICK_WEAPON_HIERARCHY_RECOVERY.md`.

## Current prototype

The prototype now contains reconstructed behavior/presentation for:

- BoxEnemy Body mesh + health fill;
- original enemy palette;
- Arena transform and gameplay camera;
- canonical/private voxel catalogs;
- Time/Weapon Cube delayed pickup lifecycle;
- recovered WidgetGold collection targets;
- private Time/Weapon Cube textures with clean fallback shaders;
- Click Pistol critical rolls and original 4-point tracer;
- ClickCannon charge/fire plans;
- ClickLauncher click threshold/fire plans;
- Flak/Rocket trajectory and collision timing;
- canonical Rocket-tail ParticleSystem behavior and private original texture
  loading, with a clean fallback texture;
- all Hero authoritative combat through PortCore;
- original Hero visual impact rules (no fake travelling Hero bullets):
  exact weapon colors, Outline/FlashOutline and 0.2 s flash timing.

The reconstructed Arena HUD is now the default prototype presentation.
Remaining progression/additive screens are still pending reconstruction.

## Runtime boundary

```text
Unity input/presentation/physics contact
    -> GameState / HeadlessArenaEngine
    -> PortCore authoritative math/state
    -> reconstructed view
```

Unity may report a projectile collision contact. Damage, rewards and Arena
progression are still calculated in PortCore.

## Manual workflow

Build/sync PortCore:

```powershell
.\tools\unity\sync-portcore.ps1
```

or:

```bash
bash tools/unity/sync-portcore.sh
```

Then open `unity/TimeCli` and run:

`TimeCli -> Setup -> Create Arena Prototype Scene`

## Documentation

- `docs/PORTCORE_UNITY_CONTRACT.md`
- `docs/ARENA_PRESENTATION_RECOVERY.md`
- `docs/SPECIAL_CUBE_RECOVERY.md`
- `docs/PROJECTILE_PRESENTATION_RECOVERY.md`
- `docs/HERO_PRESENTATION_RECOVERY.md`
- `docs/PHASE3_STATUS.md`
