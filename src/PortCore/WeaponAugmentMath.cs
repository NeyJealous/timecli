using System;

namespace TimeClickers.PortCore;

public sealed record WeaponAugmentData(
    ulong StartingCost,
    double CurveMultiplier,
    ulong BendStartLevel,
    ulong BendEndCost,
    ulong MaxLevel,
    double BaseValue,
    double IncValue);

public static class WeaponAugmentMath
{
    public static ulong GetMultiplierCost(WeaponAugmentData d, ulong level) =>
        (ulong)Math.Ceiling(Math.Pow(d.CurveMultiplier, level) * d.StartingCost);

    public static ulong GetRoundULong(ulong value)
    {
        int digits = (int)Math.Ceiling(Math.Log10(value));
        if (digits is 3 or 4) return value / 10UL * 10UL;
        if (digits > 4) return value / 100UL * 100UL;
        return value;
    }

    public static ulong GetUpgradeCost(WeaponAugmentData d, ulong level)
    {
        if (d.MaxLevel == 1) return d.StartingCost;

        if (d.BendStartLevel == 0 || level <= d.BendStartLevel)
            return GetRoundULong(GetMultiplierCost(d, level));

        float a = (float)GetMultiplierCost(d, d.BendStartLevel);
        float b = (float)d.BendEndCost;
        float t = (float)(level - d.BendStartLevel) /
                  ((float)(d.MaxLevel - d.BendStartLevel) - 1f);
        t = Math.Clamp(t, 0f, 1f);
        ulong interpolated = (ulong)(a + (b - a) * t);
        return GetRoundULong(interpolated);
    }

    public static double GetLinearModValue(WeaponAugmentData d, ulong level) =>
        d.BaseValue + d.IncValue * level;
}
