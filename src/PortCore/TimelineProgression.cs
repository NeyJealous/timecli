using System;

namespace TimeClickers.PortCore;

public enum ArenaClearResult
{
    ContinueSameWave = 0,
    AdvancedToNextWave = 1
}

public enum BossTickResult
{
    None = 0,
    FailedWaitingForRestart = 1,
    Restarted = 2
}

/// <summary>
/// Unity-independent Arena progression reconstructed from Time Clickers 1.4.5.
/// It preserves the original lower-wave farming rule, manual previous/next
/// navigation and boss timeout/restart behavior.
/// </summary>
public sealed class TimelineProgression
{
    private double? _bossRestartAt;

    public TimelineProgression(int startWave, int? maxWave = null)
    {
        if (startWave < 1)
            throw new ArgumentOutOfRangeException(nameof(startWave));

        Wave = startWave;
        MaxWave = Math.Max(startWave, maxWave ?? startWave);
    }

    public int Wave { get; private set; }
    public int MaxWave { get; private set; }
    public int EnemyKillsOnWave { get; private set; }

    public bool FightingBoss { get; private set; }
    public double BossStopTime { get; private set; }
    public float BossTimerStoppedAt { get; private set; }

    public bool IsBossWave => Wave % 5 == 0;
    public bool IsMajorBossWave => Wave % 10 == 0;
    public bool IsOnHighestWave => Wave >= MaxWave;
    public bool IsFarmingLowerWave => Wave < MaxWave;
    public bool CanGoPrevious => Wave > 1;
    public bool CanGoNext => Wave < MaxWave;
    public bool BossRestartPending => _bossRestartAt.HasValue;

    public int GetEnemiesRemaining(int enemiesNeededToAdvance) =>
        Math.Max(0, enemiesNeededToAdvance - EnemyKillsOnWave);

    public void StartArena(double nowSeconds, float bossTimeTotal)
    {
        _bossRestartAt = null;

        if (!IsBossWave)
        {
            FightingBoss = false;
            return;
        }

        BossStopTime = nowSeconds + bossTimeTotal;
        FightingBoss = true;
    }

    public float GetBossTimeRemaining(double nowSeconds)
    {
        if (!FightingBoss)
            return BossTimerStoppedAt;

        return (float)Math.Max(0.0, BossStopTime - nowSeconds);
    }

    /// <summary>
    /// Mirrors Arena.Update + FailBossArena. Failure does not move back a wave:
    /// the same boss is cleared, a 0.5 s finish delay runs, then the same arena
    /// starts again.
    /// </summary>
    public BossTickResult TickBoss(double nowSeconds, float bossTimeTotal)
    {
        if (FightingBoss && nowSeconds >= BossStopTime)
        {
            BossTimerStoppedAt = 0f;
            FightingBoss = false;
            _bossRestartAt = nowSeconds + 0.5;
            return BossTickResult.FailedWaitingForRestart;
        }

        if (!FightingBoss && _bossRestartAt.HasValue && nowSeconds >= _bossRestartAt.Value)
        {
            StartArena(nowSeconds, bossTimeTotal);
            return BossTickResult.Restarted;
        }

        return BossTickResult.None;
    }

    public ArenaClearResult CompleteEnemy(int enemiesNeededToAdvance) =>
        CompleteEnemy(enemiesNeededToAdvance, double.NaN);

    public ArenaClearResult CompleteEnemy(int enemiesNeededToAdvance, double nowSeconds)
    {
        if (enemiesNeededToAdvance < 1)
            throw new ArgumentOutOfRangeException(nameof(enemiesNeededToAdvance));

        // Exact Arena.OnEnemyDeath rule:
        // - on the highest unlocked wave, every cleared model counts;
        // - on a lower/farm wave, only the first model sets the counter to 1.
        if (IsOnHighestWave || EnemyKillsOnWave == 0)
            EnemyKillsOnWave++;

        if (IsBossWave)
        {
            if (!double.IsNaN(nowSeconds))
                BossTimerStoppedAt = GetBossTimeRemaining(nowSeconds);

            FightingBoss = false;
            _bossRestartAt = null;
            Advance();
            return ArenaClearResult.AdvancedToNextWave;
        }

        if (EnemyKillsOnWave >= enemiesNeededToAdvance)
        {
            Advance();
            return ArenaClearResult.AdvancedToNextWave;
        }

        return ArenaClearResult.ContinueSameWave;
    }

    public bool RequestPreviousArena()
    {
        if (!CanGoPrevious)
            return false;

        Wave = Math.Max(1, Wave - 1);
        ResetCurrentArenaState();
        return true;
    }

    public bool RequestNextArena()
    {
        if (!CanGoNext)
            return false;

        Wave++;
        ResetCurrentArenaState();
        return true;
    }

    /// <summary>
    /// UI convenience for reconstructing a wave selector. Original 1.4.5 uses
    /// repeated previous/next requests; this produces the same resulting state.
    /// </summary>
    public bool SelectUnlockedWave(int wave)
    {
        if (wave < 1 || wave > MaxWave)
            return false;

        if (wave == Wave)
            return true;

        Wave = wave;
        ResetCurrentArenaState();
        return true;
    }

    public void TimeWarpTo(int startWave)
    {
        if (startWave < 1)
            throw new ArgumentOutOfRangeException(nameof(startWave));

        Wave = startWave;
        MaxWave = startWave;
        ResetCurrentArenaState();
        BossTimerStoppedAt = 0f;
    }

    private void Advance()
    {
        Wave++;
        MaxWave = Math.Max(MaxWave, Wave);
        ResetCurrentArenaState();
    }

    private void ResetCurrentArenaState()
    {
        EnemyKillsOnWave = 0;
        FightingBoss = false;
        _bossRestartAt = null;
    }
}
