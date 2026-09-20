using UnityEngine;

namespace TimeCli.UnityRuntime
{
    /// <summary>
    /// Canonical Time Clickers 1.4.5 values recovered from the
    /// VirtualPS/SmallExplosion -> Fire legacy particle hierarchy.
    /// </summary>
    internal static class OriginalSmallExplosionPresentation
    {
        public const int ParticleCount = 10;

        public const float SizeMin = 1.5f;
        public const float SizeMax = 3f;
        public const float LifetimeMin = 0.1f;
        public const float LifetimeMax = 0.3f;

        public static readonly Vector3 WorldVelocity =
            Vector3.zero;

        public static readonly Vector3 LocalVelocity =
            new(0f, 6f, 0f);

        public static readonly Vector3 RandomVelocity =
            new(6f, 6f, 6f);

        public const float EmitterVelocityScale = 0.05f;
        public const float AngularVelocity = 0f;
        public const float RandomAngularVelocity = 200f;
        public const bool RandomRotation = true;

        public static readonly Vector3 Ellipsoid =
            new(0.2f, 0f, 0.2f);

        public const float MinEmitterRange = 0f;
        public const float Damping = 0.1f;

        public const float RendererLengthScale = 2f;
        public const float RendererVelocityScale = 0f;

        public const float AudioVolume = 0.5f;
        public const float AudioQueueCooldown = 0.1f;
        public const int AudioSourcesPerQueue = 6;
        public const float AudioMinDistance = 15f;
        public const float AudioMaxDistance = 100f;

        public const string AudioClipName =
            "Misc_MechAbstract_Impact_02";

        public const float AudioClipLength =
            2.335270881652832f;

        public const int AudioClipChannels = 2;
        public const int AudioClipFrequency = 48000;
        public const int AudioClipBitsPerSample = 16;

        public static readonly Color Color0 =
            new(1f, 1f, 1f, 10f / 255f);

        public static readonly Color Color1 =
            new(1f, 1f, 1f, 1f);

        public static readonly Color Color2 =
            new(
                1f,
                145f / 255f,
                82f / 255f,
                1f);

        public static readonly Color Color3 =
            new(
                1f,
                122f / 255f,
                23f / 255f,
                1f);

        public static readonly Color Color4 =
            new(
                1f,
                72f / 255f,
                0f,
                10f / 255f);

        public static Gradient BuildColorAnimation()
        {
            var gradient = new Gradient();

            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(
                        new Color(1f, 1f, 1f, 1f),
                        0f),
                    new GradientColorKey(
                        new Color(1f, 1f, 1f, 1f),
                        0.25f),
                    new GradientColorKey(
                        new Color(
                            1f,
                            145f / 255f,
                            82f / 255f,
                            1f),
                        0.5f),
                    new GradientColorKey(
                        new Color(
                            1f,
                            122f / 255f,
                            23f / 255f,
                            1f),
                        0.75f),
                    new GradientColorKey(
                        new Color(
                            1f,
                            72f / 255f,
                            0f,
                            1f),
                        1f)
                },
                new[]
                {
                    new GradientAlphaKey(
                        10f / 255f,
                        0f),
                    new GradientAlphaKey(
                        1f,
                        0.25f),
                    new GradientAlphaKey(
                        1f,
                        0.75f),
                    new GradientAlphaKey(
                        10f / 255f,
                        1f)
                });

