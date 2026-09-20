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
        public int layer { get; set; }
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
        public static Vector3 up => new(0f, 1f, 0f);

        public static Vector3 operator *(Vector3 value, float scalar) =>
            new(value.x * scalar, value.y * scalar, value.z * scalar);

        public static Vector3 operator +(Vector3 a, Vector3 b) =>
            new(a.x + b.x, a.y + b.y, a.z + b.z);

        public static Vector3 operator -(Vector3 a, Vector3 b) =>
            new(a.x - b.x, a.y - b.y, a.z - b.z);

        public float magnitude =>
            (float)Math.Sqrt(x * x + y * y + z * z);

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
        public static Quaternion AngleAxis(float angle, Vector3 axis) => new();
        public static Quaternion LookRotation(Vector3 forward) => new();
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => new();

        public static Vector3 operator *(Quaternion q, Vector3 v) => v;
    }

    public struct Rect
    {
        public Rect(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public float x { get; set; }
        public float y { get; set; }
        public float width { get; set; }
        public float height { get; set; }
    }

    public readonly struct Color
    {
        public Color(float r, float g, float b, float a = 1f)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public float r { get; }
        public float g { get; }
        public float b { get; }
        public float a { get; }

        public static Color gray => new(0.5f, 0.5f, 0.5f, 1f);
        public static Color black => new(0f, 0f, 0f, 1f);
        public static Color white => new(1f, 1f, 1f, 1f);

        public static Color Lerp(Color a, Color b, float t) =>
            new(
                a.r + (b.r - a.r) * t,
                a.g + (b.g - a.g) * t,
                a.b + (b.b - a.b) * t,
                a.a + (b.a - a.a) * t);
    }

    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public enum LightType { Directional }
    public enum AudioRolloffMode { Logarithmic, Linear, Custom }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureWrapMode { Repeat, Clamp, Mirror, MirrorOnce }
    public enum ParticleSystemSimulationSpace { Local, World, Custom }
    public enum ParticleSystemScalingMode { Hierarchy, Local, Shape }
    public enum ParticleSystemEmitterVelocityMode { Transform, Rigidbody, Custom }
    public enum ParticleSystemInheritVelocityMode { Initial, Current }
    public enum ParticleSystemShapeType { Cone = 4 }
    public enum ParticleSystemRenderMode { Billboard = 0 }
    public enum ParticleSystemSortMode
    {
        None = 0,
        Distance = 1,
        OldestInFront = 2,
        YoungestInFront = 3,
        Depth = 4
    }
    public enum ParticleSystemRenderSpace
    {
        View = 0,
        World = 1,
        Local = 2,
        Facing = 3,
        Velocity = 4
    }
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
        public Vector3 WorldToScreenPoint(Vector3 position) => position;
        public Ray ScreenPointToRay(Vector3 position) =>
            new(position, new Vector3(0f, 0f, 1f));
    }

    public sealed class Light : Behaviour
    {
        public LightType type { get; set; }
        public float intensity { get; set; }
    }

    public sealed class AudioClip : Object { }

    public sealed class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; } = default!;
        public bool playOnAwake { get; set; }
        public float pitch { get; set; } = 1f;
        public float volume { get; set; } = 1f;
        public float minDistance { get; set; } = 1f;
        public float maxDistance { get; set; } = 500f;
        public float spatialBlend { get; set; }
        public AudioRolloffMode rolloffMode { get; set; }
        public void Play() { }
    }

    public class Renderer : Component
    {
        public Material sharedMaterial { get; set; } = default!;
        public void GetPropertyBlock(MaterialPropertyBlock properties) { }
        public void SetPropertyBlock(MaterialPropertyBlock properties) { }
    }

    public sealed class MeshRenderer : Renderer { }

    public sealed class ParticleSystemRenderer : Renderer
    {
        public ParticleSystemRenderMode renderMode { get; set; }
        public ParticleSystemSortMode sortMode { get; set; }
        public float minParticleSize { get; set; }
        public float maxParticleSize { get; set; }
        public float cameraVelocityScale { get; set; }
        public float velocityScale { get; set; }
        public float lengthScale { get; set; }
        public ParticleSystemRenderSpace alignment { get; set; }
    }

    public sealed class LineRenderer : Renderer
    {
        public int positionCount { get; set; }
        public bool useWorldSpace { get; set; }
        public float startWidth { get; set; }
        public float endWidth { get; set; }
        public Color startColor { get; set; }
        public Color endColor { get; set; }
        public void SetPosition(int index, Vector3 position) { }
    }

    public sealed class MeshFilter : Component
    {
        public Mesh sharedMesh { get; set; } = default!;
    }

    public class Collider : Component { }

    public sealed class BoxCollider : Collider
    {
        public bool enabled { get; set; } = true;
        public Vector3 center { get; set; }
        public Vector3 size { get; set; }
    }

    public struct Ray
    {
        public Ray(Vector3 origin, Vector3 direction)
        {
            this.origin = origin;
            this.direction = direction;
        }

        public Vector3 origin { get; set; }
        public Vector3 direction { get; set; }
    }

    public readonly struct RaycastHit
    {
        public Vector3 point => Vector3.zero;
        public Collider collider => default!;
    }

    public static class Physics
    {
        public static bool Raycast(
            Vector3 origin,
            Vector3 direction,
            out RaycastHit hitInfo,
            float maxDistance,
            int layerMask)
        {
            hitInfo = new RaycastHit();
            return false;
        }

        public static bool Raycast(
            Ray ray,
            out RaycastHit hitInfo,
            float maxDistance,
            int layerMask)
        {
            hitInfo = new RaycastHit();
            return false;
        }

        public static RaycastHit[] RaycastAll(
            Ray ray,
            float maxDistance,
            int layerMask) =>
            Array.Empty<RaycastHit>();

        public static Collider[] OverlapSphere(
            Vector3 position,
            float radius,
            int layerMask) =>
            Array.Empty<Collider>();
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
    public sealed class Texture2D : Texture
    {
        public Texture2D(int width, int height) { }
        public string name { get; set; } = string.Empty;
        public FilterMode filterMode { get; set; }
        public TextureWrapMode wrapMode { get; set; }
        public int anisoLevel { get; set; }
        public void SetPixel(int x, int y, Color color) { }
        public void Apply() { }
    }

    public readonly struct Keyframe
    {
        public Keyframe(
            float time,
            float value,
            float inTangent,
            float outTangent)
        {
            this.time = time;
            this.value = value;
            this.inTangent = inTangent;
            this.outTangent = outTangent;
        }

        public float time { get; }
        public float value { get; }
        public float inTangent { get; }
        public float outTangent { get; }
    }

    public sealed class AnimationCurve
    {
        public AnimationCurve(params Keyframe[] keys)
        {
            this.keys = keys;
        }

        public Keyframe[] keys { get; }

        public float Evaluate(float time)
        {
            if (keys.Length == 0)
                return 0f;
            if (time <= keys[0].time)
                return keys[0].value;
            if (time >= keys[^1].time)
                return keys[^1].value;

            for (int i = 0; i < keys.Length - 1; i++)
            {
                Keyframe a = keys[i];
                Keyframe b = keys[i + 1];

                if (time > b.time)
                    continue;

                float duration = b.time - a.time;
                if (duration <= 0f)
                    return b.value;

                float u = (time - a.time) / duration;
                float u2 = u * u;
                float u3 = u2 * u;

                float h00 = 2f * u3 - 3f * u2 + 1f;
                float h10 = u3 - 2f * u2 + u;
                float h01 = -2f * u3 + 3f * u2;
                float h11 = u3 - u2;

                return
                    h00 * a.value +
                    h10 * duration * a.outTangent +
                    h01 * b.value +
                    h11 * duration * b.inTangent;
            }

            return keys[^1].value;
        }
    }

    public readonly struct GradientColorKey
    {
        public GradientColorKey(Color color, float time)
        {
            this.color = color;
            this.time = time;
        }

        public Color color { get; }
        public float time { get; }
    }

    public readonly struct GradientAlphaKey
    {
        public GradientAlphaKey(float alpha, float time)
        {
            this.alpha = alpha;
            this.time = time;
        }

        public float alpha { get; }
        public float time { get; }
    }

    public sealed class Gradient
    {
        public GradientColorKey[] colorKeys { get; private set; } =
            Array.Empty<GradientColorKey>();
        public GradientAlphaKey[] alphaKeys { get; private set; } =
            Array.Empty<GradientAlphaKey>();

        public void SetKeys(
            GradientColorKey[] colorKeys,
            GradientAlphaKey[] alphaKeys)
        {
            this.colorKeys = colorKeys;
            this.alphaKeys = alphaKeys;
        }
    }

    public sealed class ParticleSystem : Component
    {
        public struct EmitParams
        {
            public Vector3 position { get; set; }
            public Vector3 velocity { get; set; }
            public float startLifetime { get; set; }
            public float startSize { get; set; }
            public float rotation { get; set; }
            public float angularVelocity { get; set; }
            public Color startColor { get; set; }
        }

        public struct Particle
        {
            public Vector3 velocity { get; set; }
        }

        public readonly struct MinMaxCurve
        {
            public MinMaxCurve(float constant)
            {
                constantMin = constant;
                constantMax = constant;
                multiplier = 1f;
                curve = null;
            }

            public MinMaxCurve(float min, float max)
            {
                constantMin = min;
                constantMax = max;
                multiplier = 1f;
                curve = null;
            }

            public MinMaxCurve(
                float multiplier,
                AnimationCurve curve)
            {
                constantMin = 0f;
                constantMax = 0f;
                this.multiplier = multiplier;
                this.curve = curve;
            }

            public float constantMin { get; }
            public float constantMax { get; }
            public float multiplier { get; }
            public AnimationCurve? curve { get; }
        }

        public readonly struct MinMaxGradient
        {
            public MinMaxGradient(Color color)
            {
                this.color = color;
                gradientMin = null;
                gradientMax = null;
            }

            public MinMaxGradient(Gradient gradient)
            {
                color = Color.white;
                gradientMin = gradient;
                gradientMax = gradient;
            }

            public MinMaxGradient(
                Gradient min,
                Gradient max)
            {
                color = Color.white;
                gradientMin = min;
                gradientMax = max;
            }

            public Color color { get; }
            public Gradient? gradientMin { get; }
            public Gradient? gradientMax { get; }
        }

        public struct MainModule
        {
            public float duration { get; set; }
            public bool loop { get; set; }
            public bool prewarm { get; set; }
            public bool playOnAwake { get; set; }
            public ParticleSystemSimulationSpace simulationSpace { get; set; }
            public ParticleSystemScalingMode scalingMode { get; set; }
            public ParticleSystemEmitterVelocityMode emitterVelocityMode { get; set; }
            public MinMaxCurve startLifetime { get; set; }
            public MinMaxCurve startSpeed { get; set; }
            public MinMaxCurve startSize { get; set; }
            public MinMaxCurve startRotation { get; set; }
            public MinMaxGradient startColor { get; set; }
            public float gravityModifier { get; set; }
            public int maxParticles { get; set; }
        }

        public struct ShapeModule
        {
            public bool enabled { get; set; }
            public ParticleSystemShapeType shapeType { get; set; }
            public float radius { get; set; }
            public float angle { get; set; }
            public float length { get; set; }
            public float arc { get; set; }
            public float randomDirectionAmount { get; set; }
        }

        public struct EmissionModule
        {
            public bool enabled { get; set; }
            public MinMaxCurve rateOverTime { get; set; }
        }

        public struct SizeOverLifetimeModule
        {
            public bool enabled { get; set; }
            public bool separateAxes { get; set; }
            public MinMaxCurve size { get; set; }
        }

        public struct ColorOverLifetimeModule
        {
            public bool enabled { get; set; }
            public MinMaxGradient color { get; set; }
        }

        public struct ForceOverLifetimeModule
        {
            public bool enabled { get; set; }
            public ParticleSystemSimulationSpace space { get; set; }
            public bool randomized { get; set; }
            public MinMaxCurve x { get; set; }
            public MinMaxCurve y { get; set; }
            public MinMaxCurve z { get; set; }
        }

        public struct InheritVelocityModule
        {
            public bool enabled { get; set; }
            public ParticleSystemInheritVelocityMode mode { get; set; }
            public MinMaxCurve curve { get; set; }
        }

        public MainModule main => new();
        public ShapeModule shape => new();
        public EmissionModule emission => new();
        public SizeOverLifetimeModule sizeOverLifetime => new();
        public ColorOverLifetimeModule colorOverLifetime => new();
        public ForceOverLifetimeModule forceOverLifetime => new();
        public InheritVelocityModule inheritVelocity => new();

        public void Emit(int count) { }
        public void Emit(EmitParams emitParams, int count) { }
        public int GetParticles(Particle[] particles) => 0;
        public void SetParticles(Particle[] particles, int size) { }
        public void Play() { }
    }

    public sealed class Material : Object
    {
        public Material(Shader shader) { }
        public string name { get; set; } = string.Empty;
        public Color color { get; set; }
        public Texture mainTexture { get; set; } = default!;
        public void SetColor(string name, Color color) { }
        public void SetFloat(string name, float value) { }
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
        public const float PI = (float)Math.PI;
        public const float Deg2Rad = PI / 180f;

        public static float Clamp01(float value) =>
            value < 0f ? 0f : value > 1f ? 1f : value;

        public static float Lerp(float a, float b, float t) =>
            a + (b - a) * t;

        public static float Sqrt(float value) =>
            (float)Math.Sqrt(value);

        public static float Pow(float f, float p) =>
            (float)Math.Pow(f, p);

        public static float Min(float a, float b) =>
            a < b ? a : b;

        public static int Max(int a, int b) =>
            a > b ? a : b;
    }

    public static class Random
    {
        public static int Range(int minInclusive, int maxExclusive) => minInclusive;
        public static float Range(float minInclusive, float maxInclusive) => minInclusive;
        public static float value => 0.5f;
        public static Vector3 insideUnitSphere => Vector3.zero;
        public static Vector3 onUnitSphere => new(0f, 0f, 1f);
        public static Quaternion rotation => new();
    }

    public static class Time
    {
        public static double timeAsDouble => 0.0;
        public static float time => 0f;
        public static float timeScale => 1f;
        public static float deltaTime => 1f / 60f;
    }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogError(object message) { }
    }

    public enum TextAnchor
    {
        UpperLeft,
        UpperCenter,
        UpperRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        LowerLeft,
        LowerCenter,
        LowerRight
    }

    public sealed class GUIStyleState
    {
        public Color textColor { get; set; }
    }

    public sealed class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
        public bool richText { get; set; }
        public int fontSize { get; set; }
        public TextAnchor alignment { get; set; }
        public GUIStyleState normal { get; } = new();
    }

    public sealed class GUISkin
    {
        public GUIStyle box { get; } = new();
        public GUIStyle label { get; } = new();
    }

    public static class GUI
    {
        public static bool enabled { get; set; } = true;
        public static Color color { get; set; } = Color.white;
        public static GUISkin skin { get; } = new();

        public static void Label(
            Rect position,
            string text,
            GUIStyle style) { }

        public static void DrawTexture(
            Rect position,
            Texture image) { }
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
        public string text { get; set; } = string.Empty;
    }

    public static class JsonUtility
    {
        public static T FromJson<T>(string json) => default!;
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
