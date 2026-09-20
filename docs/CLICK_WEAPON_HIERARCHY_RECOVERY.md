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
  unity/TimeCli/Assets/TimeCli/PrivateGenerated/weapon_hierarchy.json
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
