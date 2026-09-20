using System;
using System.Collections.Generic;
using System.Linq;

namespace TimeClickers.PortCore;

public interface IVoxelModelCatalog
{
    IReadOnlyList<VoxelModelDescriptor> Models { get; }
    VoxelModelLayout GetLayout(string id);
}

public sealed class InMemoryVoxelModelCatalog : IVoxelModelCatalog
{
    private readonly IReadOnlyList<VoxelModelDescriptor> _models;
    private readonly Dictionary<string, VoxelModelLayout> _layouts;

    public InMemoryVoxelModelCatalog(
        IEnumerable<VoxelModelDescriptor> models,
        IEnumerable<VoxelModelLayout> layouts)
    {
        _models = new List<VoxelModelDescriptor>(models);
        _layouts = new Dictionary<string, VoxelModelLayout>(StringComparer.Ordinal);

        foreach (var layout in layouts)
            _layouts.Add(layout.Id, layout);
    }

    public IReadOnlyList<VoxelModelDescriptor> Models => _models;

    public VoxelModelLayout GetLayout(string id) =>
        _layouts.TryGetValue(id, out var layout)
            ? layout
            : throw new KeyNotFoundException($"No voxel layout registered for '{id}'.");
}

public readonly record struct ArenaSpawnRolls(
    int ModelRandomIndex,
    float RainbowEnemyRoll,
    float TimeCubeRoll,
    float WeaponCubeRoll);

public sealed record HeadlessSpawnedEnemy(
    string ModelId,
    VoxelModelLayout Layout,
    VoxelHpPlan HpPlan,
    VoxelSpawnPlan SpawnPlan,
    EnemyModelState Model,
    bool IsVeryFirstEnemy,
    bool IsForcedRainbowEnemy,
    int TimeCubeReward,
    int WeaponCubeReward);

public sealed record ArenaBlockAttackResult(
    BlockDamageResult Block,
    bool ModelCleared,
    ArenaClearResult? ArenaResult);

public sealed record HeroVolleyExecutionResult(
    HeroVolleyPlan Plan,
    IReadOnlyList<BlockDamageResult> DamageResults,
    bool ModelCleared,
    ArenaClearResult? ArenaResult);

/// <summary>
/// Deterministic orchestration layer joining the reconstructed GameState,
/// VoxelLibrary math, Arena spawn rules and BoxEnemy block state.
/// Random values are explicit inputs so regression tests do not depend on Unity.
/// </summary>
public sealed class HeadlessArenaEngine
{
    private readonly GameState _game;
    private readonly IVoxelModelCatalog _voxels;
    private readonly HeroAutoFireState[] _heroAttackStates;

    public HeadlessArenaEngine(GameState game, IVoxelModelCatalog voxels)
    {
        _game = game;
        _voxels = voxels;
        _heroAttackStates = Enumerable.Range(0, game.Heroes.Length)
            .Select(_ => new HeroAutoFireState())
            .ToArray();
    }

    public GameState Game => _game;
    public HeadlessSpawnedEnemy? CurrentEnemy { get; private set; }

    public HeadlessSpawnedEnemy SpawnCurrentEnemy(
        ArenaSpawnRolls rolls,
        double nowSeconds = 0)
    {
        if (CurrentEnemy is not null)
            throw new InvalidOperationException("An enemy model is already active.");

        int wave = _game.Arena.Wave;
        int minCount = VoxelSpawnMath.GetMinEnemyCountForWave(wave);
        int maxCount = VoxelSpawnMath.GetMaxEnemyCountForWave(wave);
        var hpPlan = VoxelSpawnMath.BuildHpPlan(
            ArenaMath.GetArenaHP(wave),
            minCount,
            maxCount);

        var descriptor = VoxelSpawnMath.SelectModel(
            _voxels.Models,
            hpPlan,
            wave,
            rolls.ModelRandomIndex);

        if (descriptor is null)
        {
            throw new InvalidOperationException(
                $"No voxel model is eligible for wave {wave}. " +
                "The original game would use SpawnRandomBlocks here.");
        }

        var artifacts = _game.ArtifactEffects;
        int[] transformed = VoxelSpawnMath.TransformEnemyCounts(
            hpPlan.ToArray(),
            artifacts.ConvertYellowToWhite,
            artifacts.ConvertWhiteToRed);

        int[] rainbowConversions =
        {
            artifacts.ConvertRedToRainbow,
            artifacts.ConvertWhiteToRainbow,
            artifacts.ConvertYellowToRainbow
        };

        bool veryFirst =
            wave == 1 &&
            _game.Arena.EnemyKillsOnWave == 0;

        bool forceRainbow = SpawnRulesMath.ShouldSpawnRainbowEnemy(
            wave,
            _game.Arena.EnemyKillsOnWave,
            artifacts,
            rolls.RainbowEnemyRoll);

        int timeCubeReward = _game.ResolveTimeCubeSpawn(rolls.TimeCubeRoll);
        int weaponCubeReward = _game.ResolveWeaponCubeSpawn(rolls.WeaponCubeRoll);

        var layout = _voxels.GetLayout(descriptor.Id);
        var spawnPlan = VoxelSpawnPlanner.Build(
            layout,
            hpPlan.BaseHp,
            transformed,
            rainbowConversions,
            timeCubeReward,
            weaponCubeReward,
            isVeryFirstEnemy: veryFirst,
            forceRainbowEnemy: forceRainbow);

        var model = VoxelSpawnPlanner.CreateEnemyModel(spawnPlan);

        if (_game.Arena.IsBossWave)
            _game.Arena.StartArena(nowSeconds, artifacts.BossTime);

        CurrentEnemy = new HeadlessSpawnedEnemy(
            descriptor.Id,
            layout,
            hpPlan,
            spawnPlan,
            model,
            veryFirst,
            forceRainbow,
            timeCubeReward,
            weaponCubeReward);

        return CurrentEnemy;
    }

