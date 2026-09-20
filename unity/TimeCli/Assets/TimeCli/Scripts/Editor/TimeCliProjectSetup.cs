using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeCli.UnityRuntime.Editor
{
    public static class TimeCliProjectSetup
    {
        private const string SceneFolder = "Assets/TimeCli/Scenes";
        private const string ScenePath = SceneFolder + "/ArenaPrototype.unity";

        [MenuItem("TimeCli/Setup/Create Arena Prototype Scene")]
        public static void CreateArenaPrototypeScene()
        {
            EnsureFolder("Assets", "TimeCli");
            EnsureFolder("Assets/TimeCli", "Scenes");

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            var root = new GameObject("TimeCli");
            root.AddComponent<TimeCliBootstrap>();
            root.AddComponent<ArenaRuntimeController>();
            root.AddComponent<PrototypeHud>();

            var cameraObject = new GameObject("Game Camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.orthographic = false;
            camera.fieldOfView = OriginalArenaPresentation.CameraFieldOfView;
            camera.nearClipPlane = OriginalArenaPresentation.CameraNearClip;
            camera.farClipPlane = OriginalArenaPresentation.CameraFarClip;
            camera.depth = OriginalArenaPresentation.CameraDepth;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.transform.position = OriginalArenaPresentation.CameraPosition;
            camera.transform.rotation = Quaternion.Euler(
                OriginalArenaPresentation.CameraEuler.X,
                OriginalArenaPresentation.CameraEuler.Y,
                OriginalArenaPresentation.CameraEuler.Z);

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = root;

            Debug.Log(
                $"TimeCli: created {ScenePath}. " +
                "Press Play to run the placeholder Arena.");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = Path.Combine(parent, name).Replace("\\", "/");

            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
