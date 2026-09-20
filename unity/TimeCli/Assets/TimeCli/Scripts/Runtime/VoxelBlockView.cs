using TimeClickers.PortCore;
using UnityEngine;

namespace TimeCli.UnityRuntime
{
    public sealed class VoxelBlockView : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_Color");
        private static readonly int UrpBaseColor = Shader.PropertyToID("_BaseColor");

        private ArenaRuntimeController _arena;
        private Renderer _renderer;
        private MaterialPropertyBlock _properties;

        public int BlockIndex { get; private set; }
        public EnemyBlockState State { get; private set; }

        public void Bind(
            ArenaRuntimeController arena,
            int blockIndex,
            EnemyBlockState state)
        {
            _arena = arena;
            BlockIndex = blockIndex;
            State = state;

            _renderer = GetComponentInChildren<Renderer>();
            _properties = new MaterialPropertyBlock();

            ApplyPlaceholderColor();
            Refresh();
        }

        public void Refresh()
        {
            if (State == null)
                return;

            if (!State.IsAlive)
                gameObject.SetActive(false);
        }

        private void OnMouseDown()
        {
            if (_arena != null && State != null && State.IsAlive)
                _arena.ClickBlock(BlockIndex);
        }

        private void ApplyPlaceholderColor()
        {
            if (_renderer == null || State == null)
                return;

            Color color = State.EnemyType switch
            {
                EnemyType.Red => new Color(0.85f, 0.15f, 0.15f),
                EnemyType.White => new Color(0.9f, 0.9f, 0.9f),
                EnemyType.Yellow => new Color(0.95f, 0.75f, 0.1f),
                EnemyType.Rainbow => new Color(0.5f, 0.25f, 0.9f),
                EnemyType.TimeCube => new Color(0.2f, 0.8f, 1f),
                EnemyType.WeaponCube => new Color(0.2f, 1f, 0.45f),
                _ => Color.gray
            };

            _renderer.GetPropertyBlock(_properties);
            _properties.SetColor(BaseColor, color);
            _properties.SetColor(UrpBaseColor, color);
            _renderer.SetPropertyBlock(_properties);
        }
    }
}
