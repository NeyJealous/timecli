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

            IVoxelModelCatalog catalog = voxelCatalog != null
                ? new UnityVoxelCatalog(voxelCatalog)
                : DebugVoxelCatalog.Create();

            Arena = new HeadlessArenaEngine(Game, catalog);

            if (voxelCatalog == null)
            {
                Debug.LogWarning(
                    "TimeCli: no canonical voxel catalog assigned. " +
                    "Using generated development geometry.");
            }
        }
    }
}
