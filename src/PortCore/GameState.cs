using System;
using System.Collections.Generic;
using System.Linq;

namespace TimeClickers.PortCore;

/// <summary>
/// Unity-independent aggregate state for a single reconstructed Time Clickers save.
/// Rendering, input, audio, ads and platform SDKs are intentionally outside this type.
/// </summary>
public sealed class GameState
{
    public GameState()
    {
        Artifacts = new ArtifactLoadout();
        WeaponAugments = new WeaponAugmentLoadout();
        Gold = new GoldWallet();
        TimeCubes = new TimeCubeBank();
        WeaponCubes = new WeaponCubeBankState();
        ArenaRewards = new ArenaRewardHistory();
        CubePickups = new SpecialCubePickupQueue();

        Heroes = CanonicalHeroes.All.Select(h => new HeroRuntime(h)).ToArray();
        ClickPistol = new SkillRuntime(CanonicalSkills.ClickPistol);

        var effects = Artifacts.BuildEffects();
        ActiveAbilities = new ActiveAbilityProgression(effects);

        Arena = new TimelineProgression(effects.StartWave);
        StartNewTimeline();
    }

    public ArtifactLoadout Artifacts { get; }
    public WeaponAugmentLoadout WeaponAugments { get; }
    public GoldWallet Gold { get; }
    public TimeCubeBank TimeCubes { get; }
    public WeaponCubeBankState WeaponCubes { get; }
    public ArenaRewardHistory ArenaRewards { get; }
    public SpecialCubePickupQueue CubePickups { get; }

    public HeroRuntime[] Heroes { get; }
    public SkillRuntime ClickPistol { get; }
    public ActiveAbilityProgression ActiveAbilities { get; }
    public AbilityRuntime[] Abilities => ActiveAbilities.Abilities;
    public TimelineProgression Arena { get; private set; }

    public ArtifactEffects ArtifactEffects => Artifacts.BuildEffects();
    public WeaponAugmentEffects WeaponAugmentEffects => WeaponAugments.BuildEffects();

    /// <summary>
    /// Matches NewGamePlus.StartNewTimeline: only Arena and GoldBank reset.
    /// </summary>
    public void StartNewTimeline()
    {
        var effects = ArtifactEffects;
        Arena.TimeWarpTo(effects.StartWave);
        Gold.TimeWarp(effects.StartingGold);
    }

    /// <summary>
    /// Matches the gameplay-state portion of NewGamePlus._DoTimeWarp.
    /// Persistent Artifacts and Weapon Augments are intentionally retained.
    /// </summary>
    public ulong TimeWarp()
    {
        ulong earnedTimeCubes = TimeCubes.TimeWarp();

        foreach (var hero in Heroes)
            hero.TimeWarp();

        ClickPistol.TimeWarp();

        var effects = ArtifactEffects;
        ActiveAbilities.TimeWarp(effects);

        Arena.TimeWarpTo(effects.StartWave);
        ArenaRewards.TimeWarp();
        Gold.TimeWarp(effects.StartingGold);
        WeaponCubes.TimeWarp();

        return earnedTimeCubes;
    }

    public bool TryPurchaseHero(int heroId, HeroBuyMode mode)
    {
        var hero = Heroes[heroId];
        var plan = hero.PlanPurchase(Gold.TotalGold, mode);

        if (!Gold.CanAfford(plan.TotalCost))
            return false;

        var result = hero.Purchase(Gold.TotalGold, mode);
        if (!result.Purchased)
            return false;

        return Gold.TrySpend(result.GoldSpent);
    }

    public bool TryPurchaseClickPistol(HeroBuyMode mode)
    {
        var plan = ClickPistol.PlanPurchase(Gold.TotalGold, mode);

        if (!Gold.CanAfford(plan.TotalCost))
            return false;

        var result = ClickPistol.Purchase(Gold.TotalGold, mode);
        if (!result.Purchased)
            return false;

        return Gold.TrySpend(result.GoldSpent);
    }

    public ProgressionTransaction BuyArtifact(ArtifactType type, double nowSeconds = 0)
    {
        var result = ArtifactEconomy.BuyOne(Artifacts, TimeCubes, type);
        if (result.Succeeded)
            RecalculateAbilities(nowSeconds);
        return result;
    }

    public ProgressionTransaction SellArtifact(ArtifactType type, double nowSeconds = 0)
    {
        var result = ArtifactEconomy.SellOne(Artifacts, TimeCubes, type);
        if (result.Succeeded)
            RecalculateAbilities(nowSeconds);
        return result;
    }

