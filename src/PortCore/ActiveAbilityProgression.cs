using System;
using System.Linq;

namespace TimeClickers.PortCore;

/// <summary>
/// Unity-independent ActiveAbilities owner reconstructed from Time Clickers 1.4.5.
/// Abilities are purchased sequentially with Gold. Dimension Shift stacks until
/// Time Warp; Cooldown removes 3600 seconds from purchased abilities except itself.
/// </summary>
public sealed class ActiveAbilityProgression
{
    public ActiveAbilityProgression(ArtifactEffects artifacts)
    {
        Abilities = AbilityCatalog.All
            .Select(spec => new AbilityRuntime(spec, artifacts))
            .ToArray();
    }

    public AbilityRuntime[] Abilities { get; }
    public int PurchasedCount { get; private set; }
    public int DimensionShifts { get; private set; }

    public bool IsPurchased(AbilityType type) => (int)type < PurchasedCount;

    public bool IsActive(AbilityType type) =>
        IsPurchased(type) &&
        Abilities[(int)type].State == AbilityState.Active;

    public double NextPurchaseCost =>
        PurchasedCount >= Abilities.Length
            ? double.PositiveInfinity
            : AbilityMath.GetCostForLevel(PurchasedCount);

    public bool TryPurchaseNext(GoldWallet gold)
    {
        if (PurchasedCount >= Abilities.Length)
            return false;

        double cost = NextPurchaseCost;
        if (!gold.TrySpend(cost))
            return false;

        PurchasedCount++;
        return true;
    }

    public bool Activate(AbilityType type, double nowSeconds)
    {
        if (!IsPurchased(type))
            return false;

        var ability = Abilities[(int)type];
        if (!ability.Activate(nowSeconds))
            return false;

        if (type == AbilityType.Cooldown)
        {
            for (int i = 0; i < PurchasedCount; i++)
            {
                if (i == (int)AbilityType.Cooldown)
                    continue;

                Abilities[i].ReduceCooldown(3600);
            }
        }
        else if (type == AbilityType.DimensionShift)
        {
            DimensionShifts++;
        }

        return true;
    }

    public void ActivateFree(AbilityType type, double nowSeconds)
    {
        if (!IsPurchased(type))
            return;

        Abilities[(int)type].ActivateFree(nowSeconds);
    }

    public void Update(double nowSeconds)
    {
        for (int i = 0; i < PurchasedCount; i++)
            Abilities[i].Update(nowSeconds);
    }

    public void Recalculate(ArtifactEffects artifacts, double nowSeconds)
    {
        foreach (var ability in Abilities)
            ability.Recalculate(artifacts, nowSeconds);
    }

    public double GetDimensionShiftMultiplier(ArtifactEffects artifacts) =>
        AbilityMath.GetDimensionShiftMultiplier(
            DimensionShifts,
            artifacts.DimensionShiftMultiplier);

    public void TimeWarp(ArtifactEffects artifacts)
    {
        PurchasedCount = 0;
        DimensionShifts = 0;

        foreach (var ability in Abilities)
            ability.TimeWarp();

        Recalculate(artifacts, 0);
    }
}
