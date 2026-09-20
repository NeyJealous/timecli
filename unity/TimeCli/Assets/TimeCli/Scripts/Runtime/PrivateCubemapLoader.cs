using UnityEngine;

namespace TimeCli.UnityRuntime
{
    /// <summary>
    /// Rebuilds an original-derived Unity cubemap from six private PNG faces.
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

        public static Cubemap Load(
            string resourcePrefix)
        {
            Texture2D positiveX =
                Resources.Load<Texture2D>(
                    resourcePrefix + "_PositiveX");
            Texture2D negativeX =
                Resources.Load<Texture2D>(
                    resourcePrefix + "_NegativeX");
            Texture2D positiveY =
                Resources.Load<Texture2D>(
                    resourcePrefix + "_PositiveY");
            Texture2D negativeY =
                Resources.Load<Texture2D>(
                    resourcePrefix + "_NegativeY");
            Texture2D positiveZ =
                Resources.Load<Texture2D>(
                    resourcePrefix + "_PositiveZ");
            Texture2D negativeZ =
                Resources.Load<Texture2D>(
                    resourcePrefix + "_NegativeZ");

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

            return cubemap;
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
