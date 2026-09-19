using System;
using System.Collections.Generic;
using System.Linq;

namespace TimeClickers.PortCore;

public sealed record BlockReward(
    double Gold,
    ulong TimeCubes,
    ulong WeaponCubes)
{
    public static readonly BlockReward None = new(0.0, 0, 0);
}

public sealed record BlockDamageResult(
    double AppliedDamage,
    double RemainingHealth,
    bool Killed,
    float OverkillNormalized,
    BlockReward Reward);

/// <summary>
/// Unity-independent BoxEnemy reconstruction. One instance represents one voxel
/// block spawned by Arena.SpawnBlock.
/// </summary>
public sealed class EnemyBlockState
{
    private int _targetMask;

    public EnemyBlockState(
        double maxHealth,
        EnemyType enemyType,
        int timeCubeCount = 0,
        int weaponCubeCount = 0,
        VoxelPoint? position = null)
    {
        if (maxHealth <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(maxHealth));

        MaxHealth = maxHealth;
        Health = maxHealth;
        EnemyType = enemyType;
        TimeCubeCount = timeCubeCount;
        WeaponCubeCount = weaponCubeCount;
        Position = position ?? VoxelPoint.Zero;
    }

    public double MaxHealth { get; }
    public double Health { get; private set; }
    public EnemyType EnemyType { get; }
    public int TimeCubeCount { get; }
    public int WeaponCubeCount { get; }
    public VoxelPoint Position { get; }
    public bool IsAlive => Health > 0.0;
    public int TargetMask => _targetMask;
    public int TargetCount { get; private set; }

    public void AddTargeted(WeaponType weapon) =>
        _targetMask |= 1 << (int)weapon;

    public void RemoveTargeted(WeaponType weapon) =>
        _targetMask &= ~(1 << (int)weapon);

    public bool IsTargetedBy(WeaponType weapon) =>
        (_targetMask & (1 << (int)weapon)) != 0;

    public void IncrementTargetCount() => TargetCount++;

    public BlockDamageResult ApplyClickDamage(
        double clickDamage,
        ArtifactEffects artifacts,
        double heroesGoldFindMultiplier,
        bool goldRushActive)
    {
        if (!IsAlive)
            return new(0.0, Health, false, 0f, BlockReward.None);

        double damage = clickDamage;

        if (_targetMask > 0)
            damage *= artifacts.TargetedMultiplier;

        damage = ApplyWeaponTargetMultiplier(
            damage, WeaponType.Pistol, artifacts.TargetedPulsePistol);
        damage = ApplyWeaponTargetMultiplier(
            damage, WeaponType.FlakCannon, artifacts.TargetedFlakCannon);
        damage = ApplyWeaponTargetMultiplier(
            damage, WeaponType.SpreadRifle, artifacts.TargetedSpreadRifle);
        damage = ApplyWeaponTargetMultiplier(
            damage, WeaponType.RocketLauncher, artifacts.TargetedRocketLauncher);
        damage = ApplyWeaponTargetMultiplier(
            damage, WeaponType.ParticleBall, artifacts.TargetedParticleBall);

        damage *= EnemyType switch
        {
            EnemyType.Red => artifacts.ClickDamageRed,
            EnemyType.White => artifacts.ClickDamageWhite,
            EnemyType.Yellow => artifacts.ClickDamageYellow,
            EnemyType.Rainbow => artifacts.ClickDamageRed,
            _ => 1.0
        };

        return ApplyDamage(
            damage,
            artifacts,
            heroesGoldFindMultiplier,
            goldRushActive);
    }

    public BlockDamageResult ApplyDamage(
        double damage,
        ArtifactEffects artifacts,
        double heroesGoldFindMultiplier,
        bool goldRushActive)
    {
        if (!IsAlive)
            return new(0.0, Health, false, 0f, BlockReward.None);

        Health -= damage;
        if (Health > 0.0)
            return new(damage, Health, false, 0f, BlockReward.None);

        float overkill = (float)(-Health / MaxHealth);
        var reward = EnemyType switch
        {
            EnemyType.TimeCube => new BlockReward(
                0.0,
                (ulong)Math.Max(0, TimeCubeCount),
                0),
            EnemyType.WeaponCube => new BlockReward(
                0.0,
                0,
                (ulong)Math.Max(0, WeaponCubeCount)),
            _ => new BlockReward(
                GoldRewardMath.GetKillGoldTotal(
                    MaxHealth,
                    EnemyType,
                    heroesGoldFindMultiplier,
                    artifacts,
                    goldRushActive),
                0,
                0)
        };

        return new(damage, Health, true, overkill, reward);
    }

    private double ApplyWeaponTargetMultiplier(
        double damage,
        WeaponType weapon,
        double multiplier)
    {
        if (multiplier > 1.0 && IsTargetedBy(weapon))
            damage *= multiplier;

        return damage;
    }
}

public sealed class EnemyModelState
{
    private readonly EnemyBlockState[] _blocks;

    public EnemyModelState(IEnumerable<EnemyBlockState> blocks)
    {
        _blocks = blocks.ToArray();
        if (_blocks.Length == 0)
            throw new ArgumentException("Enemy model must contain at least one block.", nameof(blocks));
    }

    public IReadOnlyList<EnemyBlockState> Blocks => _blocks;
    public int BlockCount => _blocks.Length;
    public int DestroyedBlocks => _blocks.Count(b => !b.IsAlive);
    public bool IsCleared => DestroyedBlocks >= BlockCount;
    public double TotalHealth => _blocks.Sum(b => b.MaxHealth);
    public double RemainingHealth => _blocks.Where(b => b.IsAlive).Sum(b => b.Health);

    public EnemyBlockState GetBlock(int index) => _blocks[index];
}
