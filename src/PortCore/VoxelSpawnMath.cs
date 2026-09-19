using System;
using System.Collections.Generic;
using System.Linq;

namespace TimeClickers.PortCore;

public sealed record VoxelModelDescriptor(
    string Id,
    int MinWave,
    int EnemyCount,
    int? BossWave = null);

public sealed record VoxelHpPlan(
    double BaseHp,
    int RedRequired,
    int WhiteRequired,
    int YellowRequired,
    int TotalRequired,
    int CandidateMaxEnemyCount)
{
    public int[] ToArray() => new[] { RedRequired, WhiteRequired, YellowRequired };
}

/// <summary>
/// Reconstructs the numeric/model-selection part of VoxelLibrary.GetVoxelModelForHP.
/// Exact block positions are asset data and are supplied separately.
/// </summary>
public static class VoxelSpawnMath
{
    public static VoxelHpPlan BuildHpPlan(
        double arenaHp,
        int minEnemyCount,
        int maxEnemyCount)
    {
        if (arenaHp <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(arenaHp));
        if (minEnemyCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(minEnemyCount));
        if (maxEnemyCount < minEnemyCount)
            throw new ArgumentOutOfRangeException(nameof(maxEnemyCount));

        // This is deliberately integer division first, matching the original IL.
        double scalingHp = 670 * (maxEnemyCount / 40);
        double scaled = arenaHp / scalingHp;

        int exponent = (int)Math.Floor(Math.Log10(scaled));
        exponent = Math.Max(exponent, 1);

        if (arenaHp / Math.Pow(10.0, exponent) >
            1000.0 * maxEnemyCount / 40.0)
        {
            exponent++;
        }

        double baseHp = Math.Pow(10.0, exponent);

        int red = (int)Math.Floor(arenaHp / baseHp);
        int white = 0;
        int yellow = 0;

        while (red > maxEnemyCount / 2)
        {
            red -= 10;
            white++;

            if (white > maxEnemyCount / 2)
            {
                white -= 10;
                yellow++;
            }
        }

        int total = red + white + yellow;

        while (total < minEnemyCount && white > 0)
        {
            white--;
            red += 10;
            total = red + white + yellow;
        }

        while (total > maxEnemyCount && red >= 10)
        {
            red -= 10;
            white++;
            total = red + white + yellow;
        }

        return new VoxelHpPlan(
            baseHp,
            red,
            white,
            yellow,
            total,
            total + 10);
    }

    public static int[] TransformEnemyCounts(
        IReadOnlyList<int> source,
        int convertYellowToWhite,
        int convertWhiteToRed)
    {
        if (source.Count < 3)
            throw new ArgumentException("Expected red/white/yellow counts.", nameof(source));

        var result = new[] { source[0], source[1], source[2] };

        for (int i = 0; i < convertYellowToWhite; i++)
        {
            if (result[2] <= 0)
                break;

            result[2]--;
            result[1]++;
        }

        for (int i = 0; i < convertWhiteToRed; i++)
        {
            if (result[1] <= 0)
                break;

            result[1]--;
            result[0]++;
        }

        return result;
    }

    public static IReadOnlyList<VoxelModelDescriptor> GetEligibleModels(
        IEnumerable<VoxelModelDescriptor> models,
        VoxelHpPlan plan,
        int wave)
    {
        // Exact boss models bypass normal candidate filtering.
        var boss = models.FirstOrDefault(m => m.BossWave == wave);
        if (boss is not null)
            return new[] { boss };

        return models
            .Where(m =>
                m.MinWave <= wave &&
                m.EnemyCount >= plan.TotalRequired &&
                m.EnemyCount <= plan.CandidateMaxEnemyCount)
            .ToArray();
    }

    public static VoxelModelDescriptor? SelectModel(
        IEnumerable<VoxelModelDescriptor> models,
        VoxelHpPlan plan,
        int wave,
        int randomIndex)
    {
        var eligible = GetEligibleModels(models, plan, wave);
        if (eligible.Count == 0)
            return null;

        int index = randomIndex % eligible.Count;
        if (index < 0)
            index += eligible.Count;

        return eligible[index];
    }

    public static int GetMinEnemyCountForWave(int wave) =>
        wave % 10 == 0 ? 100 : 40;

    public static int GetMaxEnemyCountForWave(int wave) =>
        GetMinEnemyCountForWave(wave) + 20;
}
