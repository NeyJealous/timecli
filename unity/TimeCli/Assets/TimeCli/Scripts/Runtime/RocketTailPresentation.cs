using UnityEngine;

namespace TimeCli.UnityRuntime
{
    /// <summary>
    /// Frame-rate-independent scheduler for the recovered Rocket tail call
    /// interval. Each scheduled call maps to one original
    /// VirtualPS.RocketTail invocation.
    /// </summary>
    internal static class RocketTailCadence
    {
        private const float Epsilon = 0.000001f;

        public static int Advance(
            ref float accumulator,
            float deltaTime)
        {
            if (deltaTime <= 0f)
                return 0;

            accumulator += deltaTime;

            float interval =
                OriginalProjectilePresentation.RocketTailSpawnDelay;

            int emissions =
                (int)((accumulator + Epsilon) / interval);

            if (emissions <= 0)
                return 0;

            accumulator -=
                emissions * interval;

            if (accumulator < 0f)
                accumulator = 0f;

            return emissions;
        }
    }

    /// <summary>
    /// Canonical 1.4.5 "Toon Rocket 02 PS" values recovered from level1.
    /// The old VirtualPS moves one shared ParticleSystem to the rocket tail and
    /// calls Emit((int)emissionRate), i.e. 10 particles per 25 ms tail tick.
    /// </summary>
    internal static class OriginalRocketTailPresentation
    {
        public const int ParticlesPerEmission = 10;

        public const float Duration = 5f;
        public const float LifetimeMin = 0.5f;
        public const float LifetimeMax = 1f;
        public const float SpeedMin = 2.5f;
        public const float SpeedMax = 5f;
        public const float SizeMin = 0.1f;
        public const float SizeMax = 0.25f;
        public const float RotationMin = 0f;
        public const float RotationMax = 62.831851959228516f;
        public const int MaxParticles = 500;

        public const float ShapeRadius = 0.01f;
        public const float ShapeAngle = 2.8299999237060547f;
        public const float ShapeLength = 5f;
        public const float ShapeArc = 360f;

        public const float ForceMin = -2.5f;
        public const float ForceMax = 2.5f;

        public const float RendererMaxParticleSize = 0.5f;
        public const float RendererLengthScale = 2f;

        public const float MaxGradientColorTime0 =
            23708f / 65535f;
        public const float MinGradientColorTime0 =
            19468f / 65535f;
        public const float GradientColorTime1 =
            37008f / 65535f;
        public const float GradientAlphaTime1 =
            32768f / 65535f;

        public static AnimationCurve BuildSizeOverLifetimeCurve() =>
            new(
                new Keyframe(
                    0f,
                    0.01146310567855835f,
                    7.360556125640869f,
                    7.360556125640869f),
                new Keyframe(
                    0.06742188334465027f,
                    0.5077256560325623f,
                    4.295609474182129f,
                    4.295609474182129f),
                new Keyframe(
                    0.19586680829524994f,
                    0.665798008441925f,
                    1.0575212240219116f,
                    1.0575212240219116f),
                new Keyframe(
                    0.2623741328716278f,
                    0.724615752696991f,
                    0.7404869794845581f,
                    0.7404869794845581f),
                new Keyframe(
                    0.34767886996269226f,
                    0.7755080461502075f,
                    0.45953792333602905f,
                    0.45953792333602905f),
                new Keyframe(
                    0.40585628151893616f,
                    0.7942692041397095f,
                   -0.007759913802146912f,
                   -0.007759913802146912f),
                new Keyframe(
                    0.4762858748435974f,
                    0.7704638838768005f,
                   -0.19440501928329468f,
                   -0.19440501928329468f),
                new Keyframe(
                    0.5664957761764526f,
                    0.7658804655075073f,
                    0.22222678363323212f,
                    0.22222678363323212f),
                new Keyframe(
                    0.6256465911865234f,
                    0.7951756119728088f,
                    0.4189339280128479f,
                    0.4189339280128479f),
                new Keyframe(
                    0.7228683233261108f,
                    0.8284843564033508f,
                    0.46748900413513184f,
                    0.46748900413513184f),
                new Keyframe(
                    0.8153796195983887f,
                    0.8832854628562927f,
                   -2.0649356842041016f,
                   -2.0649356842041016f),
                new Keyframe(
                    1f,
                    0.01146310567855835f,
                   -2.184659242630005f,
                   -2.184659242630005f));

