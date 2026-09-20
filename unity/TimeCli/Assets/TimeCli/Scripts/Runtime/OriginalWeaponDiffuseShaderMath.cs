using UnityEngine;

namespace TimeCli.UnityRuntime
{
    /// <summary>
    /// CPU reference for the exact Time Clickers 1.4.5
    /// Custom/Weapon Diffuse Color GLES fragment program.
    ///
    /// This is intentionally kept next to the reconstructed shader so
    /// compile/preflight tests can lock the recovered blend equation even
    /// though the real shader itself is compiled only by Unity.
    /// </summary>
    internal static class OriginalWeaponDiffuseShaderMath
    {
        public static Color Evaluate(
            Color texcol,
            Color cubeCol,
            Color color)
        {
            float mask =
                texcol.a;

            float baseR =
                texcol.r * cubeCol.r;
            float baseG =
                texcol.g * cubeCol.g;
            float baseB =
                texcol.b * cubeCol.b;
            float baseA =
                texcol.a;

            float mixedR =
                baseR +
                (baseR * color.a - baseR) *
                mask;
            float mixedG =
                baseG +
                (baseG * color.a - baseG) *
                mask;
            float mixedB =
                baseB +
                (baseB * color.a - baseB) *
                mask;
            float mixedA =
                baseA +
                (baseA * color.a - baseA) *
                mask;

            return
                new Color(
                    mixedR +
                        mask * color.r,
                    mixedG +
                        mask * color.g,
                    mixedB +
                        mask * color.b,
                    mixedA +
                        mask * color.a);
        }
    }
}
