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
        private readonly Transform _pistolRoot;
        private readonly Transform _cannonRoot;
        private readonly Transform _launcherRoot;
        private readonly Transform _pistolPivot;
        private readonly Transform _pistolVertical;
        private readonly Transform _cannonPivot;
        private readonly Transform _cannonVertical;
        private readonly Transform _launcherPivot;
        private readonly Transform _launcherVertical;
        private readonly Transform _pistolSlide;
        private readonly Transform _pistolModel;
        private readonly Transform _cannonModel;
        private readonly Transform _launcherModel;
        private readonly AudioSource _pistolAudio;
        private readonly AudioSource _cannonAudio;
        private readonly AudioSource _launcherAudio;

        private float _pistolShootStart = float.NegativeInfinity;
        private float _cannonShootStart = float.NegativeInfinity;
        private float _launcherShootStart = float.NegativeInfinity;

        private OriginalClickWeaponVisibilityAnimation.ClipCurves
            _pistolVisibilityClip;
        private OriginalClickWeaponVisibilityAnimation.ClipCurves
            _cannonVisibilityClip;
        private OriginalClickWeaponVisibilityAnimation.ClipCurves
            _launcherVisibilityClip;

        private float _pistolVisibilityStart = float.NegativeInfinity;
        private float _cannonVisibilityStart = float.NegativeInfinity;
        private float _launcherVisibilityStart = float.NegativeInfinity;
        private float _pistolVisibilityDuration;
        private float _cannonVisibilityDuration;
        private float _launcherVisibilityDuration;

        private float _pistolAppearAudioTime = float.NegativeInfinity;
        private float _cannonAppearAudioTime = float.NegativeInfinity;
        private float _launcherAppearAudioTime = float.NegativeInfinity;

        private OriginalClickWeaponPresentationView(
            Transform pistolRoot,
            Transform cannonRoot,
            Transform launcherRoot,
            Transform pistolPivot,
            Transform pistolVertical,
            Transform cannonPivot,
            Transform cannonVertical,
            Transform launcherPivot,
            Transform launcherVertical,
            Transform pistolSlide,
            Transform pistolModel,
            Transform cannonModel,
            Transform launcherModel,
            AudioSource pistolAudio,
            AudioSource cannonAudio,
            AudioSource launcherAudio)
        {
            _pistolRoot = pistolRoot;
            _cannonRoot = cannonRoot;
            _launcherRoot = launcherRoot;
            _pistolPivot = pistolPivot;
            _pistolVertical = pistolVertical;
            _cannonPivot = cannonPivot;
            _cannonVertical = cannonVertical;
            _launcherPivot = launcherPivot;
            _launcherVertical = launcherVertical;
            _pistolSlide = pistolSlide;
            _pistolModel = pistolModel;
            _cannonModel = cannonModel;
            _launcherModel = launcherModel;
            _pistolAudio = pistolAudio;
            _cannonAudio = cannonAudio;
            _launcherAudio = launcherAudio;
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

            Transform pistolSlide = CreateTransform(
                "top1",
                pistol.Model,
                OriginalClickWeaponPresentation.PistolTopRestPosition,
                new Quaternion(0f, 0f, 0f, 1f),
                Vector3.one);

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

            ClickWeaponVisualFactory.Populate(
                pistol.Model,
                pistolSlide,
                cannon.Model,
                launcher.Model);

            AudioSource pistolAudio =
                ClickWeaponAudioFactory.Attach(
                    pistol.Root,
                    OriginalClickWeaponAudioPresentation.PistolFireResource,
                    "PistolFireAudio");
            AudioSource cannonAudio =
                ClickWeaponAudioFactory.Attach(
                    cannon.Root,
                    OriginalClickWeaponAudioPresentation.CannonFireResource,
                    "CannonFireAudio");
            AudioSource launcherAudio =
                ClickWeaponAudioFactory.Attach(
                    launcher.Root,
                    OriginalClickWeaponAudioPresentation.LauncherFireResource,
                    "LauncherFireAudio");

            arena.BindRecoveredClickWeaponFireSpots(
                pistol.FireSpot,
                cannon.FireSpot,
                launcher.FireSpot);

            return new OriginalClickWeaponPresentationView(
                pistol.Root,
                cannon.Root,
                launcher.Root,
                pistol.Pivot,
                pistol.Vertical,
                cannon.Pivot,
                cannon.Vertical,
                launcher.Pivot,
                launcher.Vertical,
                pistolSlide,
                pistol.Model,
                cannon.Model,
                launcher.Model,
                pistolAudio,
                cannonAudio,
                launcherAudio);
        }

        public void UpdateAim(
            Vector3 crosshairPosition,
            float lastTapScreenY)
        {
            UpdateAnimations(Time.time);

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


        public void PlayPistolShoot()
        {
            _pistolShootStart = Time.time;
            ApplyPistolShoot(0f);
            ClickWeaponAudioFactory.Play(_pistolAudio);
        }

        public void PlayCannonShoot()
        {
            _cannonShootStart = Time.time;
            ApplyCannonShoot(0f);
            ClickWeaponAudioFactory.Play(_cannonAudio);
        }

        public void PlayLauncherShoot()
        {
            _launcherShootStart = Time.time;
            ApplyLauncherShoot(0f);
            ClickWeaponAudioFactory.Play(_launcherAudio);
        }

        public void PlayPistolAppear()
        {
            StartPistolVisibility(
                OriginalClickWeaponVisibilityAnimation.PistolAppear,
                OriginalClickWeaponVisibilityAnimation.PistolAppearDuration);
            _pistolAppearAudioTime =
                Time.time +
                OriginalClickWeaponAudioPresentation.AppearDelay;
        }

        public void PlayPistolDisappear() =>
            StartPistolVisibility(
                OriginalClickWeaponVisibilityAnimation.PistolDisappear,
                OriginalClickWeaponVisibilityAnimation.PistolDisappearDuration);

        public void PlayCannonAppear()
        {
            StartCannonVisibility(
                OriginalClickWeaponVisibilityAnimation.CannonAppear,
                OriginalClickWeaponVisibilityAnimation.CannonAppearDuration);
            _cannonAppearAudioTime =
                Time.time +
                OriginalClickWeaponAudioPresentation.AppearDelay;
        }

        public void PlayCannonDisappear() =>
            StartCannonVisibility(
                OriginalClickWeaponVisibilityAnimation.CannonDisappear,
                OriginalClickWeaponVisibilityAnimation.CannonDisappearDuration);

        public void PlayLauncherAppear()
        {
            StartLauncherVisibility(
                OriginalClickWeaponVisibilityAnimation.LauncherAppear,
                OriginalClickWeaponVisibilityAnimation.LauncherAppearDuration);
            _launcherAppearAudioTime =
                Time.time +
                OriginalClickWeaponAudioPresentation.AppearDelay;
        }

        public void PlayLauncherDisappear() =>
            StartLauncherVisibility(
                OriginalClickWeaponVisibilityAnimation.LauncherDisappear,
                OriginalClickWeaponVisibilityAnimation.LauncherDisappearDuration);

        private void UpdateAnimations(float now)
        {
            UpdateShootAnimations(now);
            UpdateVisibilityAnimations(now);
            UpdateAppearAudio(now);
        }

        private void UpdateAppearAudio(float now)
        {
            if (!float.IsNegativeInfinity(_pistolAppearAudioTime) &&
                now >= _pistolAppearAudioTime)
            {
                ClickWeaponAppearAudioView.Play(
                    _pistolRoot.position);
                _pistolAppearAudioTime = float.NegativeInfinity;
            }

            if (!float.IsNegativeInfinity(_cannonAppearAudioTime) &&
                now >= _cannonAppearAudioTime)
            {
                ClickWeaponAppearAudioView.Play(
                    _cannonRoot.position);
                _cannonAppearAudioTime = float.NegativeInfinity;
            }

            if (!float.IsNegativeInfinity(_launcherAppearAudioTime) &&
                now >= _launcherAppearAudioTime)
            {
                ClickWeaponAppearAudioView.Play(
                    _launcherRoot.position);
                _launcherAppearAudioTime = float.NegativeInfinity;
            }
        }

        private void UpdateShootAnimations(float now)
        {
            if (!float.IsNegativeInfinity(_pistolShootStart))
            {
                float elapsed = now - _pistolShootStart;
                if (elapsed <= OriginalClickWeaponShootAnimation.PistolDuration)
                {
                    ApplyPistolShoot(elapsed);
                }
                else
                {
                    _pistolSlide.localPosition =
                        OriginalClickWeaponPresentation.PistolTopRestPosition;
                    _pistolShootStart = float.NegativeInfinity;
                }
            }

            if (!float.IsNegativeInfinity(_cannonShootStart))
            {
                float elapsed = now - _cannonShootStart;
                if (elapsed <= OriginalClickWeaponShootAnimation.CannonDuration)
                {
                    ApplyCannonShoot(elapsed);
                }
                else
                {
                    _cannonModel.localPosition =
                        OriginalClickWeaponPresentation.CannonModelPosition;
                    _cannonModel.localRotation =
                        OriginalClickWeaponPresentation.CannonModelRotation;
                    _cannonShootStart = float.NegativeInfinity;
                }
            }

            if (!float.IsNegativeInfinity(_launcherShootStart))
            {
                float elapsed = now - _launcherShootStart;
                if (elapsed <= OriginalClickWeaponShootAnimation.LauncherDuration)
                {
                    ApplyLauncherShoot(elapsed);
                }
                else
                {
                    _launcherModel.localPosition =
                        OriginalClickWeaponPresentation.LauncherModelPosition;
                    _launcherModel.localRotation =
                        OriginalClickWeaponPresentation.LauncherModelRotation;
                    _launcherShootStart = float.NegativeInfinity;
                }
            }
        }

        private void UpdateVisibilityAnimations(float now)
        {
            UpdateVisibilityAnimation(
                _pistolModel,
                ref _pistolVisibilityClip,
                ref _pistolVisibilityStart,
                _pistolVisibilityDuration,
                now);

            UpdateVisibilityAnimation(
                _cannonModel,
                ref _cannonVisibilityClip,
                ref _cannonVisibilityStart,
                _cannonVisibilityDuration,
                now);

            UpdateVisibilityAnimation(
                _launcherModel,
                ref _launcherVisibilityClip,
                ref _launcherVisibilityStart,
                _launcherVisibilityDuration,
                now);
        }

        private static void UpdateVisibilityAnimation(
            Transform model,
            ref OriginalClickWeaponVisibilityAnimation.ClipCurves clip,
            ref float startTime,
            float duration,
            float now)
        {
            if (clip == null || float.IsNegativeInfinity(startTime))
                return;

            float elapsed = now - startTime;
            if (elapsed >= duration)
            {
                ApplyVisibility(model, clip, duration);
                clip = null;
                startTime = float.NegativeInfinity;
                return;
            }

            ApplyVisibility(model, clip, elapsed < 0f ? 0f : elapsed);
        }

        private void StartPistolVisibility(
            OriginalClickWeaponVisibilityAnimation.ClipCurves clip,
            float duration)
        {
            _pistolVisibilityClip = clip;
            _pistolVisibilityDuration = duration;
            _pistolVisibilityStart = Time.time;
            ApplyVisibility(_pistolModel, clip, 0f);
        }

        private void StartCannonVisibility(
            OriginalClickWeaponVisibilityAnimation.ClipCurves clip,
            float duration)
        {
            _cannonVisibilityClip = clip;
            _cannonVisibilityDuration = duration;
            _cannonVisibilityStart = Time.time;
            ApplyVisibility(_cannonModel, clip, 0f);
        }

        private void StartLauncherVisibility(
            OriginalClickWeaponVisibilityAnimation.ClipCurves clip,
            float duration)
        {
            _launcherVisibilityClip = clip;
            _launcherVisibilityDuration = duration;
            _launcherVisibilityStart = Time.time;
            ApplyVisibility(_launcherModel, clip, 0f);
        }

        private static void ApplyVisibility(
            Transform model,
            OriginalClickWeaponVisibilityAnimation.ClipCurves clip,
            float elapsed)
        {
            model.localPosition = clip.EvaluatePosition(elapsed);
            model.localRotation = clip.EvaluateRotation(elapsed);
        }

        private void ApplyPistolShoot(float elapsed)
        {
            _pistolSlide.localPosition =
                OriginalClickWeaponShootAnimation
                    .EvaluatePistolSlide(elapsed);
        }

        private void ApplyCannonShoot(float elapsed)
        {
            _cannonModel.localPosition =
                OriginalClickWeaponShootAnimation
                    .EvaluateCannon(elapsed);
            _cannonModel.localRotation =
                OriginalClickWeaponShootAnimation.CannonRotation;
        }

        private void ApplyLauncherShoot(float elapsed)
        {
            _launcherModel.localPosition =
                OriginalClickWeaponShootAnimation
                    .EvaluateLauncherPosition(elapsed);
            _launcherModel.localRotation =
                OriginalClickWeaponShootAnimation
                    .EvaluateLauncherRotation(elapsed);
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
                root,
                pivot,
                vertical,
                model,
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
                Transform root,
                Transform pivot,
                Transform vertical,
                Transform model,
                Transform fireSpot)
            {
                Root = root;
                Pivot = pivot;
                Vertical = vertical;
                Model = model;
                FireSpot = fireSpot;
            }

            public Transform Root { get; }
            public Transform Pivot { get; }
            public Transform Vertical { get; }
            public Transform Model { get; }
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

        public static readonly Vector3 PistolTopRestPosition =
            new(
                0f,
                0.14061179757118225f,
                0.0027837783563882113f);

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
