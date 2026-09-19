using System;

namespace TimeClickers.PortCore;

public static class ArenaMath
{
    public static readonly int[] TimeCubeBossWaves = { 100, 250, 500, 1000, 2000, 3000 };
    public static readonly int[] TimeCubeBossCounts = { 10, 50, 200, 500, 1500, 2500 };
    public static readonly int[] WeaponCubeBossWaves = { 1000, 1500, 2000, 3000, 4000 };
    public static readonly int[] WeaponCubeBossCounts = { 1, 150, 350, 775, 1250 };

    public const double OverpoweredMin = 7.0;
    public const double OverpoweredRange = 10.0;

    public static double GetArenaHP(int wave)
    {
        bool boss = wave % 5 == 0;
        const double baseHp = 10.0;
        int min130 = Math.Min(130, wave);

        double hp = Math.Ceiling((boss ? 10.0 : 1.0) * baseHp *
                                 Math.Pow(1.6, min130 - 1) +
                                 (wave - 1) * 10.0);

        int min500 = Math.Min(500, wave);
        if (wave > 130)
        {
            hp *= Math.Pow(1.25, min500 - 130);
            if (wave > 500)
                hp *= Math.Pow(1.125, wave - 500);
        }

        if (hp == 26.0) hp = 30.0;
        else if (hp == 46.0) hp = 50.0;
        return hp;
    }

    public static int GetTimeCubesForWave(int wave, float timeCubeMultiplier) =>
        (int)MathF.Floor(MathF.Pow(1.2999999523162842f, (wave - 40f) / 25f) * timeCubeMultiplier);

    public static int GetWeaponCubesForWave(int wave, float weaponCubeFindPercent, int weaponCubeStartWave)
    {
        float multiplier = 1f + weaponCubeFindPercent / 100f;
        int offset = 1000 - weaponCubeStartWave;
        return (int)MathF.Ceiling(
            MathF.Pow(1.2000000476837158f, (wave - 1000f + offset) / 25f) * multiplier);
    }

    public static double GetOverpoweredNormalized(double teamDps, double arenaHp)
    {
        if (teamDps < arenaHp) return 0.0;
        double v = (Math.Log10(teamDps) - Math.Log10(arenaHp) - OverpoweredMin) / OverpoweredRange;
        return Math.Min(1.0, Math.Max(0.0, v));
    }

    public static double GetRainbowBallGold(double arenaHp) => arenaHp / 1.5;
    public static double GetOfflineGoldPerSecond(double timelineGoldPerSecond) => timelineGoldPerSecond * 0.5;
    public static double GetTimeCubeDpsMultiplier(double totalTimeCubes) => 1.0 + totalTimeCubes * 0.1;
}
