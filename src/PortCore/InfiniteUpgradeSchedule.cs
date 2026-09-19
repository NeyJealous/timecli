using System;
using System.Collections.Generic;
using System.Linq;

namespace TimeClickers.PortCore;

/// <summary>
/// Recreates the repeating promotion/training/spec-ops upgrade schedule used by 1.4.5.
/// </summary>
public sealed class InfiniteUpgradeSchedule
{
    private readonly UpgradeSpec[] _baseUpgrades;
    private readonly int _generatedCycleCount;
    private readonly Dictionary<int, UpgradeSpec[]> _cycleCache = new();

    public InfiniteUpgradeSchedule(IReadOnlyList<UpgradeSpec> baseUpgrades)
    {
        if (baseUpgrades.Count == 0)
            throw new ArgumentException("At least one base upgrade is required.", nameof(baseUpgrades));

        _baseUpgrades = baseUpgrades
            .Select((u, i) => u with { UpgradeId = i + 1 })
            .ToArray();

        // 1.4.5 omits every xx85 upgrade from post-1000 Spec Ops cycles.
        _generatedCycleCount = _baseUpgrades.Count(u => u.LevelRequired % 100 != 85);
        if (_generatedCycleCount == 0)
            throw new ArgumentException("Generated upgrade cycle cannot be empty.", nameof(baseUpgrades));
    }

    public IReadOnlyList<UpgradeSpec> BaseUpgrades => _baseUpgrades;
    public int BaseUpgradeCount => _baseUpgrades.Length;
    public int GeneratedCycleCount => _generatedCycleCount;

    public int GetSpecOpsLevelFromPurchasedUpgrades(int purchasedUpgrades)
    {
        if (purchasedUpgrades < _baseUpgrades.Length)
            return 0;

        int afterBase = purchasedUpgrades - _baseUpgrades.Length;
        return 1 + afterBase / _generatedCycleCount;
    }

    public static int GetSpecOpsLevelFromRank(int rank) => (rank - 1) / 10;

    public static int GetSpecOpsStartRank(int specOpsLevel) => 1 + specOpsLevel * 10;

    public IReadOnlyList<UpgradeSpec> GetUpgradesForSpecOps(int specOpsLevel)
    {
        if (specOpsLevel <= 0)
            return _baseUpgrades;

        if (_cycleCache.TryGetValue(specOpsLevel, out var cached))
            return cached;

        int nextId = _baseUpgrades.Length + (specOpsLevel - 1) * _generatedCycleCount + 1;
        var generated = new List<UpgradeSpec>(_generatedCycleCount);

        foreach (var source in _baseUpgrades)
        {
            if (source.LevelRequired % 100 == 85)
                continue;

            generated.Add(source with
            {
                UpgradeId = nextId++,
                LevelRequired = source.LevelRequired + specOpsLevel * 1000
            });
        }

        var result = generated.ToArray();
        _cycleCache[specOpsLevel] = result;
        return result;
    }

    /// <summary>Zero-based upgrade lookup, matching the original GetUpgrade(index).</summary>
    public UpgradeSpec GetUpgrade(int index)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        int specOpsLevel = GetSpecOpsLevelFromPurchasedUpgrades(index);
        if (specOpsLevel == 0)
            return _baseUpgrades[index];

        int local = index - _baseUpgrades.Length;
        local %= _generatedCycleCount;
        return GetUpgradesForSpecOps(specOpsLevel)[local];
    }

    public int GetRank(int purchasedUpgrades)
    {
        if (purchasedUpgrades <= 0)
            return 1;

        var lastPurchased = GetUpgrade(purchasedUpgrades - 1);
        return 1 + lastPurchased.LevelRequired / 100;
    }

    public IEnumerable<UpgradeSpec> EnumeratePurchased(int purchasedUpgrades)
    {
        for (int i = 0; i < purchasedUpgrades; i++)
            yield return GetUpgrade(i);
    }
}
