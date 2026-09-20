using TimeClickers.PortCore;

static void AssertNear(
    double expected,
    double actual,
    string name,
    double tolerance = 1e-10)
{
    double scale = Math.Max(1.0, Math.Abs(expected));
    if (Math.Abs(expected - actual) > scale * tolerance)
        throw new Exception(
            $"{name}: expected {expected:R}, got {actual:R}");
}

static void AssertTrue(bool value, string name)
{
    if (!value)
        throw new Exception(name);
}

// Locked weapons must not emit plans.
{
    var game = new GameState();
    var plan = game.RegisterManualClickWeapons(0);

    AssertTrue(
        plan.Cannon is null && plan.Launcher is null,
        "Locked Cannon/Launcher must not fire");
}

// Click Cannon: charge, cap, damage, cone and decharge.
{
    var game = new GameState();
    game.WeaponAugments.SetLevel(
        WeaponAugmentType.ClickCannonUnlock,
        1);

    for (int click = 1; click <= 6; click++)
    {
        var plan =
            game.RegisterManualClickWeapons(click);

        AssertTrue(
            plan.Cannon is not null,
            $"Cannon click {click} should fire");

        int expectedProjectiles = Math.Min(click, 5);
        AssertNear(
            expectedProjectiles,
            plan.Cannon!.ProjectileCount,
            $"Cannon projectile count click {click}");

        AssertNear(
            0.25,
            plan.Cannon.DamagePerProjectile,
            "Cannon base 25% click damage");

        AssertNear(
            35.0 / 360.0,
            plan.Cannon.FireConeNormalized,
            "Cannon base fire cone");

        AssertNear(
            expectedProjectiles,
            game.ClickWeapons.CannonChargeProgress,
            "Cannon charge progress");
    }

    AssertNear(
        7.0,
        game.ClickWeapons.CannonStartDechargingTime,
        "Cannon starts decharging one second after last click");

    game.UpdateClickWeapons(
        nowSeconds: 7.1,
        deltaSeconds: 0.1);

    AssertNear(
        4.0,
        game.ClickWeapons.CannonChargeProgress,
        "Cannon decharges at 10 charge per second");
}

// Click Launcher: 10 base clicks, 10x non-critical click damage.
{
    var game = new GameState();
    game.WeaponAugments.SetLevel(
        WeaponAugmentType.ClickLauncherUnlock,
        1);

    for (int click = 1; click <= 9; click++)
    {
        var plan =
            game.RegisterManualClickWeapons(click);

        AssertTrue(
            plan.Launcher is null,
            $"Launcher must wait on click {click}");
        AssertNear(
            click,
            game.ClickWeapons.LauncherClicksProgress,
            $"Launcher click progress {click}");
    }

    var fired =
        game.RegisterManualClickWeapons(10);

    AssertTrue(
        fired.Launcher is not null,
        "Launcher must fire on the 10th base click");
    AssertNear(
        1,
        fired.Launcher!.RocketCount,
        "Launcher base rocket count");
    AssertNear(
        10.0,
        fired.Launcher.DamagePerRocket,
        "Launcher uses 10x non-critical click damage");
    AssertNear(
        1.0,
        fired.Launcher.RocketSpeedMultiplier,
        "Launcher base rocket speed multiplier");
    AssertNear(
        0,
        game.ClickWeapons.LauncherClicksProgress,
        "Launcher click progress resets after fire");
}

