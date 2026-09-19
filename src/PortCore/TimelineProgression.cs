using System;

namespace TimeClickers.PortCore;

public enum ArenaClearResult
{
    ContinueSameWave = 0,
    AdvancedToNextWave = 1
}

/// <summary>
/// Headless wave-progression state. This models the progression decisions made
/// after one complete voxel enemy/model has been destroyed; rendering and
/// individual block deaths remain Unity-facing concerns.
/// </summary>
public sealed class TimelineProgression
{
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

    public bool IsBossWave => Wave % 5 == 0;
    public bool IsOnHighestWave => Wave >= MaxWave;

    public ArenaClearResult CompleteEnemy(int enemiesNeededToAdvance)
    {
        if (enemiesNeededToAdvance < 1)
            throw new ArgumentOutOfRangeException(nameof(enemiesNeededToAdvance));

        // Mirrors Arena.OnEnemyDeath after all blocks belonging to the current
        // spawned model have been destroyed.
        if (IsOnHighestWave || EnemyKillsOnWave == 0)
            EnemyKillsOnWave++;

        if (IsBossWave || EnemyKillsOnWave >= enemiesNeededToAdvance)
        {
            Advance();
            return ArenaClearResult.AdvancedToNextWave;
        }

        return ArenaClearResult.ContinueSameWave;
    }

    public void TimeWarpTo(int startWave)
    {
        if (startWave < 1)
            throw new ArgumentOutOfRangeException(nameof(startWave));

        Wave = startWave;
        MaxWave = startWave;
        EnemyKillsOnWave = 0;
    }

    private void Advance()
    {
        Wave++;
        MaxWave = Math.Max(MaxWave, Wave);
        EnemyKillsOnWave = 0;
    }
}
