using System;
using System.Collections.Generic;
using TimeClickers.PortCore;
using UnityEngine;

namespace TimeCli.UnityRuntime
{
    public sealed class ArenaRuntimeController : MonoBehaviour
    {
        [SerializeField]
        private TimeCliBootstrap bootstrap;

        [SerializeField]
        private Transform blockRoot;

        [SerializeField]
        private GameObject blockPrefab;

        [SerializeField]
        private float blockSpacing = OriginalArenaPresentation.BlockSpacing;

        [SerializeField]
        private bool autoFireHeroes = true;

        [SerializeField]
        private Transform timeCubeCollectionPoint;

        [SerializeField]
        private Transform weaponCubeCollectionPoint;

        [SerializeField]
        private Transform clickPistolFireSpot;

        [SerializeField]
        private Transform clickCannonFireSpot;

        [SerializeField]
        private Transform clickLauncherFireSpot;

        private readonly List<VoxelBlockView> _views = new();
        private readonly Dictionary<long, SpecialCubePickupView> _pickupViews = new();

        private Vector3 _crosshairPosition;
        private float _lastTapScreenY;
        private OriginalClickWeaponPresentationView _clickWeaponPresentation;

        private void Start()
        {
            if (bootstrap == null)
                bootstrap = FindFirstObjectByType<TimeCliBootstrap>();

            if (bootstrap == null || !bootstrap.IsReady)
            {
                Debug.LogError("TimeCli: bootstrap is missing or not ready.");
                enabled = false;
                return;
            }

            EnsureBlockRoot();

            _clickWeaponPresentation =
                OriginalClickWeaponPresentationView.Create(this);

            _crosshairPosition = new Vector3(
                Screen.width * 0.5f,
                Screen.height * 0.5f,
                0f);

            _lastTapScreenY = 0f;
            _clickWeaponPresentation.UpdateAim(
                _crosshairPosition,
                _lastTapScreenY);

            SpawnCurrentEnemy();
        }

        private void Update()
        {
            if (!bootstrap.IsReady)
                return;

            _clickWeaponPresentation?.UpdateAim(
                _crosshairPosition,
                _lastTapScreenY);

            double now = Time.timeAsDouble;
            bootstrap.Game.UpdateAbilities(now);
            bootstrap.Game.UpdateCubePickups(now);
            bootstrap.Game.UpdateGoldPickups(now);

            ClickWeaponAutomaticFirePlan automaticClickWeapons =
                bootstrap.Game.UpdateClickWeapons(
                    now,
                    Time.deltaTime);

            ProcessAutomaticClickWeapons(
                automaticClickWeapons,
                now);

            SyncPickupViews(now);

            BossTickResult bossTick = bootstrap.Arena.TickBoss(now);

            if (bossTick == BossTickResult.FailedWaitingForRestart)
            {
                ClearViews();
                return;
            }

            if (bossTick == BossTickResult.Restarted)
            {
                SpawnCurrentEnemy();
                return;
            }

            if (bootstrap.Arena.CurrentEnemy == null)
            {
                if (!bootstrap.Game.Arena.BossRestartPending)
                    SpawnCurrentEnemy();

                return;
            }

            if (!autoFireHeroes)
                return;

            for (int heroId = 0; heroId < bootstrap.Game.Heroes.Length; heroId++)
            {
                var execution = bootstrap.Arena.FireHero(
                    heroId,
                    now,
                    bootstrap.Random);

                ApplyVolleyToViews(
                    heroId,
                    execution,
                    now);
                SyncPickupViews(now);

                if (bootstrap.Arena.CurrentEnemy == null)
                {
                    ClearViews();
                    break;
                }
            }
        }

