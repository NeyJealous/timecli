using System;
using System.Collections.Generic;
using TimeClickers.PortCore;

namespace TimeCli.UnityRuntime
{
    public sealed class UnityVoxelCatalog : IVoxelModelCatalog
    {
        private readonly List<VoxelModelDescriptor> _models = new();
        private readonly Dictionary<string, VoxelModelLayout> _layouts =
            new(StringComparer.Ordinal);

        public UnityVoxelCatalog(UnityVoxelCatalogAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            foreach (var entry in asset.models)
            {
                var layout = new VoxelModelLayout(
                    entry.id,
                    Convert(entry.red),
                    Convert(entry.white),
                    Convert(entry.yellow),
                    Convert(entry.blue));

                _layouts.Add(entry.id, layout);
                _models.Add(new VoxelModelDescriptor(
                    entry.id,
                    entry.minWave,
                    layout.EnemyCount,
                    entry.hasBossWave ? entry.bossWave : null));
            }
        }

        public IReadOnlyList<VoxelModelDescriptor> Models => _models;

        public VoxelModelLayout GetLayout(string id)
        {
            if (_layouts.TryGetValue(id, out var layout))
                return layout;

            throw new KeyNotFoundException(
                $"Voxel layout '{id}' is not present in the Unity catalog.");
        }

        private static VoxelPoint[] Convert(
            UnityVoxelCatalogAsset.PointEntry[] source)
        {
            var result = new VoxelPoint[source.Length];

            for (int i = 0; i < source.Length; i++)
            {
                result[i] = new VoxelPoint(
                    source[i].x,
                    source[i].y,
                    source[i].z);
            }

            return result;
        }
    }
}
