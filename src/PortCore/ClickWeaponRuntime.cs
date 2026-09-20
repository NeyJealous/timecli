using System;

namespace TimeClickers.PortCore;

public sealed record ClickCannonFirePlan(
    int ProjectileCount,
    double DamagePerProjectile,
    double FireConeNormalized,
    double ChargeProgress,
    double MaximumCharge);

public sealed record ClickLauncherFirePlan(
    int RocketCount,
    double DamagePerRocket,
    double RocketSpeedMultiplier);

public sealed record ClickWeaponFirePlan(
    ClickCannonFirePlan? Cannon,
    ClickLauncherFirePlan? Launcher)
{
    public static readonly ClickWeaponFirePlan None =
        new(null, null);
}

/// <summary>
/// Unity-independent transient state for ClickCannon and ClickLauncher.
///
/// Reconstructed from Time Clickers 1.4.5:
/// - every manual click charges/fires the Cannon when unlocked;
/// - Cannon charge starts decaying one second after the last click at 10/s;
/// - Cannon fires floor(chargeProgress) projectiles per click;
/// - Launcher counts clicks and fires when its augment-defined threshold is met;
/// - Spread Shots changes Cannon max charge and Launcher rocket count.
/// Collision/trajectory remain Unity responsibilities; PortCore owns damage.
/// </summary>
public sealed class ClickWeaponRuntime
{
    public double CannonChargeProgress { get; private set; }
    public double CannonStartDechargingTime { get; private set; }
    public int LauncherClicksProgress { get; private set; }

    public double GetCannonMaximumCharge(GameState game)
    {
        if (!game.IsAbilityActive(AbilityType.SpreadShots))
            return 5.0;

        return Math.Max(
            1,
            game.WeaponAugmentEffects.ClickCannonMaxProjectiles);
    }

    public int GetLauncherClicksRequired(GameState game) =>
        Math.Max(
            1,
            game.WeaponAugmentEffects.ClickLauncherClicks);

    public void Update(
        GameState game,
        double nowSeconds,
        double deltaSeconds)
    {
        if (deltaSeconds < 0.0)
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

        // Exact ClickCannon.Update: decharge only after
        // Time.time > startDechargingTime, at deltaTime * 10.
        if (nowSeconds > CannonStartDechargingTime &&
            CannonChargeProgress > 0.0)
        {
            CannonChargeProgress -= deltaSeconds * 10.0;
            if (CannonChargeProgress < 0.0)
                CannonChargeProgress = 0.0;
        }

        // Original recomputes maximumCharge every frame. It does not clamp
        // existing charge when Spread Shots expires; the next Shoot call does.
        _ = GetCannonMaximumCharge(game);
    }

    public ClickWeaponFirePlan RegisterManualClick(
        GameState game,
        double nowSeconds)
    {
        var effects = game.WeaponAugmentEffects;

        ClickCannonFirePlan? cannon = null;
        ClickLauncherFirePlan? launcher = null;

        if (effects.ClickCannonUnlocked)
        {
            CannonChargeProgress += 1.0;

            double maximumCharge =
                GetCannonMaximumCharge(game);

            if (CannonChargeProgress > maximumCharge)
                CannonChargeProgress = maximumCharge;

            CannonStartDechargingTime =
                nowSeconds + 1.0;

            int projectileCount =
                (int)Math.Floor(CannonChargeProgress);

            double damage =
                game.GetClickDamage(isCritical: false) *
                effects.ClickCannonDamagePerShot *
                0.01;

            cannon = new ClickCannonFirePlan(
                ProjectileCount: projectileCount,
                DamagePerProjectile: damage,
                FireConeNormalized:
                    effects.ClickCannonFireCone / 360.0,
                ChargeProgress: CannonChargeProgress,
                MaximumCharge: maximumCharge);
        }

        if (effects.ClickLauncherUnlocked)
        {
            LauncherClicksProgress++;

            int clicksRequired =
                GetLauncherClicksRequired(game);

            if (LauncherClicksProgress >= clicksRequired)
            {
                int rocketCount = 1;

                if (game.IsAbilityActive(
                    AbilityType.SpreadShots))
                {
                    rocketCount = Math.Max(
                        1,
                        effects.ClickLauncherRockets);
                }

                launcher = new ClickLauncherFirePlan(
                    RocketCount: rocketCount,
                    DamagePerRocket:
                        game.GetClickDamage(
                            isCritical: false) * 10.0,
                    RocketSpeedMultiplier:
                        effects.ClickLauncherRocketSpeed *
                        0.01);

                LauncherClicksProgress = 0;
            }
        }

        return cannon is null && launcher is null
            ? ClickWeaponFirePlan.None
            : new ClickWeaponFirePlan(
                cannon,
                launcher);
    }

    public void TimeWarp()
    {
        CannonChargeProgress = 0.0;
        CannonStartDechargingTime = 0.0;
        LauncherClicksProgress = 0;
    }
}