        public void ClickBlock(int blockIndex)
        {
            if (bootstrap == null ||
                bootstrap.Arena.CurrentEnemy == null ||
                blockIndex < 0 ||
                blockIndex >= _views.Count)
            {
                return;
            }

            double now = Time.timeAsDouble;
            Vector3 targetPosition =
                _views[blockIndex].transform.position;

            Camera camera = Camera.main;
            if (camera != null)
            {
                Vector3 screenPosition =
                    camera.WorldToScreenPoint(
                        targetPosition);

                _crosshairPosition =
                    new Vector3(
                        screenPosition.x,
                        screenPosition.y,
                        0f);

                _lastTapScreenY = screenPosition.y;
            }

            _clickWeaponPresentation?.UpdateAim(
                _crosshairPosition,
                _lastTapScreenY);

            ClickWeaponFirePlan auxiliary =
                bootstrap.Game.RegisterManualClickWeapons(
                    now);

            SpawnClickWeaponProjectiles(
                auxiliary,
                GetCrosshairTargetPoint());

            ShootClickPistol(now);
            SyncPickupViews(now);

            if (bootstrap.Arena.CurrentEnemy == null)
                ClearViews();
        }

        public void SpawnCurrentEnemy()
        {
            if (bootstrap.Arena.CurrentEnemy != null)
                return;

            var spawned = bootstrap.Arena.SpawnCurrentEnemy(
                UnityRandomSource.NextSpawnRolls(),
                Time.timeAsDouble);

            RebuildViews(spawned);
        }

        public bool RequestPreviousWave()
        {
            if (!bootstrap.Game.Arena.RequestPreviousArena())
                return false;

            RebuildAfterExternalStateChange();
            return true;
        }

        public bool RequestNextWave()
        {
            if (!bootstrap.Game.Arena.RequestNextArena())
                return false;

            RebuildAfterExternalStateChange();
            return true;
        }

        public void RebuildAfterExternalStateChange()
        {
            bootstrap.Arena.ClearActiveEnemyWithoutProgression();
            ClearViews();

            if (!bootstrap.Game.Arena.BossRestartPending)
                SpawnCurrentEnemy();
        }

        private void ApplyVolleyToViews(
            int heroId,
            HeroVolleyExecutionResult execution,
            double nowSeconds)
        {
            int count = Math.Min(
                execution.Plan.Applications.Count,
                execution.DamageResults.Count);

            WeaponType weaponType =
                bootstrap.Game.Heroes[heroId].Spec.Weapon;

            for (int i = 0; i < count; i++)
            {
                HeroDamageApplication application =
                    execution.Plan.Applications[i];

                EnemyBlockState target =
                    application.Target;

                for (int viewIndex = 0; viewIndex < _views.Count; viewIndex++)
                {
                    if (!ReferenceEquals(
                        _views[viewIndex].State,
                        target))
                    {
                        continue;
                    }

                    // In the original Hero.ApplyDamage is authoritative and
                    // BoxEnemy Outline/FlashOutline is presentation-only.
                    _views[viewIndex].ApplyHeroImpact(
                        weaponType,
                        application.IsSplash,
                        nowSeconds);

                    _views[viewIndex].Refresh();
                    break;
                }
            }
        }

        public void CollectPickup(long pickupId)
        {
            if (bootstrap == null || !bootstrap.IsReady)
                return;

            bootstrap.Game.TryCollectCubePickup(
                pickupId,
                Time.timeAsDouble);
        }

        public void BindRecoveredClickWeaponFireSpots(
            Transform pistol,
            Transform cannon,
            Transform launcher)
        {
            if (pistol != null)
                clickPistolFireSpot = pistol;
            if (cannon != null)
                clickCannonFireSpot = cannon;
            if (launcher != null)
                clickLauncherFireSpot = launcher;
        }

        public Vector3 GetClickPistolFireSpot()
        {
            return clickPistolFireSpot != null
                ? clickPistolFireSpot.position
                : OriginalArenaPresentation.ClickPistolFireSpot;
        }

        public Vector3 GetClickCannonFireSpot()
        {
            return clickCannonFireSpot != null
                ? clickCannonFireSpot.position
                : OriginalArenaPresentation.ClickCannonFireSpot;
        }

        public Vector3 GetClickLauncherFireSpot()
        {
            return clickLauncherFireSpot != null
                ? clickLauncherFireSpot.position
                : OriginalArenaPresentation.ClickLauncherFireSpot;
        }

