using System;
using System.Collections.Generic;
using System.Linq;

namespace TimeClickers.PortCore;

public enum SpecialCubeKind
{
    TimeCube = 0,
    WeaponCube = 1
}

public enum SpecialCubePickupPhase
{
    Floating = 0,
    Collecting = 1,
    Collected = 2
}

/// <summary>
/// Unity-independent lifecycle of the original TimeCube / WeaponCube pickup
/// prefabs spawned by BoxEnemy.SpawnGold.
///
/// Original timing recovered from the 1.4.5 coroutine state machines:
/// - spawn -> float immediately;
/// - after 2.0 s, BoxCollider.size *= 10;
/// - after another 3.0 s, auto-Collect(false);
/// - Collect starts a 0.5 s TweenPosition;
/// - currency is credited only when that tween completes.
/// </summary>
public sealed class SpecialCubePickupState
{
    public const double ColliderExpansionDelaySeconds = 2.0;
    public const double AutoCollectDelaySeconds = 5.0;
    public const double CollectionTweenSeconds = 0.5;

    internal SpecialCubePickupState(
        long id,
        SpecialCubeKind kind,
        ulong value,
        double spawnedAt,
        EnemyBlockState sourceBlock)
    {
        Id = id;
        Kind = kind;
        Value = value;
        SpawnedAt = spawnedAt;
        SourceBlock = sourceBlock;
    }

    public long Id { get; }
    public SpecialCubeKind Kind { get; }
    public ulong Value { get; }
    public double SpawnedAt { get; }
    public EnemyBlockState SourceBlock { get; }

    public SpecialCubePickupPhase Phase { get; private set; } =
        SpecialCubePickupPhase.Floating;

    public double? CollectionStartedAt { get; private set; }
    public double? CollectedAt { get; private set; }

    public bool IsColliderExpanded(double nowSeconds) =>
        nowSeconds >= SpawnedAt + ColliderExpansionDelaySeconds;

    public bool IsAutoCollectDue(double nowSeconds) =>
        Phase == SpecialCubePickupPhase.Floating &&
        nowSeconds >= SpawnedAt + AutoCollectDelaySeconds;

    public double CollectionProgress(double nowSeconds)
    {
        if (Phase == SpecialCubePickupPhase.Collected)
            return 1.0;

        if (Phase != SpecialCubePickupPhase.Collecting ||
            !CollectionStartedAt.HasValue)
        {
            return 0.0;
        }

        return Math.Clamp(
            (nowSeconds - CollectionStartedAt.Value) /
            CollectionTweenSeconds,
            0.0,
            1.0);
    }

    public bool TryCollect(double nowSeconds)
    {
        if (Phase != SpecialCubePickupPhase.Floating)
            return false;

        Phase = SpecialCubePickupPhase.Collecting;
        CollectionStartedAt = nowSeconds;
        return true;
    }

    internal bool Tick(double nowSeconds)
    {
        if (Phase == SpecialCubePickupPhase.Floating &&
            nowSeconds >= SpawnedAt + AutoCollectDelaySeconds)
        {
            // Coroutine timing is anchored to spawn time. If a headless update
            // arrives late, preserve the original collection completion time
            // rather than delaying it by another 0.5 s.
            Phase = SpecialCubePickupPhase.Collecting;
            CollectionStartedAt = SpawnedAt + AutoCollectDelaySeconds;
        }

        if (Phase == SpecialCubePickupPhase.Collecting &&
            CollectionStartedAt.HasValue &&
            nowSeconds >= CollectionStartedAt.Value + CollectionTweenSeconds)
        {
            Phase = SpecialCubePickupPhase.Collected;
            CollectedAt =
                CollectionStartedAt.Value + CollectionTweenSeconds;
            return true;
        }

        return false;
    }
}

public sealed record SpecialCubeCollection(
    long PickupId,
    SpecialCubeKind Kind,
    ulong Value,
    double CollectedAt);

/// <summary>
/// Owns active TimeCube / WeaponCube pickups and credits their banks only when
/// the recovered 0.5-second collection tween completes.
/// </summary>
public sealed class SpecialCubePickupQueue
{
    private readonly List<SpecialCubePickupState> _active = new();
    private long _nextId = 1;

    public IReadOnlyList<SpecialCubePickupState> Active => _active;
    public int Count => _active.Count;

    public IReadOnlyList<SpecialCubePickupState> Spawn(
        SpecialCubeKind kind,
        ulong count,
        double nowSeconds,
        EnemyBlockState sourceBlock)
    {
        if (count == 0)
            return Array.Empty<SpecialCubePickupState>();

        if (count > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(count));

        var created = new SpecialCubePickupState[(int)count];

        for (int i = 0; i < created.Length; i++)
        {
            var pickup = new SpecialCubePickupState(
                _nextId++,
                kind,
                value: 1,
                nowSeconds,
                sourceBlock);

            _active.Add(pickup);
            created[i] = pickup;
        }

        return created;
    }

    public bool TryCollect(long pickupId, double nowSeconds)
    {
        var pickup = _active.FirstOrDefault(p => p.Id == pickupId);
        return pickup is not null && pickup.TryCollect(nowSeconds);
    }

    public IReadOnlyList<SpecialCubeCollection> Tick(
        double nowSeconds,
        TimeCubeBank timeCubes,
        WeaponCubeBankState weaponCubes)
    {
        List<SpecialCubeCollection>? collected = null;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var pickup = _active[i];

            if (!pickup.Tick(nowSeconds))
                continue;

            if (pickup.Kind == SpecialCubeKind.TimeCube)
                timeCubes.AddPendingReward(pickup.Value);
            else
                weaponCubes.Add(pickup.Value);

            collected ??= new List<SpecialCubeCollection>();
            collected.Add(new SpecialCubeCollection(
                pickup.Id,
                pickup.Kind,
                pickup.Value,
                pickup.CollectedAt ?? nowSeconds));

            _active.RemoveAt(i);
        }

        if (collected is null)
            return Array.Empty<SpecialCubeCollection>();

        collected.Reverse();
        return collected;
    }

    public void Clear() => _active.Clear();
}
