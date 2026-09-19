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

        Heroes = CanonicalHeroes.All.Select(h => new HeroRuntime(h)).ToArray();
        ClickPistol = new SkillRuntime(CanonicalSkills.ClickPistol);

        var effects = Artifacts.BuildEffects();
        Abilities = AbilityCatalog.All
            .Select(a => new AbilityRuntime(a, effects))
            .ToArray();

        Arena = new TimelineProgression(effects.StartWave);
        StartNewTimeline();
    }

    public ArtifactLoadout Artifacts { get; }
    public WeaponAugmentLoadout WeaponAugments { get; }
    public GoldWallet Gold { get; }
    public TimeCubeBank TimeCubes { get; }
    public WeaponCubeBankState WeaponCubes { get; }
    public ArenaRewardHistory ArenaRewards { get; }

    public HeroRuntime[] Heroes { get; }
    public SkillRuntime ClickPistol { get; }
    public AbilityRuntime[] Abilities { get; }
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

        foreach (var ability in Abilities)
            ability.TimeWarp();

        var effects = ArtifactEffects;
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

        foreach (var ability in Abilities)
            ability.TimeWarp();

        var effects = ArtifactEffects;
        Arena.TimeWarpTo(effects.StartWave);
        ArenaRewards.TimeWarp();
        Gold.TimeWarp(effects.StartingGold);
        WeaponCubes.TimeWarp();
        RecalculateAbilities(0);

        return pendingAdded;
    }

    public void RecalculateAbilities(double nowSeconds)
    {
        var effects = ArtifactEffects;
        foreach (var ability in Abilities)
            ability.Recalculate(effects, nowSeconds);
    }

    public TeamUpgradeEffects GetTeamUpgradeEffects()
    {
        var schedules = Heroes.Select(h =>
            new HeroUpgradeProgress(h.Schedule, h.PurchasedUpgrades));

        return TeamMath.RecalculateBaseUpgradeEffects(schedules);
    }

    public bool IsAbilityActive(AbilityType type) =>
        Abilities[(int)type].State == AbilityState.Active;

    public double GetTeamDps()
    {
        var artifacts = ArtifactEffects;
        var team = GetTeamUpgradeEffects();

        double total = 0.0;
        foreach (var hero in Heroes)
        {
            total += HeroCombatMath.GetDpsForLevel(
                hero.Spec,
                hero.Schedule,
                hero.Level,
                hero.PurchasedUpgrades,
                new HeroDpsMultipliers(
                    DimensionShift: 1.0,
                    UnitedFront: team.UnitedFrontMultiplier,
                    AchievementDps: 1.0,
                    TimeCubeDps: TimeCubes.DpsMultiplier,
                    TeamDps: artifacts.TeamDpsMultiplier,
                    WeaponDps: artifacts.GetWeaponDpsMultiplier(hero.Spec.Weapon),
                    TeamWorkActive: IsAbilityActive(AbilityType.TeamWork),
                    TeamWorkDps: artifacts.TeamWorkDpsMultiplier));
        }

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
        bool isCritical = false)
    {
        var result = block.ApplyClickDamage(
            GetClickDamage(isCritical),
            ArtifactEffects,
            GetHeroesGoldFindMultiplier(),
            IsAbilityActive(AbilityType.GoldRush));

        ApplyBlockReward(block, result);
        return result;
    }

    public BlockDamageResult ApplyDamageToBlock(
        EnemyBlockState block,
        double damage)
    {
        var result = block.ApplyDamage(
            damage,
            ArtifactEffects,
            GetHeroesGoldFindMultiplier(),
            IsAbilityActive(AbilityType.GoldRush));

        ApplyBlockReward(block, result);
        return result;
    }

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
        BlockDamageResult result)
    {
        if (!result.Killed)
            return;

        if (result.Reward.Gold > 0.0)
            Gold.Add(result.Reward.Gold);

        if (result.Reward.TimeCubes > 0)
        {
            ArenaRewards.MarkTimeCubeReward(Arena.Wave);
            TimeCubes.AddPendingReward(result.Reward.TimeCubes);
        }

        if (result.Reward.WeaponCubes > 0)
        {
            ArenaRewards.MarkWeaponCubeReward(Arena.Wave);
            WeaponCubes.Add(result.Reward.WeaponCubes);
        }
    }
}
