using System;
using System.Collections.Generic;

namespace TimeClickers.PortCore;

public enum UpgradeMod
{
    FireRate = 0,
    HeroDps = 1,
    Projectiles = 2,
    GoldFind = 3,
    ClickDamage = 4,
    UnitedFront = 5,
    Promotion = 6,
    Collider = 7,
    CriticalChance = 8,
    CriticalDamage = 9,
    Splash = 10,
    Training = 11,
    SpecOps = 12
}

public enum WeaponType
{
    Pistol = 0,
    FlakCannon = 1,
    SpreadRifle = 2,
    RocketLauncher = 3,
    ParticleBall = 4
}

public sealed record UpgradeSpec(
    int UpgradeId,
    int LevelRequired,
    IReadOnlyDictionary<UpgradeMod, double> Mods)
{
    public bool Has(UpgradeMod mod) => Mods.ContainsKey(mod);

    public double Get(UpgradeMod mod, double fallback = 0.0) =>
        Mods.TryGetValue(mod, out var value) ? value : fallback;

    public bool IncreasesRank =>
        Has(UpgradeMod.Promotion) || Has(UpgradeMod.Training) || Has(UpgradeMod.SpecOps);
}

public sealed record HeroBaseSpec(
    int Id,
    WeaponType Weapon,
    double BaseDamage,
    float BaseRateOfFire,
    int BaseCostLevel,
    IReadOnlyList<UpgradeSpec> BaseUpgrades);
