using System;
using System.Collections.Generic;

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

public sealed record ClickWeaponAutomaticFirePlan(
    int PistolShots,
    IReadOnlyList<ClickCannonFirePlan> CannonShots,
    IReadOnlyList<ClickLauncherFirePlan> LauncherShots)
{
    public static readonly ClickWeaponAutomaticFirePlan None =
        new(
            0,
            Array.Empty<ClickCannonFirePlan>(),
            Array.Empty<ClickLauncherFirePlan>());
}

/// <summary>
/// Unity-independent transient state for all three ClickerWeapon instances.
///
/// Reconstructed from Time Clickers 1.4.5:
/// - manual input calls Shoot on every active/unlocked click weapon;
/// - Automatic Fire ability uses the Artifact rapid-fire delay;
/// - each weapon has its own augment-driven shots/minute timer;
/// - Cannon charge/decharge and Launcher click threshold are persistent between
///   all manual/automatic Shoot calls.
/// </summary>
public sealed class ClickWeaponRuntime
{
    // The original fields are System.Single and are compared against
    // UnityEngine.Time.time (also Single). Keep those float semantics here:
    // using double would shift exact boundary frames such as 0.1 seconds.
    private float _pistolNextRapidFireTime;
    private float _cannonNextRapidFireTime;
    private float _launcherNextRapidFireTime;

    private float _pistolNextAugmentFireTime =
        float.PositiveInfinity;
    private float _cannonNextAugmentFireTime =
        float.PositiveInfinity;
    private float _launcherNextAugmentFireTime =
        float.PositiveInfinity;

    private float _cannonChargeProgress;
    private float _cannonStartDechargingTime;

    // -1 means the equivalent of ClickerWeapon.Start has not yet been
    // observed by this portable runtime.
    private long _pistolAutoFireLevel = -1;
    private long _cannonAutoFireLevel = -1;
    private long _launcherAutoFireLevel = -1;

    public double CannonChargeProgress => _cannonChargeProgress;
    public double CannonStartDechargingTime => _cannonStartDechargingTime;
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

