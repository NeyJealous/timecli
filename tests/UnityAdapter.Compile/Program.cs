using TimeCli.UnityRuntime;
using TimeClickers.PortCore;

if (PortCoreApi.Version != "1.0.0")
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

Console.WriteLine("Unity adapter compile/preflight passed.");

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
