using System;
using System.Collections.Generic;

namespace TimeClickers.PortCore;

/// <summary>
/// Canonical numeric Weapon Augment balance from Time Clickers 1.4.5.
/// Entries are indexed by WeaponAugmentType.
/// </summary>
public static class CanonicalWeaponAugments
{
    public static IReadOnlyList<WeaponAugmentData> All { get; } = new WeaponAugmentData[]
    {
        new(0, 5.0, 0, 0, 1, 0.0, 1.0),
        new(1, 5.0, 0, 0, 1, 0.0, 1.0),
        new(5, 10.0, 0, 0, 5, 10.0, -1.0),
        new(100, 10.0, 0, 0, 4, 1.0, 1.0),
        new(2, 1.324, 25, 12000, 40, 1.0, 0.1),
        new(2, 1.06, 68, 1400, 200, 0.0, 1.0),
        new(5, 1.463, 20, 90000, 40, 1.0, 0.1),
        new(5, 1.08, 76, 20000, 200, 0.0, 2.0),
        new(2, 1.114, 67, 32860, 150, 25.0, 0.5),
        new(2, 1.08, 96, 25000, 200, 0.0, 0.5),
        new(50000, 5.0, 0, 0, 1, 0.0, 1.0),
        new(500, 4.0, 3, 500000, 15, 5.0, 1.0),
        new(100, 1.51, 10, 120000, 60, 35.0, -0.5),
        new(50, 1.611, 12, 200000, 40, 1.0, 0.1),
        new(50, 1.07, 66, 40000, 200, 0.0, 1.0),
        new(2, 1.1229, 60, 32000, 180, 1000.0, -5.0),
        new(10, 1.9, 11, 30000, 15, 100.0, 10.0),
        new(50, 1.229, 26, 110000, 75, 25.0, 1.0),
        new(100, 1.13, 40, 80000, 81, 19.0, 1.0),
        new(100, 1.122, 41, 70000, 90, 1.0, 0.1)
    };

    public static WeaponAugmentData Get(WeaponAugmentType type)
    {
        int index = (int)type;
        if (index < 0 || index >= (int)WeaponAugmentType.Total)
            throw new ArgumentOutOfRangeException(nameof(type));
        return All[index];
    }

    public static double GetModValue(WeaponAugmentType type, ulong level) =>
        WeaponAugmentMath.GetLinearModValue(Get(type), level);

    public static ulong GetUpgradeCost(WeaponAugmentType type, ulong level) =>
        WeaponAugmentMath.GetUpgradeCost(Get(type), level);
}