// Spread Shots switches Cannon maximum and Launcher rocket count.
{
    var game = new GameState();

    game.WeaponAugments.SetLevel(
        WeaponAugmentType.ClickCannonUnlock,
        1);
    game.WeaponAugments.SetLevel(
        WeaponAugmentType.ClickCannonMaxProjectiles,
        3); // base 5 + 3 = 8

    game.WeaponAugments.SetLevel(
        WeaponAugmentType.ClickLauncherUnlock,
        1);
    game.WeaponAugments.SetLevel(
        WeaponAugmentType.ClickLauncherRockets,
        2); // base 1 + 2 = 3
    game.WeaponAugments.SetLevel(
        WeaponAugmentType.ClickLauncherRocketSpeed,
        2); // 120%

    game.Gold.Add(1e9);
    AssertTrue(
        game.TryPurchaseNextAbility(),
        "Purchase Automatic Fire");
    AssertTrue(
        game.TryPurchaseNextAbility(),
        "Purchase Spread Shots");
    AssertTrue(
        game.ActivateAbility(
            AbilityType.SpreadShots,
            0),
        "Activate Spread Shots");

    ClickWeaponFirePlan last = ClickWeaponFirePlan.None;

    for (int click = 1; click <= 10; click++)
        last = game.RegisterManualClickWeapons(click * 0.05);

    AssertTrue(
        last.Cannon is not null,
        "Spread Cannon fire plan");
    AssertNear(
        8,
        last.Cannon!.MaximumCharge,
        "Spread Cannon maximum charge");
    AssertNear(
        8,
        last.Cannon.ProjectileCount,
        "Spread Cannon charge remains capped at 8");

    AssertTrue(
        last.Launcher is not null,
        "Spread Launcher fires on threshold");
    AssertNear(
        3,
        last.Launcher!.RocketCount,
        "Spread Launcher rocket count");
    AssertNear(
        1.2,
        last.Launcher.RocketSpeedMultiplier,
        "Rocket speed augment multiplier");

    // ClickerWeapon MonoBehaviours persist through Time Warp in the original.
    // Create non-zero launcher progress and verify transient component state
    // is not invented as a timeline-reset system.
    game.RegisterManualClickWeapons(0.6);
    AssertNear(
        1,
        game.ClickWeapons.LauncherClicksProgress,
        "Launcher progress before Time Warp");

    game.TimeWarp();

    AssertNear(
        8,
        game.ClickWeapons.CannonChargeProgress,
        "Time Warp preserves Cannon component charge");
    AssertNear(
        1,
        game.ClickWeapons.LauncherClicksProgress,
        "Time Warp preserves Launcher component click progress");
}

// Automatic Fire ability: each ClickerWeapon owns an independent rapid timer.
{
    var game = new GameState();

    game.WeaponAugments.SetLevel(
        WeaponAugmentType.ClickCannonUnlock,
        1);
    game.WeaponAugments.SetLevel(
        WeaponAugmentType.ClickLauncherUnlock,
        1);

    game.Gold.Add(1e9);
    AssertTrue(
        game.TryPurchaseNextAbility(),
        "Purchase Automatic Fire for timer test");
    AssertTrue(
        game.ActivateAbility(
            AbilityType.AutomaticFire,
            0),
        "Activate Automatic Fire");

    var atZero =
        game.UpdateClickWeapons(
            nowSeconds: 0,
            deltaSeconds: 0);

    AssertNear(
        1,
        atZero.PistolShots,
        "Automatic Fire Pistol immediate shot");
    AssertNear(
        1,
        atZero.CannonShots.Count,
        "Automatic Fire Cannon immediate shot");
    AssertNear(
        0,
        atZero.LauncherShots.Count,
        "Launcher threshold not reached on first automatic shot");
    AssertNear(
        1,
        game.ClickWeapons.LauncherClicksProgress,
        "Automatic Launcher Shoot increments click progress");

    var early =
        game.UpdateClickWeapons(
            nowSeconds: 0.05,
            deltaSeconds: 0.05);

    AssertNear(
        0,
        early.PistolShots,
        "Automatic Fire waits for rapid-fire delay");

    var atDelay =
        game.UpdateClickWeapons(
            nowSeconds: 0.1,
            deltaSeconds: 0.05);

    AssertNear(
        1,
        atDelay.PistolShots,
        "Automatic Fire fires at exact nextFireTime");

    // Unity stores these timers as float. Repeated 0.1f additions can
    // legitimately drift a few ULPs above a decimal frame boundary (for
    // example 0.70000005f vs 0.7f), so sample just after each subsequent
    // boundary rather than assuming ideal decimal arithmetic.
    ClickWeaponAutomaticFirePlan last =
        ClickWeaponAutomaticFirePlan.None;

    for (int i = 2; i <= 9; i++)
    {
        last = game.UpdateClickWeapons(
            nowSeconds: i * 0.101,
            deltaSeconds: 0.101);
    }

    AssertNear(
        1,
        last.LauncherShots.Count,
        "10th Automatic Fire Launcher Shoot emits rocket");
    AssertNear(
        0,
        game.ClickWeapons.LauncherClicksProgress,
        "Automatic Launcher threshold resets progress");
}

