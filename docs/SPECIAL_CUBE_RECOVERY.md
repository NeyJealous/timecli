# Special enemy / cube pickup recovery — Time Clickers 1.4.5

This document records behavior recovered directly from the Android 1.4.5
managed IL and serialized Unity data.

## BoxEnemy shader switching

`BoxEnemy.SetMaxHP` first assigns the serialized enemy body color, then applies
special presentation by `EnemyType`:

```text
3 = Rainbow
    renderer.material.shader = rainbowShader

4 = TimeCube
    renderer.material.shader = timeCubeShader

5 = WeaponCube
    renderer.material.shader = timeCubeShader
    renderer.material.mainTexture = weaponCubeTex
```

Recovered original shader asset names:

- `Custom/SimpleEnemy`
- `Custom/RainbowEnemy`
- `Custom/TimeCubeEnemy`

Modern clean replacements:

- `TimeCli/BlockVertexColor`
- `TimeCli/RainbowEnemy`
- `TimeCli/TimeCubeEnemy`
- `TimeCli/PickupTexture`

The obsolete Unity 5 compiled shader binaries are not copied into the public
modern project.

## Recovered private texture identities

`sharedassets1` contains the original-derived textures required for the
special cube presentation:

```text
TimeCube pickup:
  64x64 RGB565, 7 mip levels

WeaponCube pickup:
  64x64 RGB24, 7 mip levels

WeaponCube BoxEnemy mask:
  64x64 Alpha8, 7 mip levels
```

Public extraction tool:

```bash
python tools/unity/extract-private-special-textures.py \
  /path/to/Data-or-sharedassets1.assets \
  unity/TimeCli/Assets/TimeCli/PrivateGenerated/Resources
```

Generated private Resource names:

- `TimeCliTimeCubePickupTexture`
- `TimeCliWeaponCubePickupTexture`
- `TimeCliWeaponCubeTexture`

These files remain under ignored `PrivateGenerated/` and are not committed.

## Special block death

`BoxEnemy.SpawnGold` does not immediately add cube currency.

For TimeCube or WeaponCube, the original instantiates **one pickup GameObject
per cube**, each with value 1, at the destroyed block position/rotation.

The pickup faces `Camera.main`, then its rotation is blended 10% toward
`Quaternion.LookRotation(Random.onUnitSphere)`.

## Pickup prefab

Recovered shared prefab properties:

```text
TimeCube localScale   = 0.25
WeaponCube localScale = 0.25
```

Both use a cube mesh and their own material/texture.

## Pickup lifecycle

At Start:

```text
start FloatAway()
wait 2.0 seconds
BoxCollider.size *= 10
wait 3.0 seconds
Collect(false)
```

FloatAway:

```text
speed = Random.Range(2.5, 6.0)

while speed > 0:
    position += transform.forward * speed * deltaTime
    speed -= deltaTime * 2
```

So auto-collection begins five seconds after spawn.

## Collect

When collection begins:

1. mark collected;
2. optionally play pickup audio at volume 0.75;
3. stop FloatAway;
4. destroy BoxCollider;
5. configure `TweenPosition.duration = 0.5`;
6. tween from current world position to WidgetGold collection point;
7. wait 0.5 s;
8. credit currency;
9. destroy pickup GameObject.

Currency is credited **after** the tween.

## Exact active WidgetGold collection targets

The active hierarchy is:

`_SceneArenaRoot/WidgetGold`

A second WidgetGold exists below an inactive StatsWidget branch and is not the
gameplay target.

Recovered active WidgetGold transform:

```text
initial position =
  (17, -5.900000095, 14.100000381)

rotation quaternion =
  (-0.256685704,
    0.730564594,
   -0.546390772,
   -0.319131702)

scale =
  (0.173205093,
   0.173205048,
   0.173205048)
```

Its responsive placement uses:

```text
Camera.main.ViewportToWorldPoint(
    0.9,
    0.1,
    23.200000763)
```

Recovered local child positions:

```text
TimeCubeCollectionPoint =
  (-4.949999809,
    51.209999084,
   -47.659999847)

WeaponCubeCollectionPoint =
  (-23.180000305,
     36.259998322,
    -68.379997253)
```

`OriginalWidgetGoldPresentation` reconstructs those targets.

## PortCore

The deferred lifecycle was introduced in API 1.1 and remains part of current
PortCore 1.2.

`SpecialCubePickupQueue` reproduces:

- 2.0 s collider expansion;
- 5.0 s auto-collect trigger;
- manual collection;
- 0.5 s collection completion;
- Time Cube pending-reward credit on completion;
- Weapon Cube bank credit on completion.

## Unity presentation

`SpecialCubePickupView` reproduces:

- original 0.25 scale;
- camera-facing spawn;
- 10% random rotation perturbation;
- 2.5–6.0 launch speed;
- 2 units/s² speed decay;
- x10 collider enlargement;
- collider destruction on collect;
- 0.5-second move to the recovered WidgetGold target;
- private original pickup textures when available;
- procedural clean fallback when private textures are absent.
