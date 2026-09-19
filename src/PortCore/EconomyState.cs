using System;

namespace TimeClickers.PortCore;

public sealed class GoldWallet
{
    public double TotalGold { get; private set; }
    public double LifetimeGoldEarned { get; private set; }
    public double TimelineGoldPerSecond { get; set; }

    public bool CanAfford(double amount) => TotalGold >= amount;

    public void Add(double amount)
    {
        TotalGold += amount;
        if (double.IsNaN(TotalGold))
            TotalGold = 0.0;

        LifetimeGoldEarned += amount;
    }

    public bool TrySpend(double amount)
    {
        if (!CanAfford(amount))
            return false;

        TotalGold -= amount;
        return true;
    }

    public void TimeWarp(double startingGold)
    {
        TotalGold = startingGold;
        TimelineGoldPerSecond = 0.0;
    }

    public double GetOfflineGoldPerSecond() => TimelineGoldPerSecond * 0.5;
}

public sealed class TimeCubeBank
{
    public ulong Spendable { get; private set; }
    public ulong PendingTimelineReward { get; private set; }
    public ulong LifetimeEarned { get; private set; }

    public double DpsMultiplier => 1.0 + LifetimeEarned * 0.1;

    public void AddPendingReward(ulong amount)
    {
        PendingTimelineReward += amount;
    }

    public bool TrySpend(ulong amount)
    {
        if (Spendable < amount)
            return false;

        Spendable -= amount;
        return true;
    }

    public void Refund(ulong amount)
    {
        Spendable += amount;
    }

    public ulong TimeWarp()
    {
        ulong earned = PendingTimelineReward;
        Spendable += earned;
        LifetimeEarned += earned;
        PendingTimelineReward = 0;
        return earned;
    }
}

public sealed class WeaponCubeBankState
{
    public ulong Spendable { get; private set; }
    public ulong LifetimeEarned { get; private set; }

    public void Add(ulong amount)
    {
        Spendable += amount;
        LifetimeEarned += amount;
    }

    public bool TrySpend(ulong amount)
    {
        if (Spendable < amount)
            return false;

        Spendable -= amount;
        return true;
    }

    public void Refund(ulong amount)
    {
        Spendable += amount;
    }

    // Original DoTimeWarp resets only timeline statistics, not the balance.
    public void TimeWarp()
    {
    }
}