        public void ProcessClickWeaponProjectileImpact(
            Vector3 impactPoint,
            double damage,
            WeaponType weaponType,
            double nowSeconds)
        {
            if (bootstrap == null ||
                bootstrap.Arena.CurrentEnemy == null)
            {
                return;
            }

            Collider[] hits =
                Physics.OverlapSphere(
                    impactPoint,
                    OriginalProjectilePresentation.ImpactRadius,
                    OriginalProjectilePresentation.HitboxMask);

            var damagedBlocks = new HashSet<int>();

            for (int i = 0; i < hits.Length; i++)
            {
                VoxelBlockView view =
                    hits[i].GetComponent<VoxelBlockView>();

                if (view == null ||
                    view.State == null ||
                    !view.State.IsAlive ||
                    !damagedBlocks.Add(view.BlockIndex))
                {
                    continue;
                }

                if (bootstrap.Arena.CurrentEnemy == null)
                    break;

                ArenaBlockAttackResult result =
                    bootstrap.Arena.ClickWeaponDamageBlock(
                        view.BlockIndex,
                        damage,
                        nowSeconds);

                view.Refresh();

                if (result.ModelCleared)
                {
                    ClearViews();
                    break;
                }
            }

            SyncPickupViews(nowSeconds);

            // weaponType is presentation-relevant even though BoxEnemy damage
            // math is identical after the precomputed click damage reaches it.
            if (weaponType == WeaponType.RocketLauncher)
                SpawnRocketExplosion(impactPoint);
        }

        private void ProcessAutomaticClickWeapons(
            ClickWeaponAutomaticFirePlan plan,
            double nowSeconds)
        {
            for (int i = 0; i < plan.PistolShots; i++)
                ShootClickPistol(nowSeconds);

            Vector3 targetPosition =
                GetCrosshairTargetPoint();

            for (int i = 0;
                 i < plan.CannonShots.Count;
                 i++)
            {
                SpawnClickWeaponProjectiles(
                    new ClickWeaponFirePlan(
                        plan.CannonShots[i],
                        null),
                    targetPosition);
            }

            for (int i = 0;
                 i < plan.LauncherShots.Count;
                 i++)
            {
                SpawnClickWeaponProjectiles(
                    new ClickWeaponFirePlan(
                        null,
                        plan.LauncherShots[i]),
                    targetPosition);
            }
        }

        private Vector3 GetCrosshairTargetPoint()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return Vector3.zero;

            Ray ray =
                camera.ScreenPointToRay(
                    _crosshairPosition);

