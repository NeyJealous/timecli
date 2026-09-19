namespace TimeClickers.PortCore;

public static partial class CanonicalHeroes
{
    private static HeroBaseSpec CreateFlakCannon()
    {
        var upgrades = new UpgradeSpec[]
        {
            U(1, 10, (UpgradeMod.FireRate, 2.0), (UpgradeMod.HeroDps, 2.0)),
            U(2, 25, (UpgradeMod.FireRate, 2.5), (UpgradeMod.HeroDps, 2.5)),
            U(3, 50, (UpgradeMod.Projectiles, 8.0), (UpgradeMod.HeroDps, 2.0)),
            U(4, 75, (UpgradeMod.FireRate, 2.5), (UpgradeMod.HeroDps, 2.5)),
            U(5, 85, (UpgradeMod.CriticalChance, 0.02)),
            U(6, 100, (UpgradeMod.Promotion, 1.0)),
            U(7, 110, (UpgradeMod.FireRate, 2.0), (UpgradeMod.HeroDps, 2.0)),
            U(8, 125, (UpgradeMod.FireRate, 2.0), (UpgradeMod.HeroDps, 2.0)),
            U(9, 150, (UpgradeMod.Projectiles, 8.0), (UpgradeMod.HeroDps, 2.0)),
            U(10, 175, (UpgradeMod.FireRate, 2.5), (UpgradeMod.HeroDps, 2.5)),
            U(11, 185, (UpgradeMod.UnitedFront, 1.2)),
            U(12, 200, (UpgradeMod.Promotion, 1.0)),
            U(13, 210, (UpgradeMod.FireRate, 2.0), (UpgradeMod.HeroDps, 2.0)),
            U(14, 225, (UpgradeMod.FireRate, 2.0), (UpgradeMod.HeroDps, 2.0)),
            U(15, 250, (UpgradeMod.Projectiles, 8.0), (UpgradeMod.HeroDps, 2.0)),
            U(16, 275, (UpgradeMod.FireRate, 2.5), (UpgradeMod.HeroDps, 2.5)),
            U(17, 285, (UpgradeMod.CriticalChance, 0.02)),
            U(18, 300, (UpgradeMod.Promotion, 1.0)),
            U(19, 310, (UpgradeMod.FireRate, 2.0), (UpgradeMod.HeroDps, 2.0)),
            U(20, 325, (UpgradeMod.FireRate, 2.0), (UpgradeMod.HeroDps, 2.0)),
            U(21, 350, (UpgradeMod.Projectiles, 8.0), (UpgradeMod.HeroDps, 2.0)),
            U(22, 375, (UpgradeMod.FireRate, 2.5), (UpgradeMod.HeroDps, 2.5)),
            U(23, 385, (UpgradeMod.UnitedFront, 1.2)),
            U(24, 400, (UpgradeMod.Promotion, 1.0)),
            U(25, 410, (UpgradeMod.FireRate, 2.0), (UpgradeMod.HeroDps, 2.0)),
            U(26, 425, (UpgradeMod.FireRate, 2.0), (UpgradeMod.HeroDps, 2.0)),
            U(27, 450, (UpgradeMod.Projectiles, 8.0), (UpgradeMod.HeroDps, 2.0)),
            U(28, 475, (UpgradeMod.FireRate, 2.5), (UpgradeMod.HeroDps, 2.5)),
            U(29, 485, (UpgradeMod.ClickDamage, 0.005)),
            U(30, 500, (UpgradeMod.Training, 1.0)),
            U(31, 600, (UpgradeMod.Training, 2.0), (UpgradeMod.FireRate, 2.0)),
            U(32, 700, (UpgradeMod.Training, 3.0), (UpgradeMod.FireRate, 4.0)),
            U(33, 800, (UpgradeMod.Training, 4.0), (UpgradeMod.FireRate, 4.0), (UpgradeMod.Projectiles, 8.0)),
            U(34, 900, (UpgradeMod.Training, 5.0), (UpgradeMod.FireRate, 10.0), (UpgradeMod.Projectiles, 8.0)),
            U(35, 1000, (UpgradeMod.SpecOps, 1.0)),
        };
        return new HeroBaseSpec(1, WeaponType.FlakCannon, 22.0, 2.5f, 1, upgrades, "Flak Cannon");
    }
}
