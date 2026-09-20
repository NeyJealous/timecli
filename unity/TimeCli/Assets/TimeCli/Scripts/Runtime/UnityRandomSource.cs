using TimeClickers.PortCore;
using UnityEngine;

namespace TimeCli.UnityRuntime
{
    public sealed class UnityRandomSource : IRandomSource
    {
        public int Range(int minInclusive, int maxExclusive)
        {
            return Random.Range(minInclusive, maxExclusive);
        }

        public static ArenaSpawnRolls NextSpawnRolls()
        {
            return new ArenaSpawnRolls(
                ModelRandomIndex: Random.Range(0, 1_000_000),
                RainbowEnemyRoll: Random.value,
                TimeCubeRoll: Random.value,
                WeaponCubeRoll: Random.value);
        }
    }
}
