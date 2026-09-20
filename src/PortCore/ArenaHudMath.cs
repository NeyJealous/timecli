namespace TimeClickers.PortCore;

public sealed record ArenaHudSnapshot(
    int Wave,
    int MaxWave,
    bool IsBossWave,
    bool IsHighestWave,
    bool IsFarmingLowerWave,
    double RemainingHp,
    double TotalHp,
    double HpNormalized,
    string WaveHpText,
    float BossTimeRemaining,
    float BossTimeTotal,
    double BossTimeNormalized,
    string BossTimeText,
    int EnemiesRemaining,
    string ArenaRemainingText,
    bool ShowInfinity,
    double Gold,
    string GoldText,
    ulong TimeCubes,
    ulong PendingTimeCubes,
    ulong WeaponCubes);

/// <summary>
/// Presentation query matching the information consumed by the original
/// UIWaveHP, ArenaDisplay and WidgetGold components.
/// </summary>
public static class ArenaHudMath
{
    public static ArenaHudSnapshot Build(
        GameState game,
        EnemyModelState? activeEnemy,
        double nowSeconds)
    {
        var artifacts = game.ArtifactEffects;

        double remainingHp =
            activeEnemy?.RemainingHealth ?? 0.0;

        double totalHp =
            activeEnemy?.TotalHealth ?? 0.0;

        double hpNormalized =
            totalHp <= 0.0
                ? 0.0
                : remainingHp / totalHp;

        hpNormalized =
            System.Math.Clamp(
                hpNormalized,
                0.0,
                1.0);

        float bossTimeTotal =
            artifacts.BossTime;

        float bossTimeRemaining =
            game.Arena.IsBossWave
                ? game.Arena.GetBossTimeRemaining(
                    nowSeconds)
                : 0f;

        double bossNormalized =
            bossTimeTotal <= 0f
                ? 0.0
                : bossTimeRemaining /
                  bossTimeTotal;

        bossNormalized =
            System.Math.Clamp(
                bossNormalized,
                0.0,
                1.0);

        bool highest =
            game.Arena.IsOnHighestWave;

        int enemiesRemaining =
            game.Arena.GetEnemiesRemaining(
                artifacts.EnemiesToAdvance);

        return new ArenaHudSnapshot(
            Wave: game.Arena.Wave,
            MaxWave: game.Arena.MaxWave,
            IsBossWave: game.Arena.IsBossWave,
            IsHighestWave: highest,
            IsFarmingLowerWave:
                game.Arena.IsFarmingLowerWave,
            RemainingHp: remainingHp,
            TotalHp: totalHp,
            HpNormalized: hpNormalized,
            WaveHpText:
                DisplayFormatting.FormatValue(
                    remainingHp) +
                " HP",
            BossTimeRemaining: bossTimeRemaining,
            BossTimeTotal: bossTimeTotal,
            BossTimeNormalized: bossNormalized,
            BossTimeText:
                DisplayFormatting.FormatSeconds(
                    bossTimeRemaining),
            EnemiesRemaining: enemiesRemaining,
            ArenaRemainingText:
                highest
                    ? enemiesRemaining.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    : string.Empty,
            ShowInfinity: !highest,
            Gold: game.Gold.TotalGold,
            GoldText:
                DisplayFormatting.FormatValue(
                    game.Gold.TotalGold),
            TimeCubes: game.TimeCubes.Spendable,
            PendingTimeCubes:
                game.TimeCubes.PendingTimelineReward,
            WeaponCubes:
                game.WeaponCubes.Spendable);
    }
}
