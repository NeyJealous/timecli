# Click-weapon hierarchy recovery

Status: **canonical hierarchy and aim/vertical transform behavior promoted**.

The original 1.4.5 Arena serialized data contains named objects including:

- `HeroWeapons`
- `ClickerPistol`
- `ClickCannon`
- `ClickLauncher`

The canonical 1.4.5 APK was re-read directly. The exact parent/child transform
chain, the three Firespots, the shared aim Pivot, per-weapon Vertical transforms,
and ClickerWeapon aim/vertical IL behavior are now confirmed and promoted.

The original script performs `gunPivot.LookAt` toward
`Camera.ScreenPointToRay(crosshair)` at distance 100, then sets
`verticalT.localPosition.y` to the negative value of the serialized
`verticalCurve` evaluated at `1 - tapY / Screen.height`.

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

## Promoted canonical hierarchy

Confirmed paths:

- `_SceneArenaRoot/ClickWeapons/ClickerPistol/Pivot/Vertical/Pistol/Firespot`
- `_SceneArenaRoot/ClickWeapons/ClickCannon/Pivot/Vertical/s20/Firespot`
- `_SceneArenaRoot/ClickWeapons/ClickLauncher/Pivot/Vertical/launcher/Firespot`

All three weapon roots use the same authored Pivot local position:
`(0, -0.135000005, -0.521000028)`.

The serialized vertical curves contain exactly two keys. Pistol ends at about
`0.298796117`; Cannon/Launcher share the heavier curve ending at
`0.401019126`. Their exact times, values and tangents now live in
`OriginalClickWeaponPresentation` and are regression-tested by the Unity
adapter preflight.

The private extractor remains useful as an independent forensic verifier and
for future original-derived nodes/assets, but runtime aiming no longer depends
on the private hierarchy JSON.


## Shoot Animator recovery

The canonical AnimatorControllers are now decoded as well.

Recovered model Animator bindings:

- Pistol model `Pistol`, Animator path ID 1509, controller 439.
  `ClickerPistolShoot` lasts **0.0833333358 s** at **120 Hz** and animates
  only the child transform `top1` (binding hash `4168293638`).
- Cannon model `s20`, Animator path ID 1511, controller 437.
  `ClickCannonFire` lasts **0.0833333358 s** at **60 Hz** and animates the
  model-root position while keeping its recovered quaternion constant.
- Launcher model `launcher`, Animator path ID 1484, controller 451.
  `RocketLauncher` lasts **0.1666666716 s** at **60 Hz** and animates the
  model-root position plus all four quaternion components.

The Pistol binding hash was resolved against the actual model hierarchy:
`CRC32("top1") = 4168293638`, so the recoil is applied to the slide rather
than moving the whole weapon.

The compressed AnimationClip streams were decoded into exact Unity
`AnimationCurve` keyframes/tangents in
`OriginalClickWeaponShootAnimation.cs`. Runtime firing now starts those
curves directly, avoiding any dependency on the old Unity 5.4
AnimatorController.

The original Appear/Disappear clips are identified and timed but are not yet
promoted; model meshes/materials are also a separate reconstruction target.