    public ClickWeaponAutomaticFirePlan UpdateAutomaticFire(
        GameState game,
        double nowSeconds,
        double deltaSeconds)
    {
        if (deltaSeconds < 0.0)
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

        int pistolShots = 0;
        List<ClickCannonFirePlan>? cannonShots = null;
        List<ClickLauncherFirePlan>? launcherShots = null;

        // ClickerWeapon.Update order:
        // 1. Automatic Fire ability
        // 2. augment auto-fire
        // 3. subclass-specific Update work (Cannon decharge below).

        if (ShouldRapidFire(
            game,
            ref _pistolNextRapidFireTime,
            nowSeconds))
        {
            pistolShots++;
        }

        if (ShouldAugmentAutoFire(
            game,
            WeaponAugmentType.ClickPistolAutoFire,
            ref _pistolAutoFireLevel,
            ref _pistolNextAugmentFireTime,
            nowSeconds))
        {
            pistolShots++;
        }

        if (game.WeaponAugmentEffects.ClickCannonUnlocked)
        {
            if (ShouldRapidFire(
                game,
                ref _cannonNextRapidFireTime,
                nowSeconds))
            {
                (cannonShots ??= new())
                    .Add(ShootCannon(game, nowSeconds));
            }

            if (ShouldAugmentAutoFire(
                game,
                WeaponAugmentType.ClickCannonAutoFire,
                ref _cannonAutoFireLevel,
                ref _cannonNextAugmentFireTime,
                nowSeconds))
            {
                (cannonShots ??= new())
                    .Add(ShootCannon(game, nowSeconds));
            }
        }
        else
        {
            ObserveAugmentWithoutFiring(
                game,
                WeaponAugmentType.ClickCannonAutoFire,
                ref _cannonAutoFireLevel,
                ref _cannonNextAugmentFireTime,
                nowSeconds);
        }

        if (game.WeaponAugmentEffects.ClickLauncherUnlocked)
        {
            if (ShouldRapidFire(
                game,
                ref _launcherNextRapidFireTime,
                nowSeconds))
            {
                var fire = ShootLauncher(game);
                if (fire is not null)
                    (launcherShots ??= new()).Add(fire);
            }

            if (ShouldAugmentAutoFire(
                game,
                WeaponAugmentType.ClickLauncherAutoFire,
                ref _launcherAutoFireLevel,
                ref _launcherNextAugmentFireTime,
                nowSeconds))
            {
                var fire = ShootLauncher(game);
                if (fire is not null)
                    (launcherShots ??= new()).Add(fire);
            }
        }
        else
        {
            ObserveAugmentWithoutFiring(
                game,
                WeaponAugmentType.ClickLauncherAutoFire,
                ref _launcherAutoFireLevel,
                ref _launcherNextAugmentFireTime,
                nowSeconds);
        }

        // ClickCannon.Update runs this after ClickerWeapon.Update.
        float now = (float)nowSeconds;

        if (now > _cannonStartDechargingTime &&
            _cannonChargeProgress > 0f)
        {
            _cannonChargeProgress -=
                (float)deltaSeconds * 10f;

            if (_cannonChargeProgress < 0f)
                _cannonChargeProgress = 0f;
        }

        if (pistolShots == 0 &&
            cannonShots is null &&
            launcherShots is null)
        {
            return ClickWeaponAutomaticFirePlan.None;
        }

        IReadOnlyList<ClickCannonFirePlan> cannonResult =
            cannonShots is null
                ? Array.Empty<ClickCannonFirePlan>()
                : cannonShots;

        IReadOnlyList<ClickLauncherFirePlan> launcherResult =
            launcherShots is null
                ? Array.Empty<ClickLauncherFirePlan>()
                : launcherShots;

        return new ClickWeaponAutomaticFirePlan(
            pistolShots,
            cannonResult,
            launcherResult);
    }

    /// <summary>
    /// Compatibility wrapper used when a caller only wants state advancement.
    /// </summary>
    public void Update(
        GameState game,
        double nowSeconds,
        double deltaSeconds) =>
        _ = UpdateAutomaticFire(
            game,
            nowSeconds,
            deltaSeconds);

    public ClickWeaponFirePlan RegisterManualClick(
        GameState game,
        double nowSeconds)
    {
        ClickCannonFirePlan? cannon = null;
        ClickLauncherFirePlan? launcher = null;

        if (game.WeaponAugmentEffects.ClickCannonUnlocked)
            cannon = ShootCannon(game, nowSeconds);

        if (game.WeaponAugmentEffects.ClickLauncherUnlocked)
            launcher = ShootLauncher(game);

        return cannon is null && launcher is null
            ? ClickWeaponFirePlan.None
            : new ClickWeaponFirePlan(
                cannon,
                launcher);
    }

    public void TimeWarp()
    {
        _cannonChargeProgress = 0f;
        _cannonStartDechargingTime = 0f;
        LauncherClicksProgress = 0;

        _pistolNextRapidFireTime = 0f;
        _cannonNextRapidFireTime = 0f;
        _launcherNextRapidFireTime = 0f;

        // Weapon Augments persist through Time Warp, as do their ClickerWeapon
        // component instances. Do not erase observed augment levels/timers.
    }

    private ClickCannonFirePlan ShootCannon(
        GameState game,
        double nowSeconds)
    {
        var effects = game.WeaponAugmentEffects;

        _cannonChargeProgress += 1f;

        float maximumCharge =
            (float)GetCannonMaximumCharge(game);

        if (_cannonChargeProgress > maximumCharge)
            _cannonChargeProgress = maximumCharge;

        _cannonStartDechargingTime =
            (float)nowSeconds + 1f;

        return new ClickCannonFirePlan(
            ProjectileCount:
                (int)Math.Floor(_cannonChargeProgress),
            DamagePerProjectile:
                game.GetClickDamage(isCritical: false) *
                effects.ClickCannonDamagePerShot *
                0.01,
            FireConeNormalized:
                effects.ClickCannonFireCone / 360.0,
            ChargeProgress:
                _cannonChargeProgress,
            MaximumCharge:
                maximumCharge);
    }

