using System;

namespace TimeClickers.PortCore;

public sealed record ArtifactData(
    double BaseCost,
    ulong MaxLevel,
    double BaseValue,
    double IncValue,
    double UpgradeMultiplier,
    double UpgradeExponent,
    double UpgradeExponentScale,
    ulong BendStartLevel,
    ulong BendEndCost);

public static class ArtifactMath
{
    public static ulong GetOldUpgradeCost(ArtifactData d, ulong level)
    {
        if (d.UpgradeExponent != 0.0)
        {
            var x = Math.Pow((level + d.BaseCost) / d.UpgradeExponentScale, d.UpgradeExponent);
            return (ulong)Math.Floor(Math.Ceiling(x) * d.UpgradeMultiplier);
        }

        return (ulong)Math.Ceiling(d.BaseCost * Math.Pow(d.UpgradeMultiplier, level));
    }

    public static ulong GetNewUpgradeCost(ArtifactData d, ulong level)
    {
        if (d.BendStartLevel == 0 || level <= d.BendStartLevel)
            return GetOldUpgradeCost(d, level);

        float a = (float)GetOldUpgradeCost(d, d.BendStartLevel);
        float b = (float)d.BendEndCost;
        float t = (float)(level - d.BendStartLevel) /
                  ((float)(d.MaxLevel - d.BendStartLevel) - 1f);
        t = Math.Clamp(t, 0f, 1f);
        return (ulong)(a + (b - a) * t);
    }

    public static double GetLinearModValue(ArtifactData d, ulong level) =>
        d.BaseValue + d.IncValue * level;
}
