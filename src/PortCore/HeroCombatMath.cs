using System;

namespace TimeClickers.PortCore;

public sealed record HeroDpsMultipliers(
    double DimensionShift = 1.0,
    double UnitedFront = 1.0,
    double AchievementDps = 1.0,
    double TimeCubeDps = 1.0,
    double TeamDps = 1.0,
    double WeaponDps = 1.0,
    bool TeamWorkActive = false,
    double TeamWorkDps = 1.0);

public static class HeroCombatMath
{
    public static double GetDpsForLevel(
        HeroBaseSpec hero,
        InfiniteUpgradeSchedule schedule,
        int level,
        int purchasedUpgrades,
        HeroDpsMultipliers multipliers)
    {
        int rank = schedule.GetRank(purchasedUpgrades);

        double baseDamage = hero.BaseDamage *
                            HeroProgressionMath.GetBaseRankDpsMultiplier(rank);

        double rankBonus = 0.0;
        if (rank > 1)
        {
            level = HeroProgressionMath.GetLevelsSincePromotion(level, rank);
            rankBonus = baseDamage * 10.0;
        }

        double dps = baseDamage * level;
        double upgradeMultiplier = 1.0;

        int specOpsLevel = InfiniteUpgradeSchedule.GetSpecOpsLevelFromRank(rank);
        foreach (var upgrade in schedule.GetUpgradesForSpecOps(specOpsLevel))
        {
            if (upgrade.UpgradeId > purchasedUpgrades)
                break;

            if (upgrade.Has(UpgradeMod.HeroDps))
            {
                upgradeMultiplier *= upgrade.Get(UpgradeMod.HeroDps);
            }
            else if (upgrade.IncreasesRank)
            {
                // Promotion/Training/SpecOps starts a new rank segment.
                upgradeMultiplier = 1.0;
            }
        }

        if (multipliers.TeamWorkActive)
            upgradeMultiplier *= multipliers.TeamWorkDps;

        dps = (dps + rankBonus) *
              upgradeMultiplier *
              multipliers.DimensionShift *
              multipliers.UnitedFront *
              multipliers.AchievementDps *
              multipliers.TimeCubeDps *
              multipliers.TeamDps *
              multipliers.WeaponDps;

        return Math.Floor(dps);
    }

    public static float GetRateOfFire(
        float baseRateOfFire,
        InfiniteUpgradeSchedule schedule,
        int purchasedUpgrades)
    {
        float value = baseRateOfFire;

        foreach (var upgrade in schedule.EnumeratePurchased(purchasedUpgrades))
        {
            if (upgrade.IncreasesRank)
                value = baseRateOfFire;

            if (upgrade.Has(UpgradeMod.FireRate))
                value *= (float)upgrade.Get(UpgradeMod.FireRate);
        }

        return value;
    }

    public static int GetProjectileCount(
        int baseProjectileCount,
        InfiniteUpgradeSchedule schedule,
        int purchasedUpgrades)
    {
        int value = baseProjectileCount;

        foreach (var upgrade in schedule.EnumeratePurchased(purchasedUpgrades))
        {
            if (upgrade.IncreasesRank)
                value = baseProjectileCount;

            if (upgrade.Has(UpgradeMod.Projectiles))
                value = (int)upgrade.Get(UpgradeMod.Projectiles);
        }

        return value;
    }

    public static double GetColliderMod(
        double baseCollider,
        InfiniteUpgradeSchedule schedule,
        int purchasedUpgrades,
        double artifactColliderMultiplier = 1.0)
    {
        double value = baseCollider;

        foreach (var upgrade in schedule.EnumeratePurchased(purchasedUpgrades))
        {
            if (upgrade.IncreasesRank)
                value = baseCollider;

            if (upgrade.Has(UpgradeMod.Collider))
                value = (int)upgrade.Get(UpgradeMod.Collider);
        }

        return value * artifactColliderMultiplier;
    }

    public static double GetSplash(
        double baseSplash,
        InfiniteUpgradeSchedule schedule,
        int purchasedUpgrades)
    {
        double value = baseSplash;

        foreach (var upgrade in schedule.EnumeratePurchased(purchasedUpgrades))
        {
            if (upgrade.IncreasesRank)
                value = baseSplash;

            if (upgrade.Has(UpgradeMod.Splash))
                value = upgrade.Get(UpgradeMod.Splash);
        }

        return value;
    }

    public static double GetExtraClickDamagePercent(
        InfiniteUpgradeSchedule schedule,
        int purchasedUpgrades)
    {
        double value = 0.0;

        foreach (var upgrade in schedule.EnumeratePurchased(purchasedUpgrades))
            if (upgrade.Has(UpgradeMod.ClickDamage))
                value += upgrade.Get(UpgradeMod.ClickDamage);

        return value;
    }
}
