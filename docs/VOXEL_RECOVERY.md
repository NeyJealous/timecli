# Voxel model recovery

Time Clickers 1.4.5 does **not** store Arena enemies as ordinary prefab meshes.
The game loads Qubicle Binary voxel models from Unity `TextAsset` resources under
`Resources/Voxels/`.

## Recovered container format

The Android corpus contains one Unity serialized `TextAsset` container per
voxel model. The payload is uncompressed Qubicle Binary:

- QB version: `0x00000101`;
- color format: RGBA;
- one matrix per model;
- uncompressed voxel cells.

Only four non-transparent colors occur in the 1.4.5 corpus:

- red -> Arena block category 0;
- white -> category 1;
- black -> category 2;
- blue -> category 3.

The loader copies voxel positions into four arrays and sets
`VoxelModel.enemyCount` to their combined length.

## Corpus inventory

The APK contains **141 voxel models**.

Observed occupied voxel totals across the corpus:

- red: 3548;
- white: 2900;
- black: 403;
- blue: 1411;
- unknown colors: 0.

Individual models contain between **11 and 120 occupied voxels**.

Exact voxel positions are treated as original asset data and are not committed
to the public repository. They belong in the private reconstruction workspace /
`timecli-originals`.

## Naming / minimum wave

The original loader assigns minimum waves from file names:

- `a_` -> 100
- `b_` -> 250
- `c_` -> 500
- `d_` -> 1000
- `e_` -> 2000
- `f_` -> 4000

If the prefix before the first underscore is numeric, that number becomes both
the model's minimum wave and its exact boss-wave key. `666_...` is special:
it remains a boss key for wave 666 but its general-pool `minWave` is set to
9,999,999.

## HP decomposition / model selection

`VoxelSpawnMath` reconstructs `VoxelLibrary.GetVoxelModelForHP`.

For normal model selection the Arena requests:

- regular/boss-5 waves: 40–60 blocks;
- major boss waves (`wave % 10 == 0`): 100–120 blocks.

After HP is decomposed into red/white/yellow required counts, candidates must:

1. have `minWave <= currentWave`;
2. have `enemyCount >= requiredTotal`;
3. have `enemyCount <= requiredTotal + 10`.

An exact numeric boss model bypasses this candidate filter.

The Artifact transformations are then applied to required counts:

- Yellow -> White;
- White -> Red.

The exact block coordinates can be extracted privately with:

```bash
python tools/voxels/extract_voxel_metadata.py \
  /path/to/apk/assets/bin/Data \
  voxel_models_private.json \
  --include-layout
```

Omit `--include-layout` to emit only non-layout model metadata.
