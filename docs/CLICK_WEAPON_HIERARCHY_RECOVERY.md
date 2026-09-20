# Click-weapon hierarchy recovery

Status: **forensic extractor ready; exact hierarchy values not yet promoted**.

The original 1.4.5 Arena serialized data contains named objects including:

- `HeroWeapons`
- `ClickerPistol`
- `ClickCannon`
- `ClickLauncher`

Earlier reconstruction recovered the authored rest fire-spot positions, but not
the exact parent/child transform chain or aim/recoil pivot transforms. Those
values must not be guessed.

## Private extraction

Install the forensic dependency locally:

```bash
python -m pip install UnityPy
```

Then run the public extractor against either a reconstructed `levelN` scene
file or the original APK `assets/bin/Data` directory:

```bash
python tools/unity/extract-private-weapon-hierarchy.py \
  /path/to/apk/assets/bin/Data \
  unity/TimeCli/Assets/TimeCli/PrivateGenerated/Resources/TimeCliWeaponHierarchy.json
```

The tool scans all `levelN` scenes, including split `levelN.split0..N`
inputs. When one of the target GameObjects is found it records:

- exact GameObject and Transform path IDs;
- full ancestor path;
- child transforms up to six levels deep by default;
- local position;
- local rotation quaternion;
- local scale;
- root order;
- layer / active state;
- attached component path IDs and built-in component types.

The output is marked `originalDerived: true` and belongs only in the ignored
private workspace. It must not be committed to the public repository.

Validate it with:

```bash
python tools/unity/validate-private-weapon-hierarchy.py \
  unity/TimeCli/Assets/TimeCli/PrivateGenerated/Resources/TimeCliWeaponHierarchy.json
```

The extractor reconstructs world transforms from the serialized
local-position / quaternion / scale chain and matches descendants against the
already recovered original world-space fire spots:

- Pistol: `(0.600000024, 0.301000000, -7.30099994)`
- Cannon: `(0.06999986, 0.31599993, -7.30079996)`
- Launcher: `(-0.59999985, 0.20599997, -7.49800003)`

The validator rejects malformed vectors/quaternions, missing parent nodes,
transform cycles, reports without all three weapons, and any nearest fire-spot
transform farther than **0.05 Unity units** from the recovered coordinate.
It also prints likely fire/muzzle/barrel descendants for manual review.

When the private report is present as the `TimeCliWeaponHierarchy` Resource,
the Unity runtime reconstructs the transform-only hierarchy and binds the three
verified transform IDs to `ArenaRuntimeController`. Original meshes,
materials, scripts and binary components are not loaded.

## Batch integration

When `ORIGINAL_DATA_SOURCE` / `-OriginalDataSource` is supplied, the Arena
setup scripts now attempt hierarchy extraction automatically when UnityPy is
installed.

To make exact hierarchy recovery mandatory:

Windows:

```powershell
.\tools\unity\create-arena-prototype.ps1 \
  -OriginalDataSource "C:\path\to\apk\assets\bin\Data" \
  -RequireWeaponHierarchy
```

macOS/Linux:

```bash
UNITY_PATH="/path/to/Unity" \
ORIGINAL_DATA_SOURCE="/path/to/apk/assets/bin/Data" \
REQUIRE_WEAPON_HIERARCHY=1 \
bash tools/unity/create-arena-prototype.sh
```

## Promotion gate

The hierarchy/pivot reconstruction can be promoted into public clean-room
constants/code only after:

1. the extractor runs successfully against the canonical 1.4.5 serialized
   scene;
2. target paths for Pistol/Cannon/Launcher are reviewed;
3. Firespot / aim / recoil pivot nodes are identified from the report;
4. the resulting constants are covered by Unity-adapter preflight tests.

Until then, the existing recovered rest fire-spot positions remain the
presentation fallback.
