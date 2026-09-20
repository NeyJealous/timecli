using TimeClickers.PortCore;
using UnityEngine;

namespace TimeCli.UnityRuntime
{
    /// <summary>
    /// Canonical ClickerWeapon idle/auto-aim constants recovered from
    /// Time Clickers 1.4.5 ClickerWeapon + UIButtonIdleMode.
    /// </summary>
    internal static class OriginalClickWeaponAutoAimPresentation
    {
        public const float BasePixelsPerSecond = 200f;
        public const float ReferenceScreenWidth = 1000f;

        public const int PistolTargetHeroId = 0;
        public const int LauncherTargetHeroId = 3;
        public const int CannonTargetHeroId = 4;

        public const int PistolHeroesRequired = 1;
        public const int LauncherHeroesRequired = 4;
        public const int CannonHeroesRequired = 5;

        public const int PistolToggleKeyCode = 101;   // E
        public const int CannonToggleKeyCode = 119;  // W
        public const int LauncherToggleKeyCode = 113;// Q

        public static readonly Color ButtonOffColor =
            new(0f, 0.40668535232543945f, 1f, 1f);

        public static readonly Color ButtonOnColor =
            new(1f, 0.4846796989440918f, 0f, 1f);

        public static int GetTargetHeroId(
            ClickWeaponSlot slot) =>
            slot switch
            {
                ClickWeaponSlot.Pistol => PistolTargetHeroId,
                ClickWeaponSlot.Cannon => CannonTargetHeroId,
                ClickWeaponSlot.Launcher => LauncherTargetHeroId,
                _ => PistolTargetHeroId
            };

        public static int GetHeroesRequired(
            ClickWeaponSlot slot) =>
            slot switch
            {
                ClickWeaponSlot.Pistol => PistolHeroesRequired,
                ClickWeaponSlot.Cannon => CannonHeroesRequired,
                ClickWeaponSlot.Launcher => LauncherHeroesRequired,
                _ => int.MaxValue
            };

        public static float GetMoveDistance(
            GameState game,
            ClickWeaponSlot slot,
            float deltaTime,
            float screenWidth)
        {
            float distance =
                deltaTime *
                BasePixelsPerSecond *
                screenWidth /
                ReferenceScreenWidth;

            if (!game.IsAbilityActive(
                    AbilityType.AutomaticFire))
            {
                return distance;
            }

            WeaponAugmentEffects effects =
                game.WeaponAugmentEffects;

            double idleSpeed =
                slot switch
                {
                    ClickWeaponSlot.Pistol =>
                        effects.ClickPistolIdleSpeed,
                    ClickWeaponSlot.Cannon =>
                        effects.ClickCannonIdleSpeed,
                    ClickWeaponSlot.Launcher =>
                        effects.ClickLauncherIdleSpeed,
                    _ => 1.0
                };

            return distance * (float)idleSpeed;
        }

        public static Vector3 MoveTowards(
            Vector3 current,
            Vector3 target,
            float maxDistanceDelta)
        {
            float dx = target.x - current.x;
            float dy = target.y - current.y;
            float dz = target.z - current.z;

            float sqrDistance =
                dx * dx +
                dy * dy +
                dz * dz;

            if (sqrDistance == 0f ||
                maxDistanceDelta <= 0f)
            {
                return current;
            }

            float distance =
                Mathf.Sqrt(sqrDistance);

            if (distance <= maxDistanceDelta)
                return target;

            float scale =
                maxDistanceDelta /
                distance;

            return new Vector3(
                current.x + dx * scale,
                current.y + dy * scale,
                current.z + dz * scale);
        }
    }
}
