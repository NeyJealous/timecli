using System;
using System.Collections.Generic;
using System.Linq;

namespace TimeClickers.PortCore;

public interface IRandomSource
{
    int Range(int minInclusive, int maxExclusive);
}

public sealed class SystemRandomSource : IRandomSource
{
    private readonly Random _random;

    public SystemRandomSource(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public int Range(int minInclusive, int maxExclusive) =>
        _random.Next(minInclusive, maxExclusive);
}

public sealed class SequenceRandomSource : IRandomSource
{
    private readonly Queue<int> _values;

    public SequenceRandomSource(IEnumerable<int> values)
    {
        _values = new Queue<int>(values);
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        if (_values.Count == 0)
            throw new InvalidOperationException("No scripted random values remain.");

        int value = _values.Dequeue();
        int width = maxExclusive - minInclusive;
        int normalized = value % width;
        if (normalized < 0)
            normalized += width;
        return minInclusive + normalized;
    }
}

public sealed record HeroDamageApplication(
    EnemyBlockState Target,
    double Damage,
    bool IsSplash = false);

public sealed record HeroVolleyPlan(
    bool Fired,
    double FirePeriod,
    double BaseShotDamage,
    IReadOnlyList<HeroDamageApplication> Applications)
{
    public static HeroVolleyPlan NotFired(double firePeriod = 0.0) =>
        new(false, firePeriod, 0.0, Array.Empty<HeroDamageApplication>());
}

public sealed class HeroAutoFireState
{
    internal EnemyModelState? BoundModel { get; private set; }
    internal EnemyBlockState? SingleTarget { get; set; }
    internal List<EnemyBlockState> SpreadTargets { get; } = new();
    internal List<EnemyBlockState> RocketSplashTargets { get; } = new();
    internal EnemyBlockState?[] FlakTargets { get; } = new EnemyBlockState?[10];

    public double NextFireTime { get; internal set; }
    public int NumTimesFired { get; internal set; }

    public EnemyBlockState? CurrentSingleTarget => SingleTarget;
    public IReadOnlyList<EnemyBlockState> CurrentSpreadTargets => SpreadTargets;
    public IReadOnlyList<EnemyBlockState?> CurrentFlakTargets => FlakTargets;
    public IReadOnlyList<EnemyBlockState> CurrentRocketSplashTargets => RocketSplashTargets;

    internal void Bind(EnemyModelState model)
    {
        if (ReferenceEquals(BoundModel, model))
            return;

        BoundModel = model;
        SingleTarget = null;
        SpreadTargets.Clear();
        RocketSplashTargets.Clear();
        Array.Clear(FlakTargets, 0, FlakTargets.Length);
    }
}

/// <summary>
/// Reconstructs Hero.AcquireTarget + Hero.ApplyDamage targeting/fire behavior
/// from Time Clickers 1.4.5 without Unity rendering.
/// </summary>
public static class HeroAutoFirePlanner
{
    public static HeroVolleyPlan Plan(
        HeroRuntime hero,
        HeroAutoFireState state,
        EnemyModelState model,
        ArtifactEffects artifacts,
        HeroDpsMultipliers dpsMultipliers,
        double nowSeconds,
        IRandomSource random)
    {
        if (hero.Level == 0)
            return HeroVolleyPlan.NotFired();

        state.Bind(model);

        var alive = model.Blocks.Where(b => b.IsAlive).ToList();
        WeaponType weapon = hero.Spec.Weapon;

        // AcquireTarget() in the original only handles Rocket Launcher here.
        if (weapon == WeaponType.RocketLauncher)
            AcquireRocketTarget(state, alive, artifacts);

        float rateOfFire = HeroCombatMath.GetRateOfFire(
            hero.Spec.BaseRateOfFire,
            hero.Schedule,
            hero.PurchasedUpgrades);

        // Original Hero.ApplyDamage keeps the interval in a float local
        // (1f / GetRateOfFire()) before converting it to double for DPS math.
        float firePeriodFloat = 1f / rateOfFire;
        double firePeriod = firePeriodFloat;
        if (nowSeconds < state.NextFireTime)
            return HeroVolleyPlan.NotFired(firePeriod);

        state.NextFireTime = (float)nowSeconds + firePeriodFloat;
        state.NumTimesFired++;

        double damage = HeroCombatMath.GetDpsForLevel(
            hero.Spec,
            hero.Schedule,
            hero.Level,
            hero.PurchasedUpgrades,
            dpsMultipliers) * firePeriod;

        if (weapon == WeaponType.ParticleBall && state.NumTimesFired >= 5)
        {
            damage *= HeroCombatMath.GetColliderMod(
                hero.Spec.BaseCollider,
                hero.Schedule,
                hero.PurchasedUpgrades,
                artifacts.ParticleColliderMultiplier);

            state.NumTimesFired = 0;
        }

        return weapon switch
        {
            WeaponType.FlakCannon => PlanFlak(
                hero, state, alive, artifacts, damage, firePeriod, random),
            WeaponType.SpreadRifle => PlanSpread(
                hero, state, alive, artifacts, damage, firePeriod, random),
            WeaponType.RocketLauncher => PlanRocket(
                hero, state, damage, firePeriod),
            _ => PlanSingleTarget(
                hero, state, alive, damage, firePeriod)
        };
    }

