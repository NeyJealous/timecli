using TimeClickers.PortCore;

static void AssertNear(double expected, double actual, string name, double tolerance = 1e-6)
{
    double scale = Math.Max(1.0, Math.Abs(expected));
    if (Math.Abs(expected - actual) > scale * tolerance)
        throw new Exception($"{name}: expected {expected:R}, got {actual:R}");
}

static void AssertTrue(bool condition, string name)
{
    if (!condition) throw new Exception(name);
}

static HeroRuntime LevelOne(HeroBaseSpec spec)
{
    var hero = new HeroRuntime(spec);
    var result = hero.Purchase(double.PositiveInfinity, HeroBuyMode.OneLevel);
    if (!result.Purchased || hero.Level != 1)
        throw new Exception($"Failed to level {spec.Name} to 1");
    return hero;
}

static EnemyModelState Model(params EnemyBlockState[] blocks) => new(blocks);
static ArtifactEffects DefaultArtifacts() => new ArtifactLoadout().BuildEffects();
static HeroDpsMultipliers Neutral() => new();

// Flak Cannon: random subset, fixed target mask, no targetCount balancing.
{
    var hero = LevelOne(CanonicalHeroes.FlakCannon);
    var blocks = Enumerable.Range(0, 6)
        .Select(i => new EnemyBlockState(1000, EnemyType.Red, position: new VoxelPoint(i, 0, 0)))
        .ToArray();
    var model = Model(blocks);
    var state = new HeroAutoFireState();

    var first = HeroAutoFirePlanner.Plan(
        hero, state, model, DefaultArtifacts(), Neutral(),
        nowSeconds: 0,
        new SequenceRandomSource(new[] { 0, 4 }));

    AssertTrue(first.Fired, "Flak first volley should fire");
    AssertNear(0.4, first.FirePeriod, "Flak fire period");
    AssertNear(8.8, first.BaseShotDamage, "Flak total shot damage");
    AssertTrue(first.Applications.Count == 4, "Flak base projectile count must be 4");

    var expectedTargets = new[] { blocks[1], blocks[2], blocks[3], blocks[4] };
    for (int i = 0; i < expectedTargets.Length; i++)
    {
        AssertTrue(ReferenceEquals(first.Applications[i].Target, expectedTargets[i]),
            $"Flak target {i} mismatch");
        AssertTrue(expectedTargets[i].IsTargetedBy(WeaponType.FlakCannon),
            $"Flak target {i} mask missing");
        AssertTrue(expectedTargets[i].TargetCount == 0,
            "Flak must not participate in targetCount balancing");
        AssertNear(2.2, first.Applications[i].Damage, $"Flak projectile damage {i}");
    }

    var early = HeroAutoFirePlanner.Plan(
        hero, state, model, DefaultArtifacts(), Neutral(),
        nowSeconds: 0.1,
        new SequenceRandomSource(Array.Empty<int>()));
    AssertTrue(!early.Fired, "Flak should respect nextFireTime");
    AssertTrue(blocks[1].IsTargetedBy(WeaponType.FlakCannon),
        "Flak mask remains until next actual volley");

    var second = HeroAutoFirePlanner.Plan(
        hero, state, model, DefaultArtifacts(), Neutral(),
        nowSeconds: state.NextFireTime,
        new SequenceRandomSource(new[] { 0, 0 }));
    AssertTrue(second.Fired, "Flak second volley should fire");
    AssertTrue(!blocks[1].IsTargetedBy(WeaponType.FlakCannon),
        "Flak previous target mask must be removed before retargeting");
}

// Flak Never Misses: duplicate random targets fill missing projectiles.
{
    var hero = LevelOne(CanonicalHeroes.FlakCannon);
    var blocks = new[]
    {
        new EnemyBlockState(1000, EnemyType.Red),
        new EnemyBlockState(1000, EnemyType.Red)
    };
    var loadout = new ArtifactLoadout();
    loadout.SetLevel(ArtifactType.NeverMissFlak, 1);

    var plan = HeroAutoFirePlanner.Plan(
        hero,
        new HeroAutoFireState(),
        Model(blocks),
        loadout.BuildEffects(),
        Neutral(),
        0,
        new SequenceRandomSource(new[] { 0, 1 }));

    AssertTrue(plan.Applications.Count == 4,
        "Flak Never Misses must fill to projectile count");
    AssertTrue(
        ReferenceEquals(plan.Applications[0].Target, blocks[0]) &&
        ReferenceEquals(plan.Applications[1].Target, blocks[1]) &&
        ReferenceEquals(plan.Applications[2].Target, blocks[0]) &&
        ReferenceEquals(plan.Applications[3].Target, blocks[1]),
        "Flak Never Misses duplicate selection mismatch");
}

