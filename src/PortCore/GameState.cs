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

    public void RecalculateAbilities(double nowSeconds)
    {
        var effects = ArtifactEffects;
        foreach (var ability in Abilities)
            ability.Recalculate(effects, nowSeconds);
    }

    public double GetTeamDps()
    {
        var artifacts = ArtifactEffects;

        var schedules = Heroes.Select(h =>
            new HeroUpgradeProgress(h.Schedule, h.PurchasedUpgrades));

        var team = TeamMath.RecalculateBaseUpgradeEffects(schedules);

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
                    TeamWorkActive: false,
                    TeamWorkDps: artifacts.TeamWorkDpsMultiplier));
        }

        return total;
    }

    public double GetHeroExtraClickDamage()
    {
        var contributions = Heroes.Select(hero =>
        {
            double dps = HeroCombatMath.GetDpsForLevel(
                hero.Spec,
                hero.Schedule,
                hero.Level,
                hero.PurchasedUpgrades,
                new HeroDpsMultipliers());

            double percent = HeroCombatMath.GetExtraClickDamagePercent(
                hero.Schedule,
                hero.PurchasedUpgrades);

            return (HeroDps: dps, ExtraClickDamagePercent: percent);
        });

        return TeamMath.GetExtraClickDamage(contributions);
    }

    public double GetClickDamage()
    {
        var artifacts = ArtifactEffects;
        return ClickPistol.GetDamage(
            GetHeroExtraClickDamage(),
            artifacts.ClickDamageMultiplier);
    }
}
