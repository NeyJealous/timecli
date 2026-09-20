using TimeClickers.PortCore;
using UnityEngine;

namespace TimeCli.UnityRuntime
{
    public sealed class UnityRandomSource : IRandomSource
    {
        public int Range(int minInclusive, int maxExclusive)
        {
            return UnityEngine.Random.Range(minInclusive, maxExclusive);
        }

        public static ArenaSpawnRolls NextSpawnRolls()
        {
            return new ArenaSpawnRolls(
                ModelRandomIndex: UnityEngine.Random.Range(0, 1_000_000),
                RainbowEnemyRoll: UnityEngine.Random.value,
                TimeCubeRoll: UnityEngine.Random.value,
                WeaponCubeRoll: UnityEngine.Random.value);
        }
    }
}
