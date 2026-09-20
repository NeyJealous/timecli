using TimeClickers.PortCore;
using UnityEngine;

namespace TimeCli.UnityRuntime
{
    internal enum HeroImpactStyle
    {
        Outline,
        Flash
    }

    /// <summary>
    /// Presentation rules recovered from canonical Time Clickers 1.4.5
    /// Hero.ApplyDamage, BoxEnemy.Outline/FlashOutline, TweenColor and the
    /// serialized HeroWeapons component.
    ///
    /// Heroes do not spawn travelling projectile prefabs in 1.4.5. Their
    /// authoritative damage is immediate; the visible combat feedback is a
    /// weapon-colored BoxEnemy outline/flash.
    /// </summary>
    internal static class OriginalHeroPresentation
    {
        public const float FlashDuration = 0.2f;
        public const float FlashCooldown = 0.2f;

        public static readonly Color DefaultOutlineColor =
            new(0f, 0f, 0f, 1f);

        public static readonly Color PulsePistolColor =
            new(
                0f,
                0.40668535232543945f,
                1f,
                1f);

        public static readonly Color FlakCannonColor =
            new(
                1f,
                0.7019498944282532f,
                0f,
                1f);

        public static readonly Color SpreadRifleColor =
            new(1f, 0f, 0f, 1f);

        public static readonly Color RocketLauncherColor =
            new(
                0f,
                1f,
                0.1559889316558838f,
                1f);

        public static readonly Color ParticleBallColor =
            new(
                0.5626740455627441f,
                0f,
                1f,
                1f);

        public static Color GetWeaponColor(
            WeaponType weaponType)
        {
            return weaponType switch
            {
                WeaponType.Pistol =>
                    PulsePistolColor,
                WeaponType.FlakCannon =>
                    FlakCannonColor,
                WeaponType.SpreadRifle =>
                    SpreadRifleColor,
                WeaponType.RocketLauncher =>
                    RocketLauncherColor,
                WeaponType.ParticleBall =>
                    ParticleBallColor,
                _ =>
                    DefaultOutlineColor
            };
        }

        public static HeroImpactStyle GetImpactStyle(
            WeaponType weaponType,
            bool isSplash)
        {
            if (weaponType == WeaponType.FlakCannon)
                return HeroImpactStyle.Flash;

            if (weaponType == WeaponType.RocketLauncher &&
                isSplash)
            {
                return HeroImpactStyle.Flash;
            }

            return HeroImpactStyle.Outline;
        }

        public static Color EvaluateFlashColor(
            Color startColor,
            Color endColor,
            float elapsedSeconds)
        {
            float progress = FlashDuration <= 0f
                ? 1f
                : Mathf.Clamp01(
                    elapsedSeconds /
                    FlashDuration);

            return Color.Lerp(
                startColor,
                endColor,
                progress);
        }
    }
}
