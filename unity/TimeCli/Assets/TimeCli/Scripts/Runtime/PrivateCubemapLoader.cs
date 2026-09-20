using System.Collections.Generic;
using UnityEngine;

namespace TimeCli.UnityRuntime
{
    /// <summary>
    /// Rebuilds an original-derived Unity cubemap from six private PNG payloads
    /// stored as TextAsset .bytes resources so runtime decoding does not depend
    /// on Unity TextureImporter Read/Write settings.
    ///
    /// Serialized Time Clickers 1.4.5 Cubemap image data stores six complete
    /// ETC_RGB4 mip chains in CubemapFace enum order:
    /// +X, -X, +Y, -Y, +Z, -Z. The extraction tools decode the base level of
    /// each face into ignored Resources; modern Unity regenerates the mip chain
    /// after the exact face pixels are restored.
    /// </summary>
    internal static class PrivateCubemapLoader
    {
        public const int CanonicalSize = 64;

        private static readonly Dictionary<string, Cubemap> Cache =
            new();

        public static Cubemap Load(
            string resourcePrefix)
        {
            if (Cache.TryGetValue(
                    resourcePrefix,
                    out Cubemap cached))
            {
                return cached;
            }
            Texture2D positiveX =
                LoadFace(
                    resourcePrefix,
                    "PositiveX");
            Texture2D negativeX =
                LoadFace(
                    resourcePrefix,
                    "NegativeX");
            Texture2D positiveY =
                LoadFace(
                    resourcePrefix,
                    "PositiveY");
            Texture2D negativeY =
                LoadFace(
                    resourcePrefix,
                    "NegativeY");
            Texture2D positiveZ =
                LoadFace(
                    resourcePrefix,
                    "PositiveZ");
            Texture2D negativeZ =
                LoadFace(
                    resourcePrefix,
                    "NegativeZ");

            if (positiveX == null ||
                negativeX == null ||
                positiveY == null ||
                negativeY == null ||
                positiveZ == null ||
                negativeZ == null)
            {
                return null;
            }

            var cubemap =
                new Cubemap(
                    CanonicalSize,
                    TextureFormat.RGBA32,
                    true)
                {
                    name =
                        resourcePrefix +
                        "_Reconstructed",
                    filterMode =
                        FilterMode.Bilinear,
                    wrapMode =
                        TextureWrapMode.Clamp,
                    anisoLevel = 1
                };

            SetFace(
                cubemap,
                CubemapFace.PositiveX,
                positiveX);
            SetFace(
                cubemap,
                CubemapFace.NegativeX,
                negativeX);
            SetFace(
                cubemap,
                CubemapFace.PositiveY,
                positiveY);
            SetFace(
                cubemap,
                CubemapFace.NegativeY,
                negativeY);
            SetFace(
                cubemap,
                CubemapFace.PositiveZ,
                positiveZ);
            SetFace(
                cubemap,
                CubemapFace.NegativeZ,
                negativeZ);

            cubemap.Apply(
                true,
                false);

            Cache[resourcePrefix] =
                cubemap;

            return cubemap;
        }

        private static Texture2D LoadFace(
            string resourcePrefix,
            string faceName)
        {
            TextAsset asset =
                Resources.Load<TextAsset>(
                    resourcePrefix +
                    "_" +
                    faceName);

            if (asset == null ||
                asset.bytes == null ||
                asset.bytes.Length == 0)
            {
                return null;
            }

            var texture =
                new Texture2D(2, 2)
                {
                    name =
                        resourcePrefix +
                        "_" +
                        faceName +
                        "_Decoded",
                    filterMode =
                        FilterMode.Bilinear,
                    wrapMode =
                        TextureWrapMode.Clamp,
                    anisoLevel = 1
                };

            if (!ImageConversion.LoadImage(
                    texture,
                    asset.bytes,
                    false))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            return texture;
        }

        private static void SetFace(
            Cubemap cubemap,
            CubemapFace face,
            Texture2D texture)
        {
            if (texture.width != CanonicalSize ||
                texture.height != CanonicalSize)
            {
                Debug.LogWarning(
                    "TimeCli cubemap face " +
                    texture.name +
                    " has unexpected size " +
                    texture.width +
                    "x" +
                    texture.height +
                    "; expected " +
                    CanonicalSize +
                    "x" +
                    CanonicalSize +
                    ".");

                return;
            }

            cubemap.SetPixels(
                texture.GetPixels(),
                face,
                0);
        }
    }
}