    private static HeroVolleyPlan PlanFlak(
        HeroRuntime hero,
        HeroAutoFireState state,
        IReadOnlyList<EnemyBlockState> alive,
        ArtifactEffects artifacts,
        double damage,
        double firePeriod,
        IRandomSource random)
    {
        int projectileCount = HeroCombatMath.GetProjectileCount(
            hero.Spec.BaseProjectileCount,
            hero.Schedule,
            hero.PurchasedUpgrades);

        double perProjectile = damage / projectileCount;

        for (int i = 0; i < state.FlakTargets.Length; i++)
        {
            var previous = state.FlakTargets[i];
            if (previous is not null && previous.IsAlive)
                previous.RemoveTargeted(WeaponType.FlakCannon);
            state.FlakTargets[i] = null;
        }

        var candidates = Enumerable.Range(0, alive.Count).ToList();

        while (candidates.Count > projectileCount)
            candidates.RemoveAt(random.Range(0, candidates.Count));

        if (artifacts.FlakNeverMisses && alive.Count > 0)
        {
            while (candidates.Count < projectileCount)
                candidates.Add(random.Range(0, alive.Count));
        }

        if (candidates.Count > state.FlakTargets.Length)
        {
            throw new InvalidOperationException(
                $"Flak target count {candidates.Count} exceeds the original fixed array of {state.FlakTargets.Length}.");
        }

        var applications = new List<HeroDamageApplication>(candidates.Count);

        for (int i = 0; i < candidates.Count; i++)
        {
            var target = alive[candidates[i]];
            state.FlakTargets[i] = target;
            target.AddTargeted(WeaponType.FlakCannon);
            applications.Add(new HeroDamageApplication(target, perProjectile));
        }

        return new HeroVolleyPlan(true, firePeriod, damage, applications);
    }

    private static HeroVolleyPlan PlanSpread(
        HeroRuntime hero,
        HeroAutoFireState state,
        IReadOnlyList<EnemyBlockState> alive,
        ArtifactEffects artifacts,
        double damage,
        double firePeriod,
        IRandomSource random)
    {
        int projectileCount = HeroCombatMath.GetProjectileCount(
            hero.Spec.BaseProjectileCount,
            hero.Schedule,
            hero.PurchasedUpgrades);

        double perProjectile = damage / projectileCount;

        for (int i = 0; i < state.SpreadTargets.Count; i++)
        {
            if (!state.SpreadTargets[i].IsAlive)
            {
                state.SpreadTargets.RemoveAt(i);
                i--;
            }
        }

        int targetCountThreshold = 0;
        int desiredUniqueTargets = Math.Min(projectileCount, alive.Count);

        while (state.SpreadTargets.Count < desiredUniqueTargets)
        {
            bool addedAtThisThreshold = false;

            foreach (var candidate in alive)
            {
                if (state.SpreadTargets.Contains(candidate))
                    continue;

                if (candidate.TargetCount > targetCountThreshold)
                    continue;

                state.SpreadTargets.Add(candidate);
                candidate.AddTargeted(WeaponType.SpreadRifle);
                candidate.IncrementTargetCount();
                addedAtThisThreshold = true;

                if (state.SpreadTargets.Count >= desiredUniqueTargets)
                    break;
            }

            if (state.SpreadTargets.Count < desiredUniqueTargets)
            {
                targetCountThreshold++;
                if (!addedAtThisThreshold && targetCountThreshold > 1_000_000)
                    throw new InvalidOperationException("Unable to balance Spread Rifle targets.");
            }
        }

        if (artifacts.SpreadNeverMisses && alive.Count > 0)
        {
            while (state.SpreadTargets.Count < projectileCount)
                state.SpreadTargets.Add(alive[random.Range(0, alive.Count)]);
        }

        var applications = state.SpreadTargets
            .Select(target => new HeroDamageApplication(target, perProjectile))
            .ToArray();

        return new HeroVolleyPlan(true, firePeriod, damage, applications);
    }

