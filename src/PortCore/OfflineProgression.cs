using System;

namespace TimeClickers.PortCore;

public sealed record OfflineProgressionResult(
    double Seconds,
    double OfflineDamage,
    double GoldEarned,
    long EstimatedBaseBlocksDestroyed);

public static class OfflineProgression
{
    public const double MaxOfflineSeconds = 172800.0;

    /// <summary>
    /// Reconstructs SaveLoad offline earnings. Offline time is capped at two
    /// days. Hero damage runs at 50%; gold normally comes from the stored
    /// timeline gold/sec rate, with the original damage/15 fallback when that
    /// rate is zero.
    /// </summary>
    public static OfflineProgressionResult Calculate(
        double secondsSinceSave,
        double teamDps,
        double timelineGoldPerSecond,
        double heroesGoldFindMultiplier,
        double artifactGoldFindMultiplier,
        double currentArenaBaseHp)
    {
        double seconds = Math.Min(
            MaxOfflineSeconds,
            Math.Max(0.0, secondsSinceSave));

        double offlineDamage = teamDps * seconds * 0.5;
        double gold = timelineGoldPerSecond * 0.5 * seconds;

        if (gold == 0.0)
        {
            gold = offlineDamage / 15.0;
            gold *= heroesGoldFindMultiplier;
            gold *= artifactGoldFindMultiplier;
        }

        long estimatedBlocks = 0;
        if (offlineDamage > 0.0 && currentArenaBaseHp > 0.0)
        {
            estimatedBlocks = (long)(offlineDamage / currentArenaBaseHp);
            estimatedBlocks = Math.Min(
                estimatedBlocks,
                (long)(seconds * 0.25));
        }

        return new OfflineProgressionResult(
            seconds,
            offlineDamage,
            gold,
            estimatedBlocks);
    }
}