    public ArenaBlockAttackResult ClickBlock(
        int blockIndex,
        bool critical = false,
        double nowSeconds = 0)
    {
        var active = RequireCurrentEnemy();
        var block = active.Model.GetBlock(blockIndex);

        var damage = _game.ApplyClickToBlock(block, critical);
        return FinishAttack(active, damage, nowSeconds);
    }

    public ArenaBlockAttackResult DamageBlock(
        int blockIndex,
        double damage,
        double nowSeconds = 0)
    {
        var active = RequireCurrentEnemy();
        var block = active.Model.GetBlock(blockIndex);

        var result = _game.ApplyDamageToBlock(block, damage);
        return FinishAttack(active, result, nowSeconds);
    }

    public HeroAutoFireState GetHeroAttackState(int heroId) =>
        _heroAttackStates[heroId];

    public HeroVolleyExecutionResult FireHero(
        int heroId,
        double nowSeconds,
        IRandomSource random)
    {
        var active = RequireCurrentEnemy();
        var hero = _game.Heroes[heroId];
        var state = _heroAttackStates[heroId];

        var plan = HeroAutoFirePlanner.Plan(
            hero,
            state,
            active.Model,
            _game.ArtifactEffects,
            _game.GetHeroDpsMultipliers(heroId),
            nowSeconds,
            random);

        if (!plan.Fired)
        {
            return new HeroVolleyExecutionResult(
                plan,
                Array.Empty<BlockDamageResult>(),
                active.Model.IsCleared,
                null);
        }

        var results = new List<BlockDamageResult>(plan.Applications.Count);
        ArenaClearResult? arenaResult = null;

        foreach (var application in plan.Applications)
        {
            var result = _game.ApplyDamageToBlock(
                application.Target,
                application.Damage);
            results.Add(result);

            if (result.Killed && active.Model.IsCleared)
            {
                arenaResult = _game.Arena.CompleteEnemy(
                    _game.ArtifactEffects.EnemiesToAdvance,
                    nowSeconds);

                CurrentEnemy = null;
                break;
            }
        }

        return new HeroVolleyExecutionResult(
            plan,
            results,
            active.Model.IsCleared,
            arenaResult);
    }

    public BossTickResult TickBoss(double nowSeconds)
    {
        var result = _game.Arena.TickBoss(
            nowSeconds,
            _game.ArtifactEffects.BossTime);

        if (result == BossTickResult.FailedWaitingForRestart)
            CurrentEnemy = null;

        return result;
    }

    public void ClearActiveEnemyWithoutProgression()
    {
        CurrentEnemy = null;
    }

    private ArenaBlockAttackResult FinishAttack(
        HeadlessSpawnedEnemy active,
        BlockDamageResult blockResult,
        double nowSeconds)
    {
        if (!blockResult.Killed || !active.Model.IsCleared)
        {
            return new ArenaBlockAttackResult(
                blockResult,
                active.Model.IsCleared,
                null);
        }

        var arenaResult = _game.Arena.CompleteEnemy(
            _game.ArtifactEffects.EnemiesToAdvance,
            nowSeconds);

        CurrentEnemy = null;

        return new ArenaBlockAttackResult(
            blockResult,
            ModelCleared: true,
            ArenaResult: arenaResult);
    }

    private HeadlessSpawnedEnemy RequireCurrentEnemy() =>
        CurrentEnemy ?? throw new InvalidOperationException("No active enemy model.");
}