    private static HeroVolleyPlan PlanRocket(
        HeroRuntime hero,
        HeroAutoFireState state,
        double damage,
        double firePeriod)
    {
        if (state.SingleTarget is null)
            return new HeroVolleyPlan(true, firePeriod, damage, Array.Empty<HeroDamageApplication>());

        var applications = new List<HeroDamageApplication>
        {
            new(state.SingleTarget, damage)
        };

        double splash = HeroCombatMath.GetSplash(
            hero.Spec.BaseSplash,
            hero.Schedule,
            hero.PurchasedUpgrades);

        foreach (var target in state.RocketSplashTargets)
        {
            applications.Add(new HeroDamageApplication(
                target,
                damage * splash,
                IsSplash: true));
        }

        return new HeroVolleyPlan(true, firePeriod, damage, applications);
    }

    private static HeroVolleyPlan PlanSingleTarget(
        HeroRuntime hero,
        HeroAutoFireState state,
        IReadOnlyList<EnemyBlockState> alive,
        double damage,
        double firePeriod)
    {
        if (state.SingleTarget is not null && !state.SingleTarget.IsAlive)
            state.SingleTarget = null;

        if (state.SingleTarget is null && alive.Count > 0)
            state.SingleTarget = AcquireLeastTargeted(alive, hero.Spec.Weapon);

        var applications = state.SingleTarget is null
            ? Array.Empty<HeroDamageApplication>()
            : new[] { new HeroDamageApplication(state.SingleTarget, damage) };

        return new HeroVolleyPlan(true, firePeriod, damage, applications);
    }

    private static void AcquireRocketTarget(
        HeroAutoFireState state,
        IReadOnlyList<EnemyBlockState> alive,
        ArtifactEffects artifacts)
    {
        if (state.SingleTarget is not null && !state.SingleTarget.IsAlive)
        {
            state.SingleTarget = null;
            state.RocketSplashTargets.Clear();
        }

        if (state.SingleTarget is not null || alive.Count == 0)
            return;

        var target = AcquireLeastTargeted(alive, WeaponType.RocketLauncher);
        state.SingleTarget = target;
        state.RocketSplashTargets.Clear();

        foreach (var candidate in alive)
        {
            if (ReferenceEquals(candidate, target))
                continue;

            if (SquaredDistance(target.Position, candidate.Position) <
                artifacts.RocketSplashDistance)
            {
                state.RocketSplashTargets.Add(candidate);
            }
        }
    }

    private static EnemyBlockState AcquireLeastTargeted(
        IReadOnlyList<EnemyBlockState> alive,
        WeaponType weapon)
    {
        int targetCountThreshold = 0;

        while (true)
        {
            foreach (var candidate in alive)
            {
                if (candidate.TargetCount > targetCountThreshold)
                    continue;

                candidate.AddTargeted(weapon);
                candidate.IncrementTargetCount();
                return candidate;
            }

            targetCountThreshold++;
        }
    }

    private static float SquaredDistance(VoxelPoint a, VoxelPoint b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        float dz = a.Z - b.Z;
        return dx * dx + dy * dy + dz * dz;
    }
}
