using UnityEngine;

namespace TimeCli.UnityRuntime
{
    /// <summary>
    /// Presentation-only values recovered from the original Arena.unity scene.
    /// Gameplay does not depend on these values.
    /// </summary>
    public static class OriginalArenaPresentation
    {
        public static readonly Vector3 ArenaPosition =
            new(0f, -3.3399999f, 5.3499999f);

        public static readonly Vector3 ArenaEuler =
            new(0f, -59.999976f, 0f);

        public static readonly Vector3 CameraPosition =
            new(0f, 1f, -10f);

        public static readonly Vector3 CameraEuler =
            new(-6.469241f, 0f, 0f);

        public const float CameraNearClip = 0.3f;
        public const float CameraFarClip = 1000f;
        public const float CameraFieldOfView = 60f;
        public const float CameraDepth = -2f;

        // Original BoxEnemy prefab: unit transform scale + 1x1x1 BoxCollider.
        public const float BlockSpacing = 1f;
    }
}
