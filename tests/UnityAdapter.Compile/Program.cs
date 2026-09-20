using TimeCli.UnityRuntime;
using TimeClickers.PortCore;

if (PortCoreApi.Version != "1.2.0")
    throw new Exception("Unexpected PortCore API version.");

var bytes = BuildTinyCatalog();
var catalog = BinaryVoxelCatalog.Load(bytes);

if (catalog.Models.Count != 1)
    throw new Exception("Binary voxel loader model count mismatch.");

var layout = catalog.GetLayout("fixture");
if (layout.EnemyCount != 4)
    throw new Exception("Binary voxel loader block count mismatch.");

var debugCatalog = DebugVoxelCatalogProxy.Create();
if (debugCatalog.Models.Count == 0)
    throw new Exception("Debug catalog should not be empty.");

// Recovered ClickerWeapon vertical AnimationCurve values are evaluated
// through the same two-key Hermite segment used by Unity 5.4.
AssertClose(
    OriginalClickWeaponAimMath.EvaluatePistolVertical(0.85f),
    0.039455987f,
    0.000001f,
    "Pistol vertical curve");

AssertClose(
    OriginalClickWeaponAimMath.EvaluateHeavyVertical(0.90f),
    0.106816078f,
    0.000001f,
    "Cannon/Launcher vertical curve");

AssertClose(
    OriginalClickWeaponAimMath.EvaluatePistolVertical(1f),
    OriginalClickWeaponPresentation.PistolVerticalSecondValue,
    0.000001f,
    "Pistol vertical endpoint");

AssertClose(
    OriginalClickWeaponAimMath.EvaluateHeavyVertical(1f),
    OriginalClickWeaponPresentation.HeavyVerticalSecondValue,
    0.000001f,
    "Heavy vertical endpoint");

// Hero damage in 1.4.5 is instantaneous. Presentation is weapon-colored
// BoxEnemy Outline/FlashOutline rather than travelling Hero projectiles.
AssertColor(
    OriginalHeroPresentation.GetWeaponColor(WeaponType.Pistol),
    0f,
    0.4066853523f,
    1f,
    1f,
    "Pulse Pistol color");

AssertColor(
    OriginalHeroPresentation.GetWeaponColor(WeaponType.FlakCannon),
    1f,
    0.7019498944f,
    0f,
    1f,
    "Flak Cannon color");

AssertColor(
    OriginalHeroPresentation.GetWeaponColor(WeaponType.SpreadRifle),
    1f,
    0f,
    0f,
    1f,
    "Spread Rifle color");

AssertColor(
    OriginalHeroPresentation.GetWeaponColor(WeaponType.RocketLauncher),
    0f,
    1f,
    0.1559889317f,
    1f,
    "Rocket Launcher color");

AssertColor(
    OriginalHeroPresentation.GetWeaponColor(WeaponType.ParticleBall),
    0.5626740456f,
    0f,
    1f,
    1f,
    "Particle Ball color");

if (OriginalHeroPresentation.GetImpactStyle(
        WeaponType.FlakCannon,
        false) != HeroImpactStyle.Flash)
{
    throw new Exception("Flak Cannon must use FlashOutline.");
}

if (OriginalHeroPresentation.GetImpactStyle(
        WeaponType.RocketLauncher,
        false) != HeroImpactStyle.Outline ||
    OriginalHeroPresentation.GetImpactStyle(
        WeaponType.RocketLauncher,
        true) != HeroImpactStyle.Flash)
{
    throw new Exception(
        "Rocket primary must Outline and splash must FlashOutline.");
}

var flakMidFade =
    OriginalHeroPresentation.EvaluateFlashColor(
        OriginalHeroPresentation.FlakCannonColor,
        OriginalHeroPresentation.DefaultOutlineColor,
        OriginalHeroPresentation.FlashDuration * 0.5f);

AssertColor(
    flakMidFade,
    0.5f,
    0.3509749472f,
    0f,
    1f,
    "Flak midpoint flash fade");

// Recovered Rocket tail cadence: no emission before 25 ms, then one
// emission at 25 ms and three emissions across a 75 ms frame.
float rocketTailAccumulator = 0f;
if (RocketTailCadence.Advance(ref rocketTailAccumulator, 0.024f) != 0)
    throw new Exception("Rocket tail emitted before recovered 25 ms delay.");

if (RocketTailCadence.Advance(ref rocketTailAccumulator, 0.001f) != 1)
    throw new Exception("Rocket tail did not emit at recovered 25 ms delay.");

if (Math.Abs(rocketTailAccumulator) > 0.0001f)
    throw new Exception("Rocket tail cadence accumulator should reset at 25 ms.");

if (RocketTailCadence.Advance(ref rocketTailAccumulator, 0.075f) != 3)
    throw new Exception("Rocket tail cadence must preserve multiple emissions on long frames.");

Console.WriteLine("Unity adapter compile/preflight passed.");

static void AssertColor(
    UnityEngine.Color actual,
    float r,
    float g,
    float b,
    float a,
    string label)
{
    AssertClose(actual.r, r, 0.000001f, label + " r");
    AssertClose(actual.g, g, 0.000001f, label + " g");
    AssertClose(actual.b, b, 0.000001f, label + " b");
    AssertClose(actual.a, a, 0.000001f, label + " a");
}

static void AssertClose(
    float actual,
    float expected,
    float tolerance,
    string label)
{
    if (Math.Abs(actual - expected) > tolerance)
        throw new Exception(
            $"{label}: expected {expected}, got {actual}.");
}

static byte[] BuildTinyCatalog()
{
    using var ms = new MemoryStream();
    using var w = new BinaryWriter(ms);

    w.Write(System.Text.Encoding.ASCII.GetBytes("TCLIVOX1"));
    w.Write(1);

    WriteString(w, "fixture");
    w.Write(0);
    w.Write(-1);
    w.Write(4f);
    w.Write(1f);
    w.Write(1f);

    WritePoints(w, new[] { new VoxelPoint(0, 0, 0) });
    WritePoints(w, new[] { new VoxelPoint(1, 0, 0) });
    WritePoints(w, new[] { new VoxelPoint(2, 0, 0) });
    WritePoints(w, new[] { new VoxelPoint(3, 0, 0) });

    return ms.ToArray();
}

static void WriteString(BinaryWriter w, string value)
{
    byte[] data = System.Text.Encoding.UTF8.GetBytes(value);
    w.Write(data.Length);
    w.Write(data);
}

static void WritePoints(BinaryWriter w, IEnumerable<VoxelPoint> points)
{
    var array = points.ToArray();
    w.Write(array.Length);
    foreach (var p in array)
    {
        w.Write(p.X);
        w.Write(p.Y);
        w.Write(p.Z);
    }
}

// DebugVoxelCatalog is internal by design. Reflection verifies that the
// adapter assembly surface still contains it without changing production API.
static class DebugVoxelCatalogProxy
{
    public static IVoxelModelCatalog Create()
    {
        var type = typeof(TimeCliBootstrap).Assembly.GetType(
            "TimeCli.UnityRuntime.DebugVoxelCatalog",
            throwOnError: true)!;

        var method = type.GetMethod(
            "Create",
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic)!;

        return (IVoxelModelCatalog)method.Invoke(null, null)!;
    }
}
