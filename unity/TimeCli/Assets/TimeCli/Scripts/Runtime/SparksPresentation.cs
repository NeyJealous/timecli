using TimeClickers.PortCore;
using UnityEngine;

namespace TimeCli.UnityRuntime
{
    internal enum SparksKind
    {
        Small,
        Large
    }

    /// <summary>
    /// Canonical Time Clickers 1.4.5 VirtualPS.Sparks ParticleSystem values.
    /// The original method reads the weapon color, but does not apply it to
    /// this ParticleSystem; the recovered material/gradient therefore remain
    /// presentation-authoritative.
    /// </summary>
    internal static class OriginalSparksPresentation
    {
        public const int SmallParticleCount = 20;
        public const int LargeBaseParticleCount = 40;
        public const int LargeOverpoweredParticleCount = 20;

        public const float SmallLifetimeMin = 0.1f;
        public const float SmallLifetimeMax = 0.2f;
        public const float LargeLifetimeMin = 0.5f;
        public const float LargeLifetimeMax = 1f;
        public const float LargeOverpoweredLifetimeBonus = 0.15f;

        public const float Duration = 4f;
        public const float SpeedMin = 15f;
        public const float SpeedMax = 25f;
        public const float StartSize = 0.2f;
        public const int MaxParticles = 1000;

        public const float ConeRadius = 0.01f;
        public const float ConeAngle = 45f;
        public const float ConeLength = 5f;
        public const float ConeArc = 360f;

        public const float SizeCurveMultiplier = 3f;
        public const float ForceCurveMultiplier = 20f;

        public const float RendererMaxParticleSize = 0.5f;
        public const float RendererLengthScale = 2f;

        public static int GetParticleCount(
            SparksKind kind,
            float overpowered)
        {
            if (kind == SparksKind.Small)
                return SmallParticleCount;

            float clamped =
                Mathf.Clamp01(overpowered);

            return
                LargeBaseParticleCount +
                (int)(
                    clamped *
                    LargeOverpoweredParticleCount);
        }

        public static AnimationCurve BuildSizeOverLifetimeCurve() =>
            new(
                new Keyframe(
                    0.0824053362f,
                    1f,
                    -3.6791379452f,
                    -3.6791379452f),
                new Keyframe(
                    1f,
                    0f,
                    -2.3962776661f,
                    -2.3962776661f));

        public static AnimationCurve BuildForceMinCurve() =>
            new(
                new Keyframe(
                    0f,
                    0.0134770870f,
                    1.8984199762f,
                    1.8984199762f),
                new Keyframe(
                    0.9955455065f,
                    1f,
                    0f,
                    0f));

        public static AnimationCurve BuildForceMaxCurve() =>
            new(
                new Keyframe(
                    0f,
                    0.00269544125f,
                    -2.3666970730f,
                    -2.3666970730f),
                new Keyframe(
                    1f,
                    -1f,
                    0f,
                    0f));

        public static Gradient BuildColorOverLifetime()
        {
            var gradient =
                new Gradient();

            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(
                        new Color(
                            1f,
                            234f / 255f,
                            127f / 255f,
                            1f),
                        0f),
                    new GradientColorKey(
                        new Color(
                            1f,
                            170f / 255f,
                            0f,
                            1f),
                        32768f / 65535f),
                    new GradientColorKey(
                        new Color(
                            1f,
                            0f,
                            0f,
                            1f),
                        59174f / 65535f)
                },
                new[]
                {
                    new GradientAlphaKey(
                        1f,
                        0f),
                    new GradientAlphaKey(
                        1f,
                        39514f / 65535f),
                    new GradientAlphaKey(
                        0f,
                        1f)
                });

