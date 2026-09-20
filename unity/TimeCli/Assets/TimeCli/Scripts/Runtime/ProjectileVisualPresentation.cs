using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TimeCli.UnityRuntime
{
    /// <summary>
    /// Canonical visual values recovered from the Time Clickers 1.4.5
    /// FlakBullet and RocketPrefab serialized prefabs.
    /// </summary>
    internal static class OriginalProjectileVisualPresentation
    {
        public const string FlakMeshResource =
            "TimeCliFlakBulletMesh";
        public const string RocketMeshResource =
            "TimeCliRocketProjectileMesh";
        public const string RocketTextureResource =
            "TimeCliRocketProjectileTexture";
        public const string RocketAudioResource =
            "TimeCliRocketProjectileAudio";
        public const string RocketCubemapResourcePrefix =
            "TimeCliChannelCubemap";

        public static readonly Vector3 FlakRootScale =
            new(1f, 1f, 2f);

        public static readonly Color FlakColor =
            new(
                1f,
                0.8503905534744263f,
                0.498039186000824f,
                1f);

        public static readonly Color RocketColor =
            new(
                0.32869088649749756f,
                1f,
                0f,
                1f);

        public static readonly Vector3 RocketTailLocalPosition =
            new(0f, 0f, -0.24199999868869781f);

        public const float RocketAudioVolume = 1f;
        public const float RocketAudioPitch = 1f;
        public const float RocketAudioMinDistance = 10f;
        public const float RocketAudioMaxDistance = 100f;
    }

    /// <summary>
    /// Loads UnityPy-generated private OBJ text back into Unity coordinates.
    ///
    /// UnityPy's OBJ exporter converts Unity -> right-handed OBJ by negating X
    /// for positions/normals and emitting each triangle as C,B,A. This loader
    /// explicitly reverses both operations so the resulting Mesh is back in
    /// the original Unity coordinate system.
    /// </summary>
    internal static class PrivateProjectileObjMeshLoader
    {
        public static Mesh Load(
            string resourceName,
            string meshName)
        {
            TextAsset asset =
                Resources.Load<TextAsset>(
                    resourceName);

            if (asset == null ||
                string.IsNullOrWhiteSpace(asset.text))
            {
                return null;
            }

            return Parse(
                asset.text,
                meshName);
        }

        internal static Mesh Parse(
            string obj,
            string meshName)
        {
            var vertices =
                new List<Vector3>();
            var normals =
                new List<Vector3>();
            var uv =
                new List<Vector2>();
            var triangles =
                new List<int>();

            string[] lines =
                obj.Replace("\r", string.Empty)
                   .Split('\n');

            for (int lineIndex = 0;
                 lineIndex < lines.Length;
                 lineIndex++)
            {
                string line = lines[lineIndex].Trim();

                if (line.Length == 0 ||
                    line[0] == '#')
                {
                    continue;
                }

                if (line.StartsWith(
                    "v ",
                    StringComparison.Ordinal))
                {
                    string[] parts =
                        SplitFields(line);

                    if (parts.Length < 4)
                        continue;

                    // Undo UnityPy MeshExporter: v -x y z.
                    vertices.Add(
                        new Vector3(
                            -ParseFloat(parts[1]),
                             ParseFloat(parts[2]),
                             ParseFloat(parts[3])));
                    continue;
                }

                if (line.StartsWith(
                    "vn ",
                    StringComparison.Ordinal))
                {
                    string[] parts =
                        SplitFields(line);

                    if (parts.Length < 4)
                        continue;

                    // Undo UnityPy MeshExporter: vn -x y z.
                    normals.Add(
                        new Vector3(
                            -ParseFloat(parts[1]),
                             ParseFloat(parts[2]),
                             ParseFloat(parts[3])));
                    continue;
                }

                if (line.StartsWith(
                    "vt ",
                    StringComparison.Ordinal))
                {
                    string[] parts =
                        SplitFields(line);

                    if (parts.Length < 3)
                        continue;

                    uv.Add(
                        new Vector2(
                            ParseFloat(parts[1]),
                            ParseFloat(parts[2])));
                    continue;
                }

                if (!line.StartsWith(
                    "f ",
                    StringComparison.Ordinal))
                {
                    continue;
                }

                string[] face =
                    SplitFields(line);

                if (face.Length != 4)
                {
                    throw new FormatException(
                        "Private projectile OBJ must contain triangles.");
                }

                int i0 = ParseObjIndex(face[1], vertices.Count);
                int i1 = ParseObjIndex(face[2], vertices.Count);
                int i2 = ParseObjIndex(face[3], vertices.Count);

                // UnityPy emitted C,B,A. Restore A,B,C.
                triangles.Add(i2);
                triangles.Add(i1);
                triangles.Add(i0);
            }

            if (vertices.Count == 0 ||
                triangles.Count == 0)
            {
                throw new FormatException(
                    "Private projectile OBJ did not contain mesh geometry.");
            }

            var mesh =
                new Mesh
                {
                    name = meshName,
                    vertices = vertices.ToArray(),
                    triangles = triangles.ToArray()
                };

            if (uv.Count == vertices.Count)
                mesh.uv = uv.ToArray();

            if (normals.Count == vertices.Count)
                mesh.normals = normals.ToArray();
            else
                mesh.RecalculateNormals();

            mesh.RecalculateBounds();
            return mesh;
        }

        private static string[] SplitFields(
            string line) =>
            line.Split(
                new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries);

        private static float ParseFloat(
            string value) =>
            float.Parse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture);

        private static int ParseObjIndex(
            string token,
            int vertexCount)
        {
            int slash = token.IndexOf('/');
            string value =
                slash >= 0
                    ? token.Substring(0, slash)
                    : token;

            int index =
                int.Parse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture);

            if (index < 0)
                index = vertexCount + index;
            else
                index -= 1;

            if (index < 0 ||
                index >= vertexCount)
            {
                throw new FormatException(
                    "OBJ vertex index is out of range.");
            }

            return index;
        }
    }

    internal static class ProjectileVisualFactory
    {
        private static Material _flakMaterial;
        private static Material _rocketMaterial;
        private static AudioClip _rocketAudioClip;

        public static GameObject CreateFlak()
        {
            var root =
                new GameObject(
                    "ClickCannon_FlakBullet");

            root.transform.localScale =
                OriginalProjectileVisualPresentation
                    .FlakRootScale;

            Mesh mesh =
                PrivateProjectileObjMeshLoader.Load(
                    OriginalProjectileVisualPresentation
                        .FlakMeshResource,
                    "Flak Bullet");

            if (mesh != null)
            {
                MeshFilter filter =
                    root.AddComponent<MeshFilter>();
                MeshRenderer renderer =
                    root.AddComponent<MeshRenderer>();

                filter.sharedMesh = mesh;
                renderer.sharedMaterial =
                    GetFlakMaterial();

                return root;
            }

            AddFallbackGeometry(
                root.transform,
                new Vector3(
                    0.12f,
                    0.12f,
                    0.12f),
                GetFlakMaterial());

            return root;
        }

        public static GameObject CreateRocketProjectile(
            out Transform tailAnchor)
        {
            var root =
                new GameObject(
                    "RocketProjectile");

            Mesh mesh =
                PrivateProjectileObjMeshLoader.Load(
                    OriginalProjectileVisualPresentation
                        .RocketMeshResource,
                    "RocketProjectile");

            if (mesh != null)
            {
                MeshFilter filter =
                    root.AddComponent<MeshFilter>();
                MeshRenderer renderer =
                    root.AddComponent<MeshRenderer>();

                filter.sharedMesh = mesh;
                renderer.sharedMaterial =
                    GetRocketMaterial();
            }
            else
            {
                AddFallbackGeometry(
                    root.transform,
                    new Vector3(
                        0.18f,
                        0.18f,
                        0.42f),
                    GetRocketMaterial());
            }

            var tail =
                new GameObject(
                    "ParticleTail");

            tail.transform.SetParent(
                root.transform,
                false);
            tail.transform.localPosition =
                OriginalProjectileVisualPresentation
                    .RocketTailLocalPosition;

            tailAnchor = tail.transform;
            return root;
        }

        public static void AttachRocketAudio(
            Transform pivot)
        {
            if (pivot == null)
                return;

            if (_rocketAudioClip == null)
            {
                _rocketAudioClip =
                    Resources.Load<AudioClip>(
                        OriginalProjectileVisualPresentation
                            .RocketAudioResource);
            }

            if (_rocketAudioClip == null)
                return;

            var audioObject =
                new GameObject(
                    "RocketAudio");

            audioObject.transform.SetParent(
                pivot,
                false);

            AudioSource source =
                audioObject.AddComponent<AudioSource>();

            source.clip = _rocketAudioClip;
            source.playOnAwake = true;
            source.volume =
                OriginalProjectileVisualPresentation
                    .RocketAudioVolume;
            source.pitch =
                OriginalProjectileVisualPresentation
                    .RocketAudioPitch;
            source.rolloffMode =
                AudioRolloffMode.Logarithmic;
            source.minDistance =
                OriginalProjectileVisualPresentation
                    .RocketAudioMinDistance;
            source.maxDistance =
                OriginalProjectileVisualPresentation
                    .RocketAudioMaxDistance;
            source.spatialBlend = 1f;

            // Runtime-created AudioSource fields are assigned after AddComponent,
            // so explicitly Play to match the prefab's playOnAwake=true.
            source.Play();
        }

        private static Material GetFlakMaterial()
        {
            if (_flakMaterial != null)
                return _flakMaterial;

            Shader shader =
                Shader.Find(
                    "TimeCli/ProjectileUnlitColor");

            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            _flakMaterial =
                new Material(shader)
                {
                    name =
                        "TimeCli_Reconstructed_PlasmaBeam",
                    color =
                        OriginalProjectileVisualPresentation
                            .FlakColor
                };

            return _flakMaterial;
        }

        private static Material GetRocketMaterial()
        {
            if (_rocketMaterial != null)
                return _rocketMaterial;

            Shader shader =
                Shader.Find(
                    "TimeCli/WeaponDiffuseColor");

            if (shader == null)
                shader = Shader.Find("Unlit/Texture");

            _rocketMaterial =
                new Material(shader)
                {
                    name =
                        "TimeCli_Reconstructed_Rocket",
                    color =
                        OriginalProjectileVisualPresentation
                            .RocketColor
                };

            Texture2D texture =
                Resources.Load<Texture2D>(
                    OriginalProjectileVisualPresentation
                        .RocketTextureResource);

            if (texture != null)
            {
                texture.filterMode =
                    FilterMode.Bilinear;
                texture.wrapMode =
                    TextureWrapMode.Repeat;
                texture.anisoLevel = 1;

                _rocketMaterial.mainTexture =
                    texture;
            }

            Cubemap cubemap =
                PrivateCubemapLoader.Load(
                    OriginalProjectileVisualPresentation
                        .RocketCubemapResourcePrefix);

            if (cubemap != null)
            {
                _rocketMaterial.SetTexture(
                    "_CubeMap",
                    cubemap);
            }

            return _rocketMaterial;
        }

        private static void AddFallbackGeometry(
            Transform parent,
            Vector3 scale,
            Material material)
        {
            GameObject fallback =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere);

            fallback.name =
                "CleanFallbackGeometry";
            fallback.transform.SetParent(
                parent,
                false);
            fallback.transform.localPosition =
                Vector3.zero;
            fallback.transform.localScale =
                scale;

            Collider collider =
                fallback.GetComponent<Collider>();

            if (collider != null)
                UnityEngine.Object.Destroy(collider);

            Renderer renderer =
                fallback.GetComponent<Renderer>();

            if (renderer != null)
                renderer.sharedMaterial = material;
        }
    }
}
