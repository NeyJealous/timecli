using System;

namespace TimeClickers.PortCore;

public sealed record ProgressionTransaction(
    bool Succeeded,
    ulong CostOrRefund,
    ulong NewLevel,
    string? Reason = null);

public static class ArtifactEconomy
{
    public static bool IsMaxed(ArtifactLoadout loadout, ArtifactType type) =>
        loadout.GetLevel(type) >= CanonicalArtifacts.Get(type).MaxLevel;

    public static bool CanSell(ArtifactLoadout loadout, ArtifactType type)
    {
        ulong level = loadout.GetLevel(type);
        if (level == 0)
            return false;

        if (level == 1 && ProgressionDependencies.ArtifactDependentIsOwned(loadout, type))
            return false;

        return true;
    }

    public static ulong GetSellValue(ArtifactLoadout loadout, ArtifactType type)
    {
        ulong level = loadout.GetLevel(type);
        return level == 0
            ? 0
            : (ulong)CanonicalArtifacts.GetUpgradeCost(type, level - 1);
    }

    public static ulong GetTotalSpent(ArtifactLoadout loadout)
    {
        ulong total = 0;

        for (int i = 0; i < (int)ArtifactType.Total; i++)
        {
            var type = (ArtifactType)i;
            ulong level = loadout.GetLevel(type);
            ulong max = CanonicalArtifacts.Get(type).MaxLevel;
            ulong count = Math.Min(level, max);

            for (ulong ownedLevel = 0; ownedLevel < count; ownedLevel++)
                total += (ulong)CanonicalArtifacts.GetUpgradeCost(type, ownedLevel);
        }

        return total;
    }

    public static ProgressionTransaction BuyOne(
        ArtifactLoadout loadout,
        TimeCubeBank bank,
        ArtifactType type)
    {
        if (ProgressionDependencies.IsArtifactLocked(loadout, type))
            return new(false, 0, loadout.GetLevel(type), "locked");

        if (IsMaxed(loadout, type))
            return new(false, 0, loadout.GetLevel(type), "max-level");

        ulong cost = (ulong)loadout.GetNextUpgradeCost(type);
        if (!bank.TrySpend(cost))
            return new(false, cost, loadout.GetLevel(type), "insufficient-time-cubes");

        ulong newLevel = loadout.GetLevel(type) + 1;
        loadout.SetLevel(type, newLevel);
        return new(true, cost, newLevel);
    }

    public static ProgressionTransaction SellOne(
        ArtifactLoadout loadout,
        TimeCubeBank bank,
        ArtifactType type)
    {
        if (!CanSell(loadout, type))
            return new(false, 0, loadout.GetLevel(type), "cannot-sell");

        ulong refund = GetSellValue(loadout, type);
        ulong newLevel = loadout.GetLevel(type) - 1;
        loadout.SetLevel(type, newLevel);
        bank.Refund(refund);
        return new(true, refund, newLevel);
    }

    /// <summary>
    /// Convenience helper for headless/UI hold-to-buy. The original BuyUpgrade
    /// purchases one level per invocation; this simply repeats that exact action.
    /// </summary>
    public static int BuyMax(
        ArtifactLoadout loadout,
        TimeCubeBank bank,
        ArtifactType type)
    {
        int count = 0;
        while (true)
        {
            var result = BuyOne(loadout, bank, type);
            if (!result.Succeeded)
                return count;
            count++;
        }
    }
}

public static class WeaponAugmentEconomy
{
    public static bool IsMaxed(WeaponAugmentLoadout loadout, WeaponAugmentType type) =>
        loadout.GetLevel(type) >= CanonicalWeaponAugments.Get(type).MaxLevel;

    public static bool CanSell(WeaponAugmentLoadout loadout, WeaponAugmentType type)
    {
        ulong level = loadout.GetLevel(type);
        if (level == 0)
            return false;

        if (level == 1 && ProgressionDependencies.WeaponAugmentDependentIsOwned(loadout, type))
            return false;

        return true;
    }

    public static ulong GetSellValue(WeaponAugmentLoadout loadout, WeaponAugmentType type)
    {
        ulong level = loadout.GetLevel(type);
        return level == 0
            ? 0
            : CanonicalWeaponAugments.GetUpgradeCost(type, level - 1);
    }

    public static ulong GetTotalSpent(WeaponAugmentLoadout loadout)
    {
        ulong total = 0;

        for (int i = 0; i < (int)WeaponAugmentType.Total; i++)
        {
            var type = (WeaponAugmentType)i;
            ulong level = loadout.GetLevel(type);
            ulong max = CanonicalWeaponAugments.Get(type).MaxLevel;
            ulong count = Math.Min(level, max);

            for (ulong ownedLevel = 0; ownedLevel < count; ownedLevel++)
                total += CanonicalWeaponAugments.GetUpgradeCost(type, ownedLevel);
        }

        return total;
    }

    public static ProgressionTransaction BuyOne(
        WeaponAugmentLoadout loadout,
        WeaponCubeBankState bank,
        WeaponAugmentType type)
    {
        if (ProgressionDependencies.IsWeaponAugmentLocked(loadout, type))
            return new(false, 0, loadout.GetLevel(type), "locked");

        if (IsMaxed(loadout, type))
            return new(false, 0, loadout.GetLevel(type), "max-level");

        ulong cost = loadout.GetNextUpgradeCost(type);
        if (!bank.TrySpend(cost))
            return new(false, cost, loadout.GetLevel(type), "insufficient-weapon-cubes");

        ulong newLevel = loadout.GetLevel(type) + 1;
        loadout.SetLevel(type, newLevel);
        return new(true, cost, newLevel);
    }

    public static ProgressionTransaction SellOne(
        WeaponAugmentLoadout loadout,
        WeaponCubeBankState bank,
        WeaponAugmentType type)
    {
        if (!CanSell(loadout, type))
            return new(false, 0, loadout.GetLevel(type), "cannot-sell");

        ulong refund = GetSellValue(loadout, type);
        ulong newLevel = loadout.GetLevel(type) - 1;
        loadout.SetLevel(type, newLevel);
        bank.Refund(refund);
        return new(true, refund, newLevel);
    }

    public static int BuyMax(
        WeaponAugmentLoadout loadout,
        WeaponCubeBankState bank,
        WeaponAugmentType type)
    {
        int count = 0;
        while (true)
        {
            var result = BuyOne(loadout, bank, type);
            if (!result.Succeeded)
                return count;
            count++;
        }
    }
}
