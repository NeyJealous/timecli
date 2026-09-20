using TimeClickers.PortCore;
using UnityEngine;

namespace TimeCli.UnityRuntime
{
    /// <summary>
    /// Procedural reconstruction of the original BoxEnemy "Body" mesh.
    /// The mesh/health-bar geometry is rebuilt from the 1.4.5 serialized asset
    /// instead of shipping the original mesh binary.
    /// </summary>
    public sealed class OriginalBlockGeometry : MonoBehaviour
    {
        private static Material _normalMaterial;
        private static Material _rainbowMaterial;
        private static Material _timeCubeMaterial;
        private static Material _weaponCubeMaterial;

        private static readonly int[] Triangles =
        {
            0,1,2, 0,2,3, 0,4,1, 0,5,4, 3,5,0,
            1,4,6, 1,6,2, 2,6,7, 2,7,3, 3,7,5,

            8,9,10, 8,10,11, 12,8,11, 12,13,8, 13,9,8,
            14,12,11, 14,11,10, 15,14,10, 15,10,9, 13,15,9,

            16,17,18, 16,18,19, 16,19,20, 16,20,21,
            17,16,21, 21,20,22, 19,18,22, 19,22,20,
            21,22,23, 18,17,23, 18,23,22, 17,21,23
        };

        private static readonly Vector3[] BaseVertices =
        {
            new( 0.4f,  0.4f, -0.4f),
            new(-0.4f,  0.4f, -0.4f),
            new(-0.4f,  0.4f,  0.4f),
            new( 0.4f,  0.4f,  0.4f),
            new(-0.4f,  0.0f, -0.4f),
            new( 0.4f,  0.0f, -0.4f),
            new(-0.4f,  0.0f,  0.4f),
            new( 0.4f,  0.0f,  0.4f),

            new( 0.4f, -0.4f, -0.4f),
            new( 0.4f, -0.4f,  0.4f),
            new(-0.4f, -0.4f,  0.4f),
            new(-0.4f, -0.4f, -0.4f),
            new(-0.4f,  0.0f, -0.4f),
            new( 0.4f,  0.0f, -0.4f),
            new(-0.4f,  0.0f,  0.4f),
            new( 0.4f,  0.0f,  0.4f),

            new( 0.5f, -0.5f,  0.5f),
            new( 0.5f, -0.5f, -0.5f),
            new(-0.5f, -0.5f, -0.5f),
            new(-0.5f, -0.5f,  0.5f),
            new(-0.5f,  0.5f,  0.5f),
            new( 0.5f,  0.5f,  0.5f),
            new(-0.5f,  0.5f, -0.5f),
            new( 0.5f,  0.5f, -0.5f)
        };

        private static readonly Vector2[] Uv = BuildUv();

        private Mesh _mesh;
        private MeshRenderer _renderer;
        private Vector3[] _vertices;
        private Color[] _colors;

        public void Initialize(EnemyBlockState state)
        {
            EnsureMesh();
            ApplyEnemyType(state.EnemyType);
            RefreshHealth(state);
        }

        public void RefreshHealth(EnemyBlockState state)
        {
            if (_mesh == null || _vertices == null)
                return;

            float normalized = state.MaxHealth <= 0.0
                ? 0f
                : Mathf.Clamp01((float)(state.Health / state.MaxHealth));

            // Exact GeometryHealth.SetHealthNormalized behavior:
            // y = (healthNormalized - 0.5) * 0.8
            float y = (normalized - 0.5f) * 0.8f;

            int[] moving = { 4, 5, 6, 7, 12, 13, 14, 15 };
            for (int i = 0; i < moving.Length; i++)
            {
                int index = moving[i];
                Vector3 v = _vertices[index];
                _vertices[index] = new Vector3(v.x, y, v.z);
            }

            _mesh.vertices = _vertices;
            _mesh.RecalculateBounds();
        }

        private void EnsureMesh()
        {
            if (_mesh != null)
                return;

            _vertices = (Vector3[])BaseVertices.Clone();
            _colors = new Color[BaseVertices.Length];

            _mesh = new Mesh
            {
                name = "TimeCli_Reconstructed_BoxEnemy_Body"
            };
            _mesh.vertices = _vertices;
            _mesh.triangles = Triangles;
            _mesh.uv = Uv;
            _mesh.colors = _colors;
            _mesh.RecalculateBounds();

            var filter = GetComponent<MeshFilter>();
            if (filter == null)
                filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = _mesh;

            _renderer = GetComponent<MeshRenderer>();
            if (_renderer == null)
                _renderer = gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = GetMaterial(EnemyType.Red);

            var collider = GetComponent<BoxCollider>();
            if (collider == null)
                collider = gameObject.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = Vector3.one;
        }

