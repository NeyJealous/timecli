# Arena presentation recovery — Time Clickers 1.4.5

This note records visual/runtime values recovered from the Android 1.4.5 Unity
serialized data. These values belong to the Unity presentation layer and are
not part of PortCore gameplay state.

## Arena root transform

Original GameObject: `Arena`

```text
localPosition = (0, -3.3399999, 5.3499999)
localRotation ≈ Euler(0, -59.999976, 0)
localScale    = (1, 1, 1)
```

`Arena.SpawnBlock` parents every BoxEnemy under the Arena component transform
and then assigns the calculated voxel position as `localPosition`.

## Game Camera

Original GameObject: `Game Camera`

```text
position      = (0, 1, -10)
rotation      ≈ Euler(-6.469241, 0, 0)
projection    = perspective
fieldOfView   = 60
nearClipPlane = 0.3
farClipPlane  = 1000
depth         = -2
clearFlags    = SolidColor
background    = black
```

The original scene also contains separate UI/overlay cameras. The first
prototype intentionally uses only the gameplay camera; UI camera reconstruction
belongs to the later UI pass.

## BoxEnemy prefab geometry

Original prefab GameObject: `BoxEnemy`

Recovered transform/collider:

```text
localScale       = (1, 1, 1)
BoxCollider.size = (1, 1, 1)
BoxCollider.center = (0, 0, 0)
```

The original MeshFilter references a mesh named `Body` with:

- 24 vertices;
- 96 indices / 32 triangles;
- position + vertex color + UV channels;
- no normal channel in the serialized vertex stream.

The reconstructed procedural mesh is implemented by
`OriginalBlockGeometry.cs`.

### Health geometry

The mesh contains three shells/sections:

- vertices 0..7 — health indicator section;
- vertices 8..15 — body-color section;
- vertices 16..23 — outer outline shell.

Every refresh computes:

```text
y = (healthNormalized - 0.5) * 0.8
```

and assigns that Y coordinate to vertices:

`4, 5, 6, 7, 12, 13, 14, 15`.

At full HP, the body fills the inner cube. As HP falls, the fixed dark-red
health-indicator section replaces it from the top.

## Original enemy palette

Serialized `BoxEnemy.enemyColors`:

| EnemyType | RGBA |
|---|---|
| Red | `(1, 0, 0, 1)` |
| White | `(1, 1, 1, 1)` |
| Yellow | `(0.9960784912, 1, 0, 1)` |
| Rainbow base | `(1, 1, 1, 1)` |
| Time Cube | `(0.2470588088, 0.5532689691, 1, 1)` |
| Weapon Cube | `(0.4945437312, 1, 0.2470588088, 1)` |

The Rainbow color is white because the original switches the material to the
Rainbow shader for EnemyType 3.

The serialized GeometryHealth health-indicator color is:

```text
(0.1921568662, 0, 0, 0.1254902035)
```

## Original material/shader references

The BoxEnemy MeshRenderer uses material:

`OutlineAndBody`

whose shader is:

`Custom/SimpleEnemy`

with properties:

- `_Color`
- `_MainTex`

The old shader program is platform-compiled Unity 5 data, so the modern project
uses a clean reconstructed vertex-color shader for now. Exact Rainbow/TimeCube
shader presentation will be restored separately rather than copying obsolete
compiled shader binaries.

## Prototype status

The Unity prototype now uses:

- the recovered Arena transform;
- the recovered gameplay camera;
- unit voxel spacing;
- a procedural reconstruction of the original 24-vertex BoxEnemy mesh;
- the original enemy palette;
- the original health-fill vertex deformation.

This removes the earlier arbitrary primitive-cube presentation while keeping
all gameplay decisions in PortCore.
