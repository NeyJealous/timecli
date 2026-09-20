using System;
using TimeClickers.PortCore;
using UnityEngine;

namespace TimeCli.UnityRuntime
{
    /// <summary>
    /// Unity presentation/physics adapter for the original ClickCannon FlakBullet
    /// and ClickLauncher RocketPrefab.
    ///
    /// PortCore remains authoritative for damage/rewards/wave progression.
    /// This component only reproduces trajectory/collision timing and reports
    /// collision contacts back to ArenaRuntimeController.
    /// </summary>
    public sealed class ClickWeaponProjectileView : MonoBehaviour
    {
        private ArenaRuntimeController _arena;
        private WeaponType _weaponType;
        private double _damage;
        private float _speed;
        private float _remainingLifetime;
        private Vector3 _moveDirection;
        private Vector3 _previousCollisionPosition;
        private float _collisionAccumulator;
        private bool _processedHit;

        private bool _rocketOrbit;
        private Transform _rocketPivot;
        private Transform _rocketVisual;
        private Transform _rocketTailAnchor;
        private float _rocketRotationOffset;
        private float _rocketRadialTimer;
        private float _rocketTailAccumulator;

        public static ClickWeaponProjectileView SpawnFlak(
            ArenaRuntimeController arena,
            Vector3 start,
            Vector3 target,
            double damage,
            double fireConeNormalized)
        {
            GameObject go =
                ProjectileVisualFactory.CreateFlak();

            go.transform.position = start;
            go.transform.LookAt(target);
            go.transform.rotation = Quaternion.Slerp(
                go.transform.rotation,
                UnityEngine.Random.rotation,
                (float)fireConeNormalized);

            var view =
                go.AddComponent<ClickWeaponProjectileView>();

            view.Initialize(
                arena,
                WeaponType.FlakCannon,
                damage,
                OriginalProjectilePresentation.BaseVelocity,
                OriginalProjectilePresentation.FlakLifetime);

            return view;
        }

        public static ClickWeaponProjectileView SpawnRocket(
            ArenaRuntimeController arena,
            Vector3 start,
            Vector3 target,
            double damage,
            double speedMultiplier,
            int rocketIndex,
            int rocketCount)
        {
            var root = new GameObject(
                $"ClickLauncher_Rocket_{rocketIndex}");

            root.transform.position = start;
            root.transform.LookAt(target);

            var pivot =
                new GameObject("RocketPivot");

            pivot.transform.SetParent(
                root.transform,
                false);

            GameObject visual =
                ProjectileVisualFactory.CreateRocketProjectile(
                    out Transform tailAnchor);

            visual.transform.SetParent(
                pivot.transform,
                false);

            ProjectileVisualFactory.AttachRocketAudio(
                pivot.transform);

            var view =
                root.AddComponent<ClickWeaponProjectileView>();

            view.Initialize(
                arena,
                WeaponType.RocketLauncher,
                damage,
                OriginalProjectilePresentation.BaseVelocity *
                    (float)speedMultiplier,
                OriginalProjectilePresentation.RocketLifetime);

            view._rocketPivot = pivot.transform;
            view._rocketVisual = visual.transform;
            view._rocketTailAnchor = tailAnchor;

            if (rocketIndex > 0 &&
                rocketCount > 1)
            {
                view._rocketOrbit = true;
                view._rocketRotationOffset =
                    360f /
                    (rocketCount - 1) *
                    rocketIndex;
            }

            return view;
        }

        private void Initialize(
            ArenaRuntimeController arena,
            WeaponType weaponType,
            double damage,
            float speed,
            float lifetime)
        {
            _arena = arena;
            _weaponType = weaponType;
            _damage = damage;
            _speed = speed;
            _remainingLifetime = lifetime;

            // Original Projectile.Start captures transform.forward once into
            // moveDir. Later Rocket pivot rotation must not bend root motion.
            _moveDirection = transform.forward;
            _previousCollisionPosition =
                transform.position;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            transform.position +=
                _moveDirection *
                _speed *
                dt;

            UpdateRocketOrbit(dt);
            UpdateRocketTail(dt);

            _remainingLifetime -= dt;
            if (_remainingLifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (_processedHit)
                return;

            _collisionAccumulator += dt;

            if (_collisionAccumulator <
                OriginalProjectilePresentation.CollisionCheckInterval)
            {
                return;
            }

            _collisionAccumulator = 0f;
            CheckForCollision();
        }

        private void UpdateRocketOrbit(float dt)
        {
            if (!_rocketOrbit ||
                _rocketPivot == null ||
                _rocketVisual == null)
            {
                return;
            }

            _rocketPivot.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    Time.time *
                        OriginalProjectilePresentation.RocketRotationSpeed +
                    _rocketRotationOffset);

            if (_rocketRadialTimer < 1f)
            {
                _rocketRadialTimer +=
                    dt *
                    OriginalProjectilePresentation.RocketRadialRampSpeed;

                _rocketVisual.localPosition =
                    new Vector3(
                        Mathf.Lerp(
                            0f,
                            1f,
                            _rocketRadialTimer),
                        0f,
                        0f);

                return;
            }
        }

        private void UpdateRocketTail(float dt)
        {
            if (_weaponType != WeaponType.RocketLauncher)
                return;

            int emissionCount =
                RocketTailCadence.Advance(
                    ref _rocketTailAccumulator,
                    dt);

            if (emissionCount <= 0)
                return;

            Vector3 position =
                _rocketTailAnchor != null
                    ? _rocketTailAnchor.position
                    : (_rocketVisual != null
                        ? _rocketVisual.position
                        : transform.position);

            Quaternion rotation =
                _rocketTailAnchor != null
                    ? _rocketTailAnchor.rotation
                    : (_rocketVisual != null
                        ? _rocketVisual.rotation
                        : transform.rotation);

            for (int i = 0; i < emissionCount; i++)
            {
                RocketTailParticleView.Emit(
                    position,
                    rotation);
            }
        }

        private void CheckForCollision()
        {
            Vector3 current =
                transform.position;

            Vector3 movement =
                current -
                _previousCollisionPosition;

            float magnitude =
                movement.magnitude;

            if (magnitude > 0f &&
                Physics.Raycast(
                    _previousCollisionPosition,
                    movement,
                    out RaycastHit hit,
                    magnitude,
                    OriginalProjectilePresentation.HitboxMask))
            {
                _processedHit = true;

                _arena.ProcessClickWeaponProjectileImpact(
                    hit.point,
                    current,
                    _damage,
                    _weaponType,
                    Time.timeAsDouble);

                Destroy(gameObject);
                return;
            }

            _previousCollisionPosition =
                current;
        }
    }
}