// Weapon-Augment auto fire uses strict nextFireTime < Time.time semantics.
{
    var game = new GameState();

    // First portable update mirrors ClickerWeapon.Start with level zero.
    game.UpdateClickWeapons(
        nowSeconds: 0,
        deltaSeconds: 0);

    game.WeaponAugments.SetLevel(
        WeaponAugmentType.ClickPistolAutoFire,
        1); // 2 shots/min => 30 s

    var changed =
        game.UpdateClickWeapons(
            nowSeconds: 1.0,
            deltaSeconds: 0.01);

    AssertNear(
        0,
        changed.PistolShots,
        "Augment change only resets timer to current time");

    var immediateNextFrame =
        game.UpdateClickWeapons(
            nowSeconds: 1.001,
            deltaSeconds: 0.001);

    AssertNear(
        1,
        immediateNextFrame.PistolShots,
        "Pistol augment auto-fire starts next frame");

    var exactBoundary =
        game.UpdateClickWeapons(
            nowSeconds: 31.001,
            deltaSeconds: 0.1);

    AssertNear(
        0,
        exactBoundary.PistolShots,
        "Augment auto-fire does not fire at exact boundary");

    var afterBoundary =
        game.UpdateClickWeapons(
            nowSeconds: 31.002,
            deltaSeconds: 0.001);

    AssertNear(
        1,
        afterBoundary.PistolShots,
        "Augment auto-fire fires once boundary is exceeded");
}

// ClickerPistol fire plan: Spread Shots angles and Punchthrough are
// gameplay-owned by PortCore; Unity only rotates/raycasts the resulting rays.
{
    var game = new GameState();

    var basePlan =
        ClickPistolFireMath.Build(
            game,
            isCritical: false);

    AssertNear(
        1,
        basePlan.ProjectileCount,
        "Base Click Pistol projectile count");
    AssertTrue(
        !basePlan.PunchThrough,
        "Punchthrough disabled by default");
    AssertNear(
        game.GetClickDamage(false),
        basePlan.ClickDamage,
        "Base Click Pistol damage plan");

    game.Gold.Add(1e30);

    for (int i = 0;
         i <= (int)AbilityType.PunchThrough;
         i++)
    {
        AssertTrue(
            game.TryPurchaseNextAbility(),
            $"Purchase ability {i} for Click Pistol plan");
    }

    AssertTrue(
        game.ActivateAbility(
            AbilityType.SpreadShots,
            0),
        "Activate Spread Shots for Click Pistol plan");

    AssertTrue(
        game.ActivateAbility(
            AbilityType.PunchThrough,
            0),
        "Activate Punchthrough for Click Pistol plan");

    var spreadPlan =
        ClickPistolFireMath.Build(
            game,
            isCritical: true);

    AssertNear(
        game.ArtifactEffects.SpreadShotsProjectiles,
        spreadPlan.ProjectileCount,
        "Spread Shots total Click Pistol rays");

    AssertNear(
        2,
        spreadPlan.AdditionalYawAnglesDegrees.Count,
        "Default additional Spread Shots rays");

    AssertNear(
        -3,
        spreadPlan.AdditionalYawAnglesDegrees[0],
        "First spread angle");

    AssertNear(
        3,
        spreadPlan.AdditionalYawAnglesDegrees[1],
        "Second spread angle");

    AssertTrue(
        spreadPlan.PunchThrough,
        "Punchthrough state included in Click Pistol plan");

    AssertNear(
        game.GetClickDamage(true),
        spreadPlan.ClickDamage,
        "Critical damage reused across all spread rays");
}

Console.WriteLine(
    "Click weapon scenario passed: manual, Automatic Fire and augment timers.");
