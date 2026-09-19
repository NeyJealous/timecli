using System;
using System.Collections.Generic;

namespace TimeClickers.PortCore;

/// <summary>
/// Canonical hero combat/progression data reconstructed from Time Clickers 1.4.5.
/// UI text and original assets are deliberately not required by this catalog.
/// </summary>
public static partial class CanonicalHeroes
{
    private static UpgradeSpec U(
        int id,
        int levelRequired,
        params (UpgradeMod Mod, double Value)[] mods)
    {
        var values = new Dictionary<UpgradeMod, double>();
        foreach (var (mod, value) in mods)
            values[mod] = value;

        return new UpgradeSpec(id, levelRequired, values);
    }

    public static HeroBaseSpec PulsePistol { get; } = CreatePulsePistol();
    public static HeroBaseSpec FlakCannon { get; } = CreateFlakCannon();
    public static HeroBaseSpec SpreadRifle { get; } = CreateSpreadRifle();
    public static HeroBaseSpec RocketLauncher { get; } = CreateRocketLauncher();
    public static HeroBaseSpec ParticleBall { get; } = CreateParticleBall();

    public static IReadOnlyList<HeroBaseSpec> All { get; } = new[]
    {
        PulsePistol,
        FlakCannon,
        SpreadRifle,
        RocketLauncher,
        ParticleBall
    };

    public static HeroBaseSpec Get(int id)
    {
        if (id < 0 || id >= All.Count)
            throw new ArgumentOutOfRangeException(nameof(id));
        return All[id];
    }
}