    private ClickLauncherFirePlan? ShootLauncher(
        GameState game)
    {
        var effects = game.WeaponAugmentEffects;

        LauncherClicksProgress++;

        if (LauncherClicksProgress <
            GetLauncherClicksRequired(game))
        {
            return null;
        }

        int rocketCount = 1;

        if (game.IsAbilityActive(
            AbilityType.SpreadShots))
        {
            rocketCount = Math.Max(
                1,
                effects.ClickLauncherRockets);
        }

        LauncherClicksProgress = 0;

        return new ClickLauncherFirePlan(
            RocketCount: rocketCount,
            DamagePerRocket:
                game.GetClickDamage(
                    isCritical: false) * 10.0,
            RocketSpeedMultiplier:
                effects.ClickLauncherRocketSpeed *
                0.01);
    }

    private static bool ShouldRapidFire(
        GameState game,
        ref float nextFireTime,
        double nowSeconds)
    {
        if (!game.IsAbilityActive(
            AbilityType.AutomaticFire))
        {
            return false;
        }

        float now = (float)nowSeconds;

        // Original skips only while nextFireTime > Time.time.
        if (nextFireTime > now)
            return false;

        nextFireTime =
            now +
            (float)game.ArtifactEffects.RapidFireDelay;

        return true;
    }

    private static bool ShouldAugmentAutoFire(
        GameState game,
        WeaponAugmentType augmentType,
        ref long observedLevel,
        ref float nextFireTime,
        double nowSeconds)
    {
        float now = (float)nowSeconds;
        ulong level =
            game.WeaponAugments.GetLevel(augmentType);

        if (observedLevel < 0)
        {
            observedLevel = (long)level;
            nextFireTime =
                GetInitialAugmentFireTime(
                    game,
                    augmentType,
                    level,
                    now);

            return false;
        }

        if ((ulong)observedLevel != level)
        {
            // WeaponAugment.onWeaponAugmentChanged ->
            // ClickerWeapon.OnAutoFireWeaponAugmentChanged:
            // nextWeaponAugmentFireTime = Time.time.
            observedLevel = (long)level;
            nextFireTime = now;
            return false;
        }

        if (level == 0)
            return false;

        // Original uses next >= Time.time as the no-fire branch.
        if (nextFireTime >= now)
            return false;

        double shotsPerMinute =
            game.WeaponAugments.GetModValue(
                augmentType);

        if (shotsPerMinute <= 0.0)
            return false;

        nextFireTime =
            now +
            60f / (float)shotsPerMinute;

        return true;
    }

    private static void ObserveAugmentWithoutFiring(
        GameState game,
        WeaponAugmentType augmentType,
        ref long observedLevel,
        ref float nextFireTime,
        double nowSeconds)
    {
        float now = (float)nowSeconds;
        ulong level =
            game.WeaponAugments.GetLevel(augmentType);

        if (observedLevel < 0)
        {
            observedLevel = (long)level;
            nextFireTime =
                GetInitialAugmentFireTime(
                    game,
                    augmentType,
                    level,
                    nowSeconds);

            return;
        }

        if ((ulong)observedLevel != level)
        {
            observedLevel = (long)level;
            nextFireTime = now;
        }
    }

    private static float GetInitialAugmentFireTime(
        GameState game,
        WeaponAugmentType augmentType,
        ulong level,
        float nowSeconds)
    {
        if (level == 0)
            return float.PositiveInfinity;

        double shotsPerMinute =
            game.WeaponAugments.GetModValue(
                augmentType);

        return shotsPerMinute <= 0.0
            ? float.PositiveInfinity
            : nowSeconds +
              60f / (float)shotsPerMinute;
    }
}
