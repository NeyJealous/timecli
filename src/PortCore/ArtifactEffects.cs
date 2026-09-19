using System;
using System.Collections.Generic;

namespace TimeClickers.PortCore;

public enum ArtifactType
{
    Unknown = 0,
    GoldFind = 1,
    GoldFindRed = 2,
    GoldFindWhite = 3,
    GoldFindYellow = 4,
    StartWave = 5,
    TimeCubeChance = 6,
    TimeCubeFind = 7,
    BossTimer = 8,
    EnemiesToAdvance = 9,
    GoldFindRainbowBlock = 10,
    RainbowBallGoldMinutes = 11,
    RainbowBallChance = 12,
    RainbowConvertRed = 13,
    RainbowEnemyChance = 14,
    RainbowConvertWhite = 15,
    RainbowConvertYellow = 16,
    DamageTargeted = 17,
    DamageTargetedParticleBall = 18,
    DamageTargetedRocketLauncher = 19,
    DamageTargetedSpreadRifle = 20,
    DamageTargetedFlakCannon = 21,
    DamageTargetedPulsePistol = 22,
    ClickDamageRed = 23,
    ClickDamageWhite = 24,
    ClickDamageYellow = 25,
    ClickDamage = 26,
    CriticalStrikeChance = 27,
    CriticalStrikeMultiplier = 28,
    AbilityDuration1 = 29,
    AbilityDuration2 = 30,
    AbilityCooldown1 = 31,
    AbilityCooldown2 = 32,
    SpreadShots = 33,
    RapidFire = 34,
    TeamWork = 35,
    Overcharge = 36,
    AugmentedAim = 37,
    ExplosiveShot = 38,
    DimensionShift = 39,
    GoldRush = 40,
    TeamDps = 41,
    PulsePistolDps = 42,
    FlakCannonDps = 43,
    SpreadRifleDps = 44,
    RocketLauncherDps = 45,
    ParticleBallDps = 46,
    RocketSplashDiagonal = 47,
    NeverMissFlak = 48,
    NeverMissSpread = 49,
    ConvertWhiteToRed = 50,
    ConvertYellowToWhite = 51,
    ParticleCollider = 52,
    StartingGold = 53,
    Total = 54
}

/// <summary>
/// Portable snapshot of the aggregate values produced by Artifacts.Recalculate
/// in Time Clickers 1.4.5. Input values are the already-evaluated artifact
/// modifier values, indexed by ArtifactType.
/// </summary>
public sealed record ArtifactEffects(
    double GoldFindMultiplier,
    double GoldFindRed,
    double GoldFindWhite,
    double GoldFindYellow,
    double GoldFindRainbowBlock,
    int StartWave,
    float TimeCubeChance,
    double TimeCubeMultiplier,
    float BossTime,
    int EnemiesToAdvance,
    int RainbowBallGoldMinutes,
    int ConvertRedToRainbow,
    float RainbowEnemyChance,
    int ConvertWhiteToRainbow,
    int ConvertYellowToRainbow,
    int ConvertWhiteToRed,
    int ConvertYellowToWhite,
    double TargetedMultiplier,
    double TargetedPulsePistol,
    double TargetedFlakCannon,
    double TargetedSpreadRifle,
    double TargetedRocketLauncher,
    double TargetedParticleBall,
    double ClickDamageRed,
    double ClickDamageWhite,
    double ClickDamageYellow,
    double ClickDamageMultiplier,
    float CriticalStrikeChance,
    double AdditionalCriticalMultiplier,
    int AbilityDurationAddSeconds,
    float AbilityRechargeMultiplier,
    int SpreadShotsProjectiles,
    float RapidFireDelay,
    int RapidFireBulletsPerSecond,
    double TeamWorkDpsMultiplier,
    double AdditionalOverchargedMultiplier,
    float AdditionalAugmentedAim,
    double ExplosiveShotsMultiplier,
    double DimensionShiftMultiplier,
    double GoldRushMultiplier,
    double TeamDpsMultiplier,
    double PulsePistolDpsMultiplier,
    double FlakCannonDpsMultiplier,
    double SpreadRifleDpsMultiplier,
    double RocketLauncherDpsMultiplier,
    double ParticleBallDpsMultiplier,
    float RocketSplashDistance,
    bool FlakNeverMisses,
    bool SpreadNeverMisses,
    double ParticleColliderMultiplier,
    double StartingGold)
{
    public double GetWeaponDpsMultiplier(WeaponType weapon) => weapon switch
    {
        WeaponType.Pistol => PulsePistolDpsMultiplier,
        WeaponType.FlakCannon => FlakCannonDpsMultiplier,
        WeaponType.SpreadRifle => SpreadRifleDpsMultiplier,
        WeaponType.RocketLauncher => RocketLauncherDpsMultiplier,
        WeaponType.ParticleBall => ParticleBallDpsMultiplier,
        _ => 1.0
    };
}

