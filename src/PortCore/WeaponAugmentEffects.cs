using System;
using System.Collections.Generic;

namespace TimeClickers.PortCore;

public enum WeaponAugmentType
{
    Unknown = 0,
    ClickLauncherUnlock = 1,
    ClickLauncherClicks = 2,
    ClickLauncherRockets = 3,
    ClickLauncherIdleSpeed = 4,
    ClickLauncherAutoFire = 5,
    ClickPistolIdleSpeed = 6,
    ClickPistolAutoFire = 7,
    WeaponCubeChance = 8,
    WeaponCubeFind = 9,
    ClickCannonUnlock = 10,
    ClickCannonMaxProjectiles = 11,
    ClickCannonFireCone = 12,
    ClickCannonIdleSpeed = 13,
    ClickCannonAutoFire = 14,
    WeaponCubeStartWave = 15,
    ClickLauncherRocketSpeed = 16,
    ClickCannonDamagePerShot = 17,
    ClickPistolGoldSpawnChance = 18,
    ClickPistolGoldSpawnValue = 19,
    Total = 20
}

/// <summary>
/// Evaluated Weapon Augment modifiers, independent of the old Unity singleton.
/// Values are the result of WeaponAugment.GetModValue().
/// </summary>
public sealed record WeaponAugmentEffects(
    bool ClickLauncherUnlocked,
    int ClickLauncherClicks,
    int ClickLauncherRockets,
    double ClickLauncherIdleSpeed,
    int ClickLauncherAutoFire,
    double ClickPistolIdleSpeed,
    int ClickPistolAutoFire,
    double WeaponCubeChancePercent,
    double WeaponCubeFindPercent,
    bool ClickCannonUnlocked,
    int ClickCannonMaxProjectiles,
    double ClickCannonFireCone,
    double ClickCannonIdleSpeed,
    int ClickCannonAutoFire,
    int WeaponCubeStartWave,
    double ClickLauncherRocketSpeed,
    double ClickCannonDamagePerShot,
    double ClickPistolGoldSpawnChance,
    double ClickPistolGoldSpawnValue);

public static class WeaponAugmentEffectsMath
{
    public static WeaponAugmentEffects Recalculate(IReadOnlyList<double> values)
    {
        if (values.Count < (int)WeaponAugmentType.Total)
            throw new ArgumentException(
                $"Expected at least {(int)WeaponAugmentType.Total} augment values, got {values.Count}.",
                nameof(values));

        double V(WeaponAugmentType type) => values[(int)type];

        return new WeaponAugmentEffects(
            ClickLauncherUnlocked: V(WeaponAugmentType.ClickLauncherUnlock) != 0.0,
            ClickLauncherClicks: (int)V(WeaponAugmentType.ClickLauncherClicks),
            ClickLauncherRockets: (int)V(WeaponAugmentType.ClickLauncherRockets),
            ClickLauncherIdleSpeed: V(WeaponAugmentType.ClickLauncherIdleSpeed),
            ClickLauncherAutoFire: (int)V(WeaponAugmentType.ClickLauncherAutoFire),
            ClickPistolIdleSpeed: V(WeaponAugmentType.ClickPistolIdleSpeed),
            ClickPistolAutoFire: (int)V(WeaponAugmentType.ClickPistolAutoFire),
            WeaponCubeChancePercent: V(WeaponAugmentType.WeaponCubeChance),
            WeaponCubeFindPercent: V(WeaponAugmentType.WeaponCubeFind),
            ClickCannonUnlocked: V(WeaponAugmentType.ClickCannonUnlock) != 0.0,
            ClickCannonMaxProjectiles: (int)V(WeaponAugmentType.ClickCannonMaxProjectiles),
            ClickCannonFireCone: V(WeaponAugmentType.ClickCannonFireCone),
            ClickCannonIdleSpeed: V(WeaponAugmentType.ClickCannonIdleSpeed),
            ClickCannonAutoFire: (int)V(WeaponAugmentType.ClickCannonAutoFire),
            WeaponCubeStartWave: (int)V(WeaponAugmentType.WeaponCubeStartWave),
            ClickLauncherRocketSpeed: V(WeaponAugmentType.ClickLauncherRocketSpeed),
            ClickCannonDamagePerShot: V(WeaponAugmentType.ClickCannonDamagePerShot),
            ClickPistolGoldSpawnChance: V(WeaponAugmentType.ClickPistolGoldSpawnChance),
            ClickPistolGoldSpawnValue: V(WeaponAugmentType.ClickPistolGoldSpawnValue));
    }
}