    public ProgressionTransaction BuyWeaponAugment(WeaponAugmentType type) =>
        WeaponAugmentEconomy.BuyOne(WeaponAugments, WeaponCubes, type);

    public ProgressionTransaction SellWeaponAugment(WeaponAugmentType type) =>
        WeaponAugmentEconomy.SellOne(WeaponAugments, WeaponCubes, type);

    /// <summary>
    /// Full Artifact respec. The original does not merely refund the currently
    /// visible tree: it restores all lifetime Time Cubes, includes the pending
    /// timeline reward, resets all Artifact levels, and starts progression over.
    /// </summary>
    public ulong RespecArtifacts()
    {
        Artifacts.Reset();
        ulong pendingAdded = TimeCubes.Respec();

        foreach (var hero in Heroes)
            hero.TimeWarp();

        ClickPistol.TimeWarp();

        var effects = ArtifactEffects;
        ActiveAbilities.TimeWarp(effects);

        Arena.TimeWarpTo(effects.StartWave);
        ArenaRewards.TimeWarp();
        Gold.TimeWarp(effects.StartingGold);
        WeaponCubes.TimeWarp();
        RecalculateAbilities(0);

        return pendingAdded;
    }

    public bool TryPurchaseNextAbility() =>
        ActiveAbilities.TryPurchaseNext(Gold);

    public bool ActivateAbility(AbilityType type, double nowSeconds) =>
        ActiveAbilities.Activate(type, nowSeconds);

    public void UpdateAbilities(double nowSeconds) =>
        ActiveAbilities.Update(nowSeconds);

    public void RecalculateAbilities(double nowSeconds) =>
        ActiveAbilities.Recalculate(ArtifactEffects, nowSeconds);

    public TeamUpgradeEffects GetTeamUpgradeEffects()
    {
        var schedules = Heroes.Select(h =>
            new HeroUpgradeProgress(h.Schedule, h.PurchasedUpgrades));

        return TeamMath.RecalculateBaseUpgradeEffects(schedules);
    }

    public bool IsAbilityActive(AbilityType type) =>
        ActiveAbilities.IsActive(type);

    public HeroDpsMultipliers GetHeroDpsMultipliers(int heroId)
    {
        var hero = Heroes[heroId];
        var artifacts = ArtifactEffects;
        var team = GetTeamUpgradeEffects();

        return new HeroDpsMultipliers(
            DimensionShift: ActiveAbilities.GetDimensionShiftMultiplier(artifacts),
            UnitedFront: team.UnitedFrontMultiplier,
            AchievementDps: 1.0,
            TimeCubeDps: TimeCubes.DpsMultiplier,
            TeamDps: artifacts.TeamDpsMultiplier,
            WeaponDps: artifacts.GetWeaponDpsMultiplier(hero.Spec.Weapon),
            TeamWorkActive: IsAbilityActive(AbilityType.TeamWork),
            TeamWorkDps: artifacts.TeamWorkDpsMultiplier);
    }

    public double GetHeroDps(int heroId)
    {
        var hero = Heroes[heroId];
        return HeroCombatMath.GetDpsForLevel(
            hero.Spec,
            hero.Schedule,
            hero.Level,
            hero.PurchasedUpgrades,
            GetHeroDpsMultipliers(heroId));
    }

    public double GetTeamDps()
    {
        double total = 0.0;

        for (int i = 0; i < Heroes.Length; i++)
            total += GetHeroDps(i);

        return total;
    }

    public double GetHeroesGoldFindMultiplier() =>
        GetTeamUpgradeEffects().GoldFindMultiplier;

    public double GetHeroExtraClickDamage()
    {
        double totalPercent = 0.0;

        foreach (var hero in Heroes)
        {
            totalPercent += HeroCombatMath.GetExtraClickDamagePercent(
                hero.Schedule,
                hero.PurchasedUpgrades);
        }

        return totalPercent > 0.0
            ? totalPercent * GetTeamDps()
            : 0.0;
    }

    public float GetCriticalChance()
    {
        var artifacts = ArtifactEffects;
        var team = GetTeamUpgradeEffects();

        return SkillMath.GetCriticalChance(
            team.CriticalChance,
            achievementAdditionalChance: 0f,
            artifactAdditionalChance: artifacts.CriticalStrikeChance,
            augmentedAimActive: IsAbilityActive(AbilityType.AugmentedAim),
            augmentedAimMultiplier: artifacts.AdditionalAugmentedAim);
    }

