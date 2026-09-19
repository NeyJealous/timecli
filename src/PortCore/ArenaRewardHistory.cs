using System.Collections.Generic;

namespace TimeClickers.PortCore;

/// <summary>
/// The original Arena prevents Time/Weapon Cube special blocks from being
/// generated repeatedly on the same wave within one timeline.
/// </summary>
public sealed class ArenaRewardHistory
{
    private readonly HashSet<int> _timeCubeWaves = new();
    private readonly HashSet<int> _weaponCubeWaves = new();

    public bool HasTimeCubeReward(int wave) => _timeCubeWaves.Contains(wave);
    public bool HasWeaponCubeReward(int wave) => _weaponCubeWaves.Contains(wave);

    public void MarkTimeCubeReward(int wave) => _timeCubeWaves.Add(wave);
    public void MarkWeaponCubeReward(int wave) => _weaponCubeWaves.Add(wave);

    public void TimeWarp()
    {
        _timeCubeWaves.Clear();
        _weaponCubeWaves.Clear();
    }
}
