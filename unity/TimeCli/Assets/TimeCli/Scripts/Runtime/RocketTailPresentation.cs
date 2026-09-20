using UnityEngine;

namespace TimeCli.UnityRuntime
{
    /// <summary>
    /// Frame-rate-independent scheduler for the recovered Rocket tail emission
    /// interval. This is presentation-only and never changes PortCore state.
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
    /// Clean public fallback for the original Rocket tail particle.
    ///
    /// Only the 0.025-second emission cadence is currently treated as recovered
    /// original behavior. Geometry, material and fade below are deliberately
    /// reconstruction-owned until the original serialized particle presentation
    /// is recovered and validated.
    /// </summary>
    public sealed class RocketTailParticleView : MonoBehaviour
    {
        private const float CleanFallbackScale = 0.08f;
        private const float CleanFallbackLifetime =
            OriginalProjectilePresentation.RocketTailSpawnDelay * 6f;

        public static void Spawn(Vector3 position)
        {
            GameObject particle =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere);

            particle.name =
                "ClickLauncher_RocketTail";

            particle.transform.position =
                position;

            particle.transform.localScale =
                new Vector3(
                    CleanFallbackScale,
                    CleanFallbackScale,
                    CleanFallbackScale);

            Collider collider =
                particle.GetComponent<Collider>();

            if (collider != null)
                Destroy(collider);

            Renderer renderer =
                particle.GetComponent<Renderer>();

            if (renderer != null)
            {
                Shader shader =
                    Shader.Find(
                        "TimeCli/BlockVertexColor");

                renderer.sharedMaterial =
                    new Material(shader)
                    {
                        name =
                            "TimeCli_Reconstructed_RocketTail",
                        color =
                            new Color(
                                1f,
                                0.45f,
                                0.08f,
                                1f)
                    };
            }

            var destroy =
                particle.AddComponent<TimedVisualDestroy>();

            destroy.Lifetime =
                CleanFallbackLifetime;
        }
    }
}
