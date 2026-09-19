using System.Collections.Generic;

namespace TimeClickers.PortCore;

public sealed record SkillSpec(
    int Id,
    string Name,
    double BaseDamage,
    int BaseCost,
    IReadOnlyList<UpgradeSpec> Upgrades);

public static class CanonicalSkills
{
    public static SkillSpec ClickPistol { get; } = new(
        0,
        "Click Pistol",
        1.0,
        5,
        new UpgradeSpec[]
        {
            U(1, 10, 2.0),
            U(2, 25, 2.0),
            U(3, 50, 2.0),
            U(4, 75, 2.0),
            U(5, 100, 3.0),
            U(6, 125, 3.0)
        });

    private static UpgradeSpec U(int id, int level, double multiplier) =>
        new(
            id,
            level,
            new Dictionary<UpgradeMod, double>
            {
                [UpgradeMod.ClickDamage] = multiplier
            });
}
