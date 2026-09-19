using System;
using System.Collections.Generic;

namespace TimeClickers.PortCore;

public enum AbilityType
{
    AutomaticFire = 0,
    SpreadShots = 1,
    TeamWork = 2,
    AugmentedAim = 3,
    Overcharged = 4,
    GoldRush = 5,
    DimensionShift = 6,
    ExplosiveShots = 7,
    PunchThrough = 8,
    Cooldown = 9
}

public enum AbilityState
{
    Ready = 0,
    Active = 1,
    Recharging = 2
}

public sealed record AbilitySpec(
    AbilityType Type,
    string Name,
    int BaseDurationSeconds,
    int BaseRechargeSeconds);

public static class AbilityCatalog
{
    public static IReadOnlyList<AbilitySpec> All { get; } = new[]
    {
        new AbilitySpec(AbilityType.AutomaticFire, "Automatic Fire", 30, 60),
        new AbilitySpec(AbilityType.SpreadShots, "Spread Shots", 30, 180),
        new AbilitySpec(AbilityType.TeamWork, "Team Work", 30, 600),
        new AbilitySpec(AbilityType.AugmentedAim, "Augmented Aim", 30, 1800),
        new AbilitySpec(AbilityType.Overcharged, "Overcharged", 30, 1800),
        new AbilitySpec(AbilityType.GoldRush, "Gold Rush", 30, 1800),
        new AbilitySpec(AbilityType.DimensionShift, "Dimension Shift", 0, 28800),
        new AbilitySpec(AbilityType.ExplosiveShots, "Explosive Shots", 30, 3600),
        new AbilitySpec(AbilityType.PunchThrough, "Punchthrough", 30, 3600),
        new AbilitySpec(AbilityType.Cooldown, "Cooldown", 0, 3600)
    };

    public static AbilitySpec Get(AbilityType type) => All[(int)type];
}

/// <summary>
/// Unity-independent runtime state machine matching Ability in Time Clickers 1.4.5.
/// Time values are elapsed seconds from an arbitrary monotonic clock.
/// </summary>
public sealed class AbilityRuntime
{
    public AbilityRuntime(AbilitySpec spec, ArtifactEffects artifacts, double nowSeconds = 0)
    {
        Spec = spec;
        Recalculate(artifacts, nowSeconds);
    }

    public AbilitySpec Spec { get; }
    public AbilityState State { get; private set; } = AbilityState.Ready;
    public int DurationSeconds { get; private set; }
    public int RechargeSeconds { get; private set; }
    public double DeactivateAt { get; private set; }
    public double RechargedAt { get; private set; }

    public bool Activate(double nowSeconds)
    {
        if (State != AbilityState.Ready)
            return false;

        DeactivateAt = nowSeconds + DurationSeconds;
        RechargedAt = DeactivateAt + RechargeSeconds;
        State = AbilityState.Active;
        return true;
    }

    public void ActivateFree(double nowSeconds)
    {
        double halfDuration = DurationSeconds / 2;
        DeactivateAt = Math.Max(
            nowSeconds + halfDuration,
            DeactivateAt + halfDuration);
        State = AbilityState.Active;
    }

    public AbilityState Update(double nowSeconds)
    {
        var stateAtStart = State;

        if (stateAtStart == AbilityState.Active && nowSeconds >= DeactivateAt)
            State = AbilityState.Recharging;
        else if (stateAtStart == AbilityState.Recharging && nowSeconds >= RechargedAt)
            State = AbilityState.Ready;

        return State;
    }

    public int GetSecondsUntilRecharged(double nowSeconds)
    {
        if (nowSeconds > RechargedAt)
            return 0;

        return (int)(RechargedAt - nowSeconds);
    }

    public void SetSecondsUntilRecharged(int seconds, double nowSeconds)
    {
        if (seconds <= 0)
        {
            RechargedAt = 0;
            State = AbilityState.Ready;
            return;
        }

        RechargedAt = nowSeconds + seconds;
        State = AbilityState.Recharging;
    }

    public double GetNormalizedTimeRemaining(double nowSeconds)
    {
        if (State == AbilityState.Active)
            return DurationSeconds == 0
                ? 0.0
                : (DeactivateAt - nowSeconds) / DurationSeconds;

        if (State == AbilityState.Recharging)
            return RechargeSeconds == 0
                ? 0.0
                : (RechargedAt - nowSeconds) / RechargeSeconds;

        return 0.0;
    }

    public int GetRemainingSeconds(double nowSeconds) => State switch
    {
        AbilityState.Active => (int)(DeactivateAt - nowSeconds),
        AbilityState.Recharging => (int)(RechargedAt - nowSeconds),
        _ => DurationSeconds
    };

    public int GetRemainingActiveSeconds(double nowSeconds) =>
        State == AbilityState.Active ? (int)(DeactivateAt - nowSeconds) : 0;

    public void ReduceCooldown(int seconds)
    {
        RechargedAt -= seconds;
    }

    public void TimeWarp()
    {
        RechargedAt = 0;
        DeactivateAt = 0;
        State = AbilityState.Ready;
    }

    public void SetActiveTime(float seconds, double nowSeconds)
    {
        if (seconds <= 0)
            return;

        DeactivateAt = nowSeconds + seconds;
        State = AbilityState.Active;
    }

    public void Recalculate(ArtifactEffects artifacts, double nowSeconds)
    {
        DurationSeconds = Spec.BaseDurationSeconds > 0
            ? Spec.BaseDurationSeconds + artifacts.AbilityDurationAddSeconds
            : 0;

        RechargeSeconds = (int)MathF.Floor(
            Spec.BaseRechargeSeconds * artifacts.AbilityRechargeMultiplier);

        // The original clamps an already-running cooldown if the newly
        // recalculated recharge would finish sooner.
        double latestAllowed = nowSeconds + RechargeSeconds;
        if (RechargedAt > latestAllowed)
            RechargedAt = latestAllowed;
    }
}

public static class AbilityRuntimeMath
{
    public static void ApplyCooldownAbility(
        IReadOnlyList<AbilityRuntime> abilities,
        AbilityRuntime cooldownAbility)
    {
        foreach (var ability in abilities)
        {
            if (!ReferenceEquals(ability, cooldownAbility))
                ability.ReduceCooldown(3600);
        }
    }
}
