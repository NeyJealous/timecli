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

// Canonical click-weapon Shoot AnimationClips.
AssertClose(
    OriginalClickWeaponShootAnimation.PistolDuration,
    0.0833333358f,
    0.000001f,
    "Pistol shoot duration");

Vector3 pistolRecoilStart =
    OriginalClickWeaponShootAnimation.EvaluatePistolSlide(0f);
Vector3 pistolRecoilEnd =
    OriginalClickWeaponShootAnimation.EvaluatePistolSlide(
        OriginalClickWeaponShootAnimation.PistolDuration);

AssertClose(
    pistolRecoilStart.y,
    0.1410000026f,
    0.000001f,
    "Pistol top1 recoil start Y");
AssertClose(
    pistolRecoilStart.z,
   -0.0610000007f,
    0.000001f,
    "Pistol top1 recoil start Z");
AssertClose(
    pistolRecoilEnd.y,
    OriginalClickWeaponPresentation.PistolTopRestPosition.y,
    0.000001f,
    "Pistol top1 recoil end Y");
AssertClose(
    pistolRecoilEnd.z,
    OriginalClickWeaponPresentation.PistolTopRestPosition.z,
    0.000001f,
    "Pistol top1 recoil end Z");

AssertClose(
    OriginalClickWeaponShootAnimation.CannonDuration,
    0.0833333358f,
    0.000001f,
    "Cannon shoot duration");

Vector3 cannonRecoilStart =
    OriginalClickWeaponShootAnimation.EvaluateCannon(0f);
Vector3 cannonRecoilEnd =
    OriginalClickWeaponShootAnimation.EvaluateCannon(
        OriginalClickWeaponShootAnimation.CannonDuration);

AssertClose(
    cannonRecoilStart.z,
   -0.0500000007f,
    0.000001f,
    "Cannon recoil start Z");
AssertClose(
    cannonRecoilEnd.z,
   -0.0000003576f,
    0.000001f,
    "Cannon recoil clip end Z");

AssertClose(
    OriginalClickWeaponShootAnimation.LauncherDuration,
    0.1666666716f,
    0.000001f,
    "Launcher shoot duration");

Vector3 launcherRecoilStart =
    OriginalClickWeaponShootAnimation.EvaluateLauncherPosition(0f);
Vector3 launcherRecoilEnd =
    OriginalClickWeaponShootAnimation.EvaluateLauncherPosition(
        OriginalClickWeaponShootAnimation.LauncherDuration);

AssertClose(
    launcherRecoilStart.y,
    0f,
    0.000001f,
    "Launcher recoil start Y");
AssertClose(
    launcherRecoilStart.z,
   -0.0599999987f,
    0.000001f,
    "Launcher recoil start Z");
AssertClose(
    launcherRecoilEnd.y,
   -0.0590000004f,
    0.000001f,
    "Launcher recoil end Y");
AssertClose(
    launcherRecoilEnd.z,
    0.1899999976f,
    0.000001f,
    "Launcher recoil end Z");

if (OriginalClickWeaponShootAnimation.LauncherRotationX.keys.Length != 11 ||
    OriginalClickWeaponShootAnimation.LauncherRotationY.keys.Length != 11 ||
    OriginalClickWeaponShootAnimation.LauncherRotationZ.keys.Length != 11 ||
    OriginalClickWeaponShootAnimation.LauncherRotationW.keys.Length != 11)
{
    throw new Exception(
        "Launcher recoil quaternion must retain all 11 recovered 60 Hz keys.");
}

// Canonical "Toon Rocket 02 PS": VirtualPS emits 10 particles for
// each recovered 25 ms tail invocation.
if (OriginalRocketTailPresentation.ParticlesPerEmission != 10)
    throw new Exception("Rocket tail must emit 10 particles per invocation.");

AssertClose(
    OriginalRocketTailPresentation.LifetimeMin,
    0.5f,
    0.000001f,
    "Rocket tail lifetime min");

AssertClose(
    OriginalRocketTailPresentation.LifetimeMax,
    1f,
    0.000001f,
    "Rocket tail lifetime max");

AssertClose(
    OriginalRocketTailPresentation.SpeedMin,
    2.5f,
    0.000001f,
    "Rocket tail speed min");

AssertClose(
    OriginalRocketTailPresentation.SpeedMax,
    5f,
    0.000001f,
    "Rocket tail speed max");

AssertClose(
    OriginalRocketTailPresentation.SizeMin,
    0.1f,
    0.000001f,
    "Rocket tail size min");

AssertClose(
    OriginalRocketTailPresentation.SizeMax,
    0.25f,
    0.000001f,
    "Rocket tail size max");

var rocketTailSizeCurve =
    OriginalRocketTailPresentation.BuildSizeOverLifetimeCurve();

if (rocketTailSizeCurve.keys.Length != 12)
    throw new Exception("Rocket tail size curve must contain 12 recovered keys.");

AssertClose(
    rocketTailSizeCurve.keys[0].value,
    0.0114631057f,
    0.000001f,
    "Rocket tail size curve first value");

AssertClose(
    rocketTailSizeCurve.keys[^1].value,
    0.0114631057f,
    0.000001f,
    "Rocket tail size curve final value");

var rocketTailMaxGradient =
    OriginalRocketTailPresentation.BuildMaxColorOverLifetime();

if (rocketTailMaxGradient.colorKeys.Length != 2 ||
    rocketTailMaxGradient.alphaKeys.Length != 3)
{
    throw new Exception(
        "Rocket tail max gradient key counts do not match 1.4.5.");
}

AssertClose(
    rocketTailMaxGradient.colorKeys[0].time,
    23708f / 65535f,
    0.000001f,
    "Rocket tail max gradient first color time");

AssertClose(
    rocketTailMaxGradient.alphaKeys[1].time,
    32768f / 65535f,
    0.000001f,
    "Rocket tail gradient midpoint alpha time");

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