        public static Gradient BuildMaxColorOverLifetime()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(
                        new Color(
                            1f,
                            48f / 255f,
                            0f,
                            1f),
                        MaxGradientColorTime0),
                    new GradientColorKey(
                        new Color(
                            27f / 255f,
                            27f / 255f,
                            27f / 255f,
                            1f),
                        GradientColorTime1)
                },
                BuildAlphaKeys());
            return gradient;
        }

        public static Gradient BuildMinColorOverLifetime()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(
                        new Color(
                            1f,
                            1f,
                            0f,
                            1f),
                        MinGradientColorTime0),
                    new GradientColorKey(
                        new Color(
                            49f / 255f,
                            49f / 255f,
                            49f / 255f,
                            1f),
                        GradientColorTime1)
                },
                BuildAlphaKeys());
            return gradient;
        }

        private static GradientAlphaKey[] BuildAlphaKeys() =>
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(
                    1f,
                    GradientAlphaTime1),
                new GradientAlphaKey(0f, 1f)
            };
    }

    /// <summary>
    /// Modern Unity reconstruction of the original global
    /// "Toon Rocket 02 PS" particle system.
    ///
    /// The original texture remains private original-derived data. When
    /// TimeCliRocketTailTexture is present in Resources the exact recovered
    /// 128x128 texture is used; otherwise a procedural public fallback is used.
    /// </summary>
    internal static class RocketTailParticleView
    {
        private const string PrivateTextureResource =
            "TimeCliRocketTailTexture";

        private static ParticleSystem _particleSystem;
        private static Transform _particleTransform;
        private static Material _material;
        private static Texture2D _fallbackTexture;

        public static void Emit(
            Vector3 position,
            Quaternion rotation)
        {
            EnsureParticleSystem();

            _particleTransform.position = position;
            _particleTransform.rotation = rotation;

            _particleSystem.Emit(
                OriginalRocketTailPresentation.ParticlesPerEmission);
        }

        private static void EnsureParticleSystem()
        {
            if (_particleSystem != null)
                return;

            var root =
                new GameObject(
                    "VirtualPS_Toon Rocket 02 PS");

            _particleTransform = root.transform;
            _particleSystem =
                root.AddComponent<ParticleSystem>();

            ConfigureMain();
            ConfigureShape();
            ConfigureEmission();
            ConfigureSizeOverLifetime();
            ConfigureColorOverLifetime();
            ConfigureForceOverLifetime();
            ConfigureRenderer(root);

            // The original ParticleSystem is playOnAwake=true with continuous
            // emission disabled. Keeping the system playing lets manually
            // emitted particles simulate exactly as VirtualPS expects.
            _particleSystem.Play();
        }

        private static void ConfigureMain()
        {
            ParticleSystem.MainModule main =
                _particleSystem.main;

            main.duration =
                OriginalRocketTailPresentation.Duration;
            main.loop = true;
            main.prewarm = false;
            main.playOnAwake = true;
            main.simulationSpace =
                ParticleSystemSimulationSpace.World;
            main.scalingMode =
                ParticleSystemScalingMode.Shape;
            main.startLifetime =
                new ParticleSystem.MinMaxCurve(
                    OriginalRocketTailPresentation.LifetimeMin,
                    OriginalRocketTailPresentation.LifetimeMax);
            main.startSpeed =
                new ParticleSystem.MinMaxCurve(
                    OriginalRocketTailPresentation.SpeedMin,
                    OriginalRocketTailPresentation.SpeedMax);
            main.startSize =
                new ParticleSystem.MinMaxCurve(
                    OriginalRocketTailPresentation.SizeMin,
                    OriginalRocketTailPresentation.SizeMax);
            main.startRotation =
                new ParticleSystem.MinMaxCurve(
                    OriginalRocketTailPresentation.RotationMin,
                    OriginalRocketTailPresentation.RotationMax);
            main.startColor =
                new ParticleSystem.MinMaxGradient(
                    Color.white);
            main.gravityModifier = 0f;
            main.maxParticles =
                OriginalRocketTailPresentation.MaxParticles;
        }

        private static void ConfigureShape()
        {
            ParticleSystem.ShapeModule shape =
                _particleSystem.shape;

            shape.enabled = true;
            shape.shapeType =
                ParticleSystemShapeType.Cone;
            shape.radius =
                OriginalRocketTailPresentation.ShapeRadius;
            shape.angle =
                OriginalRocketTailPresentation.ShapeAngle;
            shape.length =
                OriginalRocketTailPresentation.ShapeLength;
            shape.arc =
                OriginalRocketTailPresentation.ShapeArc;
            shape.randomDirectionAmount = 1f;
        }

        private static void ConfigureEmission()
        {
            ParticleSystem.EmissionModule emission =
                _particleSystem.emission;

            // Original EmissionModule.enabled=false. VirtualPS reads the
            // serialized rate (10) only to decide how many particles to Emit.
            emission.enabled = false;
            emission.rateOverTime =
                new ParticleSystem.MinMaxCurve(
                    OriginalRocketTailPresentation.ParticlesPerEmission);
        }

        private static void ConfigureSizeOverLifetime()
        {
            ParticleSystem.SizeOverLifetimeModule size =
                _particleSystem.sizeOverLifetime;

            size.enabled = true;
            size.separateAxes = false;
            size.size =
                new ParticleSystem.MinMaxCurve(
                    1f,
                    OriginalRocketTailPresentation
                        .BuildSizeOverLifetimeCurve());
        }

        private static void ConfigureColorOverLifetime()
        {
            ParticleSystem.ColorOverLifetimeModule color =
                _particleSystem.colorOverLifetime;

            color.enabled = true;
            color.color =
                new ParticleSystem.MinMaxGradient(
                    OriginalRocketTailPresentation
                        .BuildMinColorOverLifetime(),
                    OriginalRocketTailPresentation
                        .BuildMaxColorOverLifetime());
        }

        private static void ConfigureForceOverLifetime()
        {
            ParticleSystem.ForceOverLifetimeModule force =
                _particleSystem.forceOverLifetime;

            force.enabled = true;
            force.space =
                ParticleSystemSimulationSpace.World;
            force.randomized = true;

            var axis =
                new ParticleSystem.MinMaxCurve(
                    OriginalRocketTailPresentation.ForceMin,
                    OriginalRocketTailPresentation.ForceMax);

            force.x = axis;
            force.y = axis;
            force.z = axis;
        }

        private static void ConfigureRenderer(
            GameObject root)
        {
            ParticleSystemRenderer renderer =
                root.GetComponent<ParticleSystemRenderer>();

            if (renderer == null)
            {
                renderer =
                    root.AddComponent<ParticleSystemRenderer>();
            }

            renderer.renderMode =
                ParticleSystemRenderMode.Billboard;
            renderer.sortMode =
                ParticleSystemSortMode.OldestInFront;
            renderer.minParticleSize = 0f;
            renderer.maxParticleSize =
                OriginalRocketTailPresentation
                    .RendererMaxParticleSize;
            renderer.cameraVelocityScale = 0f;
            renderer.velocityScale = 0f;
            renderer.lengthScale =
                OriginalRocketTailPresentation
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
                Shader.Find("TimeCli/ParticleAlpha");

            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");

            _material =
                new Material(shader)
                {
                    name =
                        "TimeCli_Reconstructed_Circle_PRT_MAT_Alpha"
                };

            Texture2D texture =
                Resources.Load<Texture2D>(
                    PrivateTextureResource);

            if (texture == null)
                texture = GetFallbackTexture();

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.anisoLevel = 3;

            _material.mainTexture = texture;
            return _material;
        }

        private static Texture2D GetFallbackTexture()
        {
            if (_fallbackTexture != null)
                return _fallbackTexture;

            const int size = 64;
            _fallbackTexture =
                new Texture2D(size, size)
                {
                    name =
                        "TimeCli_Clean_RocketTail_Fallback"
                };

            float center = (size - 1) * 0.5f;
            float radius = center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / radius;
                    float dy = (y - center) / radius;
                    float distance =
                        Mathf.Sqrt(
                            dx * dx +
                            dy * dy);

                    float alpha =
                        Mathf.Clamp01(
                            1f - distance);

                    alpha *= alpha;

                    _fallbackTexture.SetPixel(
                        x,
                        y,
                        new Color(
                            1f,
                            1f,
                            1f,
                            alpha));
                }
            }

            _fallbackTexture.Apply();
            return _fallbackTexture;
        }
    }
}
