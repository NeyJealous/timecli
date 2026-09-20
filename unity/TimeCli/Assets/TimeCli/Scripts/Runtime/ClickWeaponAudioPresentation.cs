using UnityEngine;

namespace TimeCli.UnityRuntime
{
    internal static class OriginalClickWeaponAudioPresentation
    {
        public const string PistolFireResource =
            "TimeCliClickPistolFire";
        public const string CannonFireResource =
            "TimeCliClickCannonFire";
        public const string LauncherFireResource =
            "TimeCliClickLauncherFire";

        public const string PistolClipName = "pistol1";
        public const string CannonClipName =
            "MechWeapons_Shock_Fire_02";
        public const string LauncherClipName =
            "MechWeapons_Grenade_Fire_01";

        public const float Volume = 0.75f;
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
}
