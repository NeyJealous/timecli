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

AssertEqual(10, ArenaMath.GetArenaHP(1), "Arena HP wave 1");
AssertEqual(30, ArenaMath.GetArenaHP(2), "Arena HP wave 2");
AssertEqual(50, ArenaMath.GetArenaHP(3), "Arena HP wave 3");
AssertEqual(696, ArenaMath.GetArenaHP(5), "Arena HP wave 5");
AssertEqual(6962, ArenaMath.GetArenaHP(10), "Arena HP wave 10");
AssertEqual(1267901, ArenaMath.GetArenaHP(26), "Arena HP wave 26");

AssertEqual(45, HeroProgressionMath.GetLevelUpCostForLevel(0, 0, 1), "Hero cost b0 r1 l0");
AssertEqual(97, HeroProgressionMath.GetLevelUpCostForLevel(0, 10, 1), "Hero cost b0 r1 l10");
AssertEqual(90, HeroProgressionMath.GetLevelUpCostForLevel(1, 0, 1), "Hero cost b1 r1 l0");
AssertEqual(720, HeroProgressionMath.GetLevelUpCostForLevel(2, 0, 1), "Hero cost b2 r1 l0");

AssertEqual(10, ArenaMath.TimeCubeBossCounts[0], "Time Cube boss reward 100");
AssertEqual(2500, ArenaMath.TimeCubeBossCounts[^1], "Time Cube boss reward 3000");
AssertEqual(1, ArenaMath.WeaponCubeBossCounts[0], "Weapon Cube boss reward 1000");
AssertEqual(1250, ArenaMath.WeaponCubeBossCounts[^1], "Weapon Cube boss reward 4000");

AssertEqual(1.0, ArenaMath.GetTimeCubeDpsMultiplier(0), "TC DPS multiplier 0");
AssertEqual(11.0, ArenaMath.GetTimeCubeDpsMultiplier(100), "TC DPS multiplier 100");
AssertEqual(50.0, ArenaMath.GetOfflineGoldPerSecond(100), "Offline gold");

Console.WriteLine("PortCore smoke tests passed.");
