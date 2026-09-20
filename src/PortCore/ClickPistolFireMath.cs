using System;
using System.Collections.Generic;

namespace TimeClickers.PortCore;

public sealed record ClickPistolFirePlan(
    bool IsCritical,
    double ClickDamage,
    IReadOnlyList<int> AdditionalYawAnglesDegrees,
    bool PunchThrough)
{
    public int ProjectileCount =>
        1 + AdditionalYawAnglesDegrees.Count;
}

/// <summary>
/// Unity-independent reconstruction of ClickerPistol.Shoot's gameplay plan.
/// Unity supplies the actual screen-space ray and performs physics queries,
/// while PortCore owns critical damage, Spread Shots ray count/angles and
/// Punchthrough state.
/// </summary>
public static class ClickPistolFireMath
{
    public static ClickPistolFirePlan Build(
        GameState game,
        bool isCritical)
    {
        int additionalCount = 0;

        if (game.IsAbilityActive(
            AbilityType.SpreadShots))
        {
            additionalCount =
                Math.Max(
                    1,
                    game.ArtifactEffects
                        .SpreadShotsProjectiles) -
                1;
        }

        int[] angles =
            new int[additionalCount];

        // Original loop:
        // angle = ((i / 2) + 1) * 3 * (i % 2 == 0 ? -1 : 1)
        // => -3,+3,-6,+6,-9,+9...
        for (int i = 0;
             i < additionalCount;
             i++)
        {
            angles[i] =
                ((i / 2) + 1) *
                3 *
                (i % 2 == 0
                    ? -1
                    : 1);
        }

        return new ClickPistolFirePlan(
            IsCritical: isCritical,
            ClickDamage:
                game.GetClickDamage(
                    isCritical),
            AdditionalYawAnglesDegrees:
                angles,
            PunchThrough:
                game.IsAbilityActive(
                    AbilityType.PunchThrough));
    }
}
