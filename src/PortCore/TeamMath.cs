using System;
using System.Collections.Generic;

namespace TimeClickers.PortCore;

public sealed record HeroUpgradeProgress(
    InfiniteUpgradeSchedule Schedule,
    int PurchasedUpgrades);

public sealed record TeamUpgradeEffects(
    double UnitedFrontMultiplier,
    double CriticalMultiplier,
    float CriticalChance,
    double GoldFindMultiplier);

public static class TeamMath
{
    public static TeamUpgradeEffects RecalculateBaseUpgradeEffects(
        IEnumerable<HeroUpgradeProgress> heroes)
    {
        double unitedFront = 1.0;
        double criticalMultiplier = 5.0;
        float criticalChance = 0f;
        double goldFindMultiplier = 1.0;

        foreach (var hero in heroes)
        {
            int count = Math.Min(hero.PurchasedUpgrades, hero.Schedule.BaseUpgradeCount);

            for (int i = 0; i < count; i++)
            {
                var upgrade = hero.Schedule.BaseUpgrades[i];

                if (upgrade.Has(UpgradeMod.UnitedFront))
                    unitedFront *= upgrade.Get(UpgradeMod.UnitedFront);

                if (upgrade.Has(UpgradeMod.CriticalDamage))
                    criticalMultiplier += upgrade.Get(UpgradeMod.CriticalDamage);

                if (upgrade.Has(UpgradeMod.CriticalChance))
                    criticalChance += (float)upgrade.Get(UpgradeMod.CriticalChance);

                if (upgrade.Has(UpgradeMod.GoldFind))
                    goldFindMultiplier *= upgrade.Get(UpgradeMod.GoldFind);
            }
        }

        return new TeamUpgradeEffects(
            unitedFront,
            criticalMultiplier,
            criticalChance,
            goldFindMultiplier);
    }

    public static double GetExtraClickDamage(
        IEnumerable<(double HeroDps, double ExtraClickDamagePercent)> heroes)
    {
        double totalDps = 0.0;
        double totalPercent = 0.0;

        foreach (var hero in heroes)
        {
            totalDps += hero.HeroDps;
            totalPercent += hero.ExtraClickDamagePercent;
        }

        return totalPercent > 0.0 ? totalPercent * totalDps : 0.0;
    }
}