    public double GetCriticalMultiplier()
    {
        var artifacts = ArtifactEffects;
        var team = GetTeamUpgradeEffects();

        return SkillMath.GetCriticalMultiplier(
            team.CriticalMultiplier,
            overchargedActive: IsAbilityActive(AbilityType.Overcharged),
            additionalOverchargedMultiplier: artifacts.AdditionalOverchargedMultiplier,
            artifactAdditionalCriticalMultiplier: artifacts.AdditionalCriticalMultiplier);
    }

    public double GetClickDamage(bool isCritical = false)
    {
        var artifacts = ArtifactEffects;
        double baseDamage = ClickPistol.GetDamage(
            GetHeroExtraClickDamage(),
            artifacts.ClickDamageMultiplier);

        return SkillMath.GetClickDamage(
            baseDamage,
            achievementAdditionalClickDamage: 0.0,
            isCritical,
            GetCriticalMultiplier(),
            explosiveShotsActive: IsAbilityActive(AbilityType.ExplosiveShots),
            explosiveShotsMultiplier: artifacts.ExplosiveShotsMultiplier);
    }

    public int ResolveTimeCubeSpawn(float random01) =>
        SpawnRulesMath.ResolveTimeCubeReward(
            Arena.Wave,
            ArenaRewards.HasTimeCubeReward(Arena.Wave),
            ArtifactEffects,
            random01);

    public int ResolveWeaponCubeSpawn(float random01) =>
        SpawnRulesMath.ResolveWeaponCubeReward(
            Arena.Wave,
            ArenaRewards.HasWeaponCubeReward(Arena.Wave),
            WeaponAugmentEffects,
            random01);

    public BlockDamageResult ApplyClickToBlock(
        EnemyBlockState block,
        bool isCritical = false,
        double nowSeconds = 0)
    {
        var result = block.ApplyClickDamage(
            GetClickDamage(isCritical),
            ArtifactEffects,
            GetHeroesGoldFindMultiplier(),
            IsAbilityActive(AbilityType.GoldRush));

        ApplyBlockReward(block, result, nowSeconds);
        return result;
    }

    public BlockDamageResult ApplyDamageToBlock(
        EnemyBlockState block,
        double damage,
        double nowSeconds = 0)
    {
        var result = block.ApplyDamage(
            damage,
            ArtifactEffects,
            GetHeroesGoldFindMultiplier(),
            IsAbilityActive(AbilityType.GoldRush));

        ApplyBlockReward(block, result, nowSeconds);
        return result;
    }

    public bool TryCollectCubePickup(long pickupId, double nowSeconds) =>
        CubePickups.TryCollect(pickupId, nowSeconds);

    public IReadOnlyList<SpecialCubeCollection> UpdateCubePickups(double nowSeconds) =>
        CubePickups.Tick(nowSeconds, TimeCubes, WeaponCubes);

    public OfflineProgressionResult ApplyOfflineEarnings(double secondsSinceSave)
    {
        var result = OfflineProgression.Calculate(
            secondsSinceSave,
            GetTeamDps(),
            Gold.TimelineGoldPerSecond,
            GetHeroesGoldFindMultiplier(),
            ArtifactEffects.GoldFindMultiplier,
            ArenaMath.GetArenaHP(Arena.Wave));

        if (result.GoldEarned > 0.0)
            Gold.Add(result.GoldEarned);

        return result;
    }

    private void ApplyBlockReward(
        EnemyBlockState block,
        BlockDamageResult result,
        double nowSeconds)
    {
        if (!result.Killed)
            return;

        if (result.Reward.Gold > 0.0)
            Gold.Add(result.Reward.Gold);

        if (result.Reward.TimeCubes > 0)
        {
            ArenaRewards.MarkTimeCubeReward(Arena.Wave);
            CubePickups.Spawn(
                SpecialCubeKind.TimeCube,
                result.Reward.TimeCubes,
                nowSeconds,
                block);
        }

        if (result.Reward.WeaponCubes > 0)
        {
            ArenaRewards.MarkWeaponCubeReward(Arena.Wave);
            CubePickups.Spawn(
                SpecialCubeKind.WeaponCube,
                result.Reward.WeaponCubes,
                nowSeconds,
                block);
        }
    }
}
