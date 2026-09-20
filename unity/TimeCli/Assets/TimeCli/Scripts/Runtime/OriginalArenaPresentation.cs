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

    /// <summary>
    /// Recovered WidgetGold placement used by TimeCube/WeaponCube collection.
    ///
    /// Active 1.4.5 hierarchy:
    /// _SceneArenaRoot -> WidgetGold ->
    /// TimeCubeCollectionPoint / WeaponCubeCollectionPoint.
    ///
    /// A duplicate WidgetGold component exists below an inactive StatsWidget
    /// branch; the active hierarchy below is the one used by gameplay.
    /// </summary>
    public static class OriginalWidgetGoldPresentation
    {
        public static readonly Vector3 InitialPosition =
            new(17f, -5.900000095f, 14.100000381f);

        public static readonly Quaternion Rotation =
            new(
                -0.256685704f,
                 0.730564594f,
                -0.546390772f,
                -0.319131702f);

        public static readonly Vector3 Scale =
            new(
                0.173205093f,
                0.173205048f,
                0.173205048f);

        public static readonly Vector3 ViewportPosition =
            new(0.9f, 0.1f, 23.200000763f);

        public static readonly Vector3 TimeCubeLocalPosition =
            new(
                -4.949999809f,
                51.209999084f,
                -47.659999847f);

        public static readonly Vector3 WeaponCubeLocalPosition =
            new(
                -23.180000305f,
                36.259998322f,
                -68.379997253f);

        public const int InitialScreenWidth = 1000;

        public static Vector3 GetWidgetPosition(Camera camera)
        {
            if (camera == null)
                return InitialPosition;

            // WidgetGold.Start sets lastScreenWidth=1000 and invokes
            // RefreshScreenPosition every second. On any normal modern device
            // whose width is not exactly 1000, the first call moves WidgetGold
            // to Camera.main.ViewportToWorldPoint(viewpointPos).
            return Screen.width == InitialScreenWidth
                ? InitialPosition
                : camera.ViewportToWorldPoint(ViewportPosition);
        }

        public static Vector3 GetTimeCubeCollectionPoint(Camera camera) =>
            TransformChildPoint(
                GetWidgetPosition(camera),
                TimeCubeLocalPosition);

        public static Vector3 GetWeaponCubeCollectionPoint(Camera camera) =>
            TransformChildPoint(
                GetWidgetPosition(camera),
                WeaponCubeLocalPosition);

        private static Vector3 TransformChildPoint(
            Vector3 parentPosition,
            Vector3 childLocalPosition)
        {
            Vector3 scaled = Vector3.Scale(
                childLocalPosition,
                Scale);

            return parentPosition + Rotation * scaled;
        }
    }
}
