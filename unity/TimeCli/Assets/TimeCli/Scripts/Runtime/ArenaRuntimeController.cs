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

        private readonly List<VoxelBlockView> _views = new();

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
            SpawnCurrentEnemy();
        }

        private void Update()
        {
            if (!bootstrap.IsReady)
                return;

            double now = Time.timeAsDouble;
            bootstrap.Game.UpdateAbilities(now);
            bootstrap.Game.UpdateCubePickups(now);

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

                ApplyVolleyToViews(execution);

                if (bootstrap.Arena.CurrentEnemy == null)
                {
                    ClearViews();
                    break;
                }
            }
        }

        public void ClickBlock(int blockIndex)
        {
            if (bootstrap == null || bootstrap.Arena.CurrentEnemy == null)
                return;

            var result = bootstrap.Arena.ClickBlock(
                blockIndex,
                critical: false,
                nowSeconds: Time.timeAsDouble);

            RefreshView(blockIndex);

            if (result.ModelCleared)
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

        private void ApplyVolleyToViews(HeroVolleyExecutionResult execution)
        {
            int count = Math.Min(
                execution.Plan.Applications.Count,
                execution.DamageResults.Count);

            for (int i = 0; i < count; i++)
            {
                EnemyBlockState target = execution.Plan.Applications[i].Target;

                for (int viewIndex = 0; viewIndex < _views.Count; viewIndex++)
                {
                    if (ReferenceEquals(_views[viewIndex].State, target))
                    {
                        _views[viewIndex].Refresh();
                        break;
                    }
                }
            }
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
