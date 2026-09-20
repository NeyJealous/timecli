using TimeClickers.PortCore;
using UnityEngine;

namespace TimeCli.UnityRuntime
{
    [DefaultExecutionOrder(-1000)]
    public sealed class TimeCliBootstrap : MonoBehaviour
    {
        [SerializeField]
        private UnityVoxelCatalogAsset voxelCatalog;

        public GameState Game { get; private set; }
        public HeadlessArenaEngine Arena { get; private set; }
        public UnityRandomSource Random { get; private set; }

        public bool IsReady =>
            Game != null &&
            Arena != null &&
            Random != null;

        private void Awake()
        {
            Game = new GameState();
            Random = new UnityRandomSource();

            IVoxelModelCatalog catalog;

            if (voxelCatalog != null)
            {
                catalog = new UnityVoxelCatalog(voxelCatalog);
            }
            else if (BinaryVoxelCatalog.TryLoadFromResources(out var privateCatalog))
            {
                catalog = privateCatalog;
                Debug.Log("TimeCli: loaded private canonical voxel catalog.");
            }
            else
            {
                catalog = DebugVoxelCatalog.Create();
                Debug.LogWarning(
                    "TimeCli: no canonical voxel catalog found. " +
                    "Using generated development geometry.");
            }

            Arena = new HeadlessArenaEngine(Game, catalog);
        }
    }
}
