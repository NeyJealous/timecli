using System;

namespace TimeClickers.PortCore;

public static class HeroProgressionMath
{
    public static double GetCostLevelMultiplier(int costLevel, int specOpsLevel)
    {
        _ = specOpsLevel;
        if (costLevel == 0) return 45.0;
        if (costLevel == 1) return 90.0;
        return 720.0 * Math.Pow(9.5, (costLevel - 2) * 0.77);
    }

    public static int GetSpecOpsLevelFromRank(int rank) => (rank - 1) / 10;

    public static int GetLevelsSincePromotion(int level, int rank) =>
        Math.Max(level - (rank - 1) * 100, 0);

    public static double GetLevelUpCostForLevel(int baseCostLevel, int level, int rank)
    {
        int costLevel = baseCostLevel + (rank - 1) * 5;
        int levelsSincePromotion = GetLevelsSincePromotion(level, rank);
        double baseMultiplier = GetCostLevelMultiplier(costLevel, GetSpecOpsLevelFromRank(rank));
        return Math.Floor(baseMultiplier * Math.Pow(1.08, levelsSincePromotion));
    }

    public static double GetUpgradeCostForLevel(int baseCostLevel, int level, int rank)
    {
        int costLevel = baseCostLevel + (rank - 1) * 5 + 2;
        int levelsSincePromotion = GetLevelsSincePromotion(level, rank);
        double baseMultiplier = GetCostLevelMultiplier(costLevel, GetSpecOpsLevelFromRank(rank));
        return Math.Floor(baseMultiplier * Math.Pow(1.08, levelsSincePromotion - 10));
    }

    public static readonly double[] PromoteMultipliers =
    {
        4745.0, 4745.0, 4745.0, 4745.0,
        600.0, 600.0, 600.0, 600.0, 600.0,
        100000.0
    };

    public static double GetBaseRankDpsMultiplier(int rank)
    {
        double result = 1.0;
        for (int i = 0; i < rank - 1; i++)
            result *= PromoteMultipliers[i % PromoteMultipliers.Length];
        return result;
    }
}
