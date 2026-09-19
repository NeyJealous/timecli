using System;

namespace TimeClickers.PortCore;

public enum HeroBuyMode
{
    OneLevel = 0,
    NextUpgrade = 1,
    NextRank = 2,
    Max = 3
}

public sealed record HeroPurchasePlan(
    int LevelCount,
    int UpgradeCount,
    double TotalCost,
    bool Affordable,
    bool UpgradeNext,
    bool PromotionNext,
    bool TrainingNext,
    bool SpecOpsNext)
{
    public int TotalItems => LevelCount + UpgradeCount;
}

public sealed record HeroPurchaseResult(
    bool Purchased,
    int NewLevel,
    int NewPurchasedUpgrades,
    double GoldSpent,
    int RankIncreases,
    int UnitedFrontUpgrades);

/// <summary>
/// Headless reconstruction of Hero.RecalculateMaxPurchaseCount,
/// Hero.RecalculateLevelUpCost and the state-changing portion of Hero.BuyUpgrade.
/// </summary>
public static class HeroPurchaseMath
{
    public static bool UpgradeAvailable(
        InfiniteUpgradeSchedule schedule,
        int level,
        int purchasedUpgrades) =>
        level >= schedule.GetUpgrade(purchasedUpgrades).LevelRequired;

    public static int GetLevelUpsUntilUpgrade(
        InfiniteUpgradeSchedule schedule,
        int level,
        int purchasedUpgrades) =>
        schedule.GetUpgrade(purchasedUpgrades).LevelRequired - level;

    public static int GetLevelUpsUntilRankIncrease(
        InfiniteUpgradeSchedule schedule,
        int level,
        int purchasedUpgrades)
    {
        for (int i = 0; i < 10; i++)
        {
            var upgrade = schedule.GetUpgrade(purchasedUpgrades + i);
            if (upgrade.IncreasesRank)
                return upgrade.LevelRequired - level;
        }

        return 100;
    }

    public static double GetUpgradeCost(
        HeroBaseSpec hero,
        InfiniteUpgradeSchedule schedule,
        int upgradeIndex)
    {
        var upgrade = schedule.GetUpgrade(upgradeIndex);
        int rankBeforeUpgrade = schedule.GetRank(upgradeIndex);
        return HeroProgressionMath.GetUpgradeCostForLevel(
            hero.BaseCostLevel,
            upgrade.LevelRequired,
            rankBeforeUpgrade);
    }

    public static HeroPurchasePlan Plan(
        HeroBaseSpec hero,
        InfiniteUpgradeSchedule schedule,
        int level,
        int purchasedUpgrades,
        double availableGold,
        HeroBuyMode mode)
    {
        if (level < 0)
            throw new ArgumentOutOfRangeException(nameof(level));
        if (purchasedUpgrades < 0)
            throw new ArgumentOutOfRangeException(nameof(purchasedUpgrades));

        int levelCount;
        int upgradeCount;
        int levelsUntilUpgrade = GetLevelUpsUntilUpgrade(schedule, level, purchasedUpgrades);

        if (UpgradeAvailable(schedule, level, purchasedUpgrades))
        {
            levelCount = 0;
            upgradeCount = 1;

            double cost = CalculateTotalCost(
                hero, schedule, level, purchasedUpgrades, levelCount, upgradeCount);

            // The original takes an early return here after explicitly
            // clearing all "...Next" UI flags.
            return new HeroPurchasePlan(
                levelCount,
                upgradeCount,
                cost,
                CanAfford(availableGold, cost),
                UpgradeNext: false,
                PromotionNext: false,
                TrainingNext: false,
                SpecOpsNext: false);
        }

        levelCount = 1;
        upgradeCount = 0;

        int desiredLevels = mode switch
        {
            HeroBuyMode.OneLevel => 1,
            HeroBuyMode.NextUpgrade => levelsUntilUpgrade,
            HeroBuyMode.NextRank => GetLevelUpsUntilRankIncrease(schedule, level, purchasedUpgrades),
            HeroBuyMode.Max => 1000,
            _ => 1
        };

        if (mode == HeroBuyMode.NextRank)
            levelsUntilUpgrade = desiredLevels;
        else if (mode == HeroBuyMode.Max)
            levelsUntilUpgrade = 90000;

        if (desiredLevels > 1)
        {
            int rank = schedule.GetRank(purchasedUpgrades);
            double runningCost = HeroProgressionMath.GetLevelUpCostForLevel(
                hero.BaseCostLevel, level, rank);

            for (int step = 1; step < desiredLevels; step++)
            {
                var nextUpgrade = schedule.GetUpgrade(purchasedUpgrades + upgradeCount);

                if (nextUpgrade.LevelRequired <= level + step)
                {
                    runningCost += GetUpgradeCost(
                        hero, schedule, purchasedUpgrades + upgradeCount);

                    if (CanAfford(availableGold, runningCost))
                    {
                        upgradeCount++;
                        if (nextUpgrade.IncreasesRank)
                            rank++;
                    }
                    else
                    {
                        break;
                    }
                }

                runningCost += HeroProgressionMath.GetLevelUpCostForLevel(
                    hero.BaseCostLevel,
                    level + step,
                    rank);

                if (CanAfford(availableGold, runningCost))
                    levelCount++;
                else
                    break;
            }
        }

        double totalCost = CalculateTotalCost(
            hero, schedule, level, purchasedUpgrades, levelCount, upgradeCount);

        return BuildPlanFlags(
            schedule,
            purchasedUpgrades,
            levelCount,
            upgradeCount,
            levelsUntilUpgrade,
            totalCost,
            availableGold);
    }

