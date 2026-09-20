using System;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeField : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DefaultExecutionOrder : Attribute
    {
        public DefaultExecutionOrder(int order) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string fileName { get; set; } = string.Empty;
        public string menuName { get; set; } = string.Empty;
    }

    public class Object
    {
        public static T FindFirstObjectByType<T>() where T : Object => default!;
        public static T Instantiate<T>(T original) where T : Object => original;
        public static void Destroy(Object obj) { }
    }

    public class Component : Object
    {
        public GameObject gameObject { get; internal set; } = new GameObject();
        public Transform transform => gameObject.transform;

        public T GetComponent<T>() where T : Component => default!;
        public T GetComponentInChildren<T>() where T : Component => default!;
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; } = true;
    }

    public class MonoBehaviour : Behaviour { }
    public class ScriptableObject : Object { }

    public sealed class GameObject : Object
    {
        public GameObject(string name = "")
        {
            this.name = name;
            transform = new Transform();
        }

        public string name { get; set; }
        public string tag { get; set; } = string.Empty;
        public Transform transform { get; }

        public T AddComponent<T>() where T : Component, new()
        {
            var value = new T { gameObject = this };
            return value;
        }

        public T GetComponent<T>() where T : Component => default!;
        public T GetComponentInChildren<T>() where T : Component => default!;
        public void SetActive(bool value) { }

        public static GameObject CreatePrimitive(PrimitiveType type) => new();
    }

    public sealed class Transform
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 forward { get; set; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }

        public void SetParent(Transform parent, bool worldPositionStays) { }
        public void LookAt(Vector3 worldPosition) { }
    }

    public readonly struct Vector2
    {
        public Vector2(float x, float y) { X = x; Y = y; }
        public float X { get; }
        public float Y { get; }
    }

    public readonly struct Vector3
    {
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float x { get; }
        public float y { get; }
        public float z { get; }

        public static Vector3 zero => new(0f, 0f, 0f);
        public static Vector3 one => new(1f, 1f, 1f);

        public static Vector3 operator *(Vector3 value, float scalar) =>
            new(value.x * scalar, value.y * scalar, value.z * scalar);

        public static Vector3 operator +(Vector3 a, Vector3 b) =>
            new(a.x + b.x, a.y + b.y, a.z + b.z);

        public static Vector3 Scale(Vector3 a, Vector3 b) =>
            new(a.x * b.x, a.y * b.y, a.z * b.z);

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) =>
            new(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t,
                a.z + (b.z - a.z) * t);
    }

    public readonly struct Quaternion
    {
        public Quaternion(float x, float y, float z, float w) { }

        public static Quaternion Euler(float x, float y, float z) => new();
        public static Quaternion LookRotation(Vector3 forward) => new();
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => new();

        public static Vector3 operator *(Quaternion q, Vector3 v) => v;
    }

    public readonly struct Rect
    {
        public Rect(float x, float y, float width, float height) { }
    }

    public readonly struct Color
    {
        public Color(float r, float g, float b, float a = 1f) { }
        public static Color gray => new(0.5f, 0.5f, 0.5f, 1f);
        public static Color black => new(0f, 0f, 0f, 1f);
    }

    public enum PrimitiveType { Cube }
    public enum LightType { Directional }
    public enum CameraClearFlags { Skybox = 1, SolidColor = 2, Depth = 3, Nothing = 4 }

    public sealed class Camera : Behaviour
    {
        public static Camera main => default!;
        public bool orthographic { get; set; }
        public float orthographicSize { get; set; }
        public float fieldOfView { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public float depth { get; set; }
        public CameraClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }

        public Vector3 ViewportToWorldPoint(Vector3 position) => position;
    }

    public sealed class Light : Behaviour
    {
        public LightType type { get; set; }
        public float intensity { get; set; }
    }

    public class Renderer : Component
    {
        public Material sharedMaterial { get; set; } = default!;
        public void GetPropertyBlock(MaterialPropertyBlock properties) { }
        public void SetPropertyBlock(MaterialPropertyBlock properties) { }
    }

    public sealed class MeshRenderer : Renderer { }

    public sealed class MeshFilter : Component
    {
        public Mesh sharedMesh { get; set; } = default!;
    }

    public sealed class BoxCollider : Component
    {
        public bool enabled { get; set; } = true;
        public Vector3 center { get; set; }
        public Vector3 size { get; set; }
    }

    public sealed class Mesh : Object
    {
        public string name { get; set; } = string.Empty;
        public Vector3[] vertices { get; set; } = Array.Empty<Vector3>();
        public int[] triangles { get; set; } = Array.Empty<int>();
        public Vector2[] uv { get; set; } = Array.Empty<Vector2>();
        public Color[] colors { get; set; } = Array.Empty<Color>();
        public void RecalculateBounds() { }
    }

    public class Texture : Object { }
    public sealed class Texture2D : Texture { }

    public sealed class Material : Object
    {
        public Material(Shader shader) { }
        public string name { get; set; } = string.Empty;
        public Color color { get; set; }
        public Texture mainTexture { get; set; } = default!;
    }

    public sealed class MaterialPropertyBlock
    {
        public void SetColor(int id, Color color) { }
    }

    public sealed class Shader : Object
    {
        public static int PropertyToID(string name) => 0;
        public static Shader Find(string name) => new();
    }

    public static class Mathf
    {
        public static float Clamp01(float value) =>
            value < 0f ? 0f : value > 1f ? 1f : value;
    }

    public static class Random
    {
        public static int Range(int minInclusive, int maxExclusive) => minInclusive;
        public static float Range(float minInclusive, float maxInclusive) => minInclusive;
        public static float value => 0.5f;
        public static Vector3 onUnitSphere => new(0f, 0f, 1f);
    }

    public static class Time
    {
        public static double timeAsDouble => 0.0;
        public static float deltaTime => 1f / 60f;
    }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogError(object message) { }
    }

    public sealed class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
        public bool richText { get; set; }
    }

    public sealed class GUISkin
    {
        public GUIStyle box { get; } = new();
        public GUIStyle label { get; } = new();
    }

    public static class GUI
    {
        public static bool enabled { get; set; } = true;
        public static GUISkin skin { get; } = new();
    }

    public static class GUILayout
    {
        public static void BeginArea(Rect screenRect, GUIStyle style) { }
        public static void EndArea() { }
        public static Vector2 BeginScrollView(Vector2 scrollPosition) => scrollPosition;
        public static void EndScrollView() { }
        public static void BeginHorizontal() { }
        public static void EndHorizontal() { }
        public static void BeginVertical(GUIStyle style) { }
        public static void EndVertical() { }
        public static void Label(string text) { }
        public static void Label(string text, GUIStyle style) { }
        public static void Space(float pixels) { }
        public static bool Button(string text) => false;
    }

    public static class Screen
    {
        public static int width => 1170;
        public static int height => 1080;
    }

    public sealed class TextAsset : Object
    {
        public byte[] bytes { get; set; } = Array.Empty<byte>();
    }

    public static class Resources
    {
        public static T Load<T>(string path) where T : Object => default!;
    }
}

namespace UnityEngine.SceneManagement
{
    public readonly struct Scene { }
}

namespace UnityEditor
{
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MenuItem : Attribute
    {
        public MenuItem(string itemName) { }
    }

    public static class AssetDatabase
    {
        public static bool IsValidFolder(string path) => true;
        public static string CreateFolder(string parentFolder, string newFolderName) => string.Empty;
    }

    public static class Selection
    {
        public static GameObject activeGameObject { get; set; } = default!;
    }
}

namespace UnityEditor.SceneManagement
{
    using UnityEngine.SceneManagement;

    public enum NewSceneSetup { EmptyScene }
    public enum NewSceneMode { Single }

    public static class EditorSceneManager
    {
        public static Scene NewScene(NewSceneSetup setup, NewSceneMode mode) => new();
        public static bool SaveScene(Scene scene, string dstScenePath) => true;
    }
}
