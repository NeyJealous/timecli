using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeCli.UnityRuntime
{
    /// <summary>
    /// Loads the ignored original-derived click-weapon hierarchy report.
    /// Only transform data is reconstructed; original meshes/materials/scripts
    /// are never loaded into the public project.
    /// </summary>
    public static class PrivateWeaponHierarchyPresentation
    {
        private const string ResourceName = "TimeCliWeaponHierarchy";
        private const string ExpectedFormat = "timecli-weapon-hierarchy-v1";
        private const float MaxVerifiedDistance = 0.05f;

        public static bool TryBind(ArenaRuntimeController arena)
        {
            if (arena == null)
                return false;

            TextAsset asset =
                Resources.Load<TextAsset>(ResourceName);

            if (asset == null ||
                string.IsNullOrWhiteSpace(asset.text))
            {
                return false;
            }

            WeaponHierarchyReport report;
            try
            {
                report =
                    JsonUtility.FromJson<WeaponHierarchyReport>(
                        asset.text);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "TimeCli: private weapon hierarchy JSON could not be " +
                    $"parsed: {exception.Message}");
                return false;
            }

            if (report == null ||
                report.format != ExpectedFormat ||
                !report.originalDerived ||
                report.scenes == null)
            {
                Debug.LogWarning(
                    "TimeCli: private weapon hierarchy report has an " +
                    "unexpected format.");
                return false;
            }

            WeaponHierarchyScene scene =
                FindCompleteScene(report.scenes);

            if (scene == null)
            {
                Debug.LogWarning(
                    "TimeCli: no private hierarchy scene contains verified " +
                    "Pistol/Cannon/Launcher fire spots.");
                return false;
            }

            var root =
                new GameObject(
                    "OriginalClickWeaponHierarchy_Private");

            var created =
                new Dictionary<long, Transform>();

            var nodes =
                new Dictionary<long, WeaponHierarchyNode>();

            for (int i = 0; i < scene.nodes.Length; i++)
            {
                WeaponHierarchyNode node = scene.nodes[i];
                if (node != null &&
                    node.transformPathId != 0)
                {
                    nodes[node.transformPathId] = node;
                }
            }

            foreach (long transformId in nodes.Keys)
            {
                CreateTransform(
                    transformId,
                    nodes,
                    created,
                    root.transform);
            }

            Transform pistol =
                ResolveFireSpot(
                    scene.fireSpotMatches.pistol,
                    created);
            Transform cannon =
                ResolveFireSpot(
                    scene.fireSpotMatches.cannon,
                    created);
            Transform launcher =
                ResolveFireSpot(
                    scene.fireSpotMatches.launcher,
                    created);

            if (pistol == null ||
                cannon == null ||
                launcher == null)
            {
                UnityEngine.Object.Destroy(root);
                Debug.LogWarning(
                    "TimeCli: private hierarchy fire-spot IDs were not " +
                    "present in the reconstructed transform set.");
                return false;
            }

            arena.BindRecoveredClickWeaponFireSpots(
                pistol,
                cannon,
                launcher);

            Debug.Log(
                "TimeCli: bound exact private click-weapon fire spots " +
                $"from {scene.scene}.");

            return true;
        }

        private static WeaponHierarchyScene FindCompleteScene(
            WeaponHierarchyScene[] scenes)
        {
            for (int i = 0; i < scenes.Length; i++)
            {
                WeaponHierarchyScene scene = scenes[i];
                if (scene == null ||
                    scene.nodes == null ||
                    scene.fireSpotMatches == null)
                {
                    continue;
                }

                if (IsVerified(scene.fireSpotMatches.pistol) &&
                    IsVerified(scene.fireSpotMatches.cannon) &&
                    IsVerified(scene.fireSpotMatches.launcher))
                {
                    return scene;
                }
            }

            return null;
        }

        private static bool IsVerified(
            WeaponFireSpotMatch match)
        {
            return match != null &&
                   match.transformPathId != 0 &&
                   match.distance >= 0f &&
                   match.distance <= MaxVerifiedDistance;
        }

        private static Transform ResolveFireSpot(
            WeaponFireSpotMatch match,
            Dictionary<long, Transform> created)
        {
            if (!IsVerified(match))
                return null;

            return created.TryGetValue(
                match.transformPathId,
                out Transform transform)
                    ? transform
                    : null;
        }

        private static Transform CreateTransform(
            long transformId,
            Dictionary<long, WeaponHierarchyNode> nodes,
            Dictionary<long, Transform> created,
            Transform privateRoot)
        {
            if (created.TryGetValue(
                transformId,
                out Transform existing))
            {
                return existing;
            }

            if (!nodes.TryGetValue(
                transformId,
                out WeaponHierarchyNode node))
            {
                return null;
            }

            Transform parent = privateRoot;
            if (node.parentTransformPathId != 0 &&
                nodes.ContainsKey(node.parentTransformPathId))
            {
                parent =
                    CreateTransform(
                        node.parentTransformPathId,
                        nodes,
                        created,
                        privateRoot);
            }

            var gameObject =
                new GameObject(
                    string.IsNullOrWhiteSpace(node.name)
                        ? $"Transform_{transformId}"
                        : node.name);

            gameObject.transform.SetParent(
                parent,
                false);

            gameObject.transform.localPosition =
                ToVector3(
                    node.localPosition,
                    Vector3.zero);

            gameObject.transform.localRotation =
                ToQuaternion(
                    node.localRotationQuaternion);

            gameObject.transform.localScale =
                ToVector3(
                    node.localScale,
                    Vector3.one);

            gameObject.SetActive(node.active);

            created[transformId] =
                gameObject.transform;

            return gameObject.transform;
        }

        private static Vector3 ToVector3(
            float[] values,
            Vector3 fallback)
        {
            if (values == null ||
                values.Length != 3)
            {
                return fallback;
            }

            return new Vector3(
                values[0],
                values[1],
                values[2]);
        }

        private static Quaternion ToQuaternion(
            float[] values)
        {
            if (values == null ||
                values.Length != 4)
            {
                return new Quaternion(
                    0f,
                    0f,
                    0f,
                    1f);
            }

            return new Quaternion(
                values[0],
                values[1],
                values[2],
                values[3]);
        }

        [Serializable]
        private sealed class WeaponHierarchyReport
        {
            public string format;
            public bool originalDerived;
            public WeaponHierarchyScene[] scenes;
        }

        [Serializable]
        private sealed class WeaponHierarchyScene
        {
            public string scene;
            public WeaponHierarchyNode[] nodes;
            public WeaponFireSpotMatches fireSpotMatches;
        }

        [Serializable]
        private sealed class WeaponFireSpotMatches
        {
            public WeaponFireSpotMatch pistol;
            public WeaponFireSpotMatch cannon;
            public WeaponFireSpotMatch launcher;
        }

        [Serializable]
        private sealed class WeaponFireSpotMatch
        {
            public long transformPathId;
            public string hierarchyPath;
            public float[] worldPosition;
            public float[] expectedWorldPosition;
            public float distance;
        }

        [Serializable]
        private sealed class WeaponHierarchyNode
        {
            public string name;
            public long transformPathId;
            public long parentTransformPathId;
            public float[] localPosition;
            public float[] localRotationQuaternion;
            public float[] localScale;
            public bool active;
        }
    }
}