            return gradient;
        }
    }

    /// <summary>
    /// Clean modern reconstruction of the shared VirtualPS.Sparks
    /// ParticleSystem. VirtualPS moves one shared transform to each requested
    /// impact and emits a fixed count into world space.
    /// </summary>
    internal sealed class SparksParticleView :
        MonoBehaviour
    {
        private const string PrivateTextureResource =
            "TimeCliSparksTexture";

        private static SparksParticleView _instance;
        private static Material _material;
        private static Texture2D _fallbackTexture;

        private ParticleSystem _particleSystem;

        public static void Emit(
            Vector3 position,
            WeaponType weaponType,
            SparksKind kind,
            float overpowered)
        {
            EnsureInstance();

            // VirtualPS.Sparks reads the weapon color in 1.4.5, but the value
            // is not subsequently assigned to the ParticleSystem/material.
            _ = weaponType;

            float clamped =
                Mathf.Clamp01(overpowered);

            ParticleSystem.MainModule main =
                _instance._particleSystem.main;

            float lifetime =
                kind == SparksKind.Small
                    ? UnityEngine.Random.Range(
                        OriginalSparksPresentation.SmallLifetimeMin,
                        OriginalSparksPresentation.SmallLifetimeMax)
                    : UnityEngine.Random.Range(
                        OriginalSparksPresentation.LargeLifetimeMin,
                        OriginalSparksPresentation.LargeLifetimeMax) +
                      clamped *
                      OriginalSparksPresentation
                          .LargeOverpoweredLifetimeBonus;

            main.startLifetime =
                new ParticleSystem.MinMaxCurve(
                    lifetime);

            _instance.transform.position =
                position;

            _instance._particleSystem.Emit(
                OriginalSparksPresentation.GetParticleCount(
                    kind,
                    clamped));
        }

        private static void EnsureInstance()
        {
            if (_instance != null)
                return;

            var root =
                new GameObject(
                    "VirtualPS_Sparks");

            _instance =
                root.AddComponent<SparksParticleView>();

            _instance.Initialize();
        }

        private void Initialize()
        {
            var particleObject =
                new GameObject(
                    "Particle System");

            particleObject.transform.SetParent(
                transform,
                false);

            // Canonical child quaternion is approximately (-1,0,0,0):
            // a 180 degree X rotation.
            particleObject.transform.localRotation =
                Quaternion.Euler(
                    180f,
                    0f,
                    0f);

            _particleSystem =
                particleObject.AddComponent<ParticleSystem>();

            ConfigureMain();
            ConfigureEmission();
            ConfigureShape();
            ConfigureSizeOverLifetime();
            ConfigureColorOverLifetime();
            ConfigureForceOverLifetime();
            ConfigureRenderer(particleObject);

            _particleSystem.Play();
        }

        private void ConfigureMain()
        {
            ParticleSystem.MainModule main =
                _particleSystem.main;

            main.duration =
                OriginalSparksPresentation.Duration;
            main.loop = true;
            main.prewarm = false;
            main.playOnAwake = true;
            main.simulationSpace =
                ParticleSystemSimulationSpace.World;
            main.scalingMode =
                ParticleSystemScalingMode.Shape;
            main.startLifetime =
                new ParticleSystem.MinMaxCurve(
                    OriginalSparksPresentation.LargeLifetimeMin,
                    OriginalSparksPresentation.LargeLifetimeMax);
            main.startSpeed =
                new ParticleSystem.MinMaxCurve(
                    OriginalSparksPresentation.SpeedMin,
                    OriginalSparksPresentation.SpeedMax);
            main.startSize =
                new ParticleSystem.MinMaxCurve(
                    OriginalSparksPresentation.StartSize);
            main.startRotation =
                new ParticleSystem.MinMaxCurve(0f);
            main.startColor =
                new ParticleSystem.MinMaxGradient(
                    Color.white);
            main.gravityModifier = 0f;
            main.maxParticles =
                OriginalSparksPresentation.MaxParticles;
        }

        private void ConfigureEmission()
        {
            ParticleSystem.EmissionModule emission =
                _particleSystem.emission;

            // VirtualPS performs explicit Emit(count). Serialized continuous
            // emission is disabled.
            emission.enabled = false;
        }

        private void ConfigureShape()
        {
            ParticleSystem.ShapeModule shape =
                _particleSystem.shape;

            shape.enabled = true;
            shape.shapeType =
                ParticleSystemShapeType.Cone;
            shape.radius =
                OriginalSparksPresentation.ConeRadius;
            shape.angle =
                OriginalSparksPresentation.ConeAngle;
            shape.length =
                OriginalSparksPresentation.ConeLength;
            shape.arc =
                OriginalSparksPresentation.ConeArc;
            shape.randomDirectionAmount = 0f;
        }

        private void ConfigureSizeOverLifetime()
        {
            ParticleSystem.SizeOverLifetimeModule size =
                _particleSystem.sizeOverLifetime;

            size.enabled = true;
            size.separateAxes = false;
            size.size =
                new ParticleSystem.MinMaxCurve(
                    OriginalSparksPresentation
                        .SizeCurveMultiplier,
                    OriginalSparksPresentation
                        .BuildSizeOverLifetimeCurve());
        }

        private void ConfigureColorOverLifetime()
        {
            ParticleSystem.ColorOverLifetimeModule color =
                _particleSystem.colorOverLifetime;

            color.enabled = true;
            color.color =
                new ParticleSystem.MinMaxGradient(
                    OriginalSparksPresentation
                        .BuildColorOverLifetime());
        }

        private void ConfigureForceOverLifetime()
        {
            ParticleSystem.ForceOverLifetimeModule force =
                _particleSystem.forceOverLifetime;

            force.enabled = true;
            force.space =
                ParticleSystemSimulationSpace.World;
            force.randomized = false;

            ParticleSystem.MinMaxCurve axis =
                new ParticleSystem.MinMaxCurve(
                    OriginalSparksPresentation
                        .ForceCurveMultiplier,
                    OriginalSparksPresentation
                        .BuildForceMinCurve(),
                    OriginalSparksPresentation
                        .BuildForceMaxCurve());

            force.x = axis;
            force.y = axis;
            force.z = axis;
        }

        private static void ConfigureRenderer(
            GameObject particleObject)
        {
            ParticleSystemRenderer renderer =
                particleObject
                    .GetComponent<ParticleSystemRenderer>();

            if (renderer == null)
            {
                renderer =
                    particleObject
                        .AddComponent<ParticleSystemRenderer>();
            }

            renderer.renderMode =
                ParticleSystemRenderMode.Stretch;
            renderer.sortMode =
                ParticleSystemSortMode.None;
            renderer.minParticleSize = 0f;
            renderer.maxParticleSize =
                OriginalSparksPresentation
                    .RendererMaxParticleSize;
            renderer.cameraVelocityScale = 0f;
            renderer.velocityScale = 0f;
            renderer.lengthScale =
                OriginalSparksPresentation
                    .RendererLengthScale;
            renderer.alignment =
                ParticleSystemRenderSpace.View;
            renderer.sharedMaterial =
                GetMaterial();
        }

        private static Material GetMaterial()
        {
            if (_material != null)
                return _material;

            Shader shader =
                Shader.Find(
                    "TimeCli/ParticleAdditive");

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Legacy Shaders/Particles/Additive");
            }

            _material =
                new Material(shader)
                {
                    name =
                        "TimeCli_Reconstructed_Sparks"
                };

            _material.SetColor(
                "_TintColor",
                Color.white);
            _material.SetFloat(
                "_InvFade",
                1f);

            Texture2D texture =
                Resources.Load<Texture2D>(
                    PrivateTextureResource);

            if (texture == null)
                texture = GetFallbackTexture();

            texture.filterMode =
                FilterMode.Bilinear;
            texture.wrapMode =
                TextureWrapMode.Clamp;
            texture.anisoLevel = 1;

            _material.mainTexture =
                texture;

            return _material;
        }

        private static Texture2D GetFallbackTexture()
        {
            if (_fallbackTexture != null)
                return _fallbackTexture;

            const int size = 64;

            _fallbackTexture =
                new Texture2D(
                    size,
                    size)
                {
                    name =
                        "TimeCli_Clean_Sparks_Fallback"
                };

            float center =
                (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx =
                        Mathf.Abs(
                            x - center) /
                        center;
                    float dy =
                        Mathf.Abs(
                            y - center) /
                        center;

                    float core =
                        Mathf.Clamp01(
                            1f - dx * 5f);

                    float taper =
                        Mathf.Clamp01(
                            1f - dy);

                    float intensity =
                        core *
                        taper;

                    _fallbackTexture.SetPixel(
                        x,
                        y,
                        new Color(
                            intensity,
                            intensity,
                            intensity,
                            intensity));
                }
            }

            _fallbackTexture.Apply();
            return _fallbackTexture;
        }
    }
}