public static class ArtifactEffectsMath
{
    public static ArtifactEffects Recalculate(IReadOnlyList<double> values)
    {
        if (values.Count < (int)ArtifactType.Total)
            throw new ArgumentException(
                $"Expected at least {(int)ArtifactType.Total} artifact values, got {values.Count}.",
                nameof(values));

        double V(ArtifactType type) => values[(int)type];

        float rapidFireValue = (float)V(ArtifactType.RapidFire);

        return new ArtifactEffects(
            GoldFindMultiplier: (100.0 + V(ArtifactType.GoldFind)) / 100.0,
            GoldFindRed: V(ArtifactType.GoldFindRed),
            GoldFindWhite: V(ArtifactType.GoldFindWhite),
            GoldFindYellow: V(ArtifactType.GoldFindYellow),
            GoldFindRainbowBlock: V(ArtifactType.GoldFindRainbowBlock),
            StartWave: (int)V(ArtifactType.StartWave),
            TimeCubeChance: (float)(V(ArtifactType.TimeCubeChance) / 100.0),
            TimeCubeMultiplier: (100.0 + V(ArtifactType.TimeCubeFind)) / 100.0,
            BossTime: (float)V(ArtifactType.BossTimer),
            EnemiesToAdvance: (int)V(ArtifactType.EnemiesToAdvance),
            RainbowBallGoldMinutes: (int)V(ArtifactType.RainbowBallGoldMinutes),
            ConvertRedToRainbow: (int)V(ArtifactType.RainbowConvertRed),
            RainbowEnemyChance: (float)(V(ArtifactType.RainbowEnemyChance) / 100.0),
            ConvertWhiteToRainbow: (int)V(ArtifactType.RainbowConvertWhite),
            ConvertYellowToRainbow: (int)V(ArtifactType.RainbowConvertYellow),
            ConvertWhiteToRed: (int)V(ArtifactType.ConvertWhiteToRed),
            ConvertYellowToWhite: (int)V(ArtifactType.ConvertYellowToWhite),
            TargetedMultiplier: (100.0 + V(ArtifactType.DamageTargeted)) / 100.0,
            TargetedPulsePistol: V(ArtifactType.DamageTargetedPulsePistol),
            TargetedFlakCannon: V(ArtifactType.DamageTargetedFlakCannon),
            TargetedSpreadRifle: V(ArtifactType.DamageTargetedSpreadRifle),
            TargetedRocketLauncher: V(ArtifactType.DamageTargetedRocketLauncher),
            TargetedParticleBall: V(ArtifactType.DamageTargetedParticleBall),
            ClickDamageRed: V(ArtifactType.ClickDamageRed),
            ClickDamageWhite: V(ArtifactType.ClickDamageWhite),
            ClickDamageYellow: V(ArtifactType.ClickDamageYellow),
            ClickDamageMultiplier: V(ArtifactType.ClickDamage),
            CriticalStrikeChance: (float)(V(ArtifactType.CriticalStrikeChance) / 100.0),
            AdditionalCriticalMultiplier: V(ArtifactType.CriticalStrikeMultiplier) - 1.0,
            AbilityDurationAddSeconds:
                (int)V(ArtifactType.AbilityDuration1) + (int)V(ArtifactType.AbilityDuration2),
            AbilityRechargeMultiplier: (float)(
                (100.0 - V(ArtifactType.AbilityCooldown1) - V(ArtifactType.AbilityCooldown2)) / 100.0),
            SpreadShotsProjectiles: (int)V(ArtifactType.SpreadShots),
            RapidFireDelay: 1.0f / rapidFireValue,
            RapidFireBulletsPerSecond: (int)V(ArtifactType.RapidFire),
            TeamWorkDpsMultiplier: V(ArtifactType.TeamWork),
            AdditionalOverchargedMultiplier: V(ArtifactType.Overcharge),
            AdditionalAugmentedAim: (float)V(ArtifactType.AugmentedAim),
            ExplosiveShotsMultiplier: V(ArtifactType.ExplosiveShot),
            DimensionShiftMultiplier: (100.0 + V(ArtifactType.DimensionShift)) / 100.0,
            GoldRushMultiplier: V(ArtifactType.GoldRush),
            TeamDpsMultiplier: V(ArtifactType.TeamDps),
            PulsePistolDpsMultiplier: V(ArtifactType.PulsePistolDps),
            FlakCannonDpsMultiplier: V(ArtifactType.FlakCannonDps),
            SpreadRifleDpsMultiplier: V(ArtifactType.SpreadRifleDps),
            RocketLauncherDpsMultiplier: V(ArtifactType.RocketLauncherDps),
            ParticleBallDpsMultiplier: V(ArtifactType.ParticleBallDps),
            RocketSplashDistance: V(ArtifactType.RocketSplashDiagonal) == 0.0
                ? 1.100000023841858f
                : 3.0f,
            FlakNeverMisses: V(ArtifactType.NeverMissFlak) != 0.0,
            SpreadNeverMisses: V(ArtifactType.NeverMissSpread) != 0.0,
            ParticleColliderMultiplier: V(ArtifactType.ParticleCollider),
            StartingGold: V(ArtifactType.StartingGold));
    }
}
