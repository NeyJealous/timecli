using TimeClickers.PortCore;

static void AssertEqual(double expected, double actual, string name, double relativeTolerance = 1e-12)
{
    var scale = Math.Max(1.0, Math.Abs(expected));
    if (Math.Abs(expected - actual) > scale * relativeTolerance)
        throw new Exception($"{name}: expected {expected:R}, got {actual:R}");
}

static void AssertEqual(long expected, long actual, string name)
{
    if (expected != actual)
        throw new Exception($"{name}: expected {expected}, got {actual}");
}

static UpgradeSpec U(int level, params (UpgradeMod Mod, double Value)[] mods)
{
    var dict = new Dictionary<UpgradeMod, double>();
    foreach (var (mod, value) in mods)
        dict[mod] = value;
    return new UpgradeSpec(0, level, dict);
}

static InfiniteUpgradeSchedule BuildPulsePistolSchedule()
{
    int[] levels =
    {
        10, 25, 50, 75, 85, 100,
        110, 125, 150, 175, 185, 200,
        210, 225, 250, 275, 285, 300,
        310, 325, 350, 375, 385, 400,
        410, 425, 450, 475, 485,
        500, 600, 700, 800, 900, 1000
    };

    var upgrades = levels.Select(level =>
    {
        if (level is 100 or 200 or 300 or 400)
            return U(level, (UpgradeMod.Promotion, 1));
        if (level is 500 or 600 or 700 or 800 or 900)
            return U(level, (UpgradeMod.Training, level / 100 - 4));
        if (level == 1000)
            return U(level, (UpgradeMod.SpecOps, 1));
        return U(level);
    }).ToArray();

    upgrades[0] = U(10, (UpgradeMod.FireRate, 2), (UpgradeMod.HeroDps, 2));
    upgrades[1] = U(25, (UpgradeMod.FireRate, 2), (UpgradeMod.HeroDps, 2));
    upgrades[2] = U(50, (UpgradeMod.HeroDps, 2.5));
    upgrades[3] = U(75, (UpgradeMod.FireRate, 2.5), (UpgradeMod.HeroDps, 2.5));
    upgrades[4] = U(85, (UpgradeMod.ClickDamage, 0.005));
    upgrades[6] = U(110, (UpgradeMod.FireRate, 2), (UpgradeMod.HeroDps, 2));

    return new InfiniteUpgradeSchedule(upgrades);
}

// Arena / currency
AssertEqual(10, ArenaMath.GetArenaHP(1), "Arena HP wave 1");
AssertEqual(30, ArenaMath.GetArenaHP(2), "Arena HP wave 2");
AssertEqual(50, ArenaMath.GetArenaHP(3), "Arena HP wave 3");
AssertEqual(696, ArenaMath.GetArenaHP(5), "Arena HP wave 5");
AssertEqual(6962, ArenaMath.GetArenaHP(10), "Arena HP wave 10");
AssertEqual(1267901, ArenaMath.GetArenaHP(26), "Arena HP wave 26");
AssertEqual(10, ArenaMath.TimeCubeBossCounts[0], "Time Cube boss reward 100");
AssertEqual(2500, ArenaMath.TimeCubeBossCounts[^1], "Time Cube boss reward 3000");
AssertEqual(1, ArenaMath.WeaponCubeBossCounts[0], "Weapon Cube boss reward 1000");
AssertEqual(1250, ArenaMath.WeaponCubeBossCounts[^1], "Weapon Cube boss reward 4000");
AssertEqual(1.0, ArenaMath.GetTimeCubeDpsMultiplier(0), "TC DPS multiplier 0");
AssertEqual(11.0, ArenaMath.GetTimeCubeDpsMultiplier(100), "TC DPS multiplier 100");
AssertEqual(50.0, ArenaMath.GetOfflineGoldPerSecond(100), "Offline gold");

// Hero cost progression
AssertEqual(45, HeroProgressionMath.GetLevelUpCostForLevel(0, 0, 1), "Hero cost b0 r1 l0");
AssertEqual(97, HeroProgressionMath.GetLevelUpCostForLevel(0, 10, 1), "Hero cost b0 r1 l10");
AssertEqual(90, HeroProgressionMath.GetLevelUpCostForLevel(1, 0, 1), "Hero cost b1 r1 l0");
AssertEqual(720, HeroProgressionMath.GetLevelUpCostForLevel(2, 0, 1), "Hero cost b2 r1 l0");

// Infinite upgrade schedule
var schedule = BuildPulsePistolSchedule();
AssertEqual(35, schedule.BaseUpgradeCount, "Base upgrade count");
AssertEqual(30, schedule.GeneratedCycleCount, "Generated Spec Ops cycle count");
AssertEqual(1, schedule.GetRank(0), "Rank before upgrades");
AssertEqual(2, schedule.GetRank(6), "Rank after first promotion");
AssertEqual(11, schedule.GetRank(35), "Rank after first Spec Ops");
AssertEqual(11, schedule.GetRank(36), "Rank in second cycle");
AssertEqual(21, schedule.GetRank(65), "Rank after second Spec Ops");
AssertEqual(1010, schedule.GetUpgrade(35).LevelRequired, "First generated upgrade level");
AssertEqual(2000, schedule.GetUpgrade(64).LevelRequired, "Generated Spec Ops level");

// Hero combat regression
var pulse = new HeroBaseSpec(
    0, WeaponType.Pistol, 5.0, 3.0f, 0, schedule.BaseUpgrades);

var neutral = new HeroDpsMultipliers();
AssertEqual(100, HeroCombatMath.GetDpsForLevel(pulse, schedule, 10, 1, neutral), "Pulse DPS level 10");
AssertEqual(237250, HeroCombatMath.GetDpsForLevel(pulse, schedule, 100, 6, neutral), "Pulse DPS promotion");
AssertEqual(949000, HeroCombatMath.GetDpsForLevel(pulse, schedule, 110, 7, neutral), "Pulse DPS post-promotion");
AssertEqual(6.0, HeroCombatMath.GetRateOfFire(3.0f, schedule, 1), "Pulse fire rate first upgrade");
AssertEqual(3.0, HeroCombatMath.GetRateOfFire(3.0f, schedule, 6), "Promotion resets fire rate");
AssertEqual(6.0, HeroCombatMath.GetRateOfFire(3.0f, schedule, 7), "Post-promotion fire rate");
AssertEqual(0.005, HeroCombatMath.GetExtraClickDamagePercent(schedule, 5), "Hero extra click damage");

// Click skill / abilities
AssertEqual(5, SkillMath.GetLevelUpCostForLevel(5, 0), "Click skill level cost 0");
AssertEqual(18, SkillMath.GetUpgradeCostForLevel(5, 1), "Click skill upgrade cost");
var clickUpgrades = new[]
{
    U(10, (UpgradeMod.ClickDamage, 2)),
    U(25, (UpgradeMod.ClickDamage, 2))
};
AssertEqual(84, SkillMath.GetDamageForLevel(1, 10, clickUpgrades, 2, 40, 1), "Click skill damage");
AssertEqual(250, AbilityMath.GetCostForLevel(0), "Ability cost 0");
AssertEqual(22500, AbilityMath.GetCostForLevel(1), "Ability cost 1");
AssertEqual(1.21, AbilityMath.GetDimensionShiftMultiplier(2, 1.1), "Dimension shift multiplier", 1e-10);

Console.WriteLine("PortCore smoke tests passed.");
