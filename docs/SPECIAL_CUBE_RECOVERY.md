# Special enemy / cube pickup recovery — Time Clickers 1.4.5

This document records behavior recovered directly from the Android 1.4.5
managed IL and serialized shader data.

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

The modern project uses clean replacements:

- `TimeCli/BlockVertexColor`
- `TimeCli/RainbowEnemy`
- `TimeCli/TimeCubeEnemy`

The obsolete compiled Unity 5 shader binaries are not copied into the modern
project.

For WeaponCube, `OriginalBlockGeometry` supports the original texture override
semantics through an optional private Resources texture named:

`TimeCliWeaponCubeTexture`

If that private original-derived texture is absent, the clean TimeCube shader
uses its procedural fallback presentation.

## Special block death

`BoxEnemy.SpawnGold` does not immediately add cube currency.

### TimeCube block

For `timeCubeCount` times:

1. instantiate `timeCubePrefab` at the destroyed block position/rotation;
2. make the pickup face `Camera.main`;
3. blend its rotation 10% toward
   `Quaternion.LookRotation(Random.onUnitSphere)`;
4. set `TimeCube.timeCubeValue = 1`.

After all pickup objects are spawned, the original updates Time Cube statistics
for the total count.

### WeaponCube block

The same flow is used with `weaponCubePrefab` and
`WeaponCube.weaponCubeValue = 1`.

The Weapon Cube statistics total is updated at spawn time.

## Pickup lifecycle

TimeCube and WeaponCube use equivalent coroutine timing.

### Spawn / FloatAway

At Start:

```text
start FloatAway()
wait 2.0 seconds
BoxCollider.size *= 10
wait 3.0 seconds
Collect(false)
```

`FloatAway` chooses:

```text
speed = Random.Range(2.5, 6.0)
```

Every frame while `speed > 0`:

```text
position += transform.forward * speed * deltaTime
speed -= deltaTime * 2
```

Thus the pickup launches away from the destroyed block, decelerates to rest,
becomes much easier to click after two seconds, and auto-collects at five
seconds if the player has not collected it manually.

## Collect

Collection is ignored when `isCollected` is already true.

When collection begins:

1. set `isCollected = true`;
2. optionally play pickup audio at volume `0.75`;
3. stop the FloatAway coroutine;
4. destroy the pickup BoxCollider;
5. configure `TweenPosition.duration = 0.5`;
6. set Tween start to current world position;
7. set Tween end to the relevant UI collection point;
8. play TweenPosition;
9. wait 0.5 seconds;
10. credit the currency;
11. destroy the pickup GameObject.

Collection targets:

- TimeCube -> `WidgetGold.GetTimeCubeCollectionPoint()`
- WeaponCube -> `WidgetGold.GetWeaponCubeCollectionPoint()`

Currency is therefore credited **after the 0.5-second collection tween**, not
when the special BoxEnemy dies.

## PortCore correction

PortCore API **1.1.0** now models this deferred lifecycle.

`SpecialCubePickupQueue` owns one state object per spawned cube and reproduces:

- 2.0 s collider-expansion time;
- 5.0 s auto-collect trigger;
- manual collection;
- 0.5 s collection completion;
- Time Cube pending-reward credit on completion;
- Weapon Cube bank credit on completion.

`GameState.UpdateCubePickups(nowSeconds)` advances the queue.

`GameState.TryCollectCubePickup(id, nowSeconds)` starts manual collection.

Regression coverage confirms both the auto and manual timing paths.

## Unity presentation

`SpecialCubePickupView` reproduces the visible motion:

- camera-facing spawn;
- 10% random rotation perturbation;
- random 2.5–6.0 launch speed;
- 2 units/s² speed decay;
- x10 collider enlargement after two seconds;
- collider destruction on collection;
- 0.5-second move toward a collection anchor.

Exact original HUD collection-anchor transforms are still pending scene-layout
recovery; until then the Unity adapter uses explicit optional anchor Transforms
or a visible world-space fallback.
