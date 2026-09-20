using System;
using System.Collections.Generic;
using System.Linq;

namespace TimeClickers.PortCore;

public enum GoldPickupPhase
{
    Floating = 0,
    Collecting = 1,
    Collected = 2
}

/// <summary>
/// Portable state for the physical Gold prefab used by Time Clickers 1.4.5.
///
/// Default Gold:
///   wait 0.25 s -> collider x10 -> wait 3 s -> Collect(false)
///   -> 0.5 s TweenPosition -> GoldBank.AddGold.
///
/// Click-pistol hit gold overrides the first wait to 0.5 s and float speed to 4.
/// </summary>
public sealed class GoldPickupState
{
    public const double AutoCollectWaitAfterExpansionSeconds = 3.0;
    public const double CollectionTweenSeconds = 0.5;

    internal GoldPickupState(
        long id,
        double goldValue,
        int goldSource,
        double spawnedAt,
        double timeBeforeCollection,
        float floatAwaySpeed,
        EnemyBlockState sourceBlock)
    {
        Id = id;
        GoldValue = goldValue;
        GoldSource = goldSource;
        SpawnedAt = spawnedAt;
        TimeBeforeCollection = timeBeforeCollection;
        FloatAwaySpeed = floatAwaySpeed;
        SourceBlock = sourceBlock;
    }

    public long Id { get; }
    public double GoldValue { get; }
    public int GoldSource { get; }
    public double SpawnedAt { get; }
    public double TimeBeforeCollection { get; }
    public float FloatAwaySpeed { get; }
    public EnemyBlockState SourceBlock { get; }

    public GoldPickupPhase Phase { get; private set; } =
        GoldPickupPhase.Floating;

    public double? CollectionStartedAt { get; private set; }
    public double? CollectedAt { get; private set; }

    public double ColliderExpansionAt =>
        SpawnedAt + TimeBeforeCollection;

    public double AutoCollectAt =>
        ColliderExpansionAt +
        AutoCollectWaitAfterExpansionSeconds;

    public bool IsColliderExpanded(double nowSeconds) =>
        nowSeconds >= ColliderExpansionAt;

    public bool IsAutoCollectDue(double nowSeconds) =>
        Phase == GoldPickupPhase.Floating &&
        nowSeconds >= AutoCollectAt;

    public double CollectionProgress(double nowSeconds)
    {
        if (Phase == GoldPickupPhase.Collected)
            return 1.0;

        if (Phase != GoldPickupPhase.Collecting ||
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
        if (Phase != GoldPickupPhase.Floating)
            return false;

        Phase = GoldPickupPhase.Collecting;
        CollectionStartedAt = nowSeconds;
        return true;
    }

    internal bool Tick(double nowSeconds)
    {
        if (Phase == GoldPickupPhase.Floating &&
            nowSeconds >= AutoCollectAt)
        {
            Phase = GoldPickupPhase.Collecting;
            CollectionStartedAt = AutoCollectAt;
        }

        if (Phase == GoldPickupPhase.Collecting &&
            CollectionStartedAt.HasValue &&
            nowSeconds >=
            CollectionStartedAt.Value +
            CollectionTweenSeconds)
        {
            Phase = GoldPickupPhase.Collected;
            CollectedAt =
                CollectionStartedAt.Value +
                CollectionTweenSeconds;
            return true;
        }

        return false;
    }
}

public sealed record GoldCollection(
    long PickupId,
    double GoldValue,
    int GoldSource,
    double CollectedAt);

public sealed class GoldPickupQueue
{
    public const double DefaultTimeBeforeCollection = 0.25;
    public const float DefaultFloatAwaySpeed = 2f;

    public const double HitGoldTimeBeforeCollection = 0.5;
    public const float HitGoldFloatAwaySpeed = 4f;

    private readonly List<GoldPickupState> _active = new();
    private long _nextId = 1;

    public IReadOnlyList<GoldPickupState> Active => _active;
    public int Count => _active.Count;

    public GoldPickupState Spawn(
        double goldValue,
        double nowSeconds,
        EnemyBlockState sourceBlock,
        double timeBeforeCollection =
            DefaultTimeBeforeCollection,
        float floatAwaySpeed =
            DefaultFloatAwaySpeed,
        int goldSource = 1)
    {
        var pickup = new GoldPickupState(
            _nextId++,
            goldValue,
            goldSource,
            nowSeconds,
            timeBeforeCollection,
            floatAwaySpeed,
            sourceBlock);

        _active.Add(pickup);
        return pickup;
    }

    public IReadOnlyList<GoldPickupState> SpawnKillReward(
        EnemyBlockState sourceBlock,
        double totalGold,
        double nowSeconds)
    {
        if (totalGold <= 0.0)
            return Array.Empty<GoldPickupState>();

        int count =
            sourceBlock.EnemyType == EnemyType.Rainbow
                ? 10
                : 1;

        double valuePerPickup =
            totalGold / count;

        var created =
            new GoldPickupState[count];

        for (int i = 0; i < count; i++)
        {
            created[i] = Spawn(
                valuePerPickup,
                nowSeconds,
                sourceBlock);
        }

        return created;
    }

    public GoldPickupState SpawnHitGold(
        EnemyBlockState sourceBlock,
        double goldValue,
        double nowSeconds)
    {
        return Spawn(
            goldValue,
            nowSeconds,
            sourceBlock,
            HitGoldTimeBeforeCollection,
            HitGoldFloatAwaySpeed,
            goldSource: 1);
    }

    public bool TryCollect(
        long pickupId,
        double nowSeconds)
    {
        var pickup =
            _active.FirstOrDefault(
                p => p.Id == pickupId);

        return pickup is not null &&
               pickup.TryCollect(nowSeconds);
    }

    public IReadOnlyList<GoldCollection> Tick(
        double nowSeconds,
        GoldWallet gold)
    {
        List<GoldCollection>? collected = null;

        for (int i = _active.Count - 1;
             i >= 0;
             i--)
        {
            var pickup = _active[i];

            if (!pickup.Tick(nowSeconds))
                continue;

            gold.Add(pickup.GoldValue);

            collected ??=
                new List<GoldCollection>();

            collected.Add(
                new GoldCollection(
                    pickup.Id,
                    pickup.GoldValue,
                    pickup.GoldSource,
                    pickup.CollectedAt ??
                    nowSeconds));

            _active.RemoveAt(i);
        }

        if (collected is null)
            return Array.Empty<GoldCollection>();

        collected.Reverse();
        return collected;
    }

    public void Clear() =>
        _active.Clear();
}