        private void ApplyEnemyType(EnemyType type)
        {
            Color body = OriginalEnemyPalette.Get(type);
            Color healthIndicator = OriginalEnemyPalette.HealthIndicator;

            for (int i = 0; i < 8; i++)
                _colors[i] = healthIndicator;

            for (int i = 8; i < 16; i++)
                _colors[i] = body;

            for (int i = 16; i < 24; i++)
                _colors[i] = new Color(0f, 0f, 0f, 0f);

            _mesh.colors = _colors;

            if (_renderer != null)
                _renderer.sharedMaterial = GetMaterial(type);
        }

        private static Material GetMaterial(EnemyType type)
        {
            return type switch
            {
                EnemyType.Rainbow => GetOrCreateMaterial(
                    ref _rainbowMaterial,
                    "TimeCli/RainbowEnemy",
                    "TimeCli_Reconstructed_RainbowEnemy"),
                EnemyType.TimeCube => GetOrCreateMaterial(
                    ref _timeCubeMaterial,
                    "TimeCli/TimeCubeEnemy",
                    "TimeCli_Reconstructed_TimeCubeEnemy"),
                EnemyType.WeaponCube => GetWeaponCubeMaterial(),
                _ => GetOrCreateMaterial(
                    ref _normalMaterial,
                    "TimeCli/BlockVertexColor",
                    "TimeCli_Reconstructed_SimpleEnemy")
            };
        }

        private static Material GetWeaponCubeMaterial()
        {
            if (_weaponCubeMaterial != null)
                return _weaponCubeMaterial;

            _weaponCubeMaterial = GetOrCreateMaterial(
                ref _weaponCubeMaterial,
                "TimeCli/TimeCubeEnemy",
                "TimeCli_Reconstructed_WeaponCubeEnemy");

            // Original BoxEnemy.SetMaxHP uses the TimeCube shader for
            // WeaponCube and then overrides material.mainTexture with
            // BoxEnemy.weaponCubeTex. The old texture remains private
            // original-derived data; if supplied as a Resources asset, use it.
            Texture2D texture =
                Resources.Load<Texture2D>("TimeCliWeaponCubeTexture");

            if (texture != null)
                _weaponCubeMaterial.mainTexture = texture;

            return _weaponCubeMaterial;
        }

        private static Material GetOrCreateMaterial(
            ref Material cache,
            string shaderName,
            string materialName)
        {
            if (cache != null)
                return cache;

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                shader = Shader.Find("TimeCli/BlockVertexColor");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            cache = new Material(shader)
            {
                name = materialName
            };

            return cache;
        }

        private static Vector2[] BuildUv()
        {
            var result = new Vector2[24];

            for (int i = 0; i < 8; i++)
                result[i] = new Vector2(
                    0.1726345419883728f,
                    0.5736696720123291f);

            for (int i = 8; i < 16; i++)
                result[i] = new Vector2(
                    0.24328094720840454f,
                    0.5143935084342957f);

            for (int i = 16; i < 24; i++)
                result[i] = new Vector2(
                    0.7807137370109558f,
                    0.5407401323318481f);

            return result;
        }
    }

    public static class OriginalEnemyPalette
    {
        public static readonly Color Red =
            new(1f, 0f, 0f, 1f);

        public static readonly Color White =
            new(1f, 1f, 1f, 1f);

        public static readonly Color Yellow =
            new(0.9960784912109375f, 1f, 0f, 1f);

        // Original Rainbow base color is white; the old Rainbow shader supplied
        // the animated color effect.
        public static readonly Color Rainbow =
            new(1f, 1f, 1f, 1f);

        public static readonly Color TimeCube =
            new(0.24705880880355835f, 0.5532689690589905f, 1f, 1f);

        public static readonly Color WeaponCube =
            new(0.49454373121261597f, 1f, 0.24705880880355835f, 1f);

        public static readonly Color HealthIndicator =
            new(0.1921568661928177f, 0f, 0f, 0.125490203499794f);

        public static Color Get(EnemyType type)
        {
            return type switch
            {
                EnemyType.Red => Red,
                EnemyType.White => White,
                EnemyType.Yellow => Yellow,
                EnemyType.Rainbow => Rainbow,
                EnemyType.TimeCube => TimeCube,
                EnemyType.WeaponCube => WeaponCube,
                _ => White
            };
        }
    }
}
