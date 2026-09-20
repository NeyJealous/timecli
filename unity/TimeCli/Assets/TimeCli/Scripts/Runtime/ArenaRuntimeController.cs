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
        private float blockSpacing = 0.35f;

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

            SpawnCurrentEnemy();
        }

        private void Update()
        {
            if (!bootstrap.IsReady)
                return;

            double now = Time.timeAsDouble;
            bootstrap.Game.UpdateAbilities(now);

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

            RebuildViews(spawned.Model);
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

        private void RebuildViews(EnemyModelState model)
        {
            ClearViews();

            if (blockRoot == null)
                blockRoot = transform;

            for (int i = 0; i < model.BlockCount; i++)
            {
                EnemyBlockState state = model.GetBlock(i);
                GameObject instance = CreateBlockObject();

                instance.name = $"Voxel_{i}_{state.EnemyType}";
                instance.transform.SetParent(blockRoot, false);
                instance.transform.localPosition = new Vector3(
                    state.Position.X,
                    state.Position.Y,
                    -state.Position.Z) * blockSpacing;

                var view = instance.GetComponent<VoxelBlockView>();
                if (view == null)
                    view = instance.AddComponent<VoxelBlockView>();

                view.Bind(this, i, state);
                _views.Add(view);
            }
        }

        private GameObject CreateBlockObject()
        {
            if (blockPrefab != null)
                return Instantiate(blockPrefab);

            return GameObject.CreatePrimitive(PrimitiveType.Cube);
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
