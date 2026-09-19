using System;
using System.Collections.Generic;

namespace TimeClickers.PortCore;

public static class SkillMath
{
    public static double GetLevelUpCostForLevel(int baseCost, int level) =>
        Math.Floor((baseCost + level) * Math.Pow(1.07, level));

    public static double GetUpgradeCostForLevel(int baseCost, int level) =>
        GetLevelUpCostForLevel(baseCost, level) * 3.0;

    public static double GetDamageForLevel(
        double baseDamage,
        int level,
        IReadOnlyList<UpgradeSpec> upgrades,
        int purchasedUpgrades,
        double extraHeroClickDamage,
        double artifactClickDamageMultiplier)
    {
        double damage = baseDamage * level + 1.0;
        double upgradeMultiplier = 1.0;

        int count = Math.Min(purchasedUpgrades, upgrades.Count);
        for (int i = 0; i < count; i++)
            if (upgrades[i].Has(UpgradeMod.ClickDamage))
                upgradeMultiplier *= upgrades[i].Get(UpgradeMod.ClickDamage);

        damage *= upgradeMultiplier;
        damage += extraHeroClickDamage;
        damage *= artifactClickDamageMultiplier;

        return Math.Floor(damage);
    }

    public static double GetClickDamage(
        double baseCalculatedDamage,
        double achievementAdditionalClickDamage,
        bool isCritical,
        double criticalMultiplier,
        bool explosiveShotsActive,
        double explosiveShotsMultiplier)
    {
        double damage = baseCalculatedDamage + achievementAdditionalClickDamage;

        if (isCritical)
            damage *= criticalMultiplier;

        if (explosiveShotsActive)
            damage *= explosiveShotsMultiplier;

        return damage;
    }

    public static double GetCriticalMultiplier(
        double heroCriticalMultiplier,
        bool overchargedActive,
        double additionalOverchargedMultiplier,
        double artifactAdditionalCriticalMultiplier)
    {
        double value = heroCriticalMultiplier;

        if (overchargedActive)
            value += additionalOverchargedMultiplier;

        value += artifactAdditionalCriticalMultiplier;
        return value;
    }

    public static float GetCriticalChance(
        float heroCriticalChance,
        float achievementAdditionalChance,
        float artifactAdditionalChance,
        bool augmentedAimActive,
        float augmentedAimMultiplier)
    {
        float value = heroCriticalChance + achievementAdditionalChance + artifactAdditionalChance;
        if (augmentedAimActive)
            value *= augmentedAimMultiplier;
        return value;
    }
}
