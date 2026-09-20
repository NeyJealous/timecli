using UnityEngine;

namespace TimeCli.UnityRuntime
{
    internal static class OriginalClickWeaponAudioPresentation
    {
        public const string AppearResource =
            "TimeCliClickWeaponAppear";
        public const string PistolFireResource =
            "TimeCliClickPistolFire";
        public const string CannonFireResource =
            "TimeCliClickCannonFire";
        public const string LauncherFireResource =
            "TimeCliClickLauncherFire";

        public const string AppearClipName = "PistolWield";
        public const string PistolClipName = "pistol1";
        public const string CannonClipName =
            "MechWeapons_Shock_Fire_02";
        public const string LauncherClipName =
            "MechWeapons_Grenade_Fire_01";

        public const float Volume = 0.75f;
        public const float AppearVolume = 1f;
        public const float AppearDelay = 0.05f;
        public const float OneShotQueueCooldown = 0.1f;
        public const int OneShotSourcesPerQueue = 6;
        public const float OneShotMinDistance = 15f;
        public const float OneShotMaxDistance = 100f;
        public const float Pitch = 1f;
        public const float MinDistance = 1f;
        public const float MaxDistance = 500f;
        public const float DopplerLevel = 1f;
        // Unity 5.4 panLevelCustomCurve is constant 0 for all three
        // root AudioSources, i.e. fully 2D in modern spatialBlend terms.
        public const float SpatialBlend = 0f;
        public const int Priority = 128;
    }

    internal static class ClickWeaponAudioFactory
    {
        public static AudioSource Attach(
            Transform root,
            string resourceName,
            string objectName)
        {
            if (root == null)
                return null;

            AudioClip clip =
                Resources.Load<AudioClip>(
                    resourceName);

            if (clip == null)
                return null;

            var audioObject =
                new GameObject(
                    objectName);

            audioObject.transform.SetParent(
                root,
                false);
            audioObject.transform.localPosition =
                Vector3.zero;
            audioObject.transform.localRotation =
                new Quaternion(0f, 0f, 0f, 1f);
            audioObject.transform.localScale =
                Vector3.one;

            AudioSource source =
                audioObject.AddComponent<AudioSource>();

            source.clip = clip;
            source.playOnAwake = false;
            source.volume =
                OriginalClickWeaponAudioPresentation.Volume;
            source.pitch =
                OriginalClickWeaponAudioPresentation.Pitch;
            source.priority =
                OriginalClickWeaponAudioPresentation.Priority;
            source.dopplerLevel =
                OriginalClickWeaponAudioPresentation.DopplerLevel;
            source.rolloffMode =
                AudioRolloffMode.Logarithmic;
            source.minDistance =
                OriginalClickWeaponAudioPresentation.MinDistance;
            source.maxDistance =
                OriginalClickWeaponAudioPresentation.MaxDistance;
            source.spatialBlend =
                OriginalClickWeaponAudioPresentation.SpatialBlend;

            return source;
        }

        public static void Play(AudioSource source)
        {
            if (source != null)
                source.Play();
        }
    }
    /// <summary>
    /// Exact ClickerWeapon.Appear audio path. The original waits 50 ms, then
    /// submits PistolWield to OneShotAudio's Pickups queue at volume 1.
    /// The Pickups queue owns six sources, throttles at 100 ms, and unlike the
    /// Explosions queue does not scale pitch by Time.timeScale.
    /// </summary>
    internal static class ClickWeaponAppearAudioView
    {
        private static AudioClip _clip;
        private static AudioSource[] _sources;
        private static int _nextSource;
        private static float _nextPlayTime;

        public static void Play(Vector3 position)
        {
            if (_nextPlayTime > Time.time)
                return;

            if (_clip == null)
            {
                _clip =
                    Resources.Load<AudioClip>(
                        OriginalClickWeaponAudioPresentation
                            .AppearResource);
            }

            if (_clip == null)
                return;

            EnsureSources();

            _nextPlayTime =
                Time.time +
                OriginalClickWeaponAudioPresentation
                    .OneShotQueueCooldown;

            AudioSource source =
                _sources[_nextSource];

            _nextSource =
                (_nextSource + 1) %
                _sources.Length;

            source.transform.position = position;
            source.clip = _clip;
            source.pitch = 1f;
            source.volume =
                OriginalClickWeaponAudioPresentation
                    .AppearVolume;
            source.Play();
        }

        private static void EnsureSources()
        {
            if (_sources != null)
                return;

            _sources =
                new AudioSource[
                    OriginalClickWeaponAudioPresentation
                        .OneShotSourcesPerQueue];

            for (int i = 0; i < _sources.Length; i++)
            {
                var go =
                    new GameObject(
                        "OneShotAudio_Pickups");

                AudioSource source =
                    go.AddComponent<AudioSource>();

                source.playOnAwake = false;
                source.rolloffMode =
                    AudioRolloffMode.Logarithmic;
                source.minDistance =
                    OriginalClickWeaponAudioPresentation
                        .OneShotMinDistance;
                source.maxDistance =
                    OriginalClickWeaponAudioPresentation
                        .OneShotMaxDistance;

                _sources[i] = source;
            }
        }
    }

}
