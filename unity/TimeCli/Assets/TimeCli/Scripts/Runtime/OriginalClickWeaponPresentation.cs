using UnityEngine;

namespace TimeCli.UnityRuntime
{
    /// <summary>
    /// Exact clean-room click-weapon transform hierarchy promoted from the
    /// canonical Time Clickers 1.4.5 Arena serialized scene and confirmed
    /// against ClickerWeapon IL.
    /// </summary>
    internal sealed class OriginalClickWeaponPresentationView
    {
        private readonly Transform _pistolPivot;
        private readonly Transform _pistolVertical;
        private readonly Transform _cannonPivot;
        private readonly Transform _cannonVertical;
        private readonly Transform _launcherPivot;
        private readonly Transform _launcherVertical;

        private OriginalClickWeaponPresentationView(
            Transform pistolPivot,
            Transform pistolVertical,
            Transform cannonPivot,
            Transform cannonVertical,
            Transform launcherPivot,
            Transform launcherVertical)
        {
            _pistolPivot = pistolPivot;
            _pistolVertical = pistolVertical;
            _cannonPivot = cannonPivot;
            _cannonVertical = cannonVertical;
            _launcherPivot = launcherPivot;
            _launcherVertical = launcherVertical;
        }

        public static OriginalClickWeaponPresentationView Create(
            ArenaRuntimeController arena)
        {
            var clickWeapons = new GameObject("ClickWeapons");
            clickWeapons.transform.SetParent(arena.transform, false);
            clickWeapons.transform.localPosition = Vector3.zero;
            clickWeapons.transform.localRotation =
                new Quaternion(0f, 0f, 0f, 1f);
            clickWeapons.transform.localScale = Vector3.one;

            WeaponHierarchy pistol = BuildWeapon(
                clickWeapons.transform,
                "ClickerPistol",
                OriginalClickWeaponPresentation.PistolRootPosition,
                OriginalClickWeaponPresentation.SharedPivotPosition,
                Vector3.zero,
                OriginalClickWeaponPresentation.PistolModelPosition,
                OriginalClickWeaponPresentation.PistolModelRotation,
                OriginalClickWeaponPresentation.PistolModelScale,
                OriginalClickWeaponPresentation.PistolFireSpotPosition,
                OriginalClickWeaponPresentation.PistolFireSpotRotation,
                OriginalClickWeaponPresentation.PistolFireSpotScale,
                "Pistol");

            WeaponHierarchy cannon = BuildWeapon(
                clickWeapons.transform,
                "ClickCannon",
                OriginalClickWeaponPresentation.CannonRootPosition,
                OriginalClickWeaponPresentation.SharedPivotPosition,
                Vector3.zero,
                OriginalClickWeaponPresentation.CannonModelPosition,
                OriginalClickWeaponPresentation.CannonModelRotation,
                OriginalClickWeaponPresentation.CannonModelScale,
                OriginalClickWeaponPresentation.CannonFireSpotPosition,
                OriginalClickWeaponPresentation.CannonFireSpotRotation,
                OriginalClickWeaponPresentation.CannonFireSpotScale,
                "s20");

            WeaponHierarchy launcher = BuildWeapon(
                clickWeapons.transform,
                "ClickLauncher",
                OriginalClickWeaponPresentation.LauncherRootPosition,
                OriginalClickWeaponPresentation.SharedPivotPosition,
                Vector3.zero,
                OriginalClickWeaponPresentation.LauncherModelPosition,
                OriginalClickWeaponPresentation.LauncherModelRotation,
                OriginalClickWeaponPresentation.LauncherModelScale,
                OriginalClickWeaponPresentation.LauncherFireSpotPosition,
                OriginalClickWeaponPresentation.LauncherFireSpotRotation,
                OriginalClickWeaponPresentation.LauncherFireSpotScale,
                "launcher");

            arena.BindRecoveredClickWeaponFireSpots(
                pistol.FireSpot,
                cannon.FireSpot,
                launcher.FireSpot);

            return new OriginalClickWeaponPresentationView(
                pistol.Pivot,
                pistol.Vertical,
                cannon.Pivot,
                cannon.Vertical,
                launcher.Pivot,
                launcher.Vertical);
        }

