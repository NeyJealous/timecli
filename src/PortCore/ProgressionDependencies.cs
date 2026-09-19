using System;
using System.Collections.Generic;
using System.Linq;

namespace TimeClickers.PortCore;

/// <summary>
/// Exact prerequisite graph reconstructed from the 1.4.5 Artifact and
/// WeaponAugment data tables. A node is unlocked only when every prerequisite
/// owns at least one level.
/// </summary>
public static class ProgressionDependencies
{
    private static readonly ArtifactType[][] ArtifactRequires =
    {
        Array.Empty<ArtifactType>(), // Unknown
        Array.Empty<ArtifactType>(), // GoldFind
        new[] { ArtifactType.GoldFind },
        new[] { ArtifactType.GoldFindRed },
        new[] { ArtifactType.GoldFindWhite },
        Array.Empty<ArtifactType>(), // StartWave
        new[] { ArtifactType.StartWave },
        new[] { ArtifactType.TimeCubeChance },
        new[] { ArtifactType.StartWave },
        new[] { ArtifactType.BossTimer },
        new[] { ArtifactType.GoldFindYellow },
        new[] { ArtifactType.GoldFindRainbowBlock },
        new[] { ArtifactType.StartingGold },
        new[] { ArtifactType.RainbowBallChance },
        new[] { ArtifactType.RainbowConvertRed },
        new[] { ArtifactType.RainbowEnemyChance },
        new[] { ArtifactType.RainbowBallGoldMinutes, ArtifactType.RainbowConvertWhite },
        Array.Empty<ArtifactType>(), // DamageTargeted
        new[] { ArtifactType.DamageTargetedFlakCannon },
        new[] { ArtifactType.DamageTargetedParticleBall },
        new[] { ArtifactType.DamageTargetedRocketLauncher },
        new[] { ArtifactType.DamageTargeted },
        new[] { ArtifactType.DamageTargetedSpreadRifle },
        new[] { ArtifactType.DamageTargeted },
        new[] { ArtifactType.ClickDamageRed },
        new[] { ArtifactType.CriticalStrikeMultiplier },
        new[] { ArtifactType.ClickDamageYellow, ArtifactType.DamageTargetedPulsePistol },
        new[] { ArtifactType.ClickDamageWhite },
        new[] { ArtifactType.CriticalStrikeChance },
        Array.Empty<ArtifactType>(), // AbilityDuration1
        new[] { ArtifactType.ExplosiveShot },
        new[] { ArtifactType.SpreadShots },
        new[] { ArtifactType.AugmentedAim },
        new[] { ArtifactType.AbilityDuration1 },
        new[] { ArtifactType.AbilityDuration1 },
        new[] { ArtifactType.AbilityCooldown2 },
        new[] { ArtifactType.AbilityCooldown1 },
        new[] { ArtifactType.GoldRush },
        new[] { ArtifactType.Overcharge },
        new[] { ArtifactType.TeamWork, ArtifactType.AbilityDuration2 },
        new[] { ArtifactType.RapidFire },
        Array.Empty<ArtifactType>(), // TeamDps
        new[] { ArtifactType.FlakCannonDps },
        new[] { ArtifactType.SpreadRifleDps },
        new[] { ArtifactType.RocketLauncherDps },
        new[] { ArtifactType.ParticleBallDps },
        new[] { ArtifactType.TeamDps },
        new[] { ArtifactType.TeamDps },
        new[] { ArtifactType.RocketSplashDiagonal },
        new[] { ArtifactType.ParticleCollider },
        new[] { ArtifactType.NeverMissFlak },
        new[] { ArtifactType.PulsePistolDps, ArtifactType.NeverMissSpread },
        new[] { ArtifactType.ConvertWhiteToRed },
        new[] { ArtifactType.GoldFind }
    };

    private static readonly WeaponAugmentType[][] AugmentRequires =
    {
        Array.Empty<WeaponAugmentType>(), // Unknown
        Array.Empty<WeaponAugmentType>(), // ClickLauncherUnlock
        new[] { WeaponAugmentType.ClickLauncherUnlock },
        new[] { WeaponAugmentType.ClickLauncherUnlock },
        new[] { WeaponAugmentType.ClickLauncherUnlock },
        new[] { WeaponAugmentType.ClickLauncherUnlock },
        Array.Empty<WeaponAugmentType>(), // ClickPistolIdleSpeed
        Array.Empty<WeaponAugmentType>(), // ClickPistolAutoFire
        Array.Empty<WeaponAugmentType>(), // WeaponCubeChance
        Array.Empty<WeaponAugmentType>(), // WeaponCubeFind
        Array.Empty<WeaponAugmentType>(), // ClickCannonUnlock
        new[] { WeaponAugmentType.ClickCannonUnlock },
        new[] { WeaponAugmentType.ClickCannonUnlock },
        new[] { WeaponAugmentType.ClickCannonUnlock },
        new[] { WeaponAugmentType.ClickCannonUnlock },
        Array.Empty<WeaponAugmentType>(), // WeaponCubeStartWave
        new[] { WeaponAugmentType.ClickLauncherUnlock },
        new[] { WeaponAugmentType.ClickCannonUnlock },
        Array.Empty<WeaponAugmentType>(), // ClickPistolGoldSpawnChance
        new[] { WeaponAugmentType.ClickPistolGoldSpawnChance }
    };

    public static IReadOnlyList<ArtifactType> Requires(ArtifactType type) =>
        ArtifactRequires[(int)type];

    public static IReadOnlyList<WeaponAugmentType> Requires(WeaponAugmentType type) =>
        AugmentRequires[(int)type];

    public static bool IsArtifactLocked(ArtifactLoadout loadout, ArtifactType type) =>
        ArtifactRequires[(int)type].Any(required => loadout.GetLevel(required) == 0);

    public static bool IsWeaponAugmentLocked(WeaponAugmentLoadout loadout, WeaponAugmentType type) =>
        AugmentRequires[(int)type].Any(required => loadout.GetLevel(required) == 0);

    public static bool ArtifactDependentIsOwned(ArtifactLoadout loadout, ArtifactType type)
    {
        for (int i = 0; i < (int)ArtifactType.Total; i++)
        {
            if (loadout.GetLevel((ArtifactType)i) == 0)
                continue;

            if (ArtifactRequires[i].Contains(type))
                return true;
        }

        return false;
    }

    public static bool WeaponAugmentDependentIsOwned(
        WeaponAugmentLoadout loadout,
        WeaponAugmentType type)
    {
        for (int i = 0; i < (int)WeaponAugmentType.Total; i++)
        {
            if (loadout.GetLevel((WeaponAugmentType)i) == 0)
                continue;

            if (AugmentRequires[i].Contains(type))
                return true;
        }

        return false;
    }
}
