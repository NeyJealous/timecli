using TimeClickers.PortCore;
using UnityEngine;

namespace TimeCli.UnityRuntime
{
    /// <summary>
    /// Presentation for the original TimeCube / WeaponCube pickup lifecycle.
    /// Currency timing itself remains authoritative in PortCore.
    /// </summary>
    public sealed class SpecialCubePickupView : MonoBehaviour
    {
        private ArenaRuntimeController _arena;
        private SpecialCubePickupState _state;
        private BoxCollider _collider;
        private Renderer _renderer;

        private float _floatAwaySpeed;
        private bool _colliderExpanded;
        private bool _collectionPoseCaptured;
        private Vector3 _collectionStart;
        private Vector3 _collectionTarget;

        public long PickupId => _state?.Id ?? 0;
        public SpecialCubePickupState State => _state;

        public void Bind(
            ArenaRuntimeController arena,
            SpecialCubePickupState state,
            Vector3 worldPosition)
        {
            _arena = arena;
            _state = state;

            transform.position = worldPosition;
            transform.localScale = Vector3.one * 0.45f;

            var camera = Camera.main;
            if (camera != null)
            {
                transform.LookAt(camera.transform.position);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(UnityEngine.Random.onUnitSphere),
                    0.1f);
            }

            _floatAwaySpeed = UnityEngine.Random.Range(2.5f, 6.0f);

            _collider = GetComponent<BoxCollider>();
            if (_collider == null)
                _collider = gameObject.AddComponent<BoxCollider>();

            _collider.center = Vector3.zero;
            _collider.size = Vector3.one;

            _renderer = GetComponentInChildren<Renderer>();
            ApplyVisualStyle();
        }

        public void Refresh(double nowSeconds)
        {
            if (_state == null)
                return;

            if (_state.Phase == SpecialCubePickupPhase.Floating)
            {
                if (_floatAwaySpeed > 0f)
                {
                    transform.position +=
                        transform.forward *
                        _floatAwaySpeed *
                        Time.deltaTime;

                    _floatAwaySpeed -= Time.deltaTime * 2f;
                }

                if (!_colliderExpanded &&
                    _state.IsColliderExpanded(nowSeconds))
                {
                    _collider.size = _collider.size * 10f;
                    _colliderExpanded = true;
                }

                return;
            }

            if (_state.Phase == SpecialCubePickupPhase.Collecting)
            {
                if (!_collectionPoseCaptured)
                {
                    _collectionPoseCaptured = true;
                    _collectionStart = transform.position;
                    _collectionTarget = _arena.GetPickupCollectionPoint(
                        _state.Kind,
                        transform.position);

                    if (_collider != null)
                    {
                        Destroy(_collider);
                        _collider = null;
                    }
                }

                float t = (float)_state.CollectionProgress(nowSeconds);
                transform.position = Vector3.Lerp(
                    _collectionStart,
                    _collectionTarget,
                    t);
                transform.localScale = Vector3.Lerp(
                    Vector3.one * 0.45f,
                    Vector3.one * 0.18f,
                    t);
            }
        }

        private void OnMouseDown()
        {
            if (_arena != null &&
                _state != null &&
                _state.Phase == SpecialCubePickupPhase.Floating)
            {
                _arena.CollectPickup(_state.Id);
            }
        }

        private void ApplyVisualStyle()
        {
            if (_renderer == null || _state == null)
                return;

            Shader shader = Shader.Find("TimeCli/TimeCubeEnemy");
            if (shader == null)
                shader = Shader.Find("TimeCli/BlockVertexColor");

            var material = new Material(shader)
            {
                name = _state.Kind == SpecialCubeKind.TimeCube
                    ? "TimeCli_TimeCubePickup"
                    : "TimeCli_WeaponCubePickup"
            };

            material.color = _state.Kind == SpecialCubeKind.TimeCube
                ? OriginalEnemyPalette.TimeCube
                : OriginalEnemyPalette.WeaponCube;

            _renderer.sharedMaterial = material;
        }
    }
}
