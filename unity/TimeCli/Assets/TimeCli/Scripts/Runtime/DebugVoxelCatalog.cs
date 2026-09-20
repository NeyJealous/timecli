using System;
using System.Collections.Generic;
using TimeClickers.PortCore;

namespace TimeCli.UnityRuntime
{
    internal static class DebugVoxelCatalog
    {
        public static IVoxelModelCatalog Create()
        {
            var descriptors = new List<VoxelModelDescriptor>();
            var layouts = new List<VoxelModelLayout>();

            for (int count = 11; count <= 70; count++)
                AddModel(descriptors, layouts, $"debug-small-{count}", count);

            for (int count = 100; count <= 130; count++)
                AddModel(descriptors, layouts, $"debug-large-{count}", count);

            return new InMemoryVoxelModelCatalog(descriptors, layouts);
        }

        private static void AddModel(
            ICollection<VoxelModelDescriptor> descriptors,
            ICollection<VoxelModelLayout> layouts,
            string id,
            int count)
        {
            // Keep one white and one yellow slot available so the development
            // fallback can exercise Time/Weapon Cube replacement paths.
            int blueCount = Math.Max(0, count - 2);

            descriptors.Add(new VoxelModelDescriptor(
                id,
                MinWave: 0,
                EnemyCount: count));

            layouts.Add(new VoxelModelLayout(
                id,
                Red: Array.Empty<VoxelPoint>(),
                White: new[] { new VoxelPoint(0, 0, 0) },
                Yellow: new[] { new VoxelPoint(1, 0, 0) },
                Blue: BuildLine(blueCount, 2),
                Size: new VoxelPoint(count, 1, 1)));
        }

        private static VoxelPoint[] BuildLine(int count, int startX)
        {
            var points = new VoxelPoint[count];

            for (int i = 0; i < count; i++)
                points[i] = new VoxelPoint(startX + i, 0, 0);

            return points;
        }
    }
}