// Spread Rifle: persistent balanced unique targets.
{
    var hero = LevelOne(CanonicalHeroes.SpreadRifle);
    var blocks = Enumerable.Range(0, 4)
        .Select(_ => new EnemyBlockState(1000, EnemyType.Red))
        .ToArray();

    blocks[0].IncrementTargetCount();
    blocks[0].IncrementTargetCount();
    blocks[2].IncrementTargetCount();

    var state = new HeroAutoFireState();
    var model = Model(blocks);

    var first = HeroAutoFirePlanner.Plan(
        hero, state, model, DefaultArtifacts(), Neutral(), 0,
        new SequenceRandomSource(Array.Empty<int>()));

    AssertTrue(first.Applications.Count == 3, "Spread base projectile count must be 3");
    AssertTrue(ReferenceEquals(first.Applications[0].Target, blocks[1]), "Spread target 0");
    AssertTrue(ReferenceEquals(first.Applications[1].Target, blocks[3]), "Spread target 1");
    AssertTrue(ReferenceEquals(first.Applications[2].Target, blocks[2]), "Spread target 2");
    AssertTrue(blocks[1].TargetCount == 1, "Spread increments targetCount");
    AssertTrue(blocks[3].TargetCount == 1, "Spread increments targetCount");
    AssertTrue(blocks[2].TargetCount == 2, "Spread increments existing targetCount");
    AssertNear(37.0 / 3.0, first.Applications[0].Damage, "Spread projectile damage");

    int b1Count = blocks[1].TargetCount;
    int b2Count = blocks[2].TargetCount;
    int b3Count = blocks[3].TargetCount;

    var second = HeroAutoFirePlanner.Plan(
        hero, state, model, DefaultArtifacts(), Neutral(), state.NextFireTime,
        new SequenceRandomSource(Array.Empty<int>()));

    AssertTrue(second.Applications.Count == 3, "Spread persistent target count");
    AssertTrue(blocks[1].TargetCount == b1Count &&
               blocks[2].TargetCount == b2Count &&
               blocks[3].TargetCount == b3Count,
        "Spread persistent targets must not increment targetCount every volley");
}

// Spread Never Misses: duplicates fill projectile deficit without extra targetCount.
{
    var hero = LevelOne(CanonicalHeroes.SpreadRifle);
    var only = new EnemyBlockState(1000, EnemyType.Red);
    var loadout = new ArtifactLoadout();
    loadout.SetLevel(ArtifactType.NeverMissSpread, 1);

    var plan = HeroAutoFirePlanner.Plan(
        hero,
        new HeroAutoFireState(),
        Model(only),
        loadout.BuildEffects(),
        Neutral(),
        0,
        new SequenceRandomSource(new[] { 0, 0 }));

    AssertTrue(plan.Applications.Count == 3, "Spread Never Misses projectile fill");
    AssertTrue(plan.Applications.All(x => ReferenceEquals(x.Target, only)),
        "Spread Never Misses should duplicate the only target");
    AssertTrue(only.TargetCount == 1,
        "Spread Never Misses duplicate shots must not add targetCount");
}

// Rocket Launcher: least-targeted primary plus squared-distance splash list.
{
    var hero = LevelOne(CanonicalHeroes.RocketLauncher);
    var b0 = new EnemyBlockState(1000, EnemyType.Red, position: new VoxelPoint(10, 0, 0));
    var b1 = new EnemyBlockState(1000, EnemyType.Red, position: new VoxelPoint(0, 0, 0));
    var b2 = new EnemyBlockState(1000, EnemyType.Red, position: new VoxelPoint(1, 0, 0));
    var b3 = new EnemyBlockState(1000, EnemyType.Red, position: new VoxelPoint(2, 0, 0));
    b0.IncrementTargetCount();
    b0.IncrementTargetCount();

    var model = Model(b0, b1, b2, b3);
    var state = new HeroAutoFireState();

    var first = HeroAutoFirePlanner.Plan(
        hero, state, model, DefaultArtifacts(), Neutral(), 0,
        new SequenceRandomSource(Array.Empty<int>()));

    AssertTrue(first.Applications.Count == 2, "Rocket main + one splash target");
    AssertTrue(ReferenceEquals(first.Applications[0].Target, b1), "Rocket primary target");
    AssertTrue(ReferenceEquals(first.Applications[1].Target, b2), "Rocket splash target");
    AssertTrue(first.Applications[1].IsSplash, "Rocket splash flag");
    AssertNear(245, first.Applications[0].Damage, "Rocket main damage");
    AssertNear(61.25, first.Applications[1].Damage, "Rocket base 25% splash");
    AssertTrue(b1.TargetCount == 1, "Rocket increments primary targetCount");
    AssertTrue(b1.IsTargetedBy(WeaponType.RocketLauncher), "Rocket target mask");
    AssertTrue(!b2.IsTargetedBy(WeaponType.RocketLauncher), "Rocket splash is not targeted");

    b1.ApplyDamage(5000, DefaultArtifacts(), 1, false);

    var acquireBeforeFire = HeroAutoFirePlanner.Plan(
        hero, state, model, DefaultArtifacts(), Neutral(), 0.5,
        new SequenceRandomSource(Array.Empty<int>()));

    AssertTrue(!acquireBeforeFire.Fired, "Rocket can reacquire before nextFireTime");
    AssertTrue(ReferenceEquals(state.CurrentSingleTarget, b2),
        "Rocket target should change before fire timer elapses");
}