        public void UpdateAim(
            Vector3 crosshairPosition,
            float lastTapScreenY)
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            Ray ray = camera.ScreenPointToRay(crosshairPosition);
            Vector3 target =
                ray.origin +
                ray.direction *
                OriginalProjectilePresentation.ClickWeaponMaxDistance;

            _pistolPivot.LookAt(target);
            _cannonPivot.LookAt(target);
            _launcherPivot.LookAt(target);

            float normalizedVertical =
                Screen.height > 0
                    ? 1f - lastTapScreenY / Screen.height
                    : 1f;

            _pistolVertical.localPosition =
                new Vector3(
                    0f,
                    -OriginalClickWeaponAimMath.EvaluatePistolVertical(
                        normalizedVertical),
                    0f);

            float heavyVertical =
                OriginalClickWeaponAimMath.EvaluateHeavyVertical(
                    normalizedVertical);

            _cannonVertical.localPosition =
                new Vector3(0f, -heavyVertical, 0f);

            _launcherVertical.localPosition =
                new Vector3(0f, -heavyVertical, 0f);
        }

        private static WeaponHierarchy BuildWeapon(
            Transform parent,
            string rootName,
            Vector3 rootPosition,
            Vector3 pivotPosition,
            Vector3 verticalPosition,
            Vector3 modelPosition,
            Quaternion modelRotation,
            Vector3 modelScale,
            Vector3 fireSpotPosition,
            Quaternion fireSpotRotation,
            Vector3 fireSpotScale,
            string modelName)
        {
            Transform root = CreateTransform(
                rootName,
                parent,
                rootPosition,
                new Quaternion(0f, 0f, 0f, 1f),
                Vector3.one);

            Transform pivot = CreateTransform(
                "Pivot",
                root,
                pivotPosition,
                new Quaternion(0f, 0f, 0f, 1f),
                Vector3.one);

            Transform vertical = CreateTransform(
                "Vertical",
                pivot,
                verticalPosition,
                new Quaternion(0f, 0f, 0f, 1f),
                Vector3.one);

            Transform model = CreateTransform(
                modelName,
                vertical,
                modelPosition,
                modelRotation,
                modelScale);

            Transform fireSpot = CreateTransform(
                "Firespot",
                model,
                fireSpotPosition,
                fireSpotRotation,
                fireSpotScale);

            return new WeaponHierarchy(
                pivot,
                vertical,
                fireSpot);
        }

        private static Transform CreateTransform(
            string name,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = localRotation;
            gameObject.transform.localScale = localScale;
            return gameObject.transform;
        }

