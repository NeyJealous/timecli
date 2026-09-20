using System;
using UnityEngine;

namespace TimeCli.UnityRuntime
{
    [CreateAssetMenu(
        fileName = "TimeCliVoxelCatalog",
        menuName = "TimeCli/Voxel Catalog")]
    public sealed class UnityVoxelCatalogAsset : ScriptableObject
    {
        public ModelEntry[] models = Array.Empty<ModelEntry>();

        [Serializable]
        public sealed class ModelEntry
        {
            public string id = string.Empty;
            public int minWave;
            public bool hasBossWave;
            public int bossWave;

            public PointEntry[] red = Array.Empty<PointEntry>();
            public PointEntry[] white = Array.Empty<PointEntry>();
            public PointEntry[] yellow = Array.Empty<PointEntry>();
            public PointEntry[] blue = Array.Empty<PointEntry>();
        }

        [Serializable]
        public struct PointEntry
        {
            public float x;
            public float y;
            public float z;
        }
    }
}