            return gradient;
        }

        public static float GetDampingMultiplier(
            float deltaTime)
        {
            if (deltaTime <= 0f)
                return 1f;

            // Legacy ParticleAnimator documentation defines damping as a
            // per-second speed factor: 1 = unchanged, 2 = doubles in one
            // second, 0 = stop. Exponential integration preserves that
            // frame-rate-independent meaning.
            return Mathf.Pow(Damping, deltaTime);
        }
    }

    /// <summary>
    /// Modern equivalent of the original single shared legacy
    /// SmallExplosion/Fire particle emitter.
    ///
    /// VirtualPS moves the shared emitter to each impact and emits exactly ten
    /// world-space particles. Existing particles therefore remain where they
    /// were emitted while the shared source can immediately service another
    /// explosion.
    /// </summary>
    internal sealed class SmallExplosionParticleView :
        MonoBehaviour
    {
        private const string PrivateTextureResource =
            "TimeCliSmallExplosionTexture";

        private static SmallExplosionParticleView _instance;
        private static Material _material;
        private static Texture2D _fallbackTexture;

        private ParticleSystem _particleSystem;
        private ParticleSystem.Particle[] _particles =
            new ParticleSystem.Particle[128];

        public static void Emit(Vector3 position)
        {
            EnsureInstance();
            _instance.EmitParticles(position);
            SmallExplosionAudioView.Play(position);
        }

        private static void EnsureInstance()
        {
            if (_instance != null)
                return;

            var root =
                new GameObject(
                    "VirtualPS_SmallExplosion");

            _instance =
                root.AddComponent<SmallExplosionParticleView>();

            _instance.Initialize();
        }

        private void Initialize()
        {
            _particleSystem =
                gameObject.AddComponent<ParticleSystem>();

            ConfigureMain();
            ConfigureEmission();
            ConfigureShape();
            ConfigureInheritVelocity();
            ConfigureColorAnimation();
            ConfigureRenderer();

            _particleSystem.Play();
        }

        private void ConfigureMain()
        {
            ParticleSystem.MainModule main =
                _particleSystem.main;

            main.duration = 1f;
            main.loop = true;
            main.prewarm = false;
            main.playOnAwake = true;
            main.simulationSpace =
                ParticleSystemSimulationSpace.World;
            main.scalingMode =
                ParticleSystemScalingMode.Hierarchy;
            main.emitterVelocityMode =
                ParticleSystemEmitterVelocityMode.Transform;
            main.startLifetime =
                new ParticleSystem.MinMaxCurve(
                    OriginalSmallExplosionPresentation
                        .LifetimeMin,
                    OriginalSmallExplosionPresentation
                        .LifetimeMax);
            main.startSpeed =
                new ParticleSystem.MinMaxCurve(0f);
            main.startSize =
                new ParticleSystem.MinMaxCurve(
                    OriginalSmallExplosionPresentation
                        .SizeMin,
                    OriginalSmallExplosionPresentation
                        .SizeMax);
            main.startRotation =
                new ParticleSystem.MinMaxCurve(
                    0f,
                    2f * Mathf.PI);
            main.startColor =
                new ParticleSystem.MinMaxGradient(
                    Color.white);
            main.gravityModifier = 0f;
            main.maxParticles = 128;
        }

        private void ConfigureEmission()
        {
            ParticleSystem.EmissionModule emission =
                _particleSystem.emission;

            // VirtualPS invokes Emit(10) explicitly. There is no continuous
            // emission in the serialized legacy emitter.
            emission.enabled = false;
            emission.rateOverTime =
                new ParticleSystem.MinMaxCurve(
                    OriginalSmallExplosionPresentation
                        .ParticleCount);
        }

        private void ConfigureShape()
        {
            ParticleSystem.ShapeModule shape =
                _particleSystem.shape;

            // Spawn positions are supplied explicitly so the old flattened
            // ellipsoid (0.2, 0, 0.2) can be reproduced without relying on a
            // modern ShapeModule interpretation.
            shape.enabled = false;
        }

        private void ConfigureInheritVelocity()
        {
            ParticleSystem.InheritVelocityModule inherit =
                _particleSystem.inheritVelocity;

            inherit.enabled = true;
            inherit.mode =
                ParticleSystemInheritVelocityMode.Initial;
            inherit.curve =
                new ParticleSystem.MinMaxCurve(
                    OriginalSmallExplosionPresentation
                        .EmitterVelocityScale);
        }

        private void ConfigureColorAnimation()
        {
            ParticleSystem.ColorOverLifetimeModule color =
                _particleSystem.colorOverLifetime;

            color.enabled = true;
            color.color =
                new ParticleSystem.MinMaxGradient(
                    OriginalSmallExplosionPresentation
                        .BuildColorAnimation());
        }

        private void ConfigureRenderer()
        {
            ParticleSystemRenderer renderer =
                gameObject.GetComponent<ParticleSystemRenderer>();

            if (renderer == null)
            {
                renderer =
                    gameObject.AddComponent<ParticleSystemRenderer>();
            }

            renderer.renderMode =
                ParticleSystemRenderMode.Billboard;
            renderer.sortMode =
                ParticleSystemSortMode.None;
            renderer.cameraVelocityScale = 0f;
            renderer.velocityScale =
                OriginalSmallExplosionPresentation
                    .RendererVelocityScale;
            renderer.lengthScale =
                OriginalSmallExplosionPresentation
                    .RendererLengthScale;

            // Legacy ParticleRenderer serialized +Infinity. Modern Unity only
            // needs a practically-unbounded value to avoid screen-size
            // clipping.
            renderer.maxParticleSize =
                float.MaxValue;

            renderer.sharedMaterial =
                GetMaterial();
        }

        private void EmitParticles(
            Vector3 position)
        {
            transform.position = position;

            for (int i = 0;
                 i <
                 OriginalSmallExplosionPresentation
                     .ParticleCount;
                 i++)
            {
                Vector3 unit =
                    UnityEngine.UnityEngine.Random.insideUnitSphere;

                Vector3 spawnOffset =
                    new(
                        unit.x *
                            OriginalSmallExplosionPresentation
                                .Ellipsoid.x,
                        0f,
                        unit.z *
                            OriginalSmallExplosionPresentation
                                .Ellipsoid.z);

                Vector3 randomVelocity =
                    new(
                        UnityEngine.UnityEngine.Random.Range(-6f, 6f),
                        UnityEngine.UnityEngine.Random.Range(-6f, 6f),
                        UnityEngine.UnityEngine.Random.Range(-6f, 6f));

                var emit =
                    new ParticleSystem.EmitParams
                    {
                        position =
                            position +
                            spawnOffset,
                        velocity =
                            OriginalSmallExplosionPresentation
                                .LocalVelocity +
                            randomVelocity,
                        startLifetime =
                            UnityEngine.UnityEngine.Random.Range(
                                OriginalSmallExplosionPresentation
                                    .LifetimeMin,
                                OriginalSmallExplosionPresentation
                                    .LifetimeMax),
                        startSize =
                            UnityEngine.UnityEngine.Random.Range(
                                OriginalSmallExplosionPresentation
                                    .SizeMin,
                                OriginalSmallExplosionPresentation
                                    .SizeMax),
                        rotation =
                            UnityEngine.UnityEngine.Random.Range(
                                0f,
                                2f * Mathf.PI),
                        angularVelocity =
                            UnityEngine.UnityEngine.Random.Range(
                               -OriginalSmallExplosionPresentation
                                    .RandomAngularVelocity,
                                OriginalSmallExplosionPresentation
                                    .RandomAngularVelocity) *
                            Mathf.Deg2Rad,
                        startColor =
                            Color.white
                    };

                _particleSystem.Emit(
                    emit,
                    1);
            }
        }

        private void Update()
        {
            int count =
                _particleSystem.GetParticles(
                    _particles);

            if (count <= 0)
                return;

            float damping =
                OriginalSmallExplosionPresentation
                    .GetDampingMultiplier(
                        Time.deltaTime);

            for (int i = 0; i < count; i++)
            {
                ParticleSystem.Particle particle =
                    _particles[i];

                particle.velocity *= damping;
                _particles[i] = particle;
            }

            _particleSystem.SetParticles(
                _particles,
                count);
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
                        "TimeCli_Reconstructed_NukeFireB"
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
                TextureWrapMode.Repeat;
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
                new Texture2D(size, size)
                {
                    name =
                        "TimeCli_Clean_SmallExplosion_Fallback"
                };

            float center =
                (size - 1) * 0.5f;
            float radius = center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx =
                        (x - center) / radius;
                    float dy =
                        (y - center) / radius;

                    float distance =
                        Mathf.Sqrt(
                            dx * dx +
                            dy * dy);

                    float intensity =
                        Mathf.Clamp01(
                            1f - distance);

                    intensity *= intensity;

                    _fallbackTexture.SetPixel(
                        x,
                        y,
                        new Color(
                            intensity,
                            intensity,
                            intensity,
                            1f));
                }
            }

            _fallbackTexture.Apply();
            return _fallbackTexture;
        }
    }

    /// <summary>
    /// Small private-audio bridge for the recovered impact clip. The original
    /// OneShotAudio Explosions queue uses six sources and a 0.1-second queue
    /// cooldown. If the private clip has not been generated, presentation
    /// remains silent rather than shipping original audio publicly.
    /// </summary>
    internal static class SmallExplosionAudioView
    {
        private const string PrivateAudioResource =
            "TimeCliSmallExplosionImpact";

        private static AudioClip _clip;
        private static AudioSource[] _sources;
        private static int _nextSource;
        private static float _nextPlayTime;

        public static void Play(Vector3 position)
        {
            if (_nextPlayTime > Time.time)
                return;

            if (_clip == null)
            {
                _clip =
                    Resources.Load<AudioClip>(
                        PrivateAudioResource);
            }

            if (_clip == null)
                return;

            EnsureSources();

            _nextPlayTime =
                Time.time +
                OriginalSmallExplosionPresentation
                    .AudioQueueCooldown;

            AudioSource source =
                _sources[_nextSource];

            _nextSource =
                (_nextSource + 1) %
                _sources.Length;

            source.transform.position =
                position;
            source.clip = _clip;
            source.pitch =
                Time.timeScale;
            source.volume =
                OriginalSmallExplosionPresentation
                    .AudioVolume;
            source.Play();
        }

        private static void EnsureSources()
        {
            if (_sources != null)
                return;

            _sources =
                new AudioSource[
                    OriginalSmallExplosionPresentation
                        .AudioSourcesPerQueue];

            for (int i = 0;
                 i < _sources.Length;
                 i++)
            {
                var go =
                    new GameObject(
                        "OneShotAudio_Explosions");

                AudioSource source =
                    go.AddComponent<AudioSource>();

                source.playOnAwake = false;
                source.rolloffMode =
                    AudioRolloffMode.Logarithmic;
                source.minDistance =
                    OriginalSmallExplosionPresentation
                        .AudioMinDistance;
                source.maxDistance =
                    OriginalSmallExplosionPresentation
                        .AudioMaxDistance;

                _sources[i] = source;
            }
        }
    }
}