// Particle Ball: every fifth shot receives Collider multiplier and counter resets.
{
    var hero = LevelOne(CanonicalHeroes.ParticleBall);
    var target = new EnemyBlockState(1e12, EnemyType.Red);
    var model = Model(target);
    var state = new HeroAutoFireState();
    var artifacts = DefaultArtifacts();

    double normalDamage = 0;
    double fifthDamage = 0;
    double now = 0;

    for (int shot = 1; shot <= 5; shot++)
    {
        var volley = HeroAutoFirePlanner.Plan(
            hero, state, model, artifacts, Neutral(), now,
            new SequenceRandomSource(Array.Empty<int>()));

        AssertTrue(volley.Fired, $"Particle shot {shot} should fire");
        AssertTrue(volley.Applications.Count == 1, "Particle single-target volley");

        if (shot == 1)
            normalDamage = volley.Applications[0].Damage;
        if (shot == 5)
            fifthDamage = volley.Applications[0].Damage;

        now = state.NextFireTime;
    }

    AssertNear(normalDamage * 5.0, fifthDamage, "Particle fifth-shot Collider multiplier");
    AssertTrue(state.NumTimesFired == 0, "Particle fifth shot resets fire counter");
    AssertTrue(target.TargetCount == 1, "Particle persistent targetCount increments once");
}

// Upgrade recalculation: projectile, splash and collider reset at rank boundaries.
{
    var flak = CanonicalHeroes.FlakCannon;
    var flakSchedule = new InfiniteUpgradeSchedule(flak.BaseUpgrades);
    AssertTrue(HeroCombatMath.GetProjectileCount(flak.BaseProjectileCount, flakSchedule, 0) == 4,
        "Flak base projectile count");
    AssertTrue(HeroCombatMath.GetProjectileCount(flak.BaseProjectileCount, flakSchedule, 3) == 8,
        "Flak projectile upgrade");
    AssertTrue(HeroCombatMath.GetProjectileCount(flak.BaseProjectileCount, flakSchedule, 6) == 4,
        "Flak promotion resets projectile count");

    var rocket = CanonicalHeroes.RocketLauncher;
    var rocketSchedule = new InfiniteUpgradeSchedule(rocket.BaseUpgrades);
    AssertNear(0.25, HeroCombatMath.GetSplash(rocket.BaseSplash, rocketSchedule, 0),
        "Rocket base splash");
    AssertNear(0.5, HeroCombatMath.GetSplash(rocket.BaseSplash, rocketSchedule, 2),
        "Rocket splash upgrade");
    AssertNear(0.25, HeroCombatMath.GetSplash(rocket.BaseSplash, rocketSchedule, 6),
        "Rocket promotion resets splash");

    var particle = CanonicalHeroes.ParticleBall;
    var particleSchedule = new InfiniteUpgradeSchedule(particle.BaseUpgrades);
    AssertNear(5, HeroCombatMath.GetColliderMod(particle.BaseCollider, particleSchedule, 0),
        "Particle base collider");
    AssertNear(10, HeroCombatMath.GetColliderMod(particle.BaseCollider, particleSchedule, 3),
        "Particle collider upgrade");
    AssertNear(5, HeroCombatMath.GetColliderMod(particle.BaseCollider, particleSchedule, 6),
        "Particle promotion resets collider");
}

// Headless Arena integration: actual Pulse Pistol auto-fire destroys model and advances.
{
    var game = new GameState();
    AssertTrue(game.TryPurchaseHero(0, HeroBuyMode.OneLevel), "Buy Pulse level 1");

    var catalog = new InMemoryVoxelModelCatalog(
        new[] { new VoxelModelDescriptor("one", 0, 11) },
        new[]
        {
            new VoxelModelLayout(
                "one",
                Red: Enumerable.Range(0, 11).Select(i => new VoxelPoint(i, 0, 0)).ToArray(),
                White: Array.Empty<VoxelPoint>(),
                Yellow: Array.Empty<VoxelPoint>(),
                Blue: Array.Empty<VoxelPoint>())
        });

    var arena = new HeadlessArenaEngine(game, catalog);
    var spawned = arena.SpawnCurrentEnemy(
        new ArenaSpawnRolls(0, 1f, 1f, 1f));

    AssertTrue(spawned.IsVeryFirstEnemy && spawned.Model.BlockCount == 1,
        "First-game enemy should be one block");

    var firstVolley = arena.FireHero(
        0,
        nowSeconds: 0,
        new SequenceRandomSource(Array.Empty<int>()));

    AssertTrue(firstVolley.Plan.Fired, "Pulse auto-fire should fire");
    AssertTrue(firstVolley.ModelCleared, "Pulse first shot should clear wave-1 block");
    AssertTrue(firstVolley.ArenaResult == ArenaClearResult.ContinueSameWave,
        "First of ten regular enemies should remain on wave 1");
}

Console.WriteLine("Hero auto-fire scenario passed.");