        private readonly struct WeaponHierarchy
        {
            public WeaponHierarchy(
                Transform pivot,
                Transform vertical,
                Transform fireSpot)
            {
                Pivot = pivot;
                Vertical = vertical;
                FireSpot = fireSpot;
            }

            public Transform Pivot { get; }
            public Transform Vertical { get; }
            public Transform FireSpot { get; }
        }
    }

    public static class OriginalClickWeaponPresentation
    {
        public static readonly Vector3 PistolRootPosition =
            new(0.600000024f, 0.129999995f, -7.750999928f);

        public static readonly Vector3 CannonRootPosition =
            new(0.070000000f, 0.129999995f, -7.750999928f);

        public static readonly Vector3 LauncherRootPosition =
            new(-0.600000024f, 0.129999995f, -7.750999928f);

        public static readonly Vector3 SharedPivotPosition =
            new(0f, -0.135000005f, -0.521000028f);

        public static readonly Vector3 PistolModelPosition =
            new(0f, 0.135000005f, 0.521000028f);

        public static readonly Quaternion PistolModelRotation =
            new(0f, 0f, 0f, 1f);

        public static readonly Vector3 PistolModelScale =
            Vector3.one;

        public static readonly Vector3 PistolFireSpotPosition =
            new(0f, 0.171000004f, 0.449999988f);

        public static readonly Quaternion PistolFireSpotRotation =
            new(0f, 0f, 0f, 1f);

        public static readonly Vector3 PistolFireSpotScale =
            Vector3.one;

        public static readonly Vector3 CannonModelPosition =
            new(0f, 0.151999995f, 0f);

        public static readonly Quaternion CannonModelRotation =
            new(-0.5f, -0.5f, -0.500000119f, 0.49999994f);

        public static readonly Vector3 CannonModelScale =
            Vector3.one;

        public static readonly Vector3 CannonFireSpotPosition =
            new(0.971199989f, 0f, 0.169000000f);

        public static readonly Quaternion CannonFireSpotRotation =
            new(0.5f, 0.5f, 0.500000119f, 0.49999994f);

        public static readonly Vector3 CannonFireSpotScale =
            Vector3.one;

        public static readonly Vector3 LauncherModelPosition =
            new(0f, -0.059000000f, 0.190000176f);

        public static readonly Quaternion LauncherModelRotation =
            new(
                0.000000094f,
                0.707106829f,
               -0.000000094f,
                0.707106709f);

        public static readonly Vector3 LauncherModelScale =
            new(1f, 1f, -1f);

        public static readonly Vector3 LauncherFireSpotPosition =
            new(-0.583999753f, 0.269999981f, 0f);

        public static readonly Quaternion LauncherFireSpotRotation =
            new(
               -0.000000094f,
               -0.707106829f,
                0.000000094f,
                0.707106709f);

        public static readonly Vector3 LauncherFireSpotScale =
            new(-1f, 1f, 1f);

        public const float VerticalCurveFirstTime = 0.701301575f;
        public const float VerticalCurveFirstValue = 0.00506286416f;

        public const float PistolVerticalSecondTime = 0.999999404f;
        public const float PistolVerticalSecondValue = 0.298796117f;
        public const float PistolVerticalSecondSlope = 2.99979258f;

        public const float HeavyVerticalSecondTime = 0.999677181f;
        public const float HeavyVerticalSecondValue = 0.401019126f;
        public const float HeavyVerticalSecondSlope = 4.32454586f;
    }

    internal static class OriginalClickWeaponAimMath
    {
        public static float EvaluatePistolVertical(float t) =>
            EvaluateHermite(
                t,
                OriginalClickWeaponPresentation.VerticalCurveFirstTime,
                OriginalClickWeaponPresentation.VerticalCurveFirstValue,
                0f,
                OriginalClickWeaponPresentation.PistolVerticalSecondTime,
                OriginalClickWeaponPresentation.PistolVerticalSecondValue,
                OriginalClickWeaponPresentation.PistolVerticalSecondSlope);

        public static float EvaluateHeavyVertical(float t) =>
            EvaluateHermite(
                t,
                OriginalClickWeaponPresentation.VerticalCurveFirstTime,
                OriginalClickWeaponPresentation.VerticalCurveFirstValue,
                0f,
                OriginalClickWeaponPresentation.HeavyVerticalSecondTime,
                OriginalClickWeaponPresentation.HeavyVerticalSecondValue,
                OriginalClickWeaponPresentation.HeavyVerticalSecondSlope);

        private static float EvaluateHermite(
            float t,
            float firstTime,
            float firstValue,
            float firstOutSlope,
            float secondTime,
            float secondValue,
            float secondInSlope)
        {
            if (t <= firstTime)
                return firstValue;

            if (t >= secondTime)
                return secondValue;

            float duration = secondTime - firstTime;
            float u = (t - firstTime) / duration;
            float u2 = u * u;
            float u3 = u2 * u;

            float h00 = 2f * u3 - 3f * u2 + 1f;
            float h10 = u3 - 2f * u2 + u;
            float h01 = -2f * u3 + 3f * u2;
            float h11 = u3 - u2;

            return
                h00 * firstValue +
                h10 * duration * firstOutSlope +
                h01 * secondValue +
                h11 * duration * secondInSlope;
        }
    }
}