            if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                OriginalProjectilePresentation.ClickWeaponMaxDistance,
                OriginalProjectilePresentation.HitboxMask))
            {
                return hit.point;
            }

            return ray.origin +
                   ray.direction *
                   OriginalProjectilePresentation.ClickWeaponMaxDistance;
        }

        private void ShootClickPistol(
            double nowSeconds)
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            // ClickerPistol.Shoot evaluates critical state once, then reuses
            // that critical/damage value for every Spread Shots ray.
            bool critical =
                UnityEngine.Random.value <
                bootstrap.Game.GetCriticalChance();

            ClickPistolFirePlan plan =
                ClickPistolFireMath.Build(
                    bootstrap.Game,
                    critical);

            Ray baseRay =
                camera.ScreenPointToRay(
                    _crosshairPosition);

            FireClickPistolRay(
                baseRay,
                plan,
                nowSeconds);

            for (int i = 0;
                 i < plan.AdditionalYawAnglesDegrees.Count;
                 i++)
            {
                Ray spreadRay = baseRay;
                spreadRay.direction =
                    Quaternion.AngleAxis(
                        plan.AdditionalYawAnglesDegrees[i],
                        Vector3.up) *
                    baseRay.direction;

                FireClickPistolRay(
                    spreadRay,
                    plan,
                    nowSeconds);
            }
        }

        private void FireClickPistolRay(
            Ray ray,
            ClickPistolFirePlan plan,
            double nowSeconds)
        {
            Vector3 tracerEnd;

            if (plan.PunchThrough)
            {
                RaycastHit[] hits =
                    Physics.RaycastAll(
                        ray,
                        OriginalProjectilePresentation.ClickWeaponMaxDistance,
                        OriginalProjectilePresentation.HitboxMask);

                for (int i = 0; i < hits.Length; i++)
                {
                    if (bootstrap.Arena.CurrentEnemy == null)
                        break;

                    ProcessClickPistolHit(
                        hits[i],
                        plan.ClickDamage,
                        nowSeconds);
                }

                tracerEnd =
                    ray.origin +
                    ray.direction *
                    OriginalProjectilePresentation.ClickWeaponMaxDistance;
            }
            else if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                OriginalProjectilePresentation.ClickWeaponMaxDistance,
                OriginalProjectilePresentation.HitboxMask))
            {
                tracerEnd = hit.point;

                ProcessClickPistolHit(
                    hit,
                    plan.ClickDamage,
                    nowSeconds);
            }
            else
            {
                tracerEnd =
                    ray.origin +
                    ray.direction *
                    OriginalProjectilePresentation.ClickWeaponMaxDistance;
            }

            ClickTracerView.Spawn(
                GetClickPistolFireSpot(),
                tracerEnd,
                plan.IsCritical);
        }

        private void ProcessClickPistolHit(
            RaycastHit hit,
            double clickDamage,
            double nowSeconds)
        {
            if (hit.collider == null ||
                bootstrap.Arena.CurrentEnemy == null)
            {
                return;
            }

            VoxelBlockView view =
                hit.collider.GetComponent<VoxelBlockView>();

            if (view == null ||
                view.State == null ||
                !view.State.IsAlive)
            {
                return;
            }

            ArenaBlockAttackResult result =
                bootstrap.Arena.ClickWeaponDamageBlock(
                    view.BlockIndex,
                    clickDamage,
                    nowSeconds);

            bootstrap.Game.TrySpawnClickPistolHitGold(
                view.State,
                UnityEngine.Random.value,
                nowSeconds);

            view.Refresh();

            if (result.ModelCleared)
                ClearViews();
        }

        private void SpawnClickWeaponProjectiles(
            ClickWeaponFirePlan plan,
            Vector3 targetPosition)
        {
            if (plan.Cannon is not null)
            {
                for (int i = 0;
                     i < plan.Cannon.ProjectileCount;
                     i++)
                {
                    ClickWeaponProjectileView.SpawnFlak(
                        this,
                        GetClickCannonFireSpot(),
                        targetPosition,
                        plan.Cannon.DamagePerProjectile,
                        plan.Cannon.FireConeNormalized);
                }
            }

            if (plan.Launcher is not null)
            {
                for (int i = 0;
                     i < plan.Launcher.RocketCount;
                     i++)
                {
                    ClickWeaponProjectileView.SpawnRocket(
                        this,
                        GetClickLauncherFireSpot(),
                        targetPosition,
                        plan.Launcher.DamagePerRocket,
                        plan.Launcher.RocketSpeedMultiplier,
                        i,
                        plan.Launcher.RocketCount);
                }
            }
        }

        private void SpawnRocketExplosion(
            Vector3 position)
        {
            GameObject explosion =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere);

            explosion.name =
                "RocketExplosion";
            explosion.transform.position =
                position;
            explosion.transform.localScale =
                Vector3.one * 0.55f;

            Collider collider =
                explosion.GetComponent<Collider>();

            if (collider != null)
                Destroy(collider);

            var lifetime =
                explosion.AddComponent<TimedVisualDestroy>();

            lifetime.Lifetime =
                OriginalProjectilePresentation.RocketExplosionLifetime;
        }

        public Vector3 GetPickupCollectionPoint(
            SpecialCubeKind kind,
            Vector3 fallback)
        {
            Transform target = kind == SpecialCubeKind.TimeCube
                ? timeCubeCollectionPoint
                : weaponCubeCollectionPoint;

            if (target != null)
                return target.position;

            Camera camera = Camera.main;
            if (camera == null)
                return fallback;

            return kind == SpecialCubeKind.TimeCube
                ? OriginalWidgetGoldPresentation.GetTimeCubeCollectionPoint(camera)
                : OriginalWidgetGoldPresentation.GetWeaponCubeCollectionPoint(camera);
        }

        private void SyncPickupViews(double nowSeconds)
        {
            var activeIds = new HashSet<long>();

            foreach (var pickup in bootstrap.Game.CubePickups.Active)
            {
                activeIds.Add(pickup.Id);

                if (!_pickupViews.TryGetValue(
                    pickup.Id,
                    out SpecialCubePickupView view))
                {
                    if (!TryGetSourceWorldPosition(
                        pickup.SourceBlock,
                        out Vector3 sourcePosition))
                    {
                        continue;
                    }

                    GameObject instance =
                        GameObject.CreatePrimitive(PrimitiveType.Cube);
                    instance.name =
                        $"Pickup_{pickup.Kind}_{pickup.Id}";

                    view = instance.AddComponent<SpecialCubePickupView>();
                    view.Bind(this, pickup, sourcePosition);
                    _pickupViews.Add(pickup.Id, view);
                }

                view.Refresh(nowSeconds);
            }

            List<long> remove = null;

            foreach (var pair in _pickupViews)
            {
                if (activeIds.Contains(pair.Key))
                    continue;

                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);

                remove ??= new List<long>();
                remove.Add(pair.Key);
            }

            if (remove == null)
                return;

            foreach (long id in remove)
                _pickupViews.Remove(id);
        }

        private bool TryGetSourceWorldPosition(
            EnemyBlockState sourceBlock,
            out Vector3 worldPosition)
        {
            for (int i = 0; i < _views.Count; i++)
            {
                if (!ReferenceEquals(_views[i].State, sourceBlock))
                    continue;

                worldPosition = _views[i].transform.position;
                return true;
            }

            worldPosition = Vector3.zero;
            return false;
        }

        private void RebuildViews(HeadlessSpawnedEnemy spawned)
        {
            ClearViews();

            EnsureBlockRoot();

            for (int i = 0; i < spawned.Model.BlockCount; i++)
            {
                EnemyBlockState state = spawned.Model.GetBlock(i);
                GameObject instance = CreateBlockObject();

                instance.name = $"Voxel_{i}_{state.EnemyType}";
                // Original ProjectileDamager uses hitboxMask 2560, which
                // includes layer 9. Keep reconstructed enemy colliders inside
                // that mask without requiring project-specific named layers.
                instance.layer = 9;
                instance.transform.SetParent(blockRoot, false);

                VoxelPoint position = spawned.IsVeryFirstEnemy
                    ? state.Position
                    : VoxelCoordinateMath.ToArenaLocalPosition(
                        state.Position,
                        spawned.Layout.Size);

                instance.transform.localPosition = new Vector3(
                    position.X,
                    position.Y,
                    position.Z) * blockSpacing;

                var view = instance.GetComponent<VoxelBlockView>();
                if (view == null)
                    view = instance.AddComponent<VoxelBlockView>();

                view.Bind(this, i, state);
                _views.Add(view);
            }
        }

        private void EnsureBlockRoot()
        {
            if (blockRoot != null)
                return;

            var root = new GameObject("Arena");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = OriginalArenaPresentation.ArenaPosition;
            root.transform.localRotation = Quaternion.Euler(
                OriginalArenaPresentation.ArenaEuler.x,
                OriginalArenaPresentation.ArenaEuler.y,
                OriginalArenaPresentation.ArenaEuler.z);

            blockRoot = root.transform;
        }

        private GameObject CreateBlockObject()
        {
            if (blockPrefab != null)
                return Instantiate(blockPrefab);

            var instance = new GameObject("Voxel");
            instance.AddComponent<OriginalBlockGeometry>();
            instance.AddComponent<VoxelBlockView>();
            return instance;
        }

        private void RefreshView(int index)
        {
            if (index >= 0 && index < _views.Count)
                _views[index].Refresh();
        }

        private void ClearViews()
        {
            for (int i = 0; i < _views.Count; i++)
            {
                if (_views[i] != null)
                    Destroy(_views[i].gameObject);
            }

            _views.Clear();
        }
    }
}
