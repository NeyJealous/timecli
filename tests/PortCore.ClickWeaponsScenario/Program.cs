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

    game.TimeWarp();

    AssertNear(
        0,
        game.ClickWeapons.CannonChargeProgress,
        "Time Warp resets Cannon charge");
    AssertNear(
        0,
        game.ClickWeapons.LauncherClicksProgress,
        "Time Warp resets Launcher click progress");
}

Console.WriteLine(
    "Click weapon scenario passed: Cannon/Launcher state and fire plans.");
