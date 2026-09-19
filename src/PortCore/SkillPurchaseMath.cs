using System;

namespace TimeClickers.PortCore;

public sealed record SkillPurchasePlan(
    int LevelCount,
    bool PurchasesUpgrade,
    double TotalCost,
    bool Affordable,
    bool UpgradeNext,
    bool OutOfUpgrades);

public sealed record SkillPurchaseResult(
    bool Purchased,
    int NewLevel,
    int NewPurchasedUpgrades,
    double GoldSpent,
    bool BoughtUpgrade);

/// <summary>
/// Reconstructs Skill.RecalculateMaxPurchaseCount,
/// Skill.RecalculateUpgradeCost and Skill.BuyUpgrade.
/// </summary>
public static class SkillPurchaseMath
{
    public static bool UpgradeAvailable(SkillSpec skill, int level, int purchasedUpgrades)
    {
        if (purchasedUpgrades >= skill.Upgrades.Count)
            return false;

        return level >= skill.Upgrades[purchasedUpgrades].LevelRequired;
    }

    public static bool OutOfUpgrades(SkillSpec skill, int purchasedUpgrades) =>
        purchasedUpgrades >= skill.Upgrades.Count;

    public static int GetLevelUpsUntilUpgrade(SkillSpec skill, int level, int purchasedUpgrades)
    {
        if (purchasedUpgrades >= skill.Upgrades.Count)
            return Math.Min(25, 125 - level);

        return skill.Upgrades[purchasedUpgrades].LevelRequired - level;
    }

    public static SkillPurchasePlan Plan(
        SkillSpec skill,
        int level,
        int purchasedUpgrades,
        double availableGold,
        HeroBuyMode mode)
    {
        bool outOfUpgrades = OutOfUpgrades(skill, purchasedUpgrades);

        // The old UI removes the skill panel after all upgrades. Keep a
        // mechanically valid plan for compatibility, but expose the state.
        int maxPurchaseCount = 1;
        int levelsUntilUpgrade = GetLevelUpsUntilUpgrade(skill, level, purchasedUpgrades);

        if (mode != HeroBuyMode.OneLevel)
        {
            double runningCost = SkillMath.GetLevelUpCostForLevel(skill.BaseCost, level);

            for (int step = 1; step < levelsUntilUpgrade; step++)
            {
                runningCost += SkillMath.GetLevelUpCostForLevel(
                    skill.BaseCost,
                    level + step);

                if (availableGold >= runningCost)
                    maxPurchaseCount++;
                else
                    break;
            }
        }

        bool upgradeAvailable = UpgradeAvailable(skill, level, purchasedUpgrades);
        bool purchasesUpgrade = maxPurchaseCount <= 1 && upgradeAvailable;

        double totalCost;
        if (upgradeAvailable)
        {
            totalCost = SkillMath.GetUpgradeCostForLevel(skill.BaseCost, level);
        }
        else
        {
            totalCost = 0.0;
            for (int i = 0; i < maxPurchaseCount; i++)
                totalCost += SkillMath.GetLevelUpCostForLevel(skill.BaseCost, level + i);
        }

        return new SkillPurchasePlan(
            LevelCount: purchasesUpgrade ? 0 : maxPurchaseCount,
            PurchasesUpgrade: purchasesUpgrade,
            TotalCost: totalCost,
            Affordable: availableGold >= totalCost,
            UpgradeNext: maxPurchaseCount == levelsUntilUpgrade,
            OutOfUpgrades: outOfUpgrades);
    }

    public static SkillPurchaseResult Apply(
        int level,
        int purchasedUpgrades,
        double availableGold,
        SkillPurchasePlan plan)
    {
        if (availableGold < plan.TotalCost)
            return new SkillPurchaseResult(false, level, purchasedUpgrades, 0.0, false);

        if (plan.PurchasesUpgrade)
        {
            return new SkillPurchaseResult(
                true,
                level,
                purchasedUpgrades + 1,
                plan.TotalCost,
                true);
        }

        return new SkillPurchaseResult(
            true,
            level + plan.LevelCount,
            purchasedUpgrades,
            plan.TotalCost,
            false);
    }
}

public sealed class SkillRuntime
{
    public SkillRuntime(SkillSpec spec)
    {
        Spec = spec;
    }

    public SkillSpec Spec { get; }
    public int Level { get; private set; }
    public int PurchasedUpgrades { get; private set; }

    public bool UpgradeAvailable =>
        SkillPurchaseMath.UpgradeAvailable(Spec, Level, PurchasedUpgrades);

    public bool OutOfUpgrades =>
        SkillPurchaseMath.OutOfUpgrades(Spec, PurchasedUpgrades);

    public SkillPurchasePlan PlanPurchase(double availableGold, HeroBuyMode mode) =>
        SkillPurchaseMath.Plan(Spec, Level, PurchasedUpgrades, availableGold, mode);

    public SkillPurchaseResult Purchase(double availableGold, HeroBuyMode mode)
    {
        var plan = PlanPurchase(availableGold, mode);
        var result = SkillPurchaseMath.Apply(
            Level, PurchasedUpgrades, availableGold, plan);

        if (result.Purchased)
        {
            Level = result.NewLevel;
            PurchasedUpgrades = result.NewPurchasedUpgrades;
        }

        return result;
    }

    public double GetDamage(double extraHeroClickDamage, double artifactClickDamageMultiplier) =>
        SkillMath.GetDamageForLevel(
            Spec.BaseDamage,
            Level,
            Spec.Upgrades,
            PurchasedUpgrades,
            extraHeroClickDamage,
            artifactClickDamageMultiplier);

    public void TimeWarp()
    {
        Level = 0;
        PurchasedUpgrades = 0;
    }
}