    public static double CalculateTotalCost(
        HeroBaseSpec hero,
        InfiniteUpgradeSchedule schedule,
        int level,
        int purchasedUpgrades,
        int levelCount,
        int upgradeCount)
    {
        double cost = 0.0;

        for (int i = 0; i < upgradeCount; i++)
            cost += GetUpgradeCost(hero, schedule, purchasedUpgrades + i);

        var nextUpgrade = schedule.GetUpgrade(purchasedUpgrades);
        int rank = schedule.GetRank(purchasedUpgrades);
        int encounteredUpgradeCount = 0;

        for (int i = 0; i < levelCount; i++)
        {
            int prospectiveLevel = level + i;

            if (nextUpgrade.LevelRequired <= prospectiveLevel)
            {
                if (nextUpgrade.IncreasesRank)
                    rank++;

                encounteredUpgradeCount++;
                nextUpgrade = schedule.GetUpgrade(
                    purchasedUpgrades + encounteredUpgradeCount);
            }

            cost += HeroProgressionMath.GetLevelUpCostForLevel(
                hero.BaseCostLevel,
                prospectiveLevel,
                rank);
        }

        return cost;
    }

    public static HeroPurchaseResult Apply(
        HeroBaseSpec hero,
        InfiniteUpgradeSchedule schedule,
        int level,
        int purchasedUpgrades,
        double availableGold,
        HeroPurchasePlan plan)
    {
        _ = hero;

        if (!CanAfford(availableGold, plan.TotalCost))
        {
            return new HeroPurchaseResult(
                false, level, purchasedUpgrades, 0.0, 0, 0);
        }

        int rankIncreases = 0;
        int unitedFrontUpgrades = 0;

        for (int i = 0; i < plan.UpgradeCount; i++)
        {
            var upgrade = schedule.GetUpgrade(purchasedUpgrades + i);
            if (upgrade.IncreasesRank)
                rankIncreases++;
            else if (upgrade.Has(UpgradeMod.UnitedFront))
                unitedFrontUpgrades++;
        }

        return new HeroPurchaseResult(
            true,
            level + plan.LevelCount,
            purchasedUpgrades + plan.UpgradeCount,
            plan.TotalCost,
            rankIncreases,
            unitedFrontUpgrades);
    }

    private static HeroPurchasePlan BuildPlanFlags(
        InfiniteUpgradeSchedule schedule,
        int purchasedUpgrades,
        int levelCount,
        int upgradeCount,
        int levelsUntilUpgrade,
        double cost,
        double availableGold)
    {
        bool upgradeNext = false;
        bool promotionNext = false;
        bool trainingNext = false;
        bool specOpsNext = false;

        if (levelCount == levelsUntilUpgrade)
        {
            var next = schedule.GetUpgrade(purchasedUpgrades + upgradeCount);
            promotionNext = next.Has(UpgradeMod.Promotion);
            trainingNext = next.Has(UpgradeMod.Training);
            specOpsNext = next.Has(UpgradeMod.SpecOps);
            upgradeNext = !promotionNext && !trainingNext && !specOpsNext;
        }

        return new HeroPurchasePlan(
            levelCount,
            upgradeCount,
            cost,
            CanAfford(availableGold, cost),
            upgradeNext,
            promotionNext,
            trainingNext,
            specOpsNext);
    }

    private static bool CanAfford(double availableGold, double cost) =>
        availableGold >= cost;
}

public sealed class HeroRuntime
{
    public HeroRuntime(HeroBaseSpec spec)
    {
        Spec = spec;
        Schedule = new InfiniteUpgradeSchedule(spec.BaseUpgrades);
    }

    public HeroBaseSpec Spec { get; }
    public InfiniteUpgradeSchedule Schedule { get; }
    public int Level { get; private set; }
    public int PurchasedUpgrades { get; private set; }
    public int Rank => Schedule.GetRank(PurchasedUpgrades);

    public HeroPurchasePlan PlanPurchase(double availableGold, HeroBuyMode mode) =>
        HeroPurchaseMath.Plan(
            Spec, Schedule, Level, PurchasedUpgrades, availableGold, mode);

    public HeroPurchaseResult Purchase(double availableGold, HeroBuyMode mode)
    {
        var plan = PlanPurchase(availableGold, mode);
        var result = HeroPurchaseMath.Apply(
            Spec, Schedule, Level, PurchasedUpgrades, availableGold, plan);

        if (result.Purchased)
        {
            Level = result.NewLevel;
            PurchasedUpgrades = result.NewPurchasedUpgrades;
        }

        return result;
    }

    public void TimeWarp()
    {
        Level = 0;
        PurchasedUpgrades = 0;
    }
}
