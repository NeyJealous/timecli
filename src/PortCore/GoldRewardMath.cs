using System;

namespace TimeClickers.PortCore;

public enum EnemyType
{
    Red = 0,
    White = 1,
    Yellow = 2,
    Rainbow = 3,
    TimeCube = 4,
    WeaponCube = 5
}

public static class GoldRewardMath
{
    public static double GetEnemyTypeGoldMultiplier(ArtifactEffects artifacts, EnemyType enemyType) =>
        enemyType switch
        {
            EnemyType.Red => artifacts.GoldFindRed,
            EnemyType.White => artifacts.GoldFindWhite,
            EnemyType.Yellow => artifacts.GoldFindYellow,
            EnemyType.Rainbow => artifacts.GoldFindRainbowBlock,
            _ => 1.0
        };

    /// <summary>
    /// Gold value before Unity creates the physical Gold pickup(s).
    /// Mirrors BoxEnemy.SpawnGold for normal/RGB/rainbow enemies.
    /// Cube enemies return zero because they spawn cube pickups instead.
    /// </summary>
    public static double GetKillGoldBase(
        double maxHealth,
        EnemyType enemyType,
        double heroesGoldFindMultiplier,
        ArtifactEffects artifacts,
        bool goldRushActive)
    {
        if (enemyType is EnemyType.TimeCube or EnemyType.WeaponCube)
            return 0.0;

        double gold = maxHealth / 15.0;

        if (goldRushActive)
            gold *= artifacts.GoldRushMultiplier;

        gold *= heroesGoldFindMultiplier;
        gold *= artifacts.GoldFindMultiplier;
        gold *= GetEnemyTypeGoldMultiplier(artifacts, enemyType);
        return gold;
    }

    /// <summary>
    /// Total collected value represented by SpawnGold.
    /// Rainbow blocks spawn ten pickups, each with ceil(base * 10).
    /// Other gold-bearing enemies spawn one pickup with ceil(base).
    /// </summary>
    public static double GetKillGoldTotal(
        double maxHealth,
        EnemyType enemyType,
        double heroesGoldFindMultiplier,
        ArtifactEffects artifacts,
        bool goldRushActive)
    {
        double gold = GetKillGoldBase(
            maxHealth, enemyType, heroesGoldFindMultiplier, artifacts, goldRushActive);

        if (enemyType is EnemyType.TimeCube or EnemyType.WeaponCube)
            return 0.0;

        if (enemyType == EnemyType.Rainbow)
            return 10.0 * Math.Ceiling(gold * 10.0);

        return Math.Ceiling(gold);
    }

    /// <summary>
    /// Gold spawned by the click-pistol hit-gold path.
    /// hitGoldMultiplier is the caller-supplied multiplier used by SpawnHitGold.
    /// </summary>
    public static double GetHitGold(
        double maxHealth,
        double hitGoldMultiplier,
        EnemyType enemyType,
        double heroesGoldFindMultiplier,
        ArtifactEffects artifacts,
        bool goldRushActive)
    {
        double gold = maxHealth / 15.0 * hitGoldMultiplier;

        if (goldRushActive)
            gold *= artifacts.GoldRushMultiplier;

        gold *= heroesGoldFindMultiplier;
        gold *= artifacts.GoldFindMultiplier;
        gold *= GetEnemyTypeGoldMultiplier(artifacts, enemyType);

        return Math.Ceiling(gold);
    }

    public static double GetRainbowBallGold(double arenaHp) => arenaHp / 1.5;
}
