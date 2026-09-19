using System;

namespace TimeClickers.PortCore;

public static class SpawnRulesMath
{
    public static int GetTimeCubeBossReward(int wave)
    {
        for (int i = 0; i < ArenaMath.TimeCubeBossWaves.Length; i++)
            if (ArenaMath.TimeCubeBossWaves[i] == wave)
                return ArenaMath.TimeCubeBossCounts[i];

        return 0;
    }

    public static int ResolveTimeCubeReward(
        int wave,
        bool alreadyCollectedOnWave,
        ArtifactEffects artifacts,
        float random01)
    {
        if (alreadyCollectedOnWave || wave < 100)
            return 0;

        int bossReward = GetTimeCubeBossReward(wave);
        if (bossReward > 0)
            return bossReward;

        if (wave % 5 != 0)
            return 0;

        // Every 100th wave is guaranteed in 1.4.5; otherwise Unity Random.value
        // must be <= the artifact probability.
        if (wave % 100 != 0 && random01 > artifacts.TimeCubeChance)
            return 0;

        return ArenaMath.GetTimeCubesForWave(wave, (float)artifacts.TimeCubeMultiplier);
    }

    public static int GetWeaponCubeBossReward(int wave, int weaponCubeStartWave)
    {
        int shiftedWave = wave + (1000 - weaponCubeStartWave);

        for (int i = 0; i < ArenaMath.WeaponCubeBossWaves.Length; i++)
            if (ArenaMath.WeaponCubeBossWaves[i] == shiftedWave)
                return ArenaMath.WeaponCubeBossCounts[i];

        return 0;
    }

    public static int ResolveWeaponCubeReward(
        int wave,
        bool alreadyCollectedOnWave,
        WeaponAugmentEffects augments,
        float random01)
    {
        if (alreadyCollectedOnWave || wave % 5 != 0)
            return 0;

        if (wave < augments.WeaponCubeStartWave)
            return 0;

        int bossReward = GetWeaponCubeBossReward(wave, augments.WeaponCubeStartWave);
        if (bossReward > 0)
            return bossReward;

        if (random01 * 100f > augments.WeaponCubeChancePercent)
            return 0;

        return ArenaMath.GetWeaponCubesForWave(
            wave,
            (float)augments.WeaponCubeFindPercent,
            augments.WeaponCubeStartWave);
    }

    public static bool ShouldSpawnRainbowEnemy(
        int wave,
        int arenaEnemyKills,
        ArtifactEffects artifacts,
        float random01)
    {
        if (wave % 5 == 0 || arenaEnemyKills == 0)
            return false;

        return random01 < artifacts.RainbowEnemyChance;
    }
}
