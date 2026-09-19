using System;
using System.Collections.Generic;
using System.Linq;

namespace TimeClickers.PortCore;

public readonly record struct VoxelPoint(float X, float Y, float Z)
{
    public static readonly VoxelPoint Zero = new(0f, 0f, 0f);
}

public sealed record VoxelModelLayout(
    string Id,
    VoxelPoint[] Red,
    VoxelPoint[] White,
    VoxelPoint[] Yellow,
    VoxelPoint[] Blue)
{
    public VoxelPoint[][] Blocks => new[] { Red, White, Yellow, Blue };
    public int EnemyCount => Red.Length + White.Length + Yellow.Length + Blue.Length;
}

public sealed record SpawnedBlockPlan(
    VoxelPoint Position,
    double MaxHealth,
    EnemyType EnemyType,
    int TimeCubeCount = 0,
    int WeaponCubeCount = 0);

public sealed record VoxelSpawnPlan(
    IReadOnlyList<SpawnedBlockPlan> Blocks,
    int[] RemainingRequired,
    int[] RemainingRainbowConversions)
{
    public int SpawnedBlockCount => Blocks.Count;
}

/// <summary>
/// Reconstructs the block-allocation portion of Arena.SpawnEnemy after
/// VoxelLibrary has selected a model and produced red/white/yellow HP counts.
/// Coordinates are left in raw Qubicle/model space; Unity adapters perform the
/// original centering and Z inversion.
/// </summary>
public static class VoxelSpawnPlanner
{
    public static VoxelSpawnPlan Build(
        VoxelModelLayout model,
        double baseHp,
        IReadOnlyList<int> requiredCounts,
        IReadOnlyList<int> rainbowConversions,
        int timeCubeReward = 0,
        int weaponCubeReward = 0,
        bool isVeryFirstEnemy = false,
        bool forceRainbowEnemy = false)
    {
        if (baseHp <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(baseHp));
        if (requiredCounts.Count < 3)
            throw new ArgumentException("Need red/white/yellow required counts.", nameof(requiredCounts));
        if (rainbowConversions.Count < 3)
            throw new ArgumentException("Need red/white/yellow rainbow conversion counts.", nameof(rainbowConversions));

        var required = new[]
        {
            requiredCounts[0],
            requiredCounts[1],
            requiredCounts[2]
        };
        var rainbow = new[]
        {
            rainbowConversions[0],
            rainbowConversions[1],
            rainbowConversions[2]
        };

        var result = new List<SpawnedBlockPlan>();
        var blocks = model.Blocks;

        if (isVeryFirstEnemy)
        {
            var type = rainbow[0] > 0 ? EnemyType.Rainbow : EnemyType.Red;
            result.Add(new SpawnedBlockPlan(
                new VoxelPoint(-3.8f, 2.84f, 0.05f),
                baseHp,
                type));
            return new VoxelSpawnPlan(result, required, rainbow);
        }

        if (forceRainbowEnemy)
        {
            // Original loop intentionally excludes blue/fallback slots.
            for (int category = 0; category < 3; category++)
            {
                foreach (var position in blocks[category])
                {
                    result.Add(new SpawnedBlockPlan(
                        position,
                        baseHp,
                        EnemyType.Rainbow));
                }
            }

            return new VoxelSpawnPlan(result, required, rainbow);
        }

        var consumed = new int[4];

        // Time Cube replaces the next White slot but receives yellow-tier HP.
        if (timeCubeReward > 0)
        {
            VoxelPoint position = TakeDirect(blocks[1], consumed[1], "Time Cube/white");
            consumed[1]++;
            required[1]--;

            result.Add(new SpawnedBlockPlan(
                position,
                baseHp * Math.Pow(10.0, 2),
                EnemyType.TimeCube,
                TimeCubeCount: timeCubeReward));
        }

        // Weapon Cube replaces the next Yellow slot and also receives yellow-tier HP.
        if (weaponCubeReward > 0)
        {
            VoxelPoint position = TakeDirect(blocks[2], consumed[2], "Weapon Cube/yellow");
            consumed[2]++;
            required[2]--;

            result.Add(new SpawnedBlockPlan(
                position,
                baseHp * Math.Pow(10.0, 2),
                EnemyType.WeaponCube,
                WeaponCubeCount: weaponCubeReward));
        }

        // Primary pass: satisfy each HP tier from the matching model color.
        for (int category = 0; category < 3; category++)
        {
            double hp = baseHp * Math.Pow(10.0, category);

            while (required[category] > 0 &&
                   consumed[category] < blocks[category].Length)
            {
                VoxelPoint position = blocks[category][consumed[category]++];

                if (rainbow[category] > 0)
                {
                    rainbow[category]--;
                    result.Add(new SpawnedBlockPlan(
                        position,
                        baseHp,
                        EnemyType.Rainbow));
                }
                else
                {
                    result.Add(new SpawnedBlockPlan(
                        position,
                        hp,
                        BasicEnemyType(category)));
                }

                required[category]--;
            }
        }

        // Fallback pass, highest HP tier first. For a missing matching color,
        // the original searches the other two basic color arrays cyclically,
        // then the blue generic-slot array.
        for (int category = 2; category >= 0; category--)
        {
            int alternate1 = (category + 1) % 3;
            int alternate2 = (category + 2) % 3;
            double hp = baseHp * Math.Pow(10.0, category);

            while (required[category] > 0)
            {
                VoxelPoint? position = null;

                if (consumed[alternate1] < blocks[alternate1].Length)
                    position = blocks[alternate1][consumed[alternate1]++];
                else if (consumed[alternate2] < blocks[alternate2].Length)
                    position = blocks[alternate2][consumed[alternate2]++];
                else if (consumed[3] < blocks[3].Length)
                    position = blocks[3][consumed[3]++];

                if (position is not null)
                {
                    if (rainbow[category] > 0)
                    {
                        rainbow[category]--;
                        result.Add(new SpawnedBlockPlan(
                            position.Value,
                            baseHp,
                            EnemyType.Rainbow));
                    }
                    else
                    {
                        result.Add(new SpawnedBlockPlan(
                            position.Value,
                            hp,
                            BasicEnemyType(category)));
                    }
                }

                // Arena.SpawnEnemy decrements the required count even when no
                // fallback position exists.
                required[category]--;
            }
        }

        return new VoxelSpawnPlan(result, required, rainbow);
    }

    public static EnemyModelState CreateEnemyModel(VoxelSpawnPlan plan) =>
        new(plan.Blocks.Select(block =>
            new EnemyBlockState(
                block.MaxHealth,
                block.EnemyType,
                block.TimeCubeCount,
                block.WeaponCubeCount,
                block.Position)));

    private static VoxelPoint TakeDirect(
        IReadOnlyList<VoxelPoint> source,
        int index,
        string purpose)
    {
        if (index < 0 || index >= source.Count)
            throw new InvalidOperationException(
                $"Selected voxel model has no {purpose} slot at index {index}.");

        return source[index];
    }

    private static EnemyType BasicEnemyType(int category) => category switch
    {
        0 => EnemyType.Red,
        1 => EnemyType.White,
        2 => EnemyType.Yellow,
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };
}
