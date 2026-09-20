using TimeClickers.PortCore;

static void AssertEqual(double expected, double actual, string name, double relativeTolerance = 1e-10)
{
    var scale = Math.Max(1.0, Math.Abs(expected));
    if (Math.Abs(expected - actual) > scale * relativeTolerance)
        throw new Exception($"{name}: expected {expected:R}, got {actual:R}");
}

static VoxelPoint[] Points(int count, float xOffset)
{
    var points = new VoxelPoint[count];
    for (int i = 0; i < count; i++)
        points[i] = new VoxelPoint(xOffset + i, 0, 0);
    return points;
}

static InMemoryVoxelModelCatalog BuildSyntheticCatalog()
{
    var descriptors = new List<VoxelModelDescriptor>();
    var layouts = new List<VoxelModelLayout>();

    // Cover every normal candidate size the original selector can request.
    foreach (int count in Enumerable.Range(11, 60))
    {
        descriptors.Add(new VoxelModelDescriptor($"small-{count}", 0, count));
        layouts.Add(new VoxelModelLayout(
            $"small-{count}",
            Red: Array.Empty<VoxelPoint>(),
            White: Points(1, 1000 + count * 10),
            Yellow: Points(1, 2000 + count * 10),
            Blue: Points(count - 2, 3000 + count * 10)));
    }

    foreach (int count in Enumerable.Range(100, 31))
    {
        descriptors.Add(new VoxelModelDescriptor($"large-{count}", 0, count));
        layouts.Add(new VoxelModelLayout(
            $"large-{count}",
            Red: Array.Empty<VoxelPoint>(),
            White: Points(1, 4000 + count * 10),
            Yellow: Points(1, 5000 + count * 10),
            Blue: Points(count - 2, 6000 + count * 10)));
    }

    return new InMemoryVoxelModelCatalog(descriptors, layouts);
}

var game = new GameState();
var engine = new HeadlessArenaEngine(game, BuildSyntheticCatalog());
var rolls = new ArenaSpawnRolls(
    ModelRandomIndex: 0,
    RainbowEnemyRoll: 1f,
    TimeCubeRoll: 1f,
    WeaponCubeRoll: 1f);

long spawnedModels = 0;
long destroyedBlocks = 0;

while (game.Arena.Wave <= 100)
{
    int wave = game.Arena.Wave;
    int modelsRequired = wave % 5 == 0
        ? 1
        : game.ArtifactEffects.EnemiesToAdvance;

    for (int modelIndex = 0; modelIndex < modelsRequired; modelIndex++)
    {
        var spawned = engine.SpawnCurrentEnemy(rolls, nowSeconds: spawnedModels);
        spawnedModels++;

        if (wave == 1 && modelIndex == 0 && !spawned.IsVeryFirstEnemy)
            throw new Exception("The first model of the game must use the single-block path");

        if (wave == 100 && spawned.TimeCubeReward != 10)
            throw new Exception($"Wave 100 expected 10 Time Cubes, got {spawned.TimeCubeReward}");

        ArenaBlockAttackResult? final = null;

        for (int blockIndex = 0; blockIndex < spawned.Model.BlockCount; blockIndex++)
        {
            final = engine.DamageBlock(
                blockIndex,
                double.MaxValue,
                nowSeconds: spawnedModels + blockIndex / 1000.0);
            destroyedBlocks++;
        }

        if (final is null || !final.ModelCleared)
            throw new Exception($"Wave {wave}: spawned model was not fully cleared");

        if (modelIndex < modelsRequired - 1)
        {
            if (final.ArenaResult != ArenaClearResult.ContinueSameWave)
                throw new Exception($"Wave {wave}: advanced before enough regular enemies");
        }
        else
        {
            if (final.ArenaResult != ArenaClearResult.AdvancedToNextWave)
                throw new Exception($"Wave {wave}: failed to advance after required clear count");
        }
    }

    if (game.Arena.Wave != wave + 1)
        throw new Exception($"Wave progression mismatch after {wave}");
}

AssertEqual(101, game.Arena.Wave, "Reached wave 101");
AssertEqual(0, game.TimeCubes.PendingTimelineReward, "Time Cube reward waits for pickup collection");
AssertEqual(10, game.CubePickups.Count, "Wave 100 spawned ten Time Cube pickup objects");

game.UpdateCubePickups(spawnedModels + 6.0);
AssertEqual(10, game.TimeCubes.PendingTimelineReward, "Pending Time Cubes after auto-collection");

ulong earned = game.TimeWarp();
AssertEqual(10, earned, "Time Warp earned Time Cubes");
AssertEqual(10, game.TimeCubes.Spendable, "Spendable Time Cubes after Time Warp");
AssertEqual(10, game.TimeCubes.LifetimeEarned, "Lifetime Time Cubes after Time Warp");
AssertEqual(2.0, game.TimeCubes.DpsMultiplier, "Time Cube DPS multiplier after first warp");
AssertEqual(1, game.Arena.Wave, "Arena reset after Time Warp");

var artifactPurchase = game.BuyArtifact(ArtifactType.GoldFind);
if (!artifactPurchase.Succeeded)
    throw new Exception($"GoldFind purchase failed: {artifactPurchase.Reason}");

AssertEqual(1, artifactPurchase.NewLevel, "GoldFind level after purchase");
AssertEqual(9, game.TimeCubes.Spendable, "Time Cubes after first Artifact");
AssertEqual(1.03, game.ArtifactEffects.GoldFindMultiplier, "GoldFind effect after first level");

Console.WriteLine(
    $"Timeline scenario passed: {spawnedModels} enemy models, " +
    $"{destroyedBlocks} blocks, wave 100 reward -> Time Warp -> Artifact.");
